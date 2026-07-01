using Godot;

namespace ProceduralRts.Core;

public static class CatUnitArt
{
    public static UnitArtRecipe Infantry(string id, IconGlyph statusGlyph, string mountId = "main")
    {
        var hull = new[]
        {
            new Vector2(18, 0),
            new Vector2(4, -11),
            new Vector2(-12, -10),
            new Vector2(-18, -3),
            new Vector2(-13, 0),
            new Vector2(-18, 3),
            new Vector2(-12, 10),
            new Vector2(4, 11),
        };
        var layers = new List<ArtLayer>
        {
            new(UnitShapeLayer.Polygon(UnitShapeRole.BodyFill, Offset(hull, 1.8f, 2.5f), filled: true), ColorRole.Shadow, ArtBinding.Body),
            new(UnitShapeLayer.Polygon(UnitShapeRole.BodyFill, hull, filled: true), ColorRole.Body, ArtBinding.Body),
            new(UnitShapeLayer.Polygon(UnitShapeRole.AccentStroke, hull, filled: false, width: 2.2f), ColorRole.Ink, ArtBinding.Body),
            new(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(-13, -6), new Vector2(-2, -9), 1.4f), ColorRole.Effect, ArtBinding.Body, ArtLayerZone.FactionMark, EnvironmentResponse.EffectReactive),
            new(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(-2, -9), new Vector2(12, -2), 1.4f), ColorRole.Effect, ArtBinding.Body, ArtLayerZone.FactionMark, EnvironmentResponse.EffectReactive),
            new(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(-13, 6), new Vector2(-2, 9), 1.4f), ColorRole.Effect, ArtBinding.Body, ArtLayerZone.FactionMark, EnvironmentResponse.EffectReactive),
            new(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(-2, 9), new Vector2(12, 2), 1.4f), ColorRole.Effect, ArtBinding.Body, ArtLayerZone.FactionMark, EnvironmentResponse.EffectReactive),
            new(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(-11, -10), new Vector2(-3, -8), 2.1f), ColorRole.Owner, ArtBinding.Body, ArtLayerZone.PlayerStripe),
            new(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(-11, 10), new Vector2(-3, 8), 2.1f), ColorRole.Owner, ArtBinding.Body, ArtLayerZone.PlayerStripe),
            new(UnitShapeLayer.Circle(UnitShapeRole.Glow, new Vector2(-5, 0), 3.6f, filled: false, width: 1.3f), ColorRole.Effect, ArtBinding.Body, ArtLayerZone.FactionMark, EnvironmentResponse.EffectReactive),
        };

        if (statusGlyph == IconGlyph.Settings)
        {
            layers.Add(new ArtLayer(UnitShapeLayer.Circle(UnitShapeRole.Glow, Vector2.Zero, 10.5f, filled: false, width: 1.4f), ColorRole.Effect, ArtBinding.Mount(mountId), ArtLayerZone.Effect, EnvironmentResponse.EffectReactive));
            layers.Add(new ArtLayer(UnitShapeLayer.Circle(UnitShapeRole.Glow, Vector2.Zero, 5.8f, filled: false, width: 1.1f), ColorRole.Owner, ArtBinding.Mount(mountId), ArtLayerZone.PlayerBadge));
        }
        else if (statusGlyph == IconGlyph.AttackMove)
        {
            layers.Add(new ArtLayer(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(-1, -2), new Vector2(20, -6), 1.6f), ColorRole.Ink, ArtBinding.Mount(mountId), ArtLayerZone.Weapon));
            layers.Add(new ArtLayer(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(-1, 2), new Vector2(20, 6), 1.6f), ColorRole.Ink, ArtBinding.Mount(mountId), ArtLayerZone.Weapon));
            layers.Add(new ArtLayer(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(12, -7), new Vector2(19, 0), 1.0f), ColorRole.Effect, ArtBinding.Mount(mountId), ArtLayerZone.Weapon, EnvironmentResponse.EffectReactive));
            layers.Add(new ArtLayer(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(12, 7), new Vector2(19, 0), 1.0f), ColorRole.Effect, ArtBinding.Mount(mountId), ArtLayerZone.Weapon, EnvironmentResponse.EffectReactive));
        }
        else if (statusGlyph == IconGlyph.StanceAggressive)
        {
            layers.Add(new ArtLayer(UnitShapeLayer.Circle(UnitShapeRole.Glow, Vector2.Zero, 12.5f, filled: false, width: 1.6f), ColorRole.Effect, ArtBinding.Mount(mountId), ArtLayerZone.Effect, EnvironmentResponse.EffectReactive));
            layers.Add(new ArtLayer(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(1, 0), new Vector2(18, 0), 1.4f), ColorRole.Ink, ArtBinding.Mount(mountId), ArtLayerZone.Weapon));
            layers.Add(new ArtLayer(UnitShapeLayer.Circle(UnitShapeRole.Glow, new Vector2(2, 0), 4.8f, filled: false, width: 1.0f), ColorRole.Owner, ArtBinding.Mount(mountId), ArtLayerZone.PlayerBadge));
        }
        else
        {
            layers.Add(new ArtLayer(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(0, 0), new Vector2(18, 0), 1.7f), ColorRole.Ink, ArtBinding.Mount(mountId), ArtLayerZone.Weapon));
            layers.Add(new ArtLayer(UnitShapeLayer.Circle(UnitShapeRole.Glow, new Vector2(1, 0), 4.0f, filled: false, width: 1.0f), ColorRole.Owner, ArtBinding.Mount(mountId), ArtLayerZone.PlayerBadge));
        }

        return new UnitArtRecipe(
            id,
            layers,
            ["quiet-step", "body-fixed-main"],
            statusGlyph);
    }

    public static UnitArtRecipe Vehicle(string id, IconGlyph statusGlyph, string mountId = "main")
    {
        var hull = new[]
        {
            new Vector2(-33, -5),
            new Vector2(-17, -17),
            new Vector2(12, -15),
            new Vector2(33, -2),
            new Vector2(34, 0),
            new Vector2(33, 2),
            new Vector2(12, 15),
            new Vector2(-17, 17),
            new Vector2(-33, 5),
            new Vector2(-26, 0),
        };
        var layers = new List<ArtLayer>
        {
            new(UnitShapeLayer.Polygon(UnitShapeRole.BodyFill, Offset(hull, 1.8f, 2.5f), filled: true), ColorRole.Shadow, ArtBinding.Body),
            new(UnitShapeLayer.Polygon(UnitShapeRole.BodyFill, hull, filled: true), ColorRole.Body, ArtBinding.Body),
            new(UnitShapeLayer.Polygon(UnitShapeRole.AccentStroke, hull, filled: false, width: 2.4f), ColorRole.Ink, ArtBinding.Body),
            new(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(-23, -10), new Vector2(12, -12), 1.4f), ColorRole.Effect, ArtBinding.Body, ArtLayerZone.FactionMark, EnvironmentResponse.EffectReactive),
            new(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(-23, 10), new Vector2(12, 12), 1.4f), ColorRole.Effect, ArtBinding.Body, ArtLayerZone.FactionMark, EnvironmentResponse.EffectReactive),
            new(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(-11, -3), new Vector2(20, -7), 1.1f), ColorRole.Ink, ArtBinding.Body),
            new(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(-11, 3), new Vector2(20, 7), 1.1f), ColorRole.Ink, ArtBinding.Body),
            new(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(-23, -15), new Vector2(-9, -17), 2.4f), ColorRole.Owner, ArtBinding.Body, ArtLayerZone.PlayerStripe),
            new(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(-23, 15), new Vector2(-9, 17), 2.4f), ColorRole.Owner, ArtBinding.Body, ArtLayerZone.PlayerStripe),
            new(UnitShapeLayer.Circle(UnitShapeRole.Glow, new Vector2(-2, -11), 3.0f, filled: false, width: 1.2f), ColorRole.Effect, ArtBinding.Body, ArtLayerZone.FactionMark, EnvironmentResponse.EffectReactive),
            new(UnitShapeLayer.Circle(UnitShapeRole.Glow, new Vector2(-2, 11), 3.0f, filled: false, width: 1.2f), ColorRole.Effect, ArtBinding.Body, ArtLayerZone.FactionMark, EnvironmentResponse.EffectReactive),
            new(UnitShapeLayer.Circle(UnitShapeRole.Glow, Vector2.Zero, 8.8f, filled: true), ColorRole.Shadow, ArtBinding.Mount(mountId)),
            new(UnitShapeLayer.Circle(UnitShapeRole.Glow, Vector2.Zero, 8.0f, filled: false, width: 1.9f), ColorRole.Ink, ArtBinding.Mount(mountId)),
            new(UnitShapeLayer.Circle(UnitShapeRole.Glow, Vector2.Zero, 5.4f, filled: false, width: 1.1f), ColorRole.Owner, ArtBinding.Mount(mountId), ArtLayerZone.PlayerBadge),
        };

        if (statusGlyph == IconGlyph.Settings)
        {
            layers.Add(new ArtLayer(UnitShapeLayer.Circle(UnitShapeRole.Glow, Vector2.Zero, 14.0f, filled: false, width: 1.4f), ColorRole.Effect, ArtBinding.Mount(mountId), ArtLayerZone.Effect, EnvironmentResponse.EffectReactive));
            layers.Add(new ArtLayer(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(-7, 0), new Vector2(14, 0), 1.7f), ColorRole.Effect, ArtBinding.Mount(mountId), ArtLayerZone.Weapon, EnvironmentResponse.EffectReactive));
        }
        else if (statusGlyph == IconGlyph.StanceHold)
        {
            layers.Add(new ArtLayer(UnitShapeLayer.Circle(UnitShapeRole.Glow, Vector2.Zero, 16.0f, filled: false, width: 1.5f), ColorRole.Effect, ArtBinding.Mount(mountId), ArtLayerZone.Effect, EnvironmentResponse.EffectReactive));
            layers.Add(new ArtLayer(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(1, 0), new Vector2(20, 0), 1.9f), ColorRole.Ink, ArtBinding.Mount(mountId), ArtLayerZone.Weapon));
        }
        else if (statusGlyph == IconGlyph.AttackMove)
        {
            layers.Add(new ArtLayer(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(0, -3), new Vector2(28, -6), 2.1f), ColorRole.Ink, ArtBinding.Mount(mountId), ArtLayerZone.Weapon));
            layers.Add(new ArtLayer(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(0, 3), new Vector2(28, 6), 2.1f), ColorRole.Ink, ArtBinding.Mount(mountId), ArtLayerZone.Weapon));
            layers.Add(new ArtLayer(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(19, -8), new Vector2(28, 0), 1.0f), ColorRole.Effect, ArtBinding.Mount(mountId), ArtLayerZone.Weapon, EnvironmentResponse.EffectReactive));
            layers.Add(new ArtLayer(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(19, 8), new Vector2(28, 0), 1.0f), ColorRole.Effect, ArtBinding.Mount(mountId), ArtLayerZone.Weapon, EnvironmentResponse.EffectReactive));
        }
        else
        {
            layers.Add(new ArtLayer(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(0, 0), new Vector2(26, 0), 2.3f), ColorRole.Ink, ArtBinding.Mount(mountId), ArtLayerZone.Weapon));
            layers.Add(new ArtLayer(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(10, 0), new Vector2(27, 0), 0.9f), ColorRole.Effect, ArtBinding.Mount(mountId), ArtLayerZone.Weapon, EnvironmentResponse.EffectReactive));
        }

        return new UnitArtRecipe(
            id,
            layers,
            ["glide-track", "turret-follow-main"],
            statusGlyph);
    }

    public static UnitArtRecipe Harvester(string id)
    {
        var hull = new[]
        {
            new Vector2(29, 0),
            new Vector2(8, -21),
            new Vector2(-19, -17),
            new Vector2(-33, -2),
            new Vector2(-31, 5),
            new Vector2(-16, 20),
            new Vector2(11, 18),
        };
        return new UnitArtRecipe(
            id,
            [
                new ArtLayer(UnitShapeLayer.Polygon(UnitShapeRole.BodyFill, Offset(hull, 1.8f, 2.5f), filled: true), ColorRole.Shadow, ArtBinding.Body),
                new ArtLayer(UnitShapeLayer.Polygon(UnitShapeRole.BodyFill, hull, filled: true), ColorRole.Body, ArtBinding.Body),
                new ArtLayer(UnitShapeLayer.Polygon(UnitShapeRole.AccentStroke, hull, filled: false, width: 2.5f), ColorRole.Ink, ArtBinding.Body),
                new ArtLayer(UnitShapeLayer.Line(UnitShapeRole.Cargo, new Vector2(-22, -10), new Vector2(10, -14), 2.2f), ColorRole.Effect, ArtBinding.Body, ArtLayerZone.Cargo, EnvironmentResponse.EffectReactive),
                new ArtLayer(UnitShapeLayer.Line(UnitShapeRole.Cargo, new Vector2(-22, 10), new Vector2(10, 14), 2.2f), ColorRole.Effect, ArtBinding.Body, ArtLayerZone.Cargo, EnvironmentResponse.EffectReactive),
                new ArtLayer(UnitShapeLayer.Line(UnitShapeRole.Cargo, new Vector2(-18, -16), new Vector2(-4, -18), 2.5f), ColorRole.Owner, ArtBinding.Body, ArtLayerZone.PlayerStripe),
                new ArtLayer(UnitShapeLayer.Line(UnitShapeRole.Cargo, new Vector2(-18, 16), new Vector2(-4, 18), 2.5f), ColorRole.Owner, ArtBinding.Body, ArtLayerZone.PlayerStripe),
                new ArtLayer(UnitShapeLayer.Circle(UnitShapeRole.Glow, Vector2.Zero, 8.5f, filled: false, width: 1.7f), ColorRole.Effect, ArtBinding.Mount("main"), ArtLayerZone.FactionMark, EnvironmentResponse.EffectReactive),
                new ArtLayer(UnitShapeLayer.Circle(UnitShapeRole.Glow, Vector2.Zero, 5.4f, filled: false, width: 1.0f), ColorRole.Owner, ArtBinding.Mount("main"), ArtLayerZone.PlayerBadge),
            ],
            ["cargo-pulse", "quiet-harvest-ring"],
            IconGlyph.Harvester);
    }

    public static UnitArtRecipe Aircraft(string id, IconGlyph statusGlyph, string mountId = "main")
    {
        var hull = new[]
        {
            new Vector2(32, 0),
            new Vector2(4, -8),
            new Vector2(-18, -27),
            new Vector2(-11, -6),
            new Vector2(-34, -1),
            new Vector2(-34, 1),
            new Vector2(-11, 6),
            new Vector2(-18, 27),
            new Vector2(4, 8),
        };
        return new UnitArtRecipe(
            id,
            [
                new ArtLayer(UnitShapeLayer.Polygon(UnitShapeRole.BodyFill, Offset(hull, 1.8f, 2.5f), filled: true), ColorRole.Shadow, ArtBinding.Body),
                new ArtLayer(UnitShapeLayer.Polygon(UnitShapeRole.BodyFill, hull, filled: true), ColorRole.Body, ArtBinding.Body),
                new ArtLayer(UnitShapeLayer.Polygon(UnitShapeRole.AccentStroke, hull, filled: false, width: 2.2f), ColorRole.Ink, ArtBinding.Body),
                new ArtLayer(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(-24, -15), new Vector2(11, -4), 1.3f), ColorRole.Effect, ArtBinding.Body, ArtLayerZone.FactionMark, EnvironmentResponse.EffectReactive),
                new ArtLayer(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(-24, 15), new Vector2(11, 4), 1.3f), ColorRole.Effect, ArtBinding.Body, ArtLayerZone.FactionMark, EnvironmentResponse.EffectReactive),
                new ArtLayer(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(-20, -23), new Vector2(-10, -12), 2.3f), ColorRole.Owner, ArtBinding.Body, ArtLayerZone.PlayerStripe),
                new ArtLayer(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(-20, 23), new Vector2(-10, 12), 2.3f), ColorRole.Owner, ArtBinding.Body, ArtLayerZone.PlayerStripe),
                new ArtLayer(UnitShapeLayer.Circle(UnitShapeRole.Glow, new Vector2(-5, 0), 4.6f, filled: false, width: 1.5f), ColorRole.Effect, ArtBinding.Body, ArtLayerZone.FactionMark, EnvironmentResponse.EffectReactive),
                new ArtLayer(UnitShapeLayer.Line(UnitShapeRole.Core, new Vector2(0, 0), new Vector2(25, 0), 1.5f), ColorRole.Ink, ArtBinding.Mount(mountId), ArtLayerZone.Weapon),
                new ArtLayer(UnitShapeLayer.Circle(UnitShapeRole.Glow, new Vector2(2, 0), 4.0f, filled: false, width: 1.0f), ColorRole.Owner, ArtBinding.Mount(mountId), ArtLayerZone.PlayerBadge),
            ],
            ["air-hover", "contrail-soft", "body-fixed-main"],
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
