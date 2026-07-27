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
    public Func<UnitPresentationProjection?>? PresentationProvider { get; init; }
    public Func<WorldVisualThemeState>? VisualThemeProvider { get; init; }
    private UnitPresentationProjection? _presentation;

    public override void _Process(double delta)
    {
        _presentation = PresentationProvider?.Invoke();
        if (_presentation is { } presentation)
        {
            Position = presentation.Entity.Position;
        }
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
        if (_presentation is not { } presentation)
        {
            return;
        }

        var owner = presentation.Entity.Owner.ToPlayerSlot();
        var selected = presentation.Entity.Selected;
        var hp = presentation.Entity.Hp;
        var maxHp = presentation.Entity.MaxHp;
        var relation = Relations.Relation(Viewer, owner);
        var relationAccent = SoftOldCityPalette.RelationColor(relation);
        var pulse = 0.55f + Mathf.Sin((float)Time.GetTicksMsec() / 250f + Unit.Id) * 0.25f;
        var radius = Unit.Spec.Collision.Radius;

        DrawCircle(Vector2.Zero, radius + 16 + presentation.CommandPulse * 8, new Color(relationAccent, selected ? 0.10f : 0.018f));
        if (presentation.AlertPulse > 0)
        {
            var alertRadius = radius + 14 + (1 - presentation.AlertPulse) * 20;
            DrawArc(Vector2.Zero, alertRadius, 0, Mathf.Tau, OverlayArcSegments, new Color(SoftOldCityPalette.InnerLight, presentation.AlertPulse * 0.58f), 1.2f, CrispOverlayStroke);
            DrawArc(Vector2.Zero, alertRadius + 6, 0, Mathf.Tau, OverlayArcSegments, new Color(relationAccent, presentation.AlertPulse * 0.38f), 1.8f, CrispOverlayStroke);
        }

        if (selected)
        {
            DrawArc(Vector2.Zero, radius + 9 + pulse * 2, 0, Mathf.Tau, OverlayArcSegments, new Color(SoftOldCityPalette.Ink, 0.82f), 2.0f, CrispOverlayStroke);
            DrawArc(Vector2.Zero, radius + 10 + pulse * 2, 0, Mathf.Tau, OverlayArcSegments, new Color(relationAccent, 0.54f), 1.2f, CrispOverlayStroke);
            DrawArc(Vector2.Zero, radius + 18 + presentation.CommandPulse * 10, 0, Mathf.Tau, OverlayArcSegments, new Color(relationAccent, 0.36f), 1.0f, CrispOverlayStroke);
        }

        DrawStatusGlyph(radius, presentation.Cargo);
        DrawVeterancyGlyph(radius, presentation.Entity.VeterancyRank);
        DrawCargo(radius, presentation.Cargo, presentation.HarvestPulse);
        DrawHealth(radius, relationAccent, hp, maxHp);
    }

    private void DrawStatusGlyph(float radius, int cargo)
    {
        if (!Unit.Spec.RoleTags.Contains(UnitRoleTag.Economy) || cargo <= 0)
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

    private void DrawCargo(float radius, int cargo, float harvestPulse)
    {
        if (!Unit.Spec.RoleTags.Contains(UnitRoleTag.Economy) || cargo <= 0)
        {
            return;
        }

        var width = radius * 1.85f;
        var y = radius + 12;
        var fullness = Mathf.Clamp(cargo / 700f, 0, 1);
        DrawRect(new Rect2(-width / 2, y, width, 5.2f), new Color(SoftOldCityPalette.Ink, 0.44f));
        DrawRect(new Rect2(-width / 2, y, width * fullness, 5.2f), new Color(SoftOldCityPalette.Cargo, 0.80f));
        if (harvestPulse > 0)
        {
            DrawArc(Vector2.Zero, radius + 28 + harvestPulse * 8, 0, Mathf.Tau, OverlayArcSegments, new Color(SoftOldCityPalette.Cargo, harvestPulse * 0.40f), 1.5f, CrispOverlayStroke);
        }
    }

    private UnitRedrawSignature CaptureRedrawSignature()
    {
        var theme = VisualThemeProvider?.Invoke();
        return new UnitRedrawSignature(
            _presentation?.Entity.Owner.Value ?? 0,
            _presentation?.Entity.Selected ?? false,
            Quantize(_presentation?.Entity.Facing ?? 0, 1000),
            Quantize(_presentation?.Entity.Hp ?? 0, 100),
            Quantize(_presentation?.Entity.MaxHp ?? 0, 100),
            _presentation?.Entity.VeterancyRank ?? 0,
            Quantize(_presentation?.CommandPulse ?? 0, 1000),
            Quantize(_presentation?.AlertPulse ?? 0, 1000),
            Quantize(_presentation?.HarvestPulse ?? 0, 1000),
            _presentation?.Cargo ?? 0,
            theme is null ? 0 : (int)theme.Current,
            theme is null ? 0 : (int)theme.Target,
            theme is null ? 1000 : Quantize(theme.TransitionProgress, 1000),
            theme?.Driver ?? string.Empty,
            MountFacingSignature());
    }

    private int MountFacingSignature()
    {
        var hash = new HashCode();
        foreach (var mount in _presentation?.Mounts ?? Array.Empty<WeaponMountRuntimeState>())
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
        int HarvestPulse,
        int Cargo,
        int ThemeCurrent,
        int ThemeTarget,
        int ThemeProgress,
        string ThemeDriver,
        int MountFacingHash)
    {
        public bool NeedsAnimatedRedraw => Selected || CommandPulse > 0 || AlertPulse > 0 || HarvestPulse > 0;
    }
}
