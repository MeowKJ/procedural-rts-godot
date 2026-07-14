using Godot;

namespace ProceduralRts.Core;

public sealed class PowerPlantBuilding : BuildingDesign
{
    public override string Kind => BuildingDesignIds.PowerPlant;
    public override int SortOrder => 10;

    public override BuildSpec ToSpec()
    {
        return new BuildSpec(
            Kind,
            "building.powerplant",
            "Pulse Reactor",
            520,
            new Vector2(104, 96),
            new PlacementGridFootprint(4, 3),
            360,
            ArmorTag.Structure,
            null,
            new Color("#f6c55c"),
            BuildCategory.Power,
            IconGlyph.Settings,
            300,
            5.5f,
            BuildingDesignIds.Headquarters,
            new HashSet<string> { BuildingDesignIds.Headquarters },
            24,
            0,
            560,
            MovementDomain.Land);
    }
}
