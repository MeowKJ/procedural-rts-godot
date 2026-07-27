using Godot;
using ProceduralRts.Core;
using CoreOwner = ProceduralRts.Core.Owner;

namespace ProceduralRts.World;

public partial class BuildingView : Node2D
{
    private BuildingRedrawSignature CaptureRedrawSignature()
    {
        var viewProjection = _viewProjection!.Value;
        var buildingProjection = _buildingProjection!.Value;
        var projection = _projection!.Value;
        var kind = viewProjection.Kind;
        var spec = BuildSpecCatalog.For(kind);
        var footprint = buildingProjection.Footprint;
        var position = projection.Position;
        var worldRect = new Rect2(position - footprint / 2f, footprint);
        var explored = IsProjectedBuildingExplored(projection.Owner, worldRect);
        var playerSlot = viewProjection.PlayerSlotId.Value;
        var owner = OwnerForPlayerSlot(viewProjection.PlayerSlotId);
        var faction = LegacyFaction(viewProjection.Faction);
        var viewerFactionKey = ViewerFaction is { } viewerFaction ? (int)viewerFaction : -1;
        var selected = projection.Selected;
        var powered = buildingProjection.Powered;
        var buildProgress = buildingProjection.BuildProgress;
        var constructionPaused = _buildingProjection?.IsConstructionPaused ?? false;
        var pauseReason = _buildingProjection?.PauseReason ?? ConstructionPauseReason.None;
        var hp = projection.Hp;
        var maxHp = projection.MaxHp;
        var healthFraction = maxHp <= 0 ? 0 : Mathf.Clamp(hp / maxHp, 0, 1);
        var damageSeverity = buildingProjection.DamageSeverity;
        var missingHealthFraction = buildingProjection.MissingHealthFraction;
        var rallyPulse = buildingProjection.RallyPulse;
        var hasRallyPoint = buildingProjection.RallyPoint is not null;
        var deliveryPulse = buildingProjection.DeliveryPulse;
        var dockOccupied = buildingProjection.DockOccupied;
        var bodyFacing = projection.Facing;
        var turretRelativeFacing = Mathf.AngleDifference(bodyFacing, buildingProjection.TurretFacing);
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
        var projectedQueue = _buildingProjection!.Value.ProductionQueue;
        queueCount = projectedQueue.Count;
        if (queueCount == 0)
        {
            firstKind = -1;
            firstProgress = 0;
            return;
        }

        firstKind = (int)projectedQueue[0].Kind;
        firstProgress = projectedQueue[0].Progress;
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
