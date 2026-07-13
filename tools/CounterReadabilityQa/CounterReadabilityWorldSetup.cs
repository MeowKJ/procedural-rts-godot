using Godot;
using ProceduralRts.Core;

internal static class CounterReadabilityWorldSetup
{
    public static EntityWorld CreateCombatWorld(ulong seed)
    {
        var world = new EntityWorld(seed)
        {
            WorldWidth = 2200,
            WorldHeight = 1400,
        };
        world.Relations.Set(new OwnerId(1), new OwnerId(2), PlayerRelation.Hostile);
        world.AddSystem(new CommandSystem());
        world.AddSystem(new VisionSystem());
        world.AddSystem(new TurretCombatSystem());
        world.AddSystem(new ProjectileSystem());
        world.AddSystem(new CombatSystem());
        world.AddSystem(new ProjectileSystem());
        world.AddSystem(new MovementSystem());
        world.AddSystem(new SeparationSystem());
        return world;
    }

    public static IReadOnlyList<EntityInstance> SpawnSide(
        EntityWorld world,
        IReadOnlyList<UnitGroup> groups,
        OwnerId owner,
        Vector2 center,
        int direction)
    {
        var spawned = new List<EntityInstance>(groups.Sum(group => group.Count));
        var laneOffset = -(groups.Count - 1) * 42f;
        foreach (var group in groups)
        {
            var spec = UnitDesignCatalog.Spec(group.SpecId);
            spawned.AddRange(SpawnLine(world, spec, owner, group.Count, center + new Vector2(0, laneOffset), direction));
            laneOffset += 84f;
        }

        return spawned;
    }

    public static EntityInstance SpawnBuilding(EntityWorld world, BuildSpec spec, OwnerId owner, Vector2 position, float facing)
    {
        var logicalFootprint = spec.LogicalFootprint(facing);
        var radius = MathF.Max(logicalFootprint.X, logicalFootprint.Y) * 0.5f;
        var components = new List<EntityComponentState>
        {
            new HealthComponentState(spec.MaxHp, spec.MaxHp),
            new SelectableComponentState(),
            new VisionComponentState(spec.SightRange),
            new CollisionComponentState(radius, 8, 100, BlocksMovement: true),
            new FootprintComponentState(logicalFootprint, spec.PlacementDomain),
            new ConstructionComponentState(1, spec.BuildTime, spec.Cost, spec.RefundRatio),
            new PowerComponentState(spec.PowerProvided, spec.PowerUsed, Powered: true),
            new PresentationPulseComponentState(),
        };

        if (spec.WeaponKind is { } weaponKind)
        {
            components.Add(new WeaponUserComponentState(new[] { new WeaponMountRuntimeState("main", weaponKind, facing, 0) }));
        }

        return world.Spawn(spec.ToEntitySpec(), owner, EntityTransform.At(position, facing), components);
    }

    private static IReadOnlyList<EntityInstance> SpawnLine(
        EntityWorld world,
        UnitSpec spec,
        OwnerId owner,
        int count,
        Vector2 center,
        int direction)
    {
        var spawned = new List<EntityInstance>(count);
        var columns = Math.Min(count, 4);
        var spacing = Math.Max(spec.Collision.Radius * 2.8f, 46f);
        for (var index = 0; index < count; index++)
        {
            var column = index % columns;
            var row = index / columns;
            var offset = new Vector2(
                direction * row * spacing,
                (column - (columns - 1) * 0.5f) * spacing);
            var facing = direction < 0 ? 0 : MathF.PI;
            spawned.Add(world.SpawnUnit(spec, owner, center + offset, facing));
        }

        return spawned;
    }
}
