$ErrorActionPreference = "Stop"

$project = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$presetFile = Join-Path $project "export_presets.cfg"
$presetName = "Windows Desktop"
$exportPath = Join-Path $project "builds\windows\ProceduralRTS.exe"
$artifactIgnore = Join-Path $project "artifacts\.gdignore"
$godot = Join-Path $env:LOCALAPPDATA "Microsoft\WinGet\Packages\GodotEngine.GodotEngine.Mono_Microsoft.Winget.Source_8wekyb3d8bbwe\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe"

function Assert-File($path, $label) {
    if (-not (Test-Path $path)) {
        throw "$label not found: $path"
    }
}

function Read-PresetValue($content, $name) {
    $escaped = [regex]::Escape($name)
    $match = [regex]::Match($content, "(?m)^$escaped=(?:(?:`"(?<quoted>[^`"]*)`")|(?<bare>[^\r\n]+))$")
    if (-not $match.Success) {
        return $null
    }

    if ($match.Groups["quoted"].Success) {
        return $match.Groups["quoted"].Value
    }

    return $match.Groups["bare"].Value.Trim()
}

function Get-GodotTemplateRoots {
    @(
        "$env:APPDATA\Godot\export_templates",
        "$env:LOCALAPPDATA\Godot\export_templates",
        "$env:APPDATA\Godot\templates"
    ) | Where-Object { Test-Path $_ }
}

function Test-WindowsExportTemplate {
    foreach ($root in Get-GodotTemplateRoots) {
        foreach ($version in @("4.7.stable.mono", "4.7.stable")) {
            $versionRoot = Join-Path $root $version
            $debugTemplate = Join-Path $versionRoot "windows_debug_x86_64.exe"
            $releaseTemplate = Join-Path $versionRoot "windows_release_x86_64.exe"
            if ((Test-Path $debugTemplate) -and (Test-Path $releaseTemplate)) {
                return $true
            }
        }

        if (Test-Path (Join-Path $root "windows_release_x86_64.exe")) {
            return $true
        }
    }

    return $false
}

Assert-File $godot "Godot console executable"
Assert-File $presetFile "Godot export preset file"
Assert-File $artifactIgnore "Godot ignore marker for local QA artifacts"

$projectProcessPattern = [regex]::Escape($project)
Get-CimInstance Win32_Process -Filter "Name like 'Godot%'" |
    Where-Object { $_.CommandLine -match $projectProcessPattern } |
    ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }

Push-Location $project
try {
    dotnet build .\ProceduralRts.sln
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed with exit code $LASTEXITCODE"
    }

    $content = Get-Content -Path $presetFile -Raw
    $actualPresetName = Read-PresetValue $content "name"
    $platform = Read-PresetValue $content "platform"
    $configuredExportPath = Read-PresetValue $content "export_path"
    $excludeFilter = Read-PresetValue $content "exclude_filter"
    $embeddedPck = Read-PresetValue $content "binary_format/embed_pck"
    $architecture = Read-PresetValue $content "binary_format/architecture"

    if ($actualPresetName -ne $presetName) {
        throw "Expected preset '$presetName', found '$actualPresetName'"
    }

    if ($platform -ne "Windows Desktop") {
        throw "Expected Windows Desktop platform, found '$platform'"
    }

    if ($configuredExportPath -ne "builds/windows/ProceduralRTS.exe") {
        throw "Unexpected export_path '$configuredExportPath'"
    }

    foreach ($requiredExclusion in @("tools/**", "builds/**", "artifacts/**", "captures/**", "screenshots/**", "recordings/**")) {
        if ($excludeFilter -notlike "*$requiredExclusion*") {
            throw "Windows preset must exclude local-only path '$requiredExclusion'"
        }
    }

    if ($embeddedPck -ne "true") {
        throw "Windows preset should embed the PCK for a single-file smoke export"
    }

    if ($architecture -ne "x86_64") {
        throw "Windows preset should target x86_64, found '$architecture'"
    }

    New-Item -ItemType Directory -Force -Path (Split-Path $exportPath -Parent) | Out-Null

    if (-not (Test-WindowsExportTemplate)) {
        Write-Output "Export smoke skipped: Windows export templates are not installed locally."
        Write-Output "Preset validation and C# build passed."
        exit 0
    }

    if (Test-Path $exportPath) {
        Remove-Item -LiteralPath $exportPath -Force
    }

    & $godot --headless --path $project --export-release $presetName $exportPath
    if ($LASTEXITCODE -ne 0) {
        throw "Godot Windows export failed with exit code $LASTEXITCODE"
    }

    Assert-File $exportPath "Exported Windows executable"
    $size = (Get-Item $exportPath).Length
    if ($size -lt 1024KB) {
        throw "Exported executable is unexpectedly small: $size bytes"
    }

    $smoke = Start-Process -FilePath $exportPath -ArgumentList "--headless", "--quit-after", "3" -PassThru
    if (-not $smoke.WaitForExit(15000)) {
        $smoke.Kill($true)
        throw "Exported Windows executable did not exit within the 15-second smoke timeout"
    }

    if ($smoke.ExitCode -ne 0) {
        throw "Exported Windows executable smoke failed with exit code $($smoke.ExitCode)"
    }

    Write-Output "Export smoke passed: $exportPath ($size bytes), runtime exit 0"
}
finally {
    Pop-Location
}
