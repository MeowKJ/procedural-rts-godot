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

Write-Output "WindowsReleaseModuleQa PASSED: Godot template version mapping and clean-extract bootstrap arguments are strict."
