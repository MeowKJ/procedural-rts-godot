[CmdletBinding()]
param(
    [string]$GodotPath,
    [string]$ProjectRoot = (Join-Path $PSScriptRoot "..")
)

$ErrorActionPreference = "Stop"
$outputRoot = Join-Path (Resolve-Path $ProjectRoot).Path "builds\windows-smoke"
& (Join-Path $PSScriptRoot "PackageWindowsRelease.ps1") -ProjectRoot $ProjectRoot -GodotPath $GodotPath -OutputRoot $outputRoot
if ($LASTEXITCODE -ne 0) {
    throw "Strict Windows export smoke failed with exit code $LASTEXITCODE."
}
