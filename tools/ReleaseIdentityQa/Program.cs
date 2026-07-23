using System.Security.Cryptography;
using System.Text.Json;

var root = FindProjectRoot();
var identityPath = Path.Combine(root, "assets", "release", "release-identity.json");
var identity = JsonDocument.Parse(File.ReadAllText(identityPath)).RootElement;
var version = identity.GetProperty("version").GetString() ?? throw new InvalidOperationException("release version is missing");
var tag = identity.GetProperty("tag").GetString() ?? throw new InvalidOperationException("release tag is missing");
var windowsVersion = identity.GetProperty("windowsFileVersion").GetString() ?? throw new InvalidOperationException("Windows file version is missing");
var sampleId = identity.GetProperty("sampleMapId").GetString() ?? throw new InvalidOperationException("sample id is missing");
var sampleHash = identity.GetProperty("sampleMapHash").GetString() ?? throw new InvalidOperationException("sample hash is missing");

Require(version == "0.2.0-rc.1", "release identity must name v0.2.0-rc.1");
Require(tag == $"v{version}", "release tag must derive from version");
Require(windowsVersion == "0.2.0.1", "Windows four-part version must be canonical");
Require(sampleHash.Length == 64 && sampleHash.All(char.IsAsciiHexDigit) && sampleHash == sampleHash.ToLowerInvariant(), "sample hash must be lowercase SHA-256");

var samplePath = Path.Combine(root, "assets", "maps", "authored-map-preview.mapspec.json");
var actualHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(samplePath))).ToLowerInvariant();
Require(actualHash == sampleHash, "release identity sample hash must match committed artifact");
using var sample = JsonDocument.Parse(File.ReadAllText(samplePath));
Require(sample.RootElement.GetProperty("map").GetProperty("id").GetString() == sampleId, "release identity sample id must match committed artifact");

var preset = File.ReadAllText(Path.Combine(root, "export_presets.cfg"));
Require(preset.Contains($"application/file_version=\"{windowsVersion}\"", StringComparison.Ordinal), "Windows file version must match release identity");
Require(preset.Contains($"application/product_version=\"{windowsVersion}\"", StringComparison.Ordinal), "Windows product version must match release identity");
Require(preset.Contains("binary_format/embed_pck=true", StringComparison.Ordinal), "Windows release export must embed its PCK");
var menu = File.ReadAllText(Path.Combine(root, "scripts", "main-menu", "MainMenuRoot.Build.cs"));
Require(menu.Contains("ReleaseIdentity.Current.Version", StringComparison.Ordinal) && menu.Contains("ReleaseVersion", StringComparison.Ordinal), "Main menu must render canonical release identity");
var mainMenu = File.ReadAllText(Path.Combine(root, "scripts", "MainMenuRoot.cs"));
var menuFlow = File.ReadAllText(Path.Combine(root, "scripts", "main-menu", "MainMenuRoot.Flow.cs"));
Require(mainMenu.Contains("TryStartAuthoredMapPreviewFromCommandLine", StringComparison.Ordinal) && menuFlow.Contains("AuthoredMapPreviewCommandLine.StageRequired", StringComparison.Ordinal) && menuFlow.Contains("HasSceneOverride", StringComparison.Ordinal), "Release main menu must support verified authored preview command-line smoke without stealing an editor scene override.");
var publisher = File.ReadAllText(Path.Combine(root, "tools", "PublishWindowsPrerelease.ps1"));
var windowsReleaseModuleQa = Path.Combine(root, "tools", "WindowsReleaseModuleQa.ps1");
var acceptanceValidator = Path.Combine(root, "tools", "WindowsAcceptanceEvidence", "WindowsAcceptanceEvidence.csproj");
Require(File.Exists(windowsReleaseModuleQa), "Windows release module mapping QA must be checked in");
Require(File.Exists(acceptanceValidator), "publisher must use the checked-in Windows acceptance validator");
var windowsReleaseModuleQaText = File.ReadAllText(windowsReleaseModuleQa);
Require(publisher.Contains("$tagExists = $LASTEXITCODE -eq 0", StringComparison.Ordinal), "publisher must retain missing-tag state before invoking other commands");
Require(publisher.Contains("if (-not $tagExists)", StringComparison.Ordinal), "publisher must explicitly create a missing tag at the verified commit");
Require(publisher.Contains("--target $resolvedCommit", StringComparison.Ordinal), "publisher must target the verified commit when creating the prerelease");
Require(publisher.Contains("Get-WindowsReleasePackageFileName $identity", StringComparison.Ordinal), "publisher must use the shared canonical Windows package name");
Require(publisher.Contains("WindowsAcceptanceEvidence.csproj", StringComparison.Ordinal), "publisher must validate physical Windows acceptance evidence with the shared validator");
Require(publisher.Contains("issues/comments/$issueCommentId", StringComparison.Ordinal), "publisher must retrieve the linked physical acceptance comment before publishing");
Require(publisher.Contains("issueEvidenceUrl", StringComparison.Ordinal), "publisher must bind publishing to the submitted physical acceptance evidence URL");
Require(publisher.Contains("Get-FileHash -LiteralPath $assets[0]", StringComparison.Ordinal), "publisher must verify the linked comment against the exact package SHA-256");
Require(publisher.Contains("compare/$resolvedCommit...main", StringComparison.Ordinal), "publisher must verify that the release commit is reachable from main");
Require(publisher.Contains("actions/workflows/verify-all.yml/runs?branch=main&event=push&head_sha=$resolvedCommit", StringComparison.Ordinal), "publisher must retrieve merged-main VerifyAll runs for the exact release commit");
Require(publisher.Contains("ls-remote --tags origin", StringComparison.Ordinal), "publisher must resolve the remote release tag before publishing");
Require(publisher.Contains("--verify-tag", StringComparison.Ordinal), "publisher must require an existing verified remote tag before creating a release");
Require(publisher.Contains("Test-ReleaseCommitOnMain", StringComparison.Ordinal), "publisher must use the shared main reachability contract");
Require(publisher.Contains("Assert-VerifiedMergedMainRelease", StringComparison.Ordinal), "publisher must require an exact merged-main VerifyAll result before publishing");
Require(windowsReleaseModuleQaText.Contains("Test-ReleaseCommitOnMain", StringComparison.Ordinal), "Windows release module QA must cover main reachability");
Require(windowsReleaseModuleQaText.Contains("Assert-VerifiedMergedMainRelease", StringComparison.Ordinal), "Windows release module QA must cover the merged-main VerifyAll gate");
Require(windowsReleaseModuleQaText.Contains("Resolve-RemoteReleaseTagCommit", StringComparison.Ordinal), "Windows release module QA must cover remote release tag resolution");
var mergedMainGateIndex = publisher.IndexOf("Assert-VerifiedMergedMainRelease", StringComparison.Ordinal);
var remoteTagGateIndex = publisher.IndexOf("$remoteTagCommit = Get-RemoteReleaseTagCommit", StringComparison.Ordinal);
var tagCreationIndex = publisher.IndexOf("git -C $project tag -a", StringComparison.Ordinal);
var releaseCreationIndex = publisher.IndexOf("gh release create", StringComparison.Ordinal);
Require(mergedMainGateIndex >= 0 && tagCreationIndex > mergedMainGateIndex && releaseCreationIndex > mergedMainGateIndex,
    "publisher must check exact merged-main VerifyAll before creating a tag or release");
Require(remoteTagGateIndex > mergedMainGateIndex && tagCreationIndex > remoteTagGateIndex && releaseCreationIndex > remoteTagGateIndex,
    "publisher must verify the remote release tag after merged-main verification and before creating a tag or release");

Console.WriteLine($"ReleaseIdentityQa PASSED: {tag}, Windows {windowsVersion}, sample {sampleId} {sampleHash}.");

static string FindProjectRoot()
{
    var current = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (current is not null)
    {
        if (File.Exists(Path.Combine(current.FullName, "ProceduralRts.csproj")))
        {
            return current.FullName;
        }
        current = current.Parent;
    }

    throw new InvalidOperationException("Could not find project root.");
}

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
