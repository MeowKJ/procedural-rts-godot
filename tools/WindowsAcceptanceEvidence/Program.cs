using System.Globalization;
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
    var packageName = WindowsAcceptanceContract.PackageFileName(tag, target);
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
    Require(RequiredString(evidence, "format", "Windows acceptance evidence") == WindowsAcceptanceContract.Format,
        "Windows acceptance evidence format is unsupported");
    Require(RequiredInt(evidence, "schemaVersion", "Windows acceptance evidence") == WindowsAcceptanceContract.SchemaVersion,
        "Windows acceptance evidence schemaVersion must be 2");
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
    Require(RequiredString(host, "machineClass", "Windows acceptance evidence host") == WindowsAcceptanceContract.PhysicalWindowsMachineClass,
        "Windows acceptance evidence host.machineClass must declare a physical Windows PC");
    Require(IsPhysicalWindowsDesktop(RequiredString(host, "os", "Windows acceptance evidence host")),
        "Windows acceptance evidence host.os must identify Windows 10 or Windows 11 desktop");
    Require(RequiredString(host, "architecture", "Windows acceptance evidence host") == "x86_64",
        "Windows acceptance evidence host.architecture must be x86_64");
    var attestation = RequiredObject(evidence, "attestation", "Windows acceptance evidence");
    Require(RequiredString(attestation, "kind", "Windows acceptance evidence attestation") == WindowsAcceptanceContract.PhysicalInteractiveAttestationKind,
        "Windows acceptance evidence attestation.kind must declare human physical-Windows interaction");
    RequiredString(attestation, "operator", "Windows acceptance evidence attestation");
    Require(IsIssueEvidenceUrl(RequiredString(attestation, "issueEvidenceUrl", "Windows acceptance evidence attestation")),
        "Windows acceptance evidence attestation.issueEvidenceUrl must be a #570 issue-comment permalink");
    var acceptedAt = RequiredString(evidence, "acceptedAtUtc", "Windows acceptance evidence");
    Require(IsUtcIso8601(acceptedAt),
        "Windows acceptance evidence acceptedAtUtc must be a UTC ISO-8601 timestamp ending in Z");

    var interactive = RequiredObject(evidence, "interactive", "Windows acceptance evidence");
    foreach (var check in WindowsAcceptanceContract.RequiredInteractiveChecks)
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
        const string target = "windows-x86_64";
        const string commit = "0123456789abcdef0123456789abcdef01234567";
        const string sampleHash = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        var identityPath = Path.Combine(root, "release-identity.json");
        File.WriteAllText(identityPath, $$"""
            {"version":"{{version}}","tag":"v{{version}}","target":"{{target}}","sampleMapId":"authored-map-preview","sampleMapHash":"{{sampleHash}}"}
            """);
        var packageName = WindowsAcceptanceContract.PackageFileName($"v{version}", target);
        Require(packageName == "ProceduralRTS-v0.2.0-rc.1-windows-x86_64.zip",
            "Windows acceptance package name must retain the canonical v-prefixed tag");
        File.WriteAllBytes(Path.Combine(root, packageName), [1, 2, 3, 4]);
        var packageHash = Sha256(Path.Combine(root, packageName));
        File.WriteAllText(Path.Combine(root, "BUILD_INFO.json"), $$"""
            {"version":"{{version}}","tag":"v{{version}}","commit":"{{commit}}","target":"{{target}}","sampleMapId":"authored-map-preview","sampleMapHash":"{{sampleHash}}"}
            """);
        var evidencePath = Path.Combine(root, "windows-acceptance.json");

        WriteEvidence(evidencePath, version, target, commit, sampleHash, packageName, packageHash);
        Validate(new EvidenceOptions(evidencePath, identityPath, root, commit));

        File.WriteAllText(evidencePath, "{}");
        RequireReject(evidencePath, identityPath, root, commit, "empty record");
        WriteEvidence(evidencePath, version, target, "ffffffffffffffffffffffffffffffffffffffff", sampleHash, packageName, packageHash);
        RequireReject(evidencePath, identityPath, root, commit, "stale commit");
        WriteEvidence(evidencePath, version, target, commit, sampleHash, packageName, new string('b', 64));
        RequireReject(evidencePath, identityPath, root, commit, "package hash mismatch");
        WriteEvidence(evidencePath, version, target, commit, sampleHash, packageName, packageHash, acceptedAtUtc: "07/19/2026 00:00:00Z");
        RequireReject(evidencePath, identityPath, root, commit, "non-ISO acceptance timestamp");
        foreach (var check in WindowsAcceptanceContract.RequiredInteractiveChecks)
        {
            WriteEvidence(evidencePath, version, target, commit, sampleHash, packageName, packageHash, failedInteractiveCheck: check);
            RequireReject(evidencePath, identityPath, root, commit, $"interactive check '{check}'");
        }
        WriteEvidence(evidencePath, version, target, commit, sampleHash, packageName, packageHash, machineClass: "GitHub-hosted Windows runner");
        RequireReject(evidencePath, identityPath, root, commit, "non-physical Windows host");
        WriteEvidence(evidencePath, version, target, commit, sampleHash, packageName, packageHash, hostOperatingSystem: "Windows Server 2022");
        RequireReject(evidencePath, identityPath, root, commit, "non-desktop Windows host");
        WriteEvidence(evidencePath, version, target, commit, sampleHash, packageName, packageHash, architecture: "arm64");
        RequireReject(evidencePath, identityPath, root, commit, "non-x86_64 Windows host");
        WriteEvidence(evidencePath, version, target, commit, sampleHash, packageName, packageHash, issueEvidenceUrl: "https://github.com/MeowKJ/procedural-rts-godot/actions/runs/1");
        RequireReject(evidencePath, identityPath, root, commit, "non-issue physical evidence link");
        WriteEvidence(evidencePath, version, target, commit, sampleHash, packageName, packageHash, issueEvidenceUrl: "https://github.com/MeowKJ/procedural-rts-godot/issues/570#issuecomment-");
        RequireReject(evidencePath, identityPath, root, commit, "malformed issue-comment evidence link");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

static void WriteEvidence(
    string path,
    string version,
    string target,
    string commit,
    string sampleHash,
    string packageName,
    string packageHash,
    string? failedInteractiveCheck = null,
    string machineClass = WindowsAcceptanceContract.PhysicalWindowsMachineClass,
    string hostOperatingSystem = "Windows 11",
    string architecture = "x86_64",
    string issueEvidenceUrl = WindowsAcceptanceContract.ExampleIssueEvidenceUrl,
    string acceptedAtUtc = "2026-07-19T00:00:00Z",
    int schemaVersion = WindowsAcceptanceContract.SchemaVersion)
{
    var interactive = WindowsAcceptanceContract.RequiredInteractiveChecks.ToDictionary(
        check => check,
        check => !string.Equals(check, failedInteractiveCheck, StringComparison.Ordinal),
        StringComparer.Ordinal);
    File.WriteAllText(path, JsonSerializer.Serialize(new
    {
        format = WindowsAcceptanceContract.Format,
        schemaVersion,
        version,
        tag = $"v{version}",
        commit,
        target,
        package = new { file = packageName, sha256 = packageHash },
        sampleMap = new { id = "authored-map-preview", sha256 = sampleHash },
        host = new { machineClass, os = hostOperatingSystem, architecture },
        attestation = new
        {
            kind = WindowsAcceptanceContract.PhysicalInteractiveAttestationKind,
            @operator = "redacted-human-operator",
            issueEvidenceUrl,
        },
        acceptedAtUtc,
        interactive,
    }));
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

static bool IsUtcIso8601(string value)
{
    return DateTimeOffset.TryParseExact(
        value,
        ["yyyy-MM-dd'T'HH:mm:ss'Z'", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'"],
        CultureInfo.InvariantCulture,
        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
        out _);
}

static bool IsPhysicalWindowsDesktop(string operatingSystem)
{
    return operatingSystem.StartsWith("Windows 10", StringComparison.Ordinal)
        || operatingSystem.StartsWith("Windows 11", StringComparison.Ordinal);
}

static bool IsIssueEvidenceUrl(string value)
{
    const string commentPrefix = "#issuecomment-";
    if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
        || uri.Scheme != Uri.UriSchemeHttps
        || !uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase)
        || !uri.AbsolutePath.Equals("/MeowKJ/procedural-rts-godot/issues/570", StringComparison.Ordinal)
        || !string.IsNullOrEmpty(uri.Query)
        || !uri.Fragment.StartsWith(commentPrefix, StringComparison.Ordinal))
    {
        return false;
    }

    return long.TryParse(
        uri.Fragment[commentPrefix.Length..],
        NumberStyles.None,
        CultureInfo.InvariantCulture,
        out var commentId)
        && commentId > 0;
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

static class WindowsAcceptanceContract
{
    public const string Format = "procedural-rts.windows-acceptance";
    public const int SchemaVersion = 2;
    public const string PhysicalWindowsMachineClass = "physical Windows PC";
    public const string PhysicalInteractiveAttestationKind = "human-physical-windows-interactive";
    public const string ExampleIssueEvidenceUrl = "https://github.com/MeowKJ/procedural-rts-godot/issues/570#issuecomment-1";

    public static readonly string[] RequiredInteractiveChecks =
    [
        "packageExtracted",
        "desktopLaunch",
        "authoredMapPreviewEntry",
        "authoredMapLoaded",
        "unitSelection",
        "moveCommand",
        "buildingConstructed",
        "unitProduced",
        "resourceGathering",
        "combatEngagement",
        "victoryOutcome",
        "pauseAndResume",
        "matchRestart",
        "normalSkirmishReturn",
        "mainMenuReturn",
        "cleanApplicationExit",
    ];

    public static string PackageFileName(string tag, string target) => $"ProceduralRTS-{tag}-{target}.zip";
}
