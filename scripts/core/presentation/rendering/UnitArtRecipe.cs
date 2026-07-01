namespace ProceduralRts.Core;

public sealed record UnitArtRecipe(
    string Id,
    IReadOnlyList<ArtLayer> Layers,
    IReadOnlyList<string> AnimationHints,
    IconGlyph StatusGlyph = IconGlyph.None)
{
    public IEnumerable<ArtLayer> PlayerColorZones =>
        Layers.Where(layer => layer.ColorRole == ColorRole.Owner
            || layer.Zone is ArtLayerZone.PlayerStripe or ArtLayerZone.PlayerBadge);
}
