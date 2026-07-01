using Godot;

namespace ProceduralRts.Core;

/// <summary>
/// An immutable, render-ready snapshot of one entity. The Presentation layer
/// reads these and never touches <see cref="EntityInstance"/> or component state
/// directly — this is the one-way Simulation -> View projection boundary
/// (docs/EntityFrameworkArchitecture.md "Godot 边界").
/// </summary>
public readonly record struct EntityProjection(
    EntityId Id,
    string SpecId,
    EntityKind Kind,
    OwnerId Owner,
    Vector2 Position,
    float Facing,
    float Hp,
    float MaxHp,
    bool Selected,
    int VeterancyRank = 0,
    int VeterancyKills = 0)
{
    public float HealthFraction => MaxHp <= 0 ? 0 : Mathf.Clamp(Hp / MaxHp, 0, 1);
    public bool IsAlive => Hp > 0;
}

/// <summary>
/// Builds stable, allocation-friendly projections from an <see cref="EntityWorld"/>.
/// Snapshots are ordered by EntityId so consumers (views, minimap, HUD) see a
/// deterministic sequence frame to frame.
/// </summary>
public static class EntityProjector
{
    public static IReadOnlyList<EntityProjection> Project(EntityWorld world)
    {
        var result = new List<EntityProjection>(world.Count);
        foreach (var entity in world.OrderedEntities)
        {
            result.Add(ProjectOne(world, entity));
        }

        return result;
    }

    public static EntityProjection ProjectOne(EntityWorld world, EntityInstance entity)
    {
        var hp = 0f;
        var maxHp = 0f;
        if (entity.Components.TryGet<HealthComponentState>(out var health))
        {
            hp = health.Hp;
            maxHp = health.MaxHp;
        }

        var selected = entity.Components.TryGet<SelectableComponentState>(out var sel) && sel.Selected;
        var veterancyRank = 0;
        var veterancyKills = 0;
        if (entity.Components.TryGet<VeterancyComponentState>(out var veterancy))
        {
            veterancyRank = veterancy.Rank;
            veterancyKills = veterancy.Kills;
        }

        var kind = world.TryGetSpec(entity.SpecId, out var spec) ? spec.Kind : EntityKind.Unit;

        return new EntityProjection(
            entity.Id,
            entity.SpecId,
            kind,
            entity.OwnerId,
            entity.Transform.Position,
            entity.Transform.Facing,
            hp,
            maxHp,
            selected,
            veterancyRank,
            veterancyKills);
    }
}
