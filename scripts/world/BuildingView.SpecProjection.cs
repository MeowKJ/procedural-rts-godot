using Godot;
using ProceduralRts.Core;
using CoreOwner = ProceduralRts.Core.Owner;

namespace ProceduralRts.World;

public partial class BuildingView : Node2D
{
    private static BuildingArtColors ResolveBuildingArt(EntityRenderPalette palette, EnvironmentTone environmentTone)
    {
        return new BuildingArtColors(
            palette.Resolve(ColorRole.Body, environmentTone),
            palette.Resolve(ColorRole.Ink, environmentTone),
            palette.Resolve(ColorRole.Shadow, environmentTone),
            palette.Resolve(ColorRole.Owner, environmentTone),
            palette.Resolve(ColorRole.Effect, environmentTone),
            palette.Resolve(ColorRole.Effect, environmentTone, EnvironmentResponse.EffectReactive));
    }

    private static Color OwnerColor(CoreOwner owner)
    {
        return SoftOldCityPalette.PlayerColor(owner == CoreOwner.Player ? PlayerSlotId.One : PlayerSlotId.Two);
    }

    private (Color BodyAccent, Color RelationAccent) ResolvePresentationColors(
        string kind,
        CoreOwner owner,
        FactionId faction)
    {
        var spec = BuildSpecCatalog.For(kind);
        if (ViewerFaction is { } viewerFaction)
        {
            var relation = FactionRelations.Relation(CoreOwner.Player, viewerFaction, owner, faction);
            var bodyAccent = FactionVisualPolicy.EntityAccent(CoreOwner.Player, viewerFaction, owner, faction, spec.Accent);
            var relationAccent = FactionVisualPolicy.RelationOverlay(relation);
            return (bodyAccent, relationAccent);
        }

        var fallbackBodyAccent = FactionVisualPolicy.FactionAccent(faction).Lerp(spec.Accent, 0.38f);
        return (fallbackBodyAccent, OwnerColor(owner));
    }

    private static CoreOwner OwnerForPlayerSlot(PlayerSlotId playerSlotId)
    {
        return playerSlotId == PlayerSlotId.One ? CoreOwner.Player : CoreOwner.Enemy;
    }

    private static FactionId LegacyFaction(UnitFactionId faction)
    {
        return faction switch
        {
            UnitFactionId.Dog => FactionId.Dog,
            UnitFactionId.Cat => FactionId.Cat,
            UnitFactionId.Corruption => FactionId.Corruption,
            _ => FactionId.Dog,
        };
    }

    private bool IsProjectedBuildingExplored(OwnerId owner, Rect2 worldRect)
    {
        return owner == OwnerId.FromPlayerSlot(PlayerSlotId.One) || IsExploredMemory(worldRect);
    }

    private bool IsExploredMemory(Rect2 worldRect)
    {
        return ExploredProvider?.Invoke(worldRect) ?? true;
    }
}
