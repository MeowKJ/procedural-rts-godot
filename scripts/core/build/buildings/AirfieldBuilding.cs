using Godot;

namespace ProceduralRts.Core;

public sealed class AirfieldBuilding : BuildingDesign
{
    public override string Kind => BuildingDesignIds.Airfield;
    public override int SortOrder => 50;

    public override BuildSpec ToSpec()
    {
        return new BuildSpec(
            Kind,
            "building.airfield",
            "Sky Relay Airfield",
            760,
            new Vector2(184, 132),
            520,
            ArmorTag.Structure,
            null,
            new Color("#b8d7ff"),
            BuildCategory.Air,
            IconGlyph.Air,
            820,
            11.0f,
            BuildingDesignIds.Headquarters,
            new HashSet<string> { BuildingDesignIds.Headquarters, BuildingDesignIds.PowerPlant, BuildingDesignIds.VehicleFactory },
            0,
            12,
            560,
            MovementDomain.Land);
    }
}
