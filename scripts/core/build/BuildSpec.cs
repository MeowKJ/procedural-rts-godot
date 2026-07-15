using Godot;

namespace ProceduralRts.Core;

public enum ConstructionMethodKind
{
    DogDeployInPlace,
    CatSidebarPlacement,
    SharedRestartCapture
}

public enum BuildPlacementMode
{
    DeployInPlace,
    SidebarPlacement,
    RestartCapture
}

public enum PlacementReservationKind
{
    ProductionEgress,
    RefineryDock,
}

public readonly record struct PlacementReservationSpec(
    PlacementReservationKind Kind,
    int Column,
    int Row,
    int WidthCells,
    int HeightCells);

public sealed record ConstructionMethod(
    ConstructionMethodKind Kind,
    string MethodId,
    string Label,
    UnitFactionId? Faction,
    BuildPlacementMode PlacementMode,
    EntityCommandKind BackendCommandKind,
    string BackendCommandName);

public sealed record FactionConstructionPolicy(
    UnitFactionId Faction,
    ConstructionMethodKind DefaultMethod);

public sealed record BuildConstructionPolicy(
    IReadOnlyDictionary<ConstructionMethodKind, ConstructionMethod> Methods,
    IReadOnlyDictionary<UnitFactionId, FactionConstructionPolicy> FactionPolicies,
    IReadOnlySet<ConstructionMethodKind> SharedMethods)
{
    public static BuildConstructionPolicy Standard { get; } = new(
        new Dictionary<ConstructionMethodKind, ConstructionMethod>
        {
            [ConstructionMethodKind.DogDeployInPlace] = new(
                ConstructionMethodKind.DogDeployInPlace,
                "construction.dog.deploy_in_place",
                "Dog Deploy In Place",
                UnitFactionId.Dog,
                BuildPlacementMode.DeployInPlace,
                EntityCommandKind.Build,
                nameof(StartConstructionEntityCommand)),
            [ConstructionMethodKind.CatSidebarPlacement] = new(
                ConstructionMethodKind.CatSidebarPlacement,
                "construction.cat.sidebar_placement",
                "Cat Sidebar Placement",
                UnitFactionId.Cat,
                BuildPlacementMode.SidebarPlacement,
                EntityCommandKind.Build,
                nameof(StartConstructionEntityCommand)),
            [ConstructionMethodKind.SharedRestartCapture] = new(
                ConstructionMethodKind.SharedRestartCapture,
                "construction.shared.restart_capture",
                "Shared Restart Capture",
                null,
                BuildPlacementMode.RestartCapture,
                EntityCommandKind.Build,
                nameof(StartConstructionEntityCommand)),
        },
        new Dictionary<UnitFactionId, FactionConstructionPolicy>
        {
            [UnitFactionId.Dog] = new(UnitFactionId.Dog, ConstructionMethodKind.DogDeployInPlace),
            [UnitFactionId.Cat] = new(UnitFactionId.Cat, ConstructionMethodKind.CatSidebarPlacement),
        },
        new HashSet<ConstructionMethodKind>
        {
            ConstructionMethodKind.SharedRestartCapture,
        });

    public ConstructionMethod Method(ConstructionMethodKind kind)
    {
        return Methods[kind];
    }

    public ConstructionMethod DefaultMethodFor(UnitFactionId faction)
    {
        return FactionPolicies.TryGetValue(faction, out var policy)
            ? Method(policy.DefaultMethod)
            : Method(ConstructionMethodKind.SharedRestartCapture);
    }

    public IReadOnlyList<ConstructionMethod> MethodMetadata => Methods
        .Values
        .OrderBy(method => method.Kind)
        .ToArray();

    public IReadOnlyList<ConstructionMethod> SharedMethodMetadata => SharedMethods
        .OrderBy(kind => kind)
        .Select(Method)
        .ToArray();
}

public sealed record BuildSpec(
    string Kind,
    string EntitySpecId,
    string Label,
    float MaxHp,
    Vector2 Footprint,
    PlacementGridFootprint FootprintCells,
    float SightRange,
    ArmorTag ArmorTag,
    WeaponKind? WeaponKind,
    Color Accent,
    BuildCategory Category,
    IconGlyph Icon,
    int Cost,
    float BuildTime,
    string? RequiredProducer,
    IReadOnlySet<string> RequiredBuildings,
    int PowerProvided,
    int PowerUsed,
    float BuildRadius,
    MovementDomain PlacementDomain,
    float RefundRatio = 0.5f,
    BuildConstructionPolicy? ConstructionPolicy = null,
    ElementDefenseProfile? ElementDefense = null,
    TargetTraitProfile? TargetTraits = null,
    int PlacementClearanceCells = 1,
    IReadOnlyList<PlacementReservationSpec>? PlacementReservations = null)
{
    private int _placementClearanceCells = PlacementClearanceCells >= 0
        ? PlacementClearanceCells
        : throw new ArgumentOutOfRangeException(nameof(PlacementClearanceCells));

    public int PlacementClearanceCells
    {
        get => _placementClearanceCells;
        init => _placementClearanceCells = value >= 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value));
    }

    private IReadOnlyList<PlacementReservationSpec> _placementReservations =
        PlacementReservations ?? Array.Empty<PlacementReservationSpec>();

    public IReadOnlyList<PlacementReservationSpec> PlacementReservations
    {
        get => _placementReservations;
        init => _placementReservations = value ?? Array.Empty<PlacementReservationSpec>();
    }

    public Vector2 LogicalFootprint(float facing = 0) => FootprintCells.Rotated(facing).WorldSize;

    public BuildConstructionPolicy ConstructionMethods => ConstructionPolicy ?? BuildConstructionPolicy.Standard;

    public IReadOnlyList<ConstructionMethod> ConstructionMethodMetadata => ConstructionMethods.MethodMetadata;

    public ConstructionMethod ConstructionMethod(ConstructionMethodKind kind)
    {
        return ConstructionMethods.Method(kind);
    }

    public ConstructionMethod ConstructionMethodFor(UnitFactionId faction)
    {
        return ConstructionMethods.DefaultMethodFor(faction);
    }

    public string NameKey => Kind switch
    {
        BuildingDesignIds.Headquarters => "building.headquarters.name",
        BuildingDesignIds.PowerPlant => "building.powerPlant.name",
        BuildingDesignIds.Barracks => "building.barracks.name",
        BuildingDesignIds.VehicleFactory => "building.vehicleFactory.name",
        BuildingDesignIds.Refinery => "building.refinery.name",
        BuildingDesignIds.Airfield => "building.airfield.name",
        BuildingDesignIds.GroundTurret => "building.groundTurret.name",
        BuildingDesignIds.AntiAirTurret => "building.antiAirTurret.name",
        _ => BuildingDesignIds.NameKey(Kind),
    };

    public string RoleKey => $"build.category.{Category}";

    public string ShortCode => Kind switch
    {
        BuildingDesignIds.Headquarters => "HQ",
        BuildingDesignIds.PowerPlant => "PWR",
        BuildingDesignIds.Barracks => "BAR",
        BuildingDesignIds.VehicleFactory => "FAC",
        BuildingDesignIds.Refinery => "REF",
        BuildingDesignIds.Airfield => "AIR",
        BuildingDesignIds.GroundTurret => "GUN",
        BuildingDesignIds.AntiAirTurret => "AA",
        _ => BuildingDesignIds.ShortCode(Kind),
    };

    public IconGlyph RoleGlyph => Kind switch
    {
        BuildingDesignIds.Headquarters => IconGlyph.StanceHold,
        BuildingDesignIds.GroundTurret => IconGlyph.AttackMove,
        _ => Icon,
    };

}
