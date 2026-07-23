[CmdletBinding()]
param(
    [string]$ProjectRoot = (Join-Path $PSScriptRoot ".."),
    [string]$GodotPath,
    [string]$OutputRoot,
    [string]$Commit = "HEAD",
    [string]$Tag,
    [switch]$RequireExactTag
)

$ErrorActionPreference = "Stop"
Import-Module (Join-Path $PSScriptRoot "WindowsRelease.psm1") -Force
$result = Invoke-WindowsReleasePackage @PSBoundParameters
$result | ConvertTo-Json
