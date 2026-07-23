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

Write-Output "WindowsReleaseModuleQa PASSED: Godot template version mapping is strict."
