using Godot;

namespace ProceduralRts.Core;

public static class UnitBodyRenderRecipeCache
{
    private static readonly Dictionary<UnitArtRecipe, UnitBodyRenderRecipe> CachedRecipes = [];

    public static UnitBodyRenderRecipe For(UnitArtRecipe recipe)
    {
        lock (CachedRecipes)
        {
            if (!CachedRecipes.TryGetValue(recipe, out var compiled))
            {
                compiled = UnitBodyRenderRecipe.Compile(recipe);
                CachedRecipes[recipe] = compiled;
            }

            return compiled;
        }
    }
}

public sealed class UnitBodyRenderRecipe
{
    private UnitBodyRenderRecipe(
        UnitBodyRenderLayer[] bodyLayers,
        UnitBodyMountRenderGroup[] mountGroups,
        UnitBodyRenderLayer[] runtimePulseLayers)
    {
        BodyLayers = bodyLayers;
        MountGroups = mountGroups;
        RuntimePulseLayers = runtimePulseLayers;
    }

    public IReadOnlyList<UnitBodyRenderLayer> BodyLayers { get; }
    public IReadOnlyList<UnitBodyMountRenderGroup> MountGroups { get; }
    public IReadOnlyList<UnitBodyRenderLayer> RuntimePulseLayers { get; }

    public static UnitBodyRenderRecipe Compile(UnitArtRecipe recipe)
    {
        var bodyLayers = new List<UnitBodyRenderLayer>(recipe.Layers.Count);
        var runtimePulseLayers = new List<UnitBodyRenderLayer>();
        var mountLayersById = new Dictionary<string, List<UnitBodyRenderLayer>>(StringComparer.Ordinal);
        var mountOrder = new List<string>();

        foreach (var layer in recipe.Layers)
        {
            var renderLayer = UnitBodyRenderLayer.From(layer);
            switch (layer.Binding.Kind)
            {
                case ArtBindingKind.Body:
                    bodyLayers.Add(renderLayer);
                    break;
                case ArtBindingKind.Mount:
                    if (!mountLayersById.TryGetValue(layer.Binding.Id, out var mountLayers))
                    {
                        mountLayers = [];
                        mountLayersById[layer.Binding.Id] = mountLayers;
                        mountOrder.Add(layer.Binding.Id);
                    }

                    mountLayers.Add(renderLayer);
                    break;
                case ArtBindingKind.RuntimePulse:
                    runtimePulseLayers.Add(renderLayer);
                    break;
            }
        }

        var mountGroups = new UnitBodyMountRenderGroup[mountOrder.Count];
        for (var index = 0; index < mountOrder.Count; index++)
        {
            var mountId = mountOrder[index];
            mountGroups[index] = new UnitBodyMountRenderGroup(mountId, mountLayersById[mountId].ToArray());
        }

        return new UnitBodyRenderRecipe(bodyLayers.ToArray(), mountGroups, runtimePulseLayers.ToArray());
    }
}

public sealed record UnitBodyMountRenderGroup(string MountId, IReadOnlyList<UnitBodyRenderLayer> Layers);

public readonly record struct UnitBodyRenderLayer(
    UnitShapeLayer Shape,
    ColorRole ColorRole,
    EnvironmentResponse EnvironmentResponse,
    Vector2[]? ClosedPoints)
{
    public static UnitBodyRenderLayer From(ArtLayer layer)
    {
        return new UnitBodyRenderLayer(
            layer.Shape,
            layer.ColorRole,
            layer.EnvironmentResponse,
            ClosedPolyline(layer.Shape));
    }

    private static Vector2[]? ClosedPolyline(UnitShapeLayer shape)
    {
        if (shape.Kind != UnitShapeKind.Polygon || shape.Points.Length == 0)
        {
            return null;
        }

        var closed = new Vector2[shape.Points.Length + 1];
        Array.Copy(shape.Points, closed, shape.Points.Length);
        closed[^1] = shape.Points[0];
        return closed;
    }
}
