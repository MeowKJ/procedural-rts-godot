using ProceduralRts.Core;

var failures = new List<string>();
MapSpecArtifactScenarios.Run(failures);
if (failures.Count > 0)
{
    Console.Error.WriteLine("MapSpecArtifactQa FAILED");
    foreach (var failure in failures)
    {
        Console.Error.WriteLine($"- {failure}");
    }

    Environment.Exit(1);
}

var artifact = MapSpecArtifactCodec.Encode(ArtifactFixtureMap.Create());
Console.WriteLine($"MapSpecArtifactQa PASSED: {artifact.Length} canonical bytes, sha256 {artifact.Sha256}.");
