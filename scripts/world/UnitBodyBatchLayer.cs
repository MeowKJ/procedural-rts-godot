using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.World;

public partial class UnitBodyBatchLayer : Node2D
{
    private const float RedrawIntervalSeconds = 1f / 30f;
    private float _redrawTimer;

    public required IReadOnlyList<UnitInstance> Units { get; init; }
    public required PlayerSlotId Viewer { get; init; }
    public required PlayerRelationTable Relations { get; init; }
    public Func<int, UnitPresentationProjection?>? PresentationProvider { get; init; }
    public Func<WorldVisualThemeState>? VisualThemeProvider { get; init; }
    public Rect2 CullingWorldRect { get; set; } = new(new Vector2(-1_000_000, -1_000_000), new Vector2(2_000_000, 2_000_000));

    public override void _Process(double delta)
    {
        if (HasVisibleMovingUnit())
        {
            _redrawTimer = RedrawIntervalSeconds;
            QueueRedraw();
            return;
        }

        _redrawTimer -= (float)delta;
        if (_redrawTimer > 0)
        {
            return;
        }

        _redrawTimer = RedrawIntervalSeconds;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var environmentTone = EnvironmentTonePalette.For(VisualThemeProvider?.Invoke());
        foreach (var unit in Units)
        {
            var presentation = PresentationProvider?.Invoke(unit.Id);
            if (presentation is not { } current
                || current.Entity.Hp <= 0
                || !CullingWorldRect.Intersects(UnitWorldRect(current.Entity.Position, unit.Spec.Collision.Radius)))
            {
                continue;
            }

            var owner = current.Entity.Owner.ToPlayerSlot();
            var palette = EntityRenderPalette.SoftOldCity(SoftOldCityPalette.PlayerColor(owner));
            UnitVisualRenderer.DrawUnitArtRecipe(
                this,
                unit.Spec.Art,
                palette,
                current.Entity.Position,
                1,
                current.Entity.Facing,
                UnitMountFacingSource.FromRuntimeMounts(current.Mounts),
                environmentTone);
        }
    }

    private bool HasVisibleMovingUnit()
    {
        foreach (var unit in Units)
        {
            var presentation = PresentationProvider?.Invoke(unit.Id);
            if (presentation is not { } current
                || current.Entity.Hp <= 0
                || !CullingWorldRect.Intersects(UnitWorldRect(current.Entity.Position, unit.Spec.Collision.Radius))
                || !current.IsMoving)
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private static Rect2 UnitWorldRect(Vector2 position, float radius)
    {
        return new Rect2(position - Vector2.One * radius, Vector2.One * radius * 2f);
    }
}
