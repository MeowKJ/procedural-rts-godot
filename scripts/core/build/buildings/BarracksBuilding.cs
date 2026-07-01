using Godot;

namespace ProceduralRts.Core;

public sealed class BarracksBuilding : BuildingDesign
{
    public override string Kind => BuildingDesignIds.Barracks;
    public override int SortOrder => 20;

    public override BuildSpec ToSpec()
    {
        return new BuildSpec(
            Kind,
            "building.barracks",
            "Infantry Matrix",
            680,
            new Vector2(126, 104),
            420,
            ArmorTag.Structure,
            null,
            new Color("#8fffe1"),
            BuildCategory.Infantry,
            IconGlyph.Infantry,
            420,
            7.0f,
            BuildingDesignIds.Headquarters,
            new HashSet<string> { BuildingDesignIds.Headquarters, BuildingDesignIds.PowerPlant },
            0,
            6,
            560,
            MovementDomain.Land);
    }
}
