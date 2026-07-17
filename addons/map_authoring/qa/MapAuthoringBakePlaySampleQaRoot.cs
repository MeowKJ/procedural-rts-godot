using Godot;
using ProceduralRts.Core;
using ProceduralRts.MapAuthoring.Editor;
using ProceduralRts.MapAuthoring.Nodes;

namespace ProceduralRts.MapAuthoring.Qa;

public partial class MapAuthoringBakePlaySampleQaRoot : Node
{
    private const string SampleScene = "res://addons/map_authoring/samples/AuthoredMapPreview.tscn";

    public override void _Ready()
    {
        try
        {
            var packed = ResourceLoader.Load<PackedScene>(SampleScene)
                ?? throw new InvalidOperationException($"Could not load {SampleScene}.");
            var root = packed.Instantiate<MapRoot>();
            try
            {
                var evaluation = MapAuthoringValidationRunner.Evaluate(root);
                if (!evaluation.IsClean)
                    throw new InvalidOperationException(
                        $"Typed sample diagnostics: {string.Join(',', evaluation.Report.Diagnostics.Select(value => value.Code))}.");
                var first = MapSpecArtifactCodec.Encode(evaluation.CleanMap!);
                var second = MapSpecArtifactCodec.Encode(evaluation.CleanMap!);
                Require(first.Sha256 == second.Sha256 && first.ToArray().SequenceEqual(second.ToArray()),
                    "Typed sample must bake deterministically twice.");

                var target = MapAuthoringArtifactPath.Resolve(root.ArtifactPath);
                if (OS.GetEnvironment("MAP_AUTHORING_GENERATE_SAMPLE") == "1")
                    _ = MapAuthoringArtifactWriter.Write(evaluation.CleanMap!, target);
                var committed = File.ReadAllBytes(target.AbsolutePath);
                Require(first.ToArray().SequenceEqual(committed), "Committed sample artifact must match typed scene bytes.");
                var map = MapSpecArtifactCodec.Decode(committed);
                Require(map.Id == "authored-map-preview"
                    && map.OwnerStarts.Count == 2 && map.Buildings.Count == 4 && map.Units.Count == 2
                    && map.Resources.Count == 1 && map.Obstacles.Count == 1 && map.TerrainCells.Count == 1
                    && map.Triggers.Count == 1 && map.Objectives.Count == 1 && map.NarrativeNodes.Count == 1,
                    "Typed sample must preserve every required authored collection.");
                GD.Print($"Map Authoring sample parity PASSED: {first.Length} bytes sha256 {first.Sha256}.");
            }
            finally
            {
                root.Free();
            }
            CallDeferred(nameof(QuitSuccess));
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            CallDeferred(nameof(QuitFailure));
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    public void QuitSuccess() => GetTree().Quit();
    public void QuitFailure() => GetTree().Quit(1);
}
