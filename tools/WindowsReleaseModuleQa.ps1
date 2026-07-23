[CmdletBinding()]
param(
    [string]$ProjectRoot = (Join-Path $PSScriptRoot "..")
)

$ErrorActionPreference = "Stop"
$root = (Resolve-Path $ProjectRoot).Path
Import-Module (Join-Path $root "tools\WindowsRelease.psm1") -Force

function Assert-TemplateVersion {
    param([string]$BuildVersion, [string]$ExpectedTemplateVersion)

    $actual = Get-GodotExportTemplateVersion $BuildVersion
    if ($actual -ne $ExpectedTemplateVersion) {
        throw "Godot template version mapping failed: '$BuildVersion' resolved '$actual', expected '$ExpectedTemplateVersion'."
    }
}

Assert-TemplateVersion "4.7.stable.mono.official.5b4e0cb0f" "4.7.stable.mono"
Assert-TemplateVersion "4.7.stable.mono" "4.7.stable.mono"

$smokeArguments = @(Get-WindowsReleaseCleanExtractArguments -SamplePath "C:\clean\authored-map-preview.mapspec.json" -SampleHash "65ddc348ea79a76832237f30b7287436fd23f615cd04ccc3e2db524603b206e7")
$expectedSmokeArguments = @(
    "--headless",
    "--quit-after", "3",
    "--scene", "res://scenes/AuthoredMapPreviewBootstrap.tscn",
    "--",
    "--authored-map-preview", "C:\clean\authored-map-preview.mapspec.json",
    "--authored-map-sha256", "65ddc348ea79a76832237f30b7287436fd23f615cd04ccc3e2db524603b206e7"
)
if ($smokeArguments.Count -ne $expectedSmokeArguments.Count) {
    throw "Clean-extract smoke argument count changed."
}
for ($index = 0; $index -lt $expectedSmokeArguments.Count; $index++) {
    if ($smokeArguments[$index] -cne $expectedSmokeArguments[$index]) {
        throw "Clean-extract smoke argument changed at index $index."
    }
}

$layoutIdentity = [pscustomobject]@{ version = "0.2.0-rc.1" }
$layout = Get-WindowsReleasePackageLayout -PackageRoot "C:\clean" -Identity $layoutIdentity
$expectedExportRoot = Join-Path "C:\clean" "ProceduralRTS-0.2.0-rc.1-windows-x86_64"
$expectedSample = Join-Path $expectedExportRoot "assets\maps\authored-map-preview.mapspec.json"
if ($layout.ExportRoot -cne $expectedExportRoot -or $layout.Executable -cne (Join-Path $expectedExportRoot "ProceduralRTS.exe") -or $layout.EmbeddedSample -cne $expectedSample) {
    throw "Clean-extract package layout no longer satisfies the authored map path contract."
}

$malformedRejected = $false
try {
    Get-GodotExportTemplateVersion "4.7.stable.mono.official.not-hex" | Out-Null
}
catch {
    $malformedRejected = $true
}

if (-not $malformedRejected) {
    throw "Malformed Godot official metadata must be rejected."
}

Write-Output "WindowsReleaseModuleQa PASSED: template mapping, clean-extract bootstrap arguments, and package layout are strict."
