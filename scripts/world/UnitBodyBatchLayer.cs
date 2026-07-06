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
    public Func<int, EntityProjection?>? ProjectionProvider { get; init; }
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
            if (unit.Hp <= 0 || !CullingWorldRect.Intersects(UnitWorldRect(unit.Position, unit.Spec.Collision.Radius)))
            {
                continue;
            }

            var projection = ProjectionProvider?.Invoke(unit.Id);
            var hp = projection?.Hp ?? unit.Hp;
            if (hp <= 0)
            {
                continue;
            }

            var owner = projection?.Owner.ToPlayerSlot() ?? unit.PlayerSlotId;
            var palette = EntityRenderPalette.SoftOldCity(SoftOldCityPalette.PlayerColor(owner));
            UnitVisualRenderer.DrawUnitArtRecipe(
                this,
                unit.Spec.Art,
                palette,
                projection?.Position ?? unit.Position,
                1,
                projection?.Facing ?? unit.Facing,
                UnitMountFacingSource.FromRuntimeMounts(unit.WeaponMounts),
                environmentTone);
        }
    }

    private bool HasVisibleMovingUnit()
    {
        foreach (var unit in Units)
        {
            if (unit.Hp <= 0
                || !CullingWorldRect.Intersects(UnitWorldRect(unit.Position, unit.Spec.Collision.Radius))
                || !IsMoving(unit))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private static bool IsMoving(UnitInstance unit)
    {
        return unit.Velocity.LengthSquared() > 0.01f
            || (unit.MoveTarget is { } target && unit.Position.DistanceSquaredTo(target) > 1f);
    }

    private static Rect2 UnitWorldRect(Vector2 position, float radius)
    {
        return new Rect2(position - Vector2.One * radius, Vector2.One * radius * 2f);
    }
}
