using Godot;
using ProceduralRts.Core;
using CoreOwner = ProceduralRts.Core.Owner;

namespace ProceduralRts.World;

public partial class FootprintLayer
{
    private bool TryResolveFootprintSpecStyle(UnitInstance unit, out FootprintSpecStyle specStyle)
    {
        var spec = unit.Spec;
        var descriptor = UnitDesignDefinitionCatalog.RuntimeDescriptors[spec.Id];
        if (descriptor.DesignId != spec.Id)
        {
            specStyle = default;
            return false;
        }

        var ownerAccent = UnitFactionAccent(unit.Spec.Faction, unit.PlayerSlotId, descriptor.Accent);
        if (HasOwnerArtAccent(spec.Art))
        {
            ownerAccent = ownerAccent.Lerp(SoftOldCityPalette.PlayerColor(unit.PlayerSlotId), 0.22f);
        }

        var resourceWorker = IsResourceWorker(spec);
        specStyle = new FootprintSpecStyle(
            StyleForSpec(descriptor, ownerAccent, resourceWorker),
            descriptor.MovementDomain,
            descriptor.Radius);
        return true;
    }

    private static bool IsResourceWorker(UnitSpec spec)
    {
        if (spec.RoleTags.Contains(UnitRoleTag.Economy)
            || spec.RoleTags.Contains(UnitRoleTag.Worker)
            || spec.Archetype == UnitArchetype.Harvester
            || spec.Icon == IconGlyph.Harvester)
        {
            return true;
        }

        foreach (var layer in spec.Art.Layers)
        {
            if (layer.Zone == ArtLayerZone.Cargo || layer.Shape.Role == UnitShapeRole.Cargo)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasOwnerArtAccent(UnitArtRecipe art)
    {
        foreach (var layer in art.Layers)
        {
            if (layer.ColorRole == ColorRole.Owner
                || layer.Zone is ArtLayerZone.PlayerStripe or ArtLayerZone.PlayerBadge)
            {
                return true;
            }
        }

        return false;
    }

    private static FootprintStyle StyleForSpec(UnitSpecRuntimeDescriptor descriptor, Color ownerAccent, bool resourceWorker)
    {
        return descriptor.MovementDomain switch
        {
            MovementDomain.Air => new FootprintStyle(
                FootprintMarkKind.Contrail,
                38,
                1.4f,
                1.4f,
                22,
                descriptor.Radius * 0.22f,
                Tint(new Color("#d8f7ff", 0.16f), ownerAccent, 0.20f)),
            MovementDomain.Naval => new FootprintStyle(
                FootprintMarkKind.Wake,
                30,
                2.2f,
                1.3f,
                34,
                descriptor.Radius * 0.46f,
                Tint(new Color("#8fffe1", 0.18f), ownerAccent, 0.18f)),
            _ => LandStyleForSpec(descriptor, ownerAccent, resourceWorker),
        };
    }

    private static FootprintStyle LandStyleForSpec(UnitSpecRuntimeDescriptor descriptor, Color ownerAccent, bool resourceWorker)
    {
        var accent = resourceWorker
            ? ownerAccent.Lerp(SoftOldCityPalette.Cargo, 0.42f)
            : ownerAccent;

        return descriptor.WeightClass switch
        {
            UnitWeightClass.Light => new FootprintStyle(
                FootprintMarkKind.Step,
                24,
                1.55f,
                1.25f,
                9,
                descriptor.Radius * 0.28f,
                Tint(new Color("#d8f7ff", 0.16f), accent, resourceWorker ? 0.24f : 0.16f)),
            UnitWeightClass.Heavy => new FootprintStyle(
                FootprintMarkKind.TrackPlate,
                18,
                3.25f,
                4.2f,
                18,
                descriptor.Radius * 0.46f,
                Tint(new Color("#f6c55c", 0.13f), accent, resourceWorker ? 0.28f : 0.18f)),
            _ => new FootprintStyle(
                FootprintMarkKind.TwinTread,
                19,
                2.45f,
                2.4f,
                16,
                descriptor.Radius * 0.42f,
                Tint(new Color("#59f1ff", 0.14f), accent, resourceWorker ? 0.24f : 0.18f)),
        };
    }

    private static Color Tint(Color baseColor, Color accent, float amount)
    {
        return new Color(baseColor.Lerp(accent, amount), baseColor.A);
    }

    private static Color UnitFactionAccent(UnitFactionId faction, PlayerSlotId playerSlotId, Color fallback)
    {
        var factionAccent = faction switch
        {
            UnitFactionId.Dog => new Color("#64c7c7"),
            UnitFactionId.Cat => new Color("#c98293"),
            UnitFactionId.Corruption => new Color("#9d4259"),
            _ => fallback,
        };
        var playerAccent = SoftOldCityPalette.PlayerColor(playerSlotId);
        return factionAccent.Lerp(playerAccent, 0.36f);
    }
}
