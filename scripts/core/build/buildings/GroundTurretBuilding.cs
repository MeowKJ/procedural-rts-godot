using Godot;

namespace ProceduralRts.Core;

public sealed class GroundTurretBuilding : BuildingDesign
{
    public override string Kind => BuildingDesignIds.GroundTurret;
    public override int SortOrder => 60;

    public override BuildSpec ToSpec()
    {
        return new BuildSpec(
            Kind,
            "building.groundturret",
            "Sentinel Ground Turret",
            520,
            new Vector2(84, 84),
            430,
            ArmorTag.Structure,
            WeaponKind.VectorCannon,
            new Color("#ffb35c"),
            BuildCategory.Defense,
            IconGlyph.Turret,
            360,
            6.2f,
            BuildingDesignIds.Headquarters,
            new HashSet<string> { BuildingDesignIds.Headquarters, BuildingDesignIds.PowerPlant },
            0,
            5,
            560,
            MovementDomain.Land);
    }
}
