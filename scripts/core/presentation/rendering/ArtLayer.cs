namespace ProceduralRts.Core;

public enum ArtLayerZone
{
    Body,
    FactionMark,
    PlayerStripe,
    PlayerBadge,
    Weapon,
    Cargo,
    Effect
}

public sealed record ArtLayer(
    UnitShapeLayer Shape,
    ColorRole ColorRole,
    ArtBinding Binding,
    ArtLayerZone Zone = ArtLayerZone.Body,
    EnvironmentResponse EnvironmentResponse = EnvironmentResponse.Normal)
;
