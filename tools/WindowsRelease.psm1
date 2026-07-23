Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Assert-ReleaseCondition {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) {
        throw $Message
    }
}

function Get-ReleaseProjectRoot {
    param([string]$ProjectRoot)
    return (Resolve-Path $ProjectRoot).Path
}

function Write-ReleaseUtf8 {
    param([string]$Path, [string]$Text)
    [System.IO.File]::WriteAllText($Path, $Text, [System.Text.UTF8Encoding]::new($false))
}

function Get-ReleaseIdentity {
    param([string]$ProjectRoot)

    $path = Join-Path $ProjectRoot "assets\release\release-identity.json"
    Assert-ReleaseCondition (Test-Path -LiteralPath $path) "Release identity is missing: $path"
    $identity = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
    foreach ($field in @("version", "tag", "windowsFileVersion", "godot", "target", "sampleMapId", "sampleMapHash")) {
        Assert-ReleaseCondition (-not [string]::IsNullOrWhiteSpace([string]$identity.$field)) "Release identity field '$field' is required."
    }
    Assert-ReleaseCondition ($identity.version -match '^\d+\.\d+\.\d+-rc\.\d+$') "Release version must be an RC semver value."
    Assert-ReleaseCondition ($identity.tag -eq "v$($identity.version)") "Release tag must be derived from release version."
    Assert-ReleaseCondition ($identity.windowsFileVersion -match '^\d+\.\d+\.\d+\.\d+$') "Windows file version must contain four numeric components."
    Assert-ReleaseCondition ($identity.target -eq "windows-x86_64") "Release target must be windows-x86_64."
    Assert-ReleaseCondition ($identity.sampleMapHash -match '^[a-f0-9]{64}$') "Release sample hash must be lowercase SHA-256."
    return $identity
}

function Get-ReleasePresetValue {
    param([string]$Content, [string]$Name)
    $escaped = [regex]::Escape($Name)
    $pattern = '(?m)^' + $escaped + '[ \t]*=[ \t]*(?:"(?<quoted>[^"]*)"|(?<boolean>true|false))[ \t]*\r?$'
    $match = [regex]::Match($Content, $pattern)
    if (-not $match.Success) {
        throw "Windows export preset is missing '$Name'."
    }
    if ($match.Groups["quoted"].Success) {
        return $match.Groups["quoted"].Value
    }
    return $match.Groups["boolean"].Value
}

function Assert-WindowsExportPreset {
    param([string]$ProjectRoot, $Identity)

    $path = Join-Path $ProjectRoot "export_presets.cfg"
    Assert-ReleaseCondition (Test-Path -LiteralPath $path) "Godot export preset is missing: $path"
    $content = Get-Content -LiteralPath $path -Raw
    Assert-ReleaseCondition ((Get-ReleasePresetValue $content "name") -eq "Windows Desktop") "Windows export preset name changed."
    Assert-ReleaseCondition ((Get-ReleasePresetValue $content "platform") -eq "Windows Desktop") "Windows export preset platform changed."
    Assert-ReleaseCondition ((Get-ReleasePresetValue $content "binary_format/architecture") -eq "x86_64") "Windows export preset must target x86_64."
    Assert-ReleaseCondition ((Get-ReleasePresetValue $content "binary_format/embed_pck") -eq "true") "Windows release must embed its PCK."
    Assert-ReleaseCondition ((Get-ReleasePresetValue $content "application/file_version") -eq $Identity.windowsFileVersion) "Windows file version must match release identity."
    Assert-ReleaseCondition ((Get-ReleasePresetValue $content "application/product_version") -eq $Identity.windowsFileVersion) "Windows product version must match release identity."

    $exclude = Get-ReleasePresetValue $content "exclude_filter"
    foreach ($required in @("tools/**", "builds/**", "artifacts/**", "captures/**", "screenshots/**", "recordings/**", "addons/map_authoring/**")) {
        Assert-ReleaseCondition ($exclude.Contains($required, [StringComparison]::Ordinal)) "Windows export preset must exclude '$required'."
    }
}

function Resolve-WindowsGodot {
    param([string]$GodotPath)

    if (-not [string]::IsNullOrWhiteSpace($GodotPath)) {
        Assert-ReleaseCondition (Test-Path -LiteralPath $GodotPath) "Godot console executable is missing: $GodotPath"
        return (Resolve-Path $GodotPath).Path
    }

    $candidates = @()
    if (-not [string]::IsNullOrWhiteSpace($env:LOCALAPPDATA)) {
        $candidates += Join-Path $env:LOCALAPPDATA "Microsoft\WinGet\Packages\GodotEngine.GodotEngine.Mono_Microsoft.Winget.Source_8wekyb3d8bbwe\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe"
    }
    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate) {
            return (Resolve-Path $candidate).Path
        }
    }

    throw "Godot 4.7 Mono console executable was not found. Pass -GodotPath explicitly."
}

function Assert-WindowsExportTemplates {
    param([string]$GodotPath)

    $godotVersion = (& $GodotPath --version | Out-String).Trim()
    Assert-ReleaseCondition (-not [string]::IsNullOrWhiteSpace($godotVersion)) "Godot executable did not report a version."
    Assert-ReleaseCondition (-not [string]::IsNullOrWhiteSpace($env:APPDATA)) "APPDATA is required to resolve the Godot export template path."
    $templateVersion = $godotVersion
    $officialMarker = ".official."
    $officialIndex = $godotVersion.IndexOf($officialMarker, [StringComparison]::Ordinal)
    if ($officialIndex -gt 0) {
        $templateVersion = $godotVersion.Substring(0, $officialIndex)
    }
    $templateRoot = Join-Path $env:APPDATA "Godot\export_templates\$templateVersion"
    foreach ($template in @("version.txt", "windows_debug_x86_64.exe", "windows_release_x86_64.exe")) {
        Assert-ReleaseCondition (Test-Path -LiteralPath (Join-Path $templateRoot $template) -PathType Leaf) "Required Godot export template is missing at $templateRoot: $template"
    }
    $installedTemplateVersion = (Get-Content -LiteralPath (Join-Path $templateRoot "version.txt") -Raw).Trim()
    Assert-ReleaseCondition ($installedTemplateVersion -eq $templateVersion) "Godot export template version '$installedTemplateVersion' does not match expected '$templateVersion'."
    return $godotVersion
}

function Invoke-ReleaseGit {
    param([string]$ProjectRoot, [string[]]$Arguments)

    $result = & git -C $ProjectRoot @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "git $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
    return ($result | Out-String).Trim()
}

function Get-ReleaseSample {
    param([string]$ProjectRoot, $Identity)

    $path = Join-Path $ProjectRoot "assets\maps\authored-map-preview.mapspec.json"
    Assert-ReleaseCondition (Test-Path -LiteralPath $path) "Committed authored sample is missing: $path"
    $hash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
    Assert-ReleaseCondition ($hash -eq $Identity.sampleMapHash) "Committed authored sample SHA-256 differs from release identity."
    $sample = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
    Assert-ReleaseCondition ($sample.map.id -eq $Identity.sampleMapId) "Committed authored sample id differs from release identity."
    return [pscustomobject]@{ Path = $path; Hash = $hash; MapId = $sample.map.id }
}

function Assert-ReleaseCommit {
    param([string]$ProjectRoot, $Identity, [string]$Commit, [string]$Tag, [switch]$RequireExactTag)

    $resolvedCommit = Invoke-ReleaseGit $ProjectRoot @("rev-parse", "$Commit^{commit}")
    $headCommit = Invoke-ReleaseGit $ProjectRoot @("rev-parse", "HEAD^{commit}")
    Assert-ReleaseCondition ($headCommit -eq $resolvedCommit) "Release checkout HEAD must equal the requested commit."
    Assert-ReleaseCondition ($Tag -eq $Identity.tag) "Release tag must match the canonical release identity."
    if ($RequireExactTag) {
        $tagCommit = Invoke-ReleaseGit $ProjectRoot @("rev-parse", "$Tag^{commit}")
        Assert-ReleaseCondition ($tagCommit -eq $resolvedCommit) "Release tag '$Tag' does not point at the requested commit."
    }
    return $resolvedCommit
}

function Write-DeterministicZip {
    param([string]$SourceRoot, [string]$Destination, [datetimeoffset]$Timestamp)

    if (Test-Path -LiteralPath $Destination) {
        Remove-Item -LiteralPath $Destination -Force
    }

    $stream = [System.IO.File]::Open($Destination, [System.IO.FileMode]::CreateNew)
    try {
        $archive = [System.IO.Compression.ZipArchive]::new($stream, [System.IO.Compression.ZipArchiveMode]::Create, $false)
        try {
            Get-ChildItem -LiteralPath $SourceRoot -Recurse -File |
                Sort-Object FullName |
                ForEach-Object {
                    $relative = $_.FullName.Substring($SourceRoot.Length).TrimStart('\', '/') -replace '\\', '/'
                    $entry = $archive.CreateEntry($relative, [System.IO.Compression.CompressionLevel]::Optimal)
                    $entry.LastWriteTime = $Timestamp
                    $input = [System.IO.File]::OpenRead($_.FullName)
                    try {
                        $output = $entry.Open()
                        try { $input.CopyTo($output) } finally { $output.Dispose() }
                    }
                    finally { $input.Dispose() }
                }
        }
        finally { $archive.Dispose() }
    }
    finally { $stream.Dispose() }
}

function Write-ReleaseChecksums {
    param([string[]]$Files, [string]$Destination)

    $lines = foreach ($file in $Files) {
        $hash = (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToLowerInvariant()
        "$hash *$([System.IO.Path]::GetFileName($file))"
    }
    Write-ReleaseUtf8 $Destination (($lines -join "`n") + "`n")
}

function Test-WindowsReleasePackage {
    param([string]$ReleaseRoot, $Identity)

    $root = Get-ReleaseProjectRoot $ReleaseRoot
    $zip = Join-Path $root "ProceduralRTS-$($Identity.version)-windows-x86_64.zip"
    $buildInfo = Join-Path $root "BUILD_INFO.json"
    $sample = Join-Path $root "authored-map-preview.mapspec.json"
    $checksums = Join-Path $root "SHA256SUMS.txt"
    foreach ($path in @($zip, $buildInfo, $sample, $checksums)) {
        Assert-ReleaseCondition (Test-Path -LiteralPath $path) "Required release asset is missing: $path"
    }

    foreach ($line in Get-Content -LiteralPath $checksums) {
        $match = [regex]::Match($line, '^(?<hash>[a-f0-9]{64}) \*(?<name>.+)$')
        Assert-ReleaseCondition $match.Success "Malformed SHA256SUMS entry: $line"
        $asset = Join-Path $root $match.Groups["name"].Value
        Assert-ReleaseCondition (Test-Path -LiteralPath $asset) "Checksum asset is missing: $asset"
        Assert-ReleaseCondition (((Get-FileHash -LiteralPath $asset -Algorithm SHA256).Hash.ToLowerInvariant()) -eq $match.Groups["hash"].Value) "Checksum mismatch: $asset"
    }

    $extract = Join-Path ([System.IO.Path]::GetTempPath()) ("procedural-rts-clean-" + [guid]::NewGuid().ToString("N"))
    Assert-ReleaseCondition (-not (Test-Path -LiteralPath $extract)) "Clean extract directory unexpectedly exists: $extract"
    try {
        Expand-Archive -LiteralPath $zip -DestinationPath $extract -Force
        $extractedInfo = Get-ChildItem -LiteralPath $extract -Recurse -File -Filter "BUILD_INFO.json" | Select-Object -First 1
        $extractedSample = Get-ChildItem -LiteralPath $extract -Recurse -File -Filter "authored-map-preview.mapspec.json" | Select-Object -First 1
        $exe = Get-ChildItem -LiteralPath $extract -Recurse -File -Filter "ProceduralRTS.exe" | Select-Object -First 1
        Assert-ReleaseCondition ($null -ne $extractedInfo -and $null -ne $extractedSample -and $null -ne $exe) "Clean package extract is incomplete."
        Assert-ReleaseCondition ((Get-Content -LiteralPath $buildInfo -Raw) -ceq (Get-Content -LiteralPath $extractedInfo.FullName -Raw)) "Embedded BUILD_INFO.json differs from release asset."
        Assert-ReleaseCondition (((Get-FileHash -LiteralPath $extractedSample.FullName -Algorithm SHA256).Hash.ToLowerInvariant()) -eq $Identity.sampleMapHash) "Extracted sample hash differs from release identity."

        $stdout = Join-Path $root "clean-extract-runtime.stdout.log"
        $stderr = Join-Path $root "clean-extract-runtime.stderr.log"
        $start = [System.Diagnostics.ProcessStartInfo]::new()
        $start.FileName = $exe.FullName
        $start.UseShellExecute = $false
        $start.RedirectStandardOutput = $true
        $start.RedirectStandardError = $true
        foreach ($argument in @("--headless", "--quit-after", "3", "--", "--authored-map-preview", $extractedSample.FullName, "--authored-map-sha256", $Identity.sampleMapHash)) {
            [void]$start.ArgumentList.Add($argument)
        }
        $process = [System.Diagnostics.Process]::Start($start)
        $outTask = $process.StandardOutput.ReadToEndAsync()
        $errTask = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit(30000)) {
            $process.Kill($true)
            throw "Clean extracted release executable did not exit within 30 seconds."
        }
        $stdoutText = $outTask.GetAwaiter().GetResult()
        $stderrText = $errTask.GetAwaiter().GetResult()
        Write-ReleaseUtf8 $stdout $stdoutText
        Write-ReleaseUtf8 $stderr $stderrText
        Assert-ReleaseCondition ($process.ExitCode -eq 0) "Clean extracted release executable exited with $($process.ExitCode)."
        $runtimeLog = $stdoutText + "`n" + $stderrText
        Assert-ReleaseCondition ($runtimeLog.Contains("Authored map preview staged: id=$($Identity.sampleMapId) sha256=$($Identity.sampleMapHash)", [StringComparison]::Ordinal)) "Clean extracted runtime did not confirm the authored sample id/hash."

        $evidence = [ordered]@{
            package = [System.IO.Path]::GetFileName($zip)
            packageSha256 = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()
            cleanExtract = $true
            buildInfoMatches = $true
            sampleMapId = $Identity.sampleMapId
            sampleMapHash = $Identity.sampleMapHash
            runtimeExitCode = $process.ExitCode
        }
        Write-ReleaseUtf8 (Join-Path $root "clean-extract-smoke.json") (($evidence | ConvertTo-Json) + "`n")
    }
    finally {
        if (Test-Path -LiteralPath $extract) {
            Remove-Item -LiteralPath $extract -Recurse -Force
        }
    }
}

function Invoke-WindowsReleasePackage {
    param(
        [string]$ProjectRoot = (Join-Path $PSScriptRoot ".."),
        [string]$GodotPath,
        [string]$OutputRoot,
        [string]$Commit = "HEAD",
        [string]$Tag,
        [switch]$RequireExactTag
    )

    $project = Get-ReleaseProjectRoot $ProjectRoot
    $identity = Get-ReleaseIdentity $project
    if ([string]::IsNullOrWhiteSpace($Tag)) {
        $Tag = $identity.tag
    }
    if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
        $OutputRoot = Join-Path $project "builds\release"
    }
    $godot = Resolve-WindowsGodot $GodotPath
    Assert-WindowsExportPreset $project $identity
    $godotVersion = Assert-WindowsExportTemplates $godot
    $resolvedCommit = Assert-ReleaseCommit $project $identity $Commit $Tag $RequireExactTag
    $sample = Get-ReleaseSample $project $identity
    $builtAt = Invoke-ReleaseGit $project @("show", "-s", "--format=%cI", $resolvedCommit)
    $timestamp = [datetimeoffset]::Parse($builtAt, [Globalization.CultureInfo]::InvariantCulture)
    Assert-ReleaseCondition ($godotVersion.StartsWith("4.7", [StringComparison]::Ordinal)) "Release export must use Godot 4.7 Mono."

    $output = [System.IO.Path]::GetFullPath($OutputRoot)
    if (Test-Path -LiteralPath $output) {
        Remove-Item -LiteralPath $output -Recurse -Force
    }
    $packageRoot = Join-Path $output "package"
    $exportRoot = Join-Path $packageRoot "ProceduralRTS-$($identity.version)-windows-x86_64"
    New-Item -ItemType Directory -Force -Path $exportRoot | Out-Null
    $exe = Join-Path $exportRoot "ProceduralRTS.exe"

    Push-Location $project
    try {
        & dotnet build ProceduralRts.csproj --no-restore
        Assert-ReleaseCondition ($LASTEXITCODE -eq 0) "Release C# build failed with exit code $LASTEXITCODE."
        & $godot --headless --path $project --export-release "Windows Desktop" $exe
        Assert-ReleaseCondition ($LASTEXITCODE -eq 0) "Windows release export failed with exit code $LASTEXITCODE."
    }
    finally {
        Pop-Location
    }

    Assert-ReleaseCondition (Test-Path -LiteralPath $exe) "Windows release export did not produce $exe"
    Assert-ReleaseCondition ((Get-Item -LiteralPath $exe).Length -gt 1MB) "Windows release executable is unexpectedly small."

    $buildInfo = [ordered]@{
        version = $identity.version
        commit = $resolvedCommit
        tag = $Tag
        godot = $godotVersion
        target = $identity.target
        builtAt = $builtAt
        sampleMapId = $sample.MapId
        sampleMapHash = $sample.Hash
    }
    $buildInfoText = ($buildInfo | ConvertTo-Json) + "`n"
    $embeddedInfo = Join-Path $packageRoot "BUILD_INFO.json"
    $embeddedSample = Join-Path $packageRoot "authored-map-preview.mapspec.json"
    Write-ReleaseUtf8 $embeddedInfo $buildInfoText
    Copy-Item -LiteralPath $sample.Path -Destination $embeddedSample -Force

    $externalInfo = Join-Path $output "BUILD_INFO.json"
    $externalSample = Join-Path $output "authored-map-preview.mapspec.json"
    Write-ReleaseUtf8 $externalInfo $buildInfoText
    Copy-Item -LiteralPath $sample.Path -Destination $externalSample -Force
    $zip = Join-Path $output "ProceduralRTS-$($identity.version)-windows-x86_64.zip"
    Write-DeterministicZip $packageRoot $zip $timestamp
    $checksums = Join-Path $output "SHA256SUMS.txt"
    Write-ReleaseChecksums -Files @($zip, $externalInfo, $externalSample) -Destination $checksums
    Test-WindowsReleasePackage -ReleaseRoot $output -Identity $identity

    return [pscustomobject]@{
        ReleaseRoot = $output
        Package = $zip
        BuildInfo = $externalInfo
        Checksums = $checksums
        Sample = $externalSample
    }
}

Export-ModuleMember -Function Get-ReleaseIdentity, Invoke-WindowsReleasePackage, Test-WindowsReleasePackage
