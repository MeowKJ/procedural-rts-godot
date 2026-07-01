using Godot;

namespace ProceduralRts.Core;

public sealed class AntiAirTurretBuilding : BuildingDesign
{
    public override string Kind => BuildingDesignIds.AntiAirTurret;
    public override int SortOrder => 70;

    public override BuildSpec ToSpec()
    {
        return new BuildSpec(
            Kind,
            "building.antiairturret",
            "Skyguard Anti-Air Turret",
            480,
            new Vector2(88, 88),
            500,
            ArmorTag.Structure,
            WeaponKind.SkySpear,
            new Color("#b5f8ff"),
            BuildCategory.Defense,
            IconGlyph.Air,
            420,
            6.8f,
            BuildingDesignIds.Headquarters,
            new HashSet<string> { BuildingDesignIds.Headquarters, BuildingDesignIds.PowerPlant, BuildingDesignIds.Airfield },
            0,
            6,
            560,
            MovementDomain.Land);
    }
}
