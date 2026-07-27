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

    public required Func<BuildingViewProjection?> ViewProjectionProvider { get; init; }
    public Func<Rect2, bool>? ExploredProvider { get; init; }
    public Func<WorldVisualThemeState>? VisualThemeProvider { get; init; }
    public FactionId? ViewerFaction { get; init; }

    public override void _Process(double delta)
    {
        _viewProjection = ViewProjectionProvider();
        if (_viewProjection is not { } viewProjection)
        {
            Visible = false;
            return;
        }

        _buildingProjection = viewProjection.Presentation;
        _projection = _buildingProjection.Value.Entity;
        Position = _projection.Value.Position;
        Rotation = _projection.Value.Facing;
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
        if (_viewProjection is not { } viewProjection || _buildingProjection is not { } buildingProjection || _projection is not { } projection)
        {
            return;
        }

        var kind = viewProjection.Kind;
        var spec = BuildSpecCatalog.For(kind);
        var size = buildingProjection.Footprint;
        var worldRect = new Rect2(projection.Position - buildingProjection.Footprint / 2f, buildingProjection.Footprint);
        var explored = IsProjectedBuildingExplored(projection.Owner, worldRect);
        if (!explored)
        {
            return;
        }

        var owner = OwnerForPlayerSlot(viewProjection.PlayerSlotId);
        var faction = LegacyFaction(viewProjection.Faction);
        var (bodyAccent, relationAccent) = ResolvePresentationColors(kind, owner, faction);
        var ownerColor = SoftOldCityPalette.PlayerColor(viewProjection.PlayerSlotId);
        var environmentTone = EnvironmentTonePalette.For(VisualThemeProvider?.Invoke());
        var artPalette = EntityRenderPalette.SoftOldCity(ownerColor, bodyAccent);
        var art = ResolveBuildingArt(artPalette, environmentTone);
        var rect = new Rect2(-size / 2f, size);
        var pulse = 0.58f + Mathf.Sin((float)Time.GetTicksMsec() / 420f + viewProjection.Id) * 0.18f;
        var powered = buildingProjection.Powered;
        var buildProgress = buildingProjection.BuildProgress;
        var constructionPaused = buildingProjection.IsConstructionPaused;
        var pauseReason = buildingProjection.PauseReason;
        var projectedMaxHp = projection.MaxHp;
        var projectedHp = projection.Hp;
        var healthFraction = projectedMaxHp <= 0 ? 0 : Mathf.Clamp(projectedHp / projectedMaxHp, 0, 1);
        var damageSeverity = buildingProjection.DamageSeverity;
        var missingHealthFraction = buildingProjection.MissingHealthFraction;

        DrawFootprint(rect, bodyAccent, art);
        DrawStructure(rect, bodyAccent, art, pulse, powered, buildProgress, constructionPaused, pauseReason, kind);
        DrawDamageReadability(rect, art, pulse, damageSeverity, missingHealthFraction);
        DrawOwnershipZones(rect, art.Owner, art);
        DrawSelection(size, relationAccent, pulse);
        DrawHealth(size, projectedMaxHp, relationAccent);
        DrawProduction(size, bodyAccent, art);
    }
}
