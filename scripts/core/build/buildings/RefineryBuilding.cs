using Godot;

namespace ProceduralRts.Core;

public sealed class RefineryBuilding : BuildingDesign
{
    public override string Kind => BuildingDesignIds.Refinery;
    public override int SortOrder => 40;

    public override BuildSpec ToSpec()
    {
        return new BuildSpec(
            Kind,
            "building.refinery",
            "Ion Refinery",
            780,
            new Vector2(152, 118),
            new PlacementGridFootprint(5, 4),
            430,
            ArmorTag.Structure,
            null,
            new Color("#f6c55c"),
            BuildCategory.Economy,
            IconGlyph.Harvester,
            640,
            9.0f,
            BuildingDesignIds.Headquarters,
            new HashSet<string> { BuildingDesignIds.Headquarters, BuildingDesignIds.PowerPlant },
            0,
            8,
            560,
            MovementDomain.Land,
            PlacementReservations:
            [
                new PlacementReservationSpec(PlacementReservationKind.RefineryDock, 5, 1, 3, 2),
            ]);
    }
}
