using Godot;

namespace ProceduralRts.Core;

public sealed class HeadquartersBuilding : BuildingDesign
{
    public override string Kind => BuildingDesignIds.Headquarters;
    public override int SortOrder => 0;

    public override BuildSpec ToSpec()
    {
        return new BuildSpec(
            Kind,
            "building.headquarters",
            "Nexus Command",
            1200,
            new Vector2(170, 132),
            new PlacementGridFootprint(6, 5),
            620,
            ArmorTag.Structure,
            WeaponKind.IonEmitter,
            new Color("#59f1ff"),
            BuildCategory.Command,
            IconGlyph.Building,
            1200,
            14.0f,
            null,
            new HashSet<string>(),
            0,
            12,
            560,
            MovementDomain.Land);
    }
}
