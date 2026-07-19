[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ReleaseRoot,
    [Parameter(Mandatory = $true)]
    [string]$Commit,
    [Parameter(Mandatory = $true)]
    [string]$WindowsAcceptanceEvidence,
    [string]$ProjectRoot = (Join-Path $PSScriptRoot ".."),
    [switch]$Publish
)

$ErrorActionPreference = "Stop"
Import-Module (Join-Path $PSScriptRoot "WindowsRelease.psm1") -Force

$project = (Resolve-Path $ProjectRoot).Path
$identity = Get-ReleaseIdentity $project
$release = (Resolve-Path $ReleaseRoot).Path
if (-not (Test-Path -LiteralPath $WindowsAcceptanceEvidence)) {
    throw "Physical Windows acceptance evidence is required: $WindowsAcceptanceEvidence"
}

Test-WindowsReleasePackage $release $identity
$resolvedCommit = ((& git -C $project rev-parse "$Commit^{commit}") | Out-String).Trim()
if ($LASTEXITCODE -ne 0) {
    throw "Could not resolve release commit '$Commit'."
}

$validator = Join-Path $project "tools\WindowsAcceptanceEvidence\WindowsAcceptanceEvidence.csproj"
& dotnet run --project $validator -- --evidence $WindowsAcceptanceEvidence --identity (Join-Path $project "assets\release\release-identity.json") --release-root $release --commit $resolvedCommit
if ($LASTEXITCODE -ne 0) {
    throw "Physical Windows acceptance evidence did not satisfy the release contract."
}

$tagCommit = ((& git -C $project rev-parse "$($identity.tag)^{commit}" 2>$null) | Out-String).Trim()
$tagExists = $LASTEXITCODE -eq 0
if ($tagExists -and $tagCommit -ne $resolvedCommit) {
    throw "Existing tag '$($identity.tag)' points at $tagCommit rather than verified commit $resolvedCommit."
}

$assets = @(
    (Join-Path $release "ProceduralRTS-$($identity.version)-windows-x86_64.zip"),
    (Join-Path $release "BUILD_INFO.json"),
    (Join-Path $release "SHA256SUMS.txt"),
    (Join-Path $release "authored-map-preview.mapspec.json"),
    (Resolve-Path $WindowsAcceptanceEvidence).Path
)
foreach ($asset in $assets) {
    if (-not (Test-Path -LiteralPath $asset)) {
        throw "Missing prerelease asset: $asset"
    }
}

if (-not $Publish) {
    Write-Output "Publish preflight passed for $($identity.tag) at $resolvedCommit. Re-run with -Publish after confirming the physical Windows acceptance evidence."
    exit 0
}

if (-not $tagExists) {
    & git -C $project tag -a $identity.tag $resolvedCommit -m "Procedural RTS $($identity.version)"
    if ($LASTEXITCODE -ne 0) { throw "Failed to create release tag $($identity.tag)." }
    & git -C $project push origin $identity.tag
    if ($LASTEXITCODE -ne 0) { throw "Failed to push release tag $($identity.tag)." }
}

& gh auth status
if ($LASTEXITCODE -ne 0) { throw "GitHub authentication is required to publish the prerelease." }
& gh release create $identity.tag @assets --repo MeowKJ/procedural-rts-godot --target $resolvedCommit --title "Procedural RTS $($identity.version)" --prerelease --notes "Windows x86_64 Map Authoring Preview RC. This prerelease is unsigned and has no installer or auto-update. It does not include macOS packaging, Linux runtime acceptance, or campaign expansion. Verify SHA256SUMS.txt before running."
if ($LASTEXITCODE -ne 0) { throw "Failed to publish prerelease $($identity.tag)." }
