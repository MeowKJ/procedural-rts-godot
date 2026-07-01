using Godot;
using ProceduralRts.Core;
using CoreOwner = ProceduralRts.Core.Owner;

namespace ProceduralRts.World;

public partial class BuildingView : Node2D
{
    private const float RedrawIntervalSeconds = 1f / 20f;
    private const int LargeArcSegments = 48;
    private const int MediumArcSegments = 40;
    private const int SmallArcSegments = 32;
    private float _redrawTimer;
    private BuildingRedrawSignature? _lastRedrawSignature;
    private EntityProjection? _projection;
    private BuildingPresentationProjection? _buildingProjection;
    private BuildingViewProjection? _viewProjection;
    private readonly record struct BuildingArtColors(
        Color Body,
        Color Ink,
        Color Shadow,
        Color Owner,
        Color Effect,
        Color Highlight);

    public required GameState State { get; init; }
    public required BuildingModel Building { get; init; }
    public Func<EntityProjection?>? ProjectionProvider { get; init; }
    public Func<BuildingPresentationProjection?>? BuildingProjectionProvider { get; init; }
    public Func<BuildingViewProjection?>? ViewProjectionProvider { get; init; }
    public Func<Rect2, bool>? ExploredProvider { get; init; }
    public Func<WorldVisualThemeState>? VisualThemeProvider { get; init; }
    public FactionId? ViewerFaction { get; init; }

    public override void _Process(double delta)
    {
        _viewProjection = ViewProjectionProvider?.Invoke();
        _buildingProjection = _viewProjection?.Presentation ?? BuildingProjectionProvider?.Invoke();
        _projection = _buildingProjection?.Entity ?? ProjectionProvider?.Invoke();
        Position = _projection?.Position ?? Building.Position;
        Rotation = _projection?.Facing ?? Building.Facing;
        var signature = CaptureRedrawSignature();
        var redrawDirty = _lastRedrawSignature != signature;
        _redrawTimer -= (float)delta;
        if ((redrawDirty || signature.NeedsAnimatedRedraw) && _redrawTimer <= 0)
        {
            _redrawTimer = RedrawIntervalSeconds;
            _lastRedrawSignature = signature;
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        var kind = _viewProjection?.Kind ?? Building.Kind;
        var spec = BuildSpecCatalog.For(kind);
        var size = _buildingProjection?.Footprint ?? spec.Footprint;
        var worldRect = _buildingProjection is { } buildingProjection
            ? new Rect2(buildingProjection.Entity.Position - buildingProjection.Footprint / 2f, buildingProjection.Footprint)
            : new Rect2(Building.Position - spec.Footprint / 2f, spec.Footprint);
        var explored = _buildingProjection is { } projection
            ? IsProjectedBuildingExplored(projection.Entity.Owner, worldRect)
            : IsLegacyBuildingExplored(worldRect);
        if (!explored)
        {
            return;
        }

        var owner = _viewProjection is { } viewProjection
            ? OwnerForPlayerSlot(viewProjection.PlayerSlotId)
            : Building.Owner;
        var faction = _viewProjection is { } identityProjection
            ? LegacyFaction(identityProjection.Faction)
            : Building.FactionId;
        var (bodyAccent, relationAccent) = ResolvePresentationColors(kind, owner, faction);
        var ownerColor = _viewProjection is { } ownerProjection
            ? SoftOldCityPalette.PlayerColor(ownerProjection.PlayerSlotId)
            : OwnerColor(Building.Owner);
        var environmentTone = EnvironmentTonePalette.For(VisualThemeProvider?.Invoke());
        var artPalette = EntityRenderPalette.SoftOldCity(ownerColor, bodyAccent);
        var art = ResolveBuildingArt(artPalette, environmentTone);
        var rect = new Rect2(-size / 2f, size);
        var pulse = 0.58f + Mathf.Sin((float)Time.GetTicksMsec() / 420f + Building.Id) * 0.18f;
        var powered = _buildingProjection?.Powered ?? Building.Powered;
        var buildProgress = _buildingProjection?.BuildProgress ?? Building.BuildProgress;
        var constructionPaused = _buildingProjection?.IsConstructionPaused ?? false;
        var pauseReason = _buildingProjection?.PauseReason ?? ConstructionPauseReason.None;
        var projectedMaxHp = _projection?.MaxHp ?? spec.MaxHp;
        var projectedHp = _projection?.Hp ?? Building.Hp;
        var healthFraction = projectedMaxHp <= 0 ? 0 : Mathf.Clamp(projectedHp / projectedMaxHp, 0, 1);
        var damageSeverity = _buildingProjection?.DamageSeverity
            ?? BuildingPresentationProjection.DamageSeverityFor(healthFraction, projectedHp > 0);
        var missingHealthFraction = _buildingProjection?.MissingHealthFraction ?? (1f - healthFraction);

        DrawFootprint(rect, bodyAccent, art);
        DrawStructure(rect, bodyAccent, art, pulse, powered, buildProgress, constructionPaused, pauseReason, kind);
        DrawDamageReadability(rect, art, pulse, damageSeverity, missingHealthFraction);
        DrawOwnershipZones(rect, art.Owner, art);
        DrawSelection(size, relationAccent, pulse);
        DrawHealth(size, projectedMaxHp, relationAccent);
        DrawProduction(size, bodyAccent, art);
    }
}
