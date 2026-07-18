[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ReleaseRoot,
    [string]$ProjectRoot = (Join-Path $PSScriptRoot "..")
)

$ErrorActionPreference = "Stop"
Import-Module (Join-Path $PSScriptRoot "WindowsRelease.psm1") -Force
Test-WindowsReleasePackage $ReleaseRoot (Get-ReleaseIdentity (Resolve-Path $ProjectRoot).Path)
Write-Output "Windows release clean-extract smoke passed: $ReleaseRoot"
