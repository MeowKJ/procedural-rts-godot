using System.Security.Cryptography;
using System.Text.Json;

if (args.SequenceEqual(["--self-test"], StringComparer.Ordinal))
{
    RunSelfTest();
    Console.WriteLine("WindowsAcceptanceEvidence PASSED: valid evidence accepted; malformed, stale, hash-mismatched, and incomplete records rejected.");
    return;
}

var options = EvidenceOptions.Parse(args);
Validate(options);
Console.WriteLine($"Windows acceptance evidence PASSED: {options.Commit}.");

static void Validate(EvidenceOptions options)
{
    var identity = ReadJson(options.IdentityPath, "release identity");
    var version = RequiredString(identity, "version", "release identity");
    var tag = RequiredString(identity, "tag", "release identity");
    var target = RequiredString(identity, "target", "release identity");
    var sampleId = RequiredString(identity, "sampleMapId", "release identity");
    var sampleHash = RequiredSha256(identity, "sampleMapHash", "release identity");

    Require(tag == $"v{version}", "release identity tag must derive from its version");
    Require(IsCommit(options.Commit), "release commit must be a full lowercase SHA-1");
    var packageName = $"ProceduralRTS-{version}-windows-x86_64.zip";
    var packagePath = Path.Combine(options.ReleaseRoot, packageName);
    Require(File.Exists(packagePath), $"release package is missing: {packagePath}");
    var packageHash = Sha256(packagePath);
    var buildInfo = ReadJson(Path.Combine(options.ReleaseRoot, "BUILD_INFO.json"), "release BUILD_INFO");
    Require(RequiredString(buildInfo, "version", "release BUILD_INFO") == version,
        "release BUILD_INFO version does not match release identity");
    Require(RequiredString(buildInfo, "tag", "release BUILD_INFO") == tag,
        "release BUILD_INFO tag does not match release identity");
    Require(RequiredString(buildInfo, "commit", "release BUILD_INFO") == options.Commit,
        "release BUILD_INFO commit does not match the verified release commit");
    Require(RequiredString(buildInfo, "target", "release BUILD_INFO") == target,
        "release BUILD_INFO target does not match release identity");
    Require(RequiredString(buildInfo, "sampleMapId", "release BUILD_INFO") == sampleId,
        "release BUILD_INFO sample id does not match release identity");
    Require(RequiredSha256(buildInfo, "sampleMapHash", "release BUILD_INFO") == sampleHash,
        "release BUILD_INFO sample SHA-256 does not match release identity");

    var evidence = ReadJson(options.EvidencePath, "Windows acceptance evidence");
    Require(RequiredString(evidence, "format", "Windows acceptance evidence") == "procedural-rts.windows-acceptance",
        "Windows acceptance evidence format is unsupported");
    Require(RequiredInt(evidence, "schemaVersion", "Windows acceptance evidence") == 1,
        "Windows acceptance evidence schemaVersion must be 1");
    Require(RequiredString(evidence, "version", "Windows acceptance evidence") == version,
        "Windows acceptance evidence version does not match release identity");
    Require(RequiredString(evidence, "tag", "Windows acceptance evidence") == tag,
        "Windows acceptance evidence tag does not match release identity");
    Require(RequiredString(evidence, "commit", "Windows acceptance evidence") == options.Commit,
        "Windows acceptance evidence commit does not match the verified release commit");
    Require(RequiredString(evidence, "target", "Windows acceptance evidence") == target,
        "Windows acceptance evidence target does not match release identity");

    var package = RequiredObject(evidence, "package", "Windows acceptance evidence");
    Require(RequiredString(package, "file", "Windows acceptance evidence package") == packageName,
        "Windows acceptance evidence package file does not match the release package");
    Require(RequiredSha256(package, "sha256", "Windows acceptance evidence package") == packageHash,
        "Windows acceptance evidence package SHA-256 does not match the release package");

    var sample = RequiredObject(evidence, "sampleMap", "Windows acceptance evidence");
    Require(RequiredString(sample, "id", "Windows acceptance evidence sampleMap") == sampleId,
        "Windows acceptance evidence sample id does not match release identity");
    Require(RequiredSha256(sample, "sha256", "Windows acceptance evidence sampleMap") == sampleHash,
        "Windows acceptance evidence sample SHA-256 does not match release identity");

    var host = RequiredObject(evidence, "host", "Windows acceptance evidence");
    RequiredString(host, "machineClass", "Windows acceptance evidence host");
    RequiredString(host, "os", "Windows acceptance evidence host");
    RequiredString(host, "architecture", "Windows acceptance evidence host");
    var acceptedAt = RequiredString(evidence, "acceptedAtUtc", "Windows acceptance evidence");
    Require(DateTimeOffset.TryParse(acceptedAt, out var parsedAt) && parsedAt.Offset == TimeSpan.Zero,
        "Windows acceptance evidence acceptedAtUtc must be a UTC ISO-8601 timestamp");

    var interactive = RequiredObject(evidence, "interactive", "Windows acceptance evidence");
    foreach (var check in new[]
    {
        "packageExtracted",
        "desktopLaunch",
        "authoredMapPreviewEntry",
        "authoredMapLoaded",
        "normalSkirmishReturn",
    })
    {
        Require(RequiredTrue(interactive, check, "Windows acceptance evidence interactive"),
            $"Windows acceptance evidence interactive check '{check}' must be true");
    }
}

static void RunSelfTest()
{
    var root = Path.Combine(Path.GetTempPath(), "procedural-rts-windows-acceptance-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(root);
    try
    {
        const string version = "0.2.0-rc.1";
        const string commit = "0123456789abcdef0123456789abcdef01234567";
        const string sampleHash = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        var identityPath = Path.Combine(root, "release-identity.json");
        File.WriteAllText(identityPath, $$"""
            {"version":"{{version}}","tag":"v{{version}}","target":"windows-x86_64","sampleMapId":"authored-map-preview","sampleMapHash":"{{sampleHash}}"}
            """);
        var packageName = $"ProceduralRTS-{version}-windows-x86_64.zip";
        File.WriteAllBytes(Path.Combine(root, packageName), [1, 2, 3, 4]);
        var packageHash = Sha256(Path.Combine(root, packageName));
        File.WriteAllText(Path.Combine(root, "BUILD_INFO.json"), $$"""
            {"version":"{{version}}","tag":"v{{version}}","commit":"{{commit}}","target":"windows-x86_64","sampleMapId":"authored-map-preview","sampleMapHash":"{{sampleHash}}"}
            """);
        var evidencePath = Path.Combine(root, "windows-acceptance.json");

        WriteEvidence(evidencePath, version, commit, sampleHash, packageName, packageHash);
        Validate(new EvidenceOptions(evidencePath, identityPath, root, commit));

        File.WriteAllText(evidencePath, "{}");
        RequireReject(evidencePath, identityPath, root, commit, "empty record");
        WriteEvidence(evidencePath, version, "ffffffffffffffffffffffffffffffffffffffff", sampleHash, packageName, packageHash);
        RequireReject(evidencePath, identityPath, root, commit, "stale commit");
        WriteEvidence(evidencePath, version, commit, sampleHash, packageName, new string('b', 64));
        RequireReject(evidencePath, identityPath, root, commit, "package hash mismatch");
        WriteEvidence(evidencePath, version, commit, sampleHash, packageName, packageHash, normalSkirmishReturn: false);
        RequireReject(evidencePath, identityPath, root, commit, "missing interactive check");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

static void WriteEvidence(
    string path,
    string version,
    string commit,
    string sampleHash,
    string packageName,
    string packageHash,
    bool normalSkirmishReturn = true)
{
    File.WriteAllText(path, $$"""
        {
          "format": "procedural-rts.windows-acceptance",
          "schemaVersion": 1,
          "version": "{{version}}",
          "tag": "v{{version}}",
          "commit": "{{commit}}",
          "target": "windows-x86_64",
          "package": { "file": "{{packageName}}", "sha256": "{{packageHash}}" },
          "sampleMap": { "id": "authored-map-preview", "sha256": "{{sampleHash}}" },
          "host": { "machineClass": "physical Windows PC", "os": "Windows 11", "architecture": "x86_64" },
          "acceptedAtUtc": "2026-07-19T00:00:00Z",
          "interactive": {
            "packageExtracted": true,
            "desktopLaunch": true,
            "authoredMapPreviewEntry": true,
            "authoredMapLoaded": true,
            "normalSkirmishReturn": {{normalSkirmishReturn.ToString().ToLowerInvariant()}}
          }
        }
        """);
}

static void RequireReject(string evidencePath, string identityPath, string releaseRoot, string commit, string label)
{
    try
    {
        Validate(new EvidenceOptions(evidencePath, identityPath, releaseRoot, commit));
    }
    catch (InvalidOperationException)
    {
        return;
    }

    throw new InvalidOperationException($"Windows acceptance validator accepted {label}.");
}

static JsonElement ReadJson(string path, string label)
{
    Require(File.Exists(path), $"{label} is missing: {path}");
    using var document = JsonDocument.Parse(File.ReadAllText(path));
    Require(document.RootElement.ValueKind == JsonValueKind.Object, $"{label} must be a JSON object");
    return document.RootElement.Clone();
}

static JsonElement RequiredObject(JsonElement parent, string name, string label)
{
    Require(parent.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Object,
        $"{label}.{name} must be an object");
    return value;
}

static string RequiredString(JsonElement parent, string name, string label)
{
    Require(parent.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String,
        $"{label}.{name} must be a string");
    var text = value.GetString();
    Require(!string.IsNullOrWhiteSpace(text), $"{label}.{name} must not be blank");
    return text!;
}

static int RequiredInt(JsonElement parent, string name, string label)
{
    if (!parent.TryGetProperty(name, out var value) || !value.TryGetInt32(out var number))
    {
        throw new InvalidOperationException($"{label}.{name} must be an integer");
    }

    return number;
}

static bool RequiredTrue(JsonElement parent, string name, string label)
{
    Require(parent.TryGetProperty(name, out var value) && (value.ValueKind is JsonValueKind.True or JsonValueKind.False),
        $"{label}.{name} must be a boolean");
    return value.GetBoolean();
}

static string RequiredSha256(JsonElement parent, string name, string label)
{
    var value = RequiredString(parent, name, label);
    Require(value.Length == 64 && value.All(char.IsAsciiHexDigit) && value == value.ToLowerInvariant(),
        $"{label}.{name} must be a lowercase SHA-256");
    return value;
}

static string Sha256(string path)
{
    return Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
}

static bool IsCommit(string value)
{
    return value.Length == 40 && value.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f');
}

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

sealed record EvidenceOptions(string EvidencePath, string IdentityPath, string ReleaseRoot, string Commit)
{
    public static EvidenceOptions Parse(string[] args)
    {
        if (args.Length != 8)
        {
            throw new InvalidOperationException("Usage: --evidence <path> --identity <path> --release-root <path> --commit <sha>.");
        }

        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var index = 0; index < args.Length; index += 2)
        {
            var key = args[index];
            var value = args[index + 1];
            if (!values.TryAdd(key, value))
            {
                throw new InvalidOperationException($"Duplicate option: {key}");
            }
        }

        return new EvidenceOptions(
            Required(values, "--evidence"),
            Required(values, "--identity"),
            Required(values, "--release-root"),
            Required(values, "--commit"));
    }

    private static string Required(IReadOnlyDictionary<string, string> values, string name)
    {
        if (!values.TryGetValue(name, out var value) || string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Missing required option: {name}");
        }

        return value;
    }
}
