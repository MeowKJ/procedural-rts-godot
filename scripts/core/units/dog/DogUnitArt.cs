using Godot;

namespace ProceduralRts.Core;

public static class DogUnitArt
{
    public static UnitArtRecipe Infantry(string id, IconGlyph statusGlyph, string mountId = "main")
    {
        var hull = new[]
        {
            new Vector2(18, 0),
            new Vector2(9, -10),
            new Vector2(-10, -13),
            new Vector2(-18, -6),
            new Vector2(-15, 0),
            new Vector2(-18, 6),
            new Vector2(-10, 13),
            new Vector2(9, 10),
        };
        var layers = new List<ArtLayer>
        {
            new(UnitShapeLayer.Polygon(UnitShapeRole.BodyFill, Offset(hull, 2.0f, 2.8f), filled: true), ColorRole.Shadow, ArtBinding.Body),
            new(UnitShapeLayer.Polygon(UnitShapeRole.BodyFill, hull, filled: true), ColorRole.Body, ArtBinding.Body),
            new(UnitShapeLayer.Polygon(UnitShapeRole.AccentStroke, hull, filled: false, width: 2.6f), ColorRole.Ink, ArtBinding.Body),
            new(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(-10, -7), new Vector2(7, -7), 2.0f), ColorRole.Effect, ArtBinding.Body, ArtLayerZone.FactionMark, EnvironmentResponse.EffectReactive),
            new(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(-10, 7), new Vector2(7, 7), 2.0f), ColorRole.Effect, ArtBinding.Body, ArtLayerZone.FactionMark, EnvironmentResponse.EffectReactive),
            new(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(-5, -11), new Vector2(4, -11), 2.4f), ColorRole.Owner, ArtBinding.Body, ArtLayerZone.PlayerStripe),
            new(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(-5, 11), new Vector2(4, 11), 2.4f), ColorRole.Owner, ArtBinding.Body, ArtLayerZone.PlayerStripe),
            new(UnitShapeLayer.Circle(UnitShapeRole.Glow, new Vector2(-8, 0), 3.1f, filled: true), ColorRole.Effect, ArtBinding.Body, ArtLayerZone.FactionMark, EnvironmentResponse.EffectReactive),
            new(UnitShapeLayer.Circle(UnitShapeRole.Glow, new Vector2(5, 0), 2.2f, filled: false, width: 1.3f), ColorRole.Ink, ArtBinding.Body),
        };

        if (statusGlyph == IconGlyph.Settings)
        {
            layers.Add(new ArtLayer(UnitShapeLayer.Circle(UnitShapeRole.Glow, Vector2.Zero, 11.5f, filled: false, width: 1.5f), ColorRole.Effect, ArtBinding.Body, ArtLayerZone.Effect, EnvironmentResponse.EffectReactive));
            layers.Add(new ArtLayer(UnitShapeLayer.Circle(UnitShapeRole.Glow, Vector2.Zero, 6.5f, filled: false, width: 1.3f), ColorRole.Owner, ArtBinding.Mount(mountId), ArtLayerZone.PlayerBadge));
        }
        else if (statusGlyph == IconGlyph.AttackMove)
        {
            layers.Add(new ArtLayer(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(-2, -4), new Vector2(17, -4), 1.8f), ColorRole.Ink, ArtBinding.Mount(mountId), ArtLayerZone.Weapon));
            layers.Add(new ArtLayer(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(-2, 4), new Vector2(17, 4), 1.8f), ColorRole.Ink, ArtBinding.Mount(mountId), ArtLayerZone.Weapon));
            layers.Add(new ArtLayer(UnitShapeLayer.Circle(UnitShapeRole.Glow, new Vector2(1, 0), 5.8f, filled: false, width: 1.5f), ColorRole.Owner, ArtBinding.Mount(mountId), ArtLayerZone.PlayerBadge));
        }
        else
        {
            layers.Add(new ArtLayer(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(-2, 0), new Vector2(19, 0), 2.1f), ColorRole.Ink, ArtBinding.Mount(mountId), ArtLayerZone.Weapon));
            layers.Add(new ArtLayer(UnitShapeLayer.Circle(UnitShapeRole.Glow, new Vector2(1, 0), 4.4f, filled: false, width: 1.2f), ColorRole.Owner, ArtBinding.Mount(mountId), ArtLayerZone.PlayerBadge));
        }

        return new UnitArtRecipe(
            id,
            layers,
            ["light-step", "body-fixed-main"],
            statusGlyph);
    }

    public static UnitArtRecipe Vehicle(string id, IconGlyph statusGlyph, string mountId = "main")
    {
        var hull = new[]
        {
            new Vector2(-32, -13),
            new Vector2(-25, -19),
            new Vector2(17, -18),
            new Vector2(30, -8),
            new Vector2(32, 0),
            new Vector2(30, 8),
            new Vector2(17, 18),
            new Vector2(-25, 19),
            new Vector2(-32, 13),
        };
        var layers = new List<ArtLayer>
        {
            new(UnitShapeLayer.Polygon(UnitShapeRole.BodyFill, Offset(hull, 2.4f, 3.0f), filled: true), ColorRole.Shadow, ArtBinding.Body),
            new(UnitShapeLayer.Polygon(UnitShapeRole.BodyFill, hull, filled: true), ColorRole.Body, ArtBinding.Body),
            new(UnitShapeLayer.Polygon(UnitShapeRole.AccentStroke, hull, filled: false, width: 2.8f), ColorRole.Ink, ArtBinding.Body),
            new(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(-23, -15), new Vector2(18, -15), 3.8f), ColorRole.Shadow, ArtBinding.Body),
            new(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(-23, 15), new Vector2(18, 15), 3.8f), ColorRole.Shadow, ArtBinding.Body),
            new(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(-22, -9), new Vector2(15, -9), 1.5f), ColorRole.Effect, ArtBinding.Body, ArtLayerZone.FactionMark, EnvironmentResponse.EffectReactive),
            new(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(-22, 9), new Vector2(15, 9), 1.5f), ColorRole.Effect, ArtBinding.Body, ArtLayerZone.FactionMark, EnvironmentResponse.EffectReactive),
            new(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(-14, -2), new Vector2(13, -2), 1.2f), ColorRole.Ink, ArtBinding.Body),
            new(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(-14, 2), new Vector2(13, 2), 1.2f), ColorRole.Ink, ArtBinding.Body),
            new(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(-25, -18), new Vector2(-11, -18), 2.6f), ColorRole.Owner, ArtBinding.Body, ArtLayerZone.PlayerStripe),
            new(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(-25, 18), new Vector2(-11, 18), 2.6f), ColorRole.Owner, ArtBinding.Body, ArtLayerZone.PlayerStripe),
            new(UnitShapeLayer.Circle(UnitShapeRole.Glow, new Vector2(-2, -14), 3.3f, filled: true), ColorRole.Effect, ArtBinding.Body, ArtLayerZone.FactionMark, EnvironmentResponse.EffectReactive),
            new(UnitShapeLayer.Circle(UnitShapeRole.Glow, new Vector2(-2, 14), 3.3f, filled: true), ColorRole.Effect, ArtBinding.Body, ArtLayerZone.FactionMark, EnvironmentResponse.EffectReactive),
            new(UnitShapeLayer.Circle(UnitShapeRole.Glow, Vector2.Zero, 10.5f, filled: true), ColorRole.Shadow, ArtBinding.Mount(mountId)),
            new(UnitShapeLayer.Circle(UnitShapeRole.Glow, Vector2.Zero, 9.5f, filled: false, width: 2.2f), ColorRole.Ink, ArtBinding.Mount(mountId)),
            new(UnitShapeLayer.Circle(UnitShapeRole.Glow, Vector2.Zero, 6.4f, filled: false, width: 1.4f), ColorRole.Owner, ArtBinding.Mount(mountId), ArtLayerZone.PlayerBadge),
        };

        if (statusGlyph == IconGlyph.Settings)
        {
            layers.Add(new ArtLayer(UnitShapeLayer.Circle(UnitShapeRole.Glow, Vector2.Zero, 15.5f, filled: false, width: 1.6f), ColorRole.Effect, ArtBinding.Mount(mountId), ArtLayerZone.Effect, EnvironmentResponse.EffectReactive));
            layers.Add(new ArtLayer(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(-9, 0), new Vector2(16, 0), 2.0f), ColorRole.Effect, ArtBinding.Mount(mountId), ArtLayerZone.Weapon, EnvironmentResponse.EffectReactive));
        }
        else if (statusGlyph == IconGlyph.StanceHold)
        {
            layers.Add(new ArtLayer(UnitShapeLayer.Circle(UnitShapeRole.Glow, Vector2.Zero, 17.5f, filled: false, width: 2.0f), ColorRole.Effect, ArtBinding.Mount(mountId), ArtLayerZone.Effect, EnvironmentResponse.EffectReactive));
            layers.Add(new ArtLayer(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(2, 0), new Vector2(22, 0), 2.4f), ColorRole.Ink, ArtBinding.Mount(mountId), ArtLayerZone.Weapon));
        }
        else if (statusGlyph == IconGlyph.AttackMove)
        {
            layers.Add(new ArtLayer(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(2, -3), new Vector2(30, -3), 2.8f), ColorRole.Ink, ArtBinding.Mount(mountId), ArtLayerZone.Weapon));
            layers.Add(new ArtLayer(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(2, 3), new Vector2(30, 3), 2.8f), ColorRole.Ink, ArtBinding.Mount(mountId), ArtLayerZone.Weapon));
            layers.Add(new ArtLayer(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(19, -7), new Vector2(27, 0), 1.4f), ColorRole.Effect, ArtBinding.Mount(mountId), ArtLayerZone.Weapon, EnvironmentResponse.EffectReactive));
            layers.Add(new ArtLayer(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(19, 7), new Vector2(27, 0), 1.4f), ColorRole.Effect, ArtBinding.Mount(mountId), ArtLayerZone.Weapon, EnvironmentResponse.EffectReactive));
        }
        else
        {
            layers.Add(new ArtLayer(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(-2, 0), new Vector2(27, 0), 3.2f), ColorRole.Ink, ArtBinding.Mount(mountId), ArtLayerZone.Weapon));
            layers.Add(new ArtLayer(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(8, 0), new Vector2(28, 0), 1.0f), ColorRole.Effect, ArtBinding.Mount(mountId), ArtLayerZone.Weapon, EnvironmentResponse.EffectReactive));
        }

        return new UnitArtRecipe(
            id,
            layers,
            ["tracked-idle", "turret-follow-main"],
            statusGlyph);
    }

    public static UnitArtRecipe Harvester(string id)
    {
        var hull = new[]
        {
            new Vector2(28, 0),
            new Vector2(16, -22),
            new Vector2(-18, -22),
            new Vector2(-32, -8),
            new Vector2(-32, 8),
            new Vector2(-18, 22),
            new Vector2(16, 22),
        };
        return new UnitArtRecipe(
            id,
            [
                new ArtLayer(UnitShapeLayer.Polygon(UnitShapeRole.BodyFill, Offset(hull, 2.4f, 3.0f), filled: true), ColorRole.Shadow, ArtBinding.Body),
                new ArtLayer(UnitShapeLayer.Polygon(UnitShapeRole.BodyFill, hull, filled: true), ColorRole.Body, ArtBinding.Body),
                new ArtLayer(UnitShapeLayer.Polygon(UnitShapeRole.AccentStroke, hull, filled: false, width: 2.8f), ColorRole.Ink, ArtBinding.Body),
                new ArtLayer(UnitShapeLayer.Line(UnitShapeRole.Cargo, new Vector2(-20, -12), new Vector2(12, -12), 2.6f), ColorRole.Effect, ArtBinding.Body, ArtLayerZone.Cargo, EnvironmentResponse.EffectReactive),
                new ArtLayer(UnitShapeLayer.Line(UnitShapeRole.Cargo, new Vector2(-20, 12), new Vector2(12, 12), 2.6f), ColorRole.Effect, ArtBinding.Body, ArtLayerZone.Cargo, EnvironmentResponse.EffectReactive),
                new ArtLayer(UnitShapeLayer.Line(UnitShapeRole.Cargo, new Vector2(-16, -18), new Vector2(-2, -18), 3.0f), ColorRole.Owner, ArtBinding.Body, ArtLayerZone.PlayerStripe),
                new ArtLayer(UnitShapeLayer.Line(UnitShapeRole.Cargo, new Vector2(-16, 18), new Vector2(-2, 18), 3.0f), ColorRole.Owner, ArtBinding.Body, ArtLayerZone.PlayerStripe),
                new ArtLayer(UnitShapeLayer.Circle(UnitShapeRole.Glow, Vector2.Zero, 9.5f, filled: false, width: 2.0f), ColorRole.Effect, ArtBinding.Mount("main"), ArtLayerZone.FactionMark, EnvironmentResponse.EffectReactive),
                new ArtLayer(UnitShapeLayer.Circle(UnitShapeRole.Glow, Vector2.Zero, 6.0f, filled: false, width: 1.2f), ColorRole.Owner, ArtBinding.Mount("main"), ArtLayerZone.PlayerBadge),
            ],
            ["cargo-pulse", "harvest-ring"],
            IconGlyph.Harvester);
    }

    public static UnitArtRecipe Aircraft(string id, IconGlyph statusGlyph, string mountId = "main")
    {
        var hull = new[]
        {
            new Vector2(31, 0),
            new Vector2(12, -14),
            new Vector2(-9, -16),
            new Vector2(-29, -9),
            new Vector2(-38, 0),
            new Vector2(-29, 9),
            new Vector2(-9, 16),
            new Vector2(12, 14),
        };
        return new UnitArtRecipe(
            id,
            [
                new ArtLayer(UnitShapeLayer.Polygon(UnitShapeRole.BodyFill, Offset(hull, 2.2f, 2.8f), filled: true), ColorRole.Shadow, ArtBinding.Body),
                new ArtLayer(UnitShapeLayer.Polygon(UnitShapeRole.BodyFill, hull, filled: true), ColorRole.Body, ArtBinding.Body),
                new ArtLayer(UnitShapeLayer.Polygon(UnitShapeRole.AccentStroke, hull, filled: false, width: 2.4f), ColorRole.Ink, ArtBinding.Body),
                new ArtLayer(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(-25, -8), new Vector2(10, -11), 1.5f), ColorRole.Effect, ArtBinding.Body, ArtLayerZone.FactionMark, EnvironmentResponse.EffectReactive),
                new ArtLayer(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(-25, 8), new Vector2(10, 11), 1.5f), ColorRole.Effect, ArtBinding.Body, ArtLayerZone.FactionMark, EnvironmentResponse.EffectReactive),
                new ArtLayer(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(-18, -14), new Vector2(-5, -15), 2.8f), ColorRole.Owner, ArtBinding.Body, ArtLayerZone.PlayerStripe),
                new ArtLayer(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(-18, 14), new Vector2(-5, 15), 2.8f), ColorRole.Owner, ArtBinding.Body, ArtLayerZone.PlayerStripe),
                new ArtLayer(UnitShapeLayer.Circle(UnitShapeRole.Glow, new Vector2(-8, 0), 4.4f, filled: false, width: 1.8f), ColorRole.Effect, ArtBinding.Body, ArtLayerZone.FactionMark, EnvironmentResponse.EffectReactive),
                new ArtLayer(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(2, 0), new Vector2(27, 0), 1.9f), ColorRole.Ink, ArtBinding.Mount(mountId), ArtLayerZone.Weapon),
                new ArtLayer(UnitShapeLayer.Circle(UnitShapeRole.Glow, new Vector2(3, 0), 4.5f, filled: false, width: 1.1f), ColorRole.Owner, ArtBinding.Mount(mountId), ArtLayerZone.PlayerBadge),
            ],
            ["air-patrol", "contrail-soft", "body-fixed-main"],
            statusGlyph);
    }

    private static Vector2[] Offset(IReadOnlyList<Vector2> points, float x, float y)
    {
        var shifted = new Vector2[points.Count];
        var offset = new Vector2(x, y);
        for (var index = 0; index < points.Count; index++)
        {
            shifted[index] = points[index] + offset;
        }

        return shifted;
    }
}
