using Godot;

namespace ProceduralRts.Core;

public sealed class VehicleFactoryBuilding : BuildingDesign
{
    public override string Kind => BuildingDesignIds.VehicleFactory;
    public override int SortOrder => 30;

    public override BuildSpec ToSpec()
    {
        return new BuildSpec(
            Kind,
            "building.vehiclefactory",
            "Vector Foundry",
            860,
            new Vector2(168, 124),
            new PlacementGridFootprint(6, 4),
            460,
            ArmorTag.Structure,
            null,
            new Color("#59f1ff"),
            BuildCategory.Vehicle,
            IconGlyph.Tank,
            720,
            10.5f,
            BuildingDesignIds.Headquarters,
            new HashSet<string> { BuildingDesignIds.Headquarters, BuildingDesignIds.PowerPlant },
            0,
            10,
            560,
            MovementDomain.Land);
    }
}
