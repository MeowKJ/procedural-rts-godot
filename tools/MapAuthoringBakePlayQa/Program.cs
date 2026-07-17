using System.Text.Json;
using ProceduralRts;
using ProceduralRts.Core;
using ProceduralRts.MapAuthoring.Editor;

var root = FindRoot(Directory.GetCurrentDirectory());
var artifactPath = Path.Combine(root, "assets", "maps", "authored-map-preview.mapspec.json");
var artifactBytes = File.ReadAllBytes(artifactPath);
var map = MapSpecArtifactCodec.Decode(artifactBytes);
var failures = new List<string>();
var evidence = MapAuthoringBakePlayScenarios.Run(map, artifactBytes, failures);
if (failures.Count > 0)
{
    Console.Error.WriteLine("MapAuthoringBakePlayQa FAILED");
    foreach (var failure in failures) Console.Error.WriteLine($"- {failure}");
    Environment.Exit(1);
}

if (args is ["--evidence-dir", var evidenceDirectory])
{
    Directory.CreateDirectory(evidenceDirectory);
    File.WriteAllText(Path.Combine(evidenceDirectory, "artifact-parity.json"),
        JsonSerializer.Serialize(evidence, new JsonSerializerOptions { WriteIndented = true }));
}
else if (args.Length != 0)
{
    throw new ArgumentException("usage: MapAuthoringBakePlayQa [--evidence-dir path]");
}

Console.WriteLine($"MapAuthoringBakePlayQa PASSED: {evidence.Length} bytes sha256 {evidence.Sha256}.");

static string FindRoot(string start)
{
    for (var directory = new DirectoryInfo(start); directory is not null; directory = directory.Parent)
        if (File.Exists(Path.Combine(directory.FullName, "ProceduralRts.csproj"))) return directory.FullName;
    throw new InvalidOperationException("Project root not found.");
}
