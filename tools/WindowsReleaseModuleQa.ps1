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

$layoutIdentity = [pscustomobject]@{ version = "0.2.0-rc.1"; tag = "v0.2.0-rc.1"; target = "windows-x86_64" }
$packageName = Get-WindowsReleasePackageFileName $layoutIdentity
if ($packageName -cne "ProceduralRTS-v0.2.0-rc.1-windows-x86_64.zip") {
    throw "Release package name must derive from the canonical v-prefixed tag."
}
$layout = Get-WindowsReleasePackageLayout -PackageRoot "C:\clean" -Identity $layoutIdentity
$expectedExportRoot = Join-Path "C:\clean" "ProceduralRTS-0.2.0-rc.1-windows-x86_64"
$expectedSample = Join-Path $expectedExportRoot "assets\maps\authored-map-preview.mapspec.json"
if ($layout.ExportRoot -cne $expectedExportRoot -or $layout.Executable -cne (Join-Path $expectedExportRoot "ProceduralRTS.exe") -or $layout.EmbeddedSample -cne $expectedSample) {
    throw "Clean-extract package layout no longer satisfies the authored map path contract."
}

function Assert-MergedMainReleaseRejected {
    param(
        [string]$Name,
        [bool]$ReleaseCommitIsOnMain,
        [object[]]$VerifyAllRuns
    )

    $rejected = $false
    try {
        Assert-VerifiedMergedMainRelease -ResolvedCommit "0123456789abcdef0123456789abcdef01234567" -ReleaseCommitIsOnMain $ReleaseCommitIsOnMain -VerifyAllRuns $VerifyAllRuns | Out-Null
    }
    catch {
        $rejected = $true
    }
    if (-not $rejected) {
        throw "Merged-main release gate accepted $Name."
    }
}

$verifiedMainRun = [pscustomobject]@{
    id = 200
    run_number = 10
    head_sha = "0123456789abcdef0123456789abcdef01234567"
    head_branch = "main"
    event = "push"
    status = "completed"
    conclusion = "success"
    path = ".github/workflows/verify-all.yml"
}
foreach ($status in @("ahead", "identical")) {
    $comparison = [pscustomobject]@{ base_commit = [pscustomobject]@{ sha = $verifiedMainRun.head_sha }; status = $status }
    if (-not (Test-ReleaseCommitOnMain -ResolvedCommit $verifiedMainRun.head_sha -MainComparison $comparison)) {
        throw "Release commit main comparison must accept status '$status'."
    }
}
foreach ($status in @("behind", "diverged")) {
    $comparison = [pscustomobject]@{ base_commit = [pscustomobject]@{ sha = $verifiedMainRun.head_sha }; status = $status }
    if (Test-ReleaseCommitOnMain -ResolvedCommit $verifiedMainRun.head_sha -MainComparison $comparison) {
        throw "Release commit main comparison must reject status '$status'."
    }
}
if (Test-ReleaseCommitOnMain -ResolvedCommit $verifiedMainRun.head_sha -MainComparison ([pscustomobject]@{ base_commit = [pscustomobject]@{ sha = "ffffffffffffffffffffffffffffffffffffffff" }; status = "ahead" })) {
    throw "Release commit main comparison must reject a mismatched base SHA."
}
$releaseTag = "v0.2.0-rc.1"
$releaseCommit = $verifiedMainRun.head_sha
if ($null -ne (Resolve-RemoteReleaseTagCommit -Tag $releaseTag -LsRemoteLines @())) {
    throw "Missing remote release tag must resolve to null."
}
if ((Resolve-RemoteReleaseTagCommit -Tag $releaseTag -LsRemoteLines @("$releaseCommit`trefs/tags/$releaseTag")) -cne $releaseCommit) {
    throw "Lightweight remote release tag must resolve to its commit."
}
$annotatedTagObject = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
if ((Resolve-RemoteReleaseTagCommit -Tag $releaseTag -LsRemoteLines @("$annotatedTagObject`trefs/tags/$releaseTag", "$releaseCommit`trefs/tags/$releaseTag^{}")) -cne $releaseCommit) {
    throw "Annotated remote release tag must resolve to its peeled commit."
}
$malformedRemoteTagRejected = $false
try {
    Resolve-RemoteReleaseTagCommit -Tag $releaseTag -LsRemoteLines @("not-a-sha`trefs/tags/$releaseTag") | Out-Null
}
catch {
    $malformedRemoteTagRejected = $true
}
if (-not $malformedRemoteTagRejected) {
    throw "Malformed remote release tag output must be rejected."
}
Assert-VerifiedMergedMainRelease -ResolvedCommit "0123456789abcdef0123456789abcdef01234567" -ReleaseCommitIsOnMain $true -VerifyAllRuns @($verifiedMainRun) | Out-Null
Assert-MergedMainReleaseRejected -Name "a commit outside main" -ReleaseCommitIsOnMain $false -VerifyAllRuns @($verifiedMainRun)
Assert-MergedMainReleaseRejected -Name "a branch VerifyAll run" -ReleaseCommitIsOnMain $true -VerifyAllRuns @([pscustomobject]@{ id = 201; run_number = 11; head_sha = $verifiedMainRun.head_sha; head_branch = "codex/release"; event = "push"; status = "completed"; conclusion = "success"; path = $verifiedMainRun.path })
Assert-MergedMainReleaseRejected -Name "a manually dispatched VerifyAll run" -ReleaseCommitIsOnMain $true -VerifyAllRuns @([pscustomobject]@{ id = 201; run_number = 11; head_sha = $verifiedMainRun.head_sha; head_branch = "main"; event = "workflow_dispatch"; status = "completed"; conclusion = "success"; path = $verifiedMainRun.path })
Assert-MergedMainReleaseRejected -Name "a wrong release SHA" -ReleaseCommitIsOnMain $true -VerifyAllRuns @([pscustomobject]@{ id = 201; run_number = 11; head_sha = "ffffffffffffffffffffffffffffffffffffffff"; head_branch = "main"; event = "push"; status = "completed"; conclusion = "success"; path = $verifiedMainRun.path })
Assert-MergedMainReleaseRejected -Name "a failed VerifyAll run" -ReleaseCommitIsOnMain $true -VerifyAllRuns @([pscustomobject]@{ id = 201; run_number = 11; head_sha = $verifiedMainRun.head_sha; head_branch = "main"; event = "push"; status = "completed"; conclusion = "failure"; path = $verifiedMainRun.path })
Assert-MergedMainReleaseRejected -Name "an in-progress VerifyAll run" -ReleaseCommitIsOnMain $true -VerifyAllRuns @([pscustomobject]@{ id = 201; run_number = 11; head_sha = $verifiedMainRun.head_sha; head_branch = "main"; event = "push"; status = "in_progress"; conclusion = ""; path = $verifiedMainRun.path })
Assert-MergedMainReleaseRejected -Name "a wrong workflow path" -ReleaseCommitIsOnMain $true -VerifyAllRuns @([pscustomobject]@{ id = 201; run_number = 11; head_sha = $verifiedMainRun.head_sha; head_branch = "main"; event = "push"; status = "completed"; conclusion = "success"; path = ".github/workflows/preflight.yml" })
Assert-MergedMainReleaseRejected -Name "an empty VerifyAll result" -ReleaseCommitIsOnMain $true -VerifyAllRuns @()
Assert-MergedMainReleaseRejected -Name "a newer failed merged-main VerifyAll run" -ReleaseCommitIsOnMain $true -VerifyAllRuns @($verifiedMainRun, [pscustomobject]@{ id = 201; run_number = 11; head_sha = $verifiedMainRun.head_sha; head_branch = "main"; event = "push"; status = "completed"; conclusion = "failure"; path = $verifiedMainRun.path })

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

Write-Output "WindowsReleaseModuleQa PASSED: template mapping, canonical package name, clean-extract bootstrap arguments, package layout, remote tag resolution, main reachability, and merged-main VerifyAll gate are strict."
