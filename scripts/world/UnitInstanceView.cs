using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.World;

public partial class UnitInstanceView : Node2D
{
    private const float RedrawIntervalSeconds = 1f / 30f;
    private const int OverlayArcSegments = 40;
    private const bool CrispOverlayStroke = false;
    private float _redrawTimer;
    private UnitRedrawSignature? _lastRedrawSignature;

    public required UnitInstance Unit { get; init; }
    public required PlayerSlotId Viewer { get; init; }
    public required PlayerRelationTable Relations { get; init; }
    public Func<EntityProjection?>? ProjectionProvider { get; init; }
    public Func<WorldVisualThemeState>? VisualThemeProvider { get; init; }
    public bool DrawBodyArt { get; init; } = true;
    private EntityProjection? _projection;

    public override void _Process(double delta)
    {
        _projection = ProjectionProvider?.Invoke();
        Position = _projection?.Position ?? Unit.Position;
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
        var owner = _projection?.Owner.ToPlayerSlot() ?? Unit.PlayerSlotId;
        var selected = _projection?.Selected ?? Unit.Selected;
        var hp = _projection?.Hp ?? Unit.Hp;
        var maxHp = _projection?.MaxHp ?? Unit.Spec.Stats.MaxHp;
        var relation = Relations.Relation(Viewer, owner);
        var relationAccent = SoftOldCityPalette.RelationColor(relation);
        var pulse = 0.55f + Mathf.Sin((float)Time.GetTicksMsec() / 250f + Unit.Id) * 0.25f;
        var radius = Unit.Spec.Collision.Radius;

        DrawCircle(Vector2.Zero, radius + 16 + Unit.CommandPulse * 8, new Color(relationAccent, selected ? 0.10f : 0.018f));
        if (Unit.AlertPulse > 0)
        {
            var alertRadius = radius + 14 + (1 - Unit.AlertPulse) * 20;
            DrawArc(Vector2.Zero, alertRadius, 0, Mathf.Tau, OverlayArcSegments, new Color(SoftOldCityPalette.InnerLight, Unit.AlertPulse * 0.58f), 1.2f, CrispOverlayStroke);
            DrawArc(Vector2.Zero, alertRadius + 6, 0, Mathf.Tau, OverlayArcSegments, new Color(relationAccent, Unit.AlertPulse * 0.38f), 1.8f, CrispOverlayStroke);
        }

        if (selected)
        {
            DrawArc(Vector2.Zero, radius + 9 + pulse * 2, 0, Mathf.Tau, OverlayArcSegments, new Color(SoftOldCityPalette.Ink, 0.82f), 2.0f, CrispOverlayStroke);
            DrawArc(Vector2.Zero, radius + 10 + pulse * 2, 0, Mathf.Tau, OverlayArcSegments, new Color(relationAccent, 0.54f), 1.2f, CrispOverlayStroke);
            DrawArc(Vector2.Zero, radius + 18 + Unit.CommandPulse * 10, 0, Mathf.Tau, OverlayArcSegments, new Color(relationAccent, 0.36f), 1.0f, CrispOverlayStroke);
        }

        if (DrawBodyArt)
        {
            var facing = _projection?.Facing ?? Unit.Facing;
            var palette = EntityRenderPalette.SoftOldCity(SoftOldCityPalette.PlayerColor(owner));
            var environmentTone = EnvironmentTonePalette.For(VisualThemeProvider?.Invoke());
            UnitVisualRenderer.DrawUnitArtRecipe(
                this,
                Unit.Spec.Art,
                palette,
                Vector2.Zero,
                1,
                facing,
                UnitMountFacingSource.FromRuntimeMounts(Unit.WeaponMounts),
                environmentTone);
        }

        DrawStatusGlyph(radius);
        DrawVeterancyGlyph(radius, _projection?.VeterancyRank ?? 0);
        DrawCargo(radius);
        DrawHealth(radius, relationAccent, hp, maxHp);
    }

    private void DrawStatusGlyph(float radius)
    {
        if (!Unit.Spec.RoleTags.Contains(UnitRoleTag.Economy) || Unit.Cargo <= 0)
        {
            return;
        }

        DrawCircle(new Vector2(radius * 0.72f, radius * 0.62f), 3.8f, new Color(SoftOldCityPalette.Cargo, 0.78f));
    }

    private void DrawVeterancyGlyph(float radius, int rank)
    {
        if (rank <= 0)
        {
            return;
        }

        var y = -radius - 25f;
        var startX = -(rank - 1) * 3.8f;
        for (var index = 0; index < rank; index++)
        {
            DrawCircle(new Vector2(startX + index * 7.6f, y), 2.4f, new Color(SoftOldCityPalette.InnerLight, 0.72f));
            DrawCircle(new Vector2(startX + index * 7.6f, y), 1.25f, new Color(SoftOldCityPalette.Ink, 0.55f));
        }
    }

    private void DrawHealth(float radius, Color accent, float hp, float maxHp)
    {
        var width = radius * 2;
        var y = -radius - 19;
        var health = maxHp <= 0 ? 0 : Mathf.Clamp(hp / maxHp, 0, 1);
        DrawRect(new Rect2(-width / 2, y, width, 4.5f), new Color(SoftOldCityPalette.Ink, 0.48f));
        DrawRect(new Rect2(-width / 2, y, width * health, 4.5f), new Color(accent, 0.74f));
        DrawRect(new Rect2(-width / 2, y, width, 4.5f), new Color(SoftOldCityPalette.InnerLight, 0.22f), false, 0.8f);
    }

    private void DrawCargo(float radius)
    {
        if (!Unit.Spec.RoleTags.Contains(UnitRoleTag.Economy) || Unit.Cargo <= 0)
        {
            return;
        }

        var width = radius * 1.85f;
        var y = radius + 12;
        var fullness = Mathf.Clamp(Unit.Cargo / 700f, 0, 1);
        DrawRect(new Rect2(-width / 2, y, width, 5.2f), new Color(SoftOldCityPalette.Ink, 0.44f));
        DrawRect(new Rect2(-width / 2, y, width * fullness, 5.2f), new Color(SoftOldCityPalette.Cargo, 0.80f));
    }

    private UnitRedrawSignature CaptureRedrawSignature()
    {
        var theme = VisualThemeProvider?.Invoke();
        return new UnitRedrawSignature(
            _projection?.Owner.Value ?? Unit.PlayerSlotId.Value,
            _projection?.Selected ?? Unit.Selected,
            Quantize(_projection?.Facing ?? Unit.Facing, 1000),
            Quantize(_projection?.Hp ?? Unit.Hp, 100),
            Quantize(_projection?.MaxHp ?? Unit.Spec.Stats.MaxHp, 100),
            _projection?.VeterancyRank ?? 0,
            Quantize(Unit.CommandPulse, 1000),
            Quantize(Unit.AlertPulse, 1000),
            Unit.Cargo,
            theme is null ? 0 : (int)theme.Current,
            theme is null ? 0 : (int)theme.Target,
            theme is null ? 1000 : Quantize(theme.TransitionProgress, 1000),
            theme?.Driver ?? string.Empty,
            MountFacingSignature());
    }

    private int MountFacingSignature()
    {
        var hash = new HashCode();
        foreach (var mount in Unit.WeaponMounts)
        {
            hash.Add(mount.MountId, StringComparer.Ordinal);
            hash.Add(Quantize(mount.Facing, 1000));
        }

        return hash.ToHashCode();
    }

    private static int Quantize(float value, float scale)
    {
        return Mathf.RoundToInt(value * scale);
    }

    private readonly record struct UnitRedrawSignature(
        int Owner,
        bool Selected,
        int Facing,
        int Hp,
        int MaxHp,
        int VeterancyRank,
        int CommandPulse,
        int AlertPulse,
        int Cargo,
        int ThemeCurrent,
        int ThemeTarget,
        int ThemeProgress,
        string ThemeDriver,
        int MountFacingHash)
    {
        public bool NeedsAnimatedRedraw => Selected || CommandPulse > 0 || AlertPulse > 0;
    }
}
