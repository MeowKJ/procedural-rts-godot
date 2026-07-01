using Godot;
using ProceduralRts.Core;
using CoreOwner = ProceduralRts.Core.Owner;

namespace ProceduralRts.World;

public partial class BuildingView : Node2D
{
    private BuildingRedrawSignature CaptureRedrawSignature()
    {
        var kind = _viewProjection?.Kind ?? Building.Kind;
        var spec = BuildSpecCatalog.For(kind);
        var footprint = _buildingProjection?.Footprint ?? spec.Footprint;
        var position = _projection?.Position ?? Building.Position;
        var worldRect = new Rect2(position - footprint / 2f, footprint);
        var explored = _buildingProjection is { } buildingProjection
            ? IsProjectedBuildingExplored(buildingProjection.Entity.Owner, worldRect)
            : IsLegacyBuildingExplored(worldRect);
        var playerSlot = _viewProjection?.PlayerSlotId.Value ?? PlayerSlotForOwner(Building.Owner).Value;
        var owner = _viewProjection is { } viewProjection
            ? OwnerForPlayerSlot(viewProjection.PlayerSlotId)
            : Building.Owner;
        var faction = _viewProjection is { } identityProjection
            ? LegacyFaction(identityProjection.Faction)
            : Building.FactionId;
        var viewerFactionKey = ViewerFaction is { } viewerFaction ? (int)viewerFaction : -1;
        var selected = _projection?.Selected ?? Building.Selected;
        var powered = _buildingProjection?.Powered ?? Building.Powered;
        var buildProgress = _buildingProjection?.BuildProgress ?? Building.BuildProgress;
        var constructionPaused = _buildingProjection?.IsConstructionPaused ?? false;
        var pauseReason = _buildingProjection?.PauseReason ?? ConstructionPauseReason.None;
        var hp = _projection?.Hp ?? Building.Hp;
        var maxHp = _projection?.MaxHp ?? spec.MaxHp;
        var healthFraction = maxHp <= 0 ? 0 : Mathf.Clamp(hp / maxHp, 0, 1);
        var damageSeverity = _buildingProjection?.DamageSeverity
            ?? BuildingPresentationProjection.DamageSeverityFor(healthFraction, hp > 0);
        var missingHealthFraction = _buildingProjection?.MissingHealthFraction ?? (1f - healthFraction);
        var rallyPulse = _buildingProjection?.RallyPulse ?? Building.RallyPulse;
        var hasRallyPoint = (_buildingProjection?.RallyPoint ?? Building.RallyPoint) is not null;
        var deliveryPulse = _buildingProjection?.DeliveryPulse ?? Building.DeliveryPulse;
        var dockOccupied = _buildingProjection?.DockOccupied
            ?? (Building.DockReservedByHarvesterId is not null || Building.DockedHarvesterId is not null);
        var bodyFacing = _projection?.Facing ?? Building.Facing;
        var turretRelativeFacing = Mathf.AngleDifference(bodyFacing, Building.TurretFacing);
        var theme = VisualThemeProvider?.Invoke();
        CaptureProductionSignature(out var queueCount, out var firstProductionKind, out var firstProductionProgress);

        return new BuildingRedrawSignature(
            kind,
            (int)owner,
            (int)faction,
            viewerFactionKey,
            playerSlot,
            selected,
            Quantize(footprint.X, 10),
            Quantize(footprint.Y, 10),
            Quantize(hp, 100),
            Quantize(maxHp, 100),
            powered,
            Quantize(buildProgress, 1000),
            constructionPaused,
            (int)pauseReason,
            (int)damageSeverity,
            Quantize(missingHealthFraction, 1000),
            queueCount,
            firstProductionKind,
            Quantize(firstProductionProgress, 1000),
            hasRallyPoint,
            Quantize(rallyPulse, 1000),
            Quantize(deliveryPulse, 1000),
            dockOccupied,
            Quantize(turretRelativeFacing, 1000),
            theme is null ? 0 : (int)theme.Current,
            theme is null ? 0 : (int)theme.Target,
            theme is null ? 1000 : Quantize(theme.TransitionProgress, 1000),
            theme?.Driver ?? string.Empty,
            explored);
    }

    private void CaptureProductionSignature(out int queueCount, out int firstKind, out float firstProgress)
    {
        var projectedQueue = _buildingProjection?.ProductionQueue;
        queueCount = projectedQueue?.Count ?? Building.ProductionQueue.Count;
        if (queueCount == 0)
        {
            firstKind = -1;
            firstProgress = 0;
            return;
        }

        if (projectedQueue is not null)
        {
            firstKind = (int)projectedQueue[0].Kind;
            firstProgress = projectedQueue[0].Progress;
            return;
        }

        firstKind = (int)Building.ProductionQueue[0].Kind;
        firstProgress = Building.ProductionQueue[0].Progress;
    }

    private static PlayerSlotId PlayerSlotForOwner(CoreOwner owner)
    {
        return owner == CoreOwner.Player ? PlayerSlotId.One : PlayerSlotId.Two;
    }

    private static int Quantize(float value, float scale)
    {
        return Mathf.RoundToInt(value * scale);
    }

    private readonly record struct BuildingRedrawSignature(
        string Kind,
        int Owner,
        int Faction,
        int ViewerFaction,
        int PlayerSlot,
        bool Selected,
        int FootprintX,
        int FootprintY,
        int Hp,
        int MaxHp,
        bool Powered,
        int BuildProgress,
        bool ConstructionPaused,
        int PauseReason,
        int DamageSeverity,
        int MissingHealthFraction,
        int ProductionQueueCount,
        int FirstProductionKind,
        int FirstProductionProgress,
        bool HasRallyPoint,
        int RallyPulse,
        int DeliveryPulse,
        bool DockOccupied,
        int TurretRelativeFacing,
        int ThemeCurrent,
        int ThemeTarget,
        int ThemeProgress,
        string ThemeDriver,
        bool Explored)
    {
        public bool NeedsAnimatedRedraw =>
            Selected
            || BuildProgress < 1000
            || !Powered
            || ConstructionPaused
            || DamageSeverity >= (int)BuildingDamageReadabilityLevel.Heavy
            || RallyPulse > 0
            || DeliveryPulse > 0
            || ThemeCurrent != ThemeTarget
            || ProductionQueueCount > 0;
    }
}
