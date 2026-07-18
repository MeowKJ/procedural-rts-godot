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
var menu = File.ReadAllText(Path.Combine(root, "scripts", "main-menu", "MainMenuRoot.Build.cs"));
Require(menu.Contains("ReleaseIdentity.Current.Version", StringComparison.Ordinal) && menu.Contains("ReleaseVersion", StringComparison.Ordinal), "Main menu must render canonical release identity");

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
