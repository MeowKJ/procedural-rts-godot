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
Require(mainMenu.Contains("TryStartAuthoredMapPreviewFromCommandLine", StringComparison.Ordinal) && menuFlow.Contains("AuthoredMapPreviewCommandLine.StageRequired", StringComparison.Ordinal), "Release main menu must support verified authored preview command-line smoke.");
var publisher = File.ReadAllText(Path.Combine(root, "tools", "PublishWindowsPrerelease.ps1"));
var windowsReleaseModuleQa = Path.Combine(root, "tools", "WindowsReleaseModuleQa.ps1");
var acceptanceValidator = Path.Combine(root, "tools", "WindowsAcceptanceEvidence", "WindowsAcceptanceEvidence.csproj");
Require(File.Exists(windowsReleaseModuleQa), "Windows release module mapping QA must be checked in");
Require(File.Exists(acceptanceValidator), "publisher must use the checked-in Windows acceptance validator");
Require(publisher.Contains("$tagExists = $LASTEXITCODE -eq 0", StringComparison.Ordinal), "publisher must retain missing-tag state before invoking other commands");
Require(publisher.Contains("if (-not $tagExists)", StringComparison.Ordinal), "publisher must explicitly create a missing tag at the verified commit");
Require(publisher.Contains("--target $resolvedCommit", StringComparison.Ordinal), "publisher must target the verified commit when creating the prerelease");
Require(publisher.Contains("WindowsAcceptanceEvidence.csproj", StringComparison.Ordinal), "publisher must validate physical Windows acceptance evidence with the shared validator");

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
