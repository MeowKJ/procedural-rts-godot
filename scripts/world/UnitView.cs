using Godot;
using ProceduralRts.Core;
using CoreOwner = ProceduralRts.Core.Owner;

namespace ProceduralRts.World;

public partial class UnitView : Node2D
{
    private const float RedrawIntervalSeconds = 1f / 30f;
    private const int OverlayArcSegments = 40;
    private const bool CrispOverlayStroke = false;
    private float _redrawTimer;
    private readonly Dictionary<string, float> _mountFacings = [];

    public required GameState State { get; init; }
    public required UnitModel Unit { get; init; }

    public override void _Process(double delta)
    {
        Position = Unit.Position;
        _redrawTimer -= (float)delta;
        if (_redrawTimer <= 0)
        {
            _redrawTimer = RedrawIntervalSeconds;
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        if (!State.IsVisibleToPlayer(Unit))
        {
            return;
        }

        if (!TryResolveUnitSpecStyle(out var style))
        {
            return;
        }

        var relationAccent = State.RelationOverlay(Unit.Owner, Unit.FactionId);
        var ink = new Color("#1f1b17", 0.76f);
        var paper = new Color("#fff0d2", 0.42f);
        var pulse = 0.55f + Mathf.Sin((float)Time.GetTicksMsec() / 250f + Unit.Id) * 0.25f;

        DrawCircle(Vector2.Zero, style.Radius + 16 + Unit.CommandPulse * 8, new Color(relationAccent, Unit.Selected ? 0.10f : 0.018f));
        if (Unit.AlertPulse > 0)
        {
            var alertRadius = style.Radius + 14 + (1 - Unit.AlertPulse) * 20;
            DrawArc(Vector2.Zero, alertRadius, 0, Mathf.Tau, OverlayArcSegments, new Color(paper, Unit.AlertPulse * 0.58f), 1.2f, CrispOverlayStroke);
            DrawArc(Vector2.Zero, alertRadius + 6, 0, Mathf.Tau, OverlayArcSegments, new Color(relationAccent, Unit.AlertPulse * 0.38f), 1.8f, CrispOverlayStroke);
        }

        if (Unit.Selected)
        {
            DrawArc(Vector2.Zero, style.Radius + 9 + pulse * 2, 0, Mathf.Tau, OverlayArcSegments, ink, 2.0f, CrispOverlayStroke);
            DrawArc(Vector2.Zero, style.Radius + 10 + pulse * 2, 0, Mathf.Tau, OverlayArcSegments, new Color(relationAccent, 0.54f), 1.2f, CrispOverlayStroke);
            DrawArc(Vector2.Zero, style.Radius + 18 + Unit.CommandPulse * 10, 0, Mathf.Tau, OverlayArcSegments, new Color(relationAccent, 0.36f), 1.0f, CrispOverlayStroke);
        }

        UnitVisualRenderer.DrawUnitArtRecipe(
            this,
            style.Art,
            style.Palette,
            Vector2.Zero,
            1,
            Unit.Facing,
            MountFacingsFor(style.Spec),
            EnvironmentTonePalette.For(State.VisualTheme));
        DrawStatusGlyph(style);
        DrawCargo(style);
        DrawHealth(style, relationAccent);
    }

    private bool TryResolveUnitSpecStyle(out UnitViewSpecStyle style)
    {
        var spec = Unit.Spec;
        var descriptor = Unit.RuntimeDescriptor;
        if (descriptor.DesignId != spec.Id)
        {
            style = default;
            return false;
        }

        var ownerColor = SoftOldCityPalette.PlayerColor(PlayerSlotForOwner(Unit.Owner));
        style = new UnitViewSpecStyle(
            spec,
            descriptor.Radius,
            descriptor.MaxHp,
            UnitPresentationCatalog.ForSpec(spec).Art,
            spec.RoleTags,
            EntityRenderPalette.SoftOldCity(ownerColor, descriptor.Accent));
        return true;
    }

    private IReadOnlyDictionary<string, float> MountFacingsFor(UnitSpec spec)
    {
        _mountFacings.Clear();
        foreach (var mount in spec.Weapons)
        {
            _mountFacings[mount.MountId] = mount.FacingMode == WeaponMountFacingMode.BodyFixed
                ? Unit.Facing
                : Unit.TurretFacing;
        }

        return _mountFacings;
    }

    private void DrawStatusGlyph(UnitViewSpecStyle style)
    {
        if (!style.RoleTags.Contains(UnitRoleTag.Economy) || Unit.Cargo <= 0)
        {
            return;
        }

        DrawCircle(new Vector2(style.Radius * 0.72f, style.Radius * 0.62f), 3.8f, new Color("#b98232", 0.78f));
    }

    private void DrawHealth(UnitViewSpecStyle style, Color accent)
    {
        var width = style.Radius * 2;
        var y = -style.Radius - 19;
        var health = style.MaxHp <= 0 ? 0 : Mathf.Clamp(Unit.Hp / style.MaxHp, 0, 1);
        DrawRect(new Rect2(-width / 2, y, width, 4.5f), new Color("#1f1b17", 0.48f));
        DrawRect(new Rect2(-width / 2, y, width * health, 4.5f), new Color(accent, 0.74f));
        DrawRect(new Rect2(-width / 2, y, width, 4.5f), new Color("#fff0d2", 0.22f), false, 0.8f);
    }

    private void DrawCargo(UnitViewSpecStyle style)
    {
        if (!style.RoleTags.Contains(UnitRoleTag.Economy) || Unit.Cargo <= 0)
        {
            return;
        }

        var width = style.Radius * 1.85f;
        var y = style.Radius + 12;
        var fullness = Mathf.Clamp((float)Unit.Cargo / GameState.HarvesterCargoCapacity, 0, 1);
        DrawRect(new Rect2(-width / 2, y, width, 5.2f), new Color("#1f1b17", 0.44f));
        DrawRect(new Rect2(-width / 2, y, width * fullness, 5.2f), new Color("#b98232", 0.80f));

        if (Unit.HarvestPulse > 0)
        {
            DrawArc(Vector2.Zero, style.Radius + 28 + Unit.HarvestPulse * 8, 0, Mathf.Tau, OverlayArcSegments, new Color("#b98232", Unit.HarvestPulse * 0.40f), 1.5f, CrispOverlayStroke);
        }
    }

    private static PlayerSlotId PlayerSlotForOwner(CoreOwner owner)
    {
        return owner == CoreOwner.Player ? PlayerSlotId.One : PlayerSlotId.Two;
    }

    private readonly record struct UnitViewSpecStyle(
        UnitSpec Spec,
        float Radius,
        float MaxHp,
        UnitArtRecipe Art,
        IReadOnlySet<UnitRoleTag> RoleTags,
        EntityRenderPalette Palette);
}
