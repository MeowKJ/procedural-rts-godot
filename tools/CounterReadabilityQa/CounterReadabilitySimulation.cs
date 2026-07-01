using Godot;
using ProceduralRts.Core;

internal static class CounterReadabilitySimulation
{
    public const int MaxTicks = 30 * 60;
    private const float FixedDelta = 1f / 30f;

    public static BattleOutcome RunUnitDuel(string name, IReadOnlyList<UnitGroup> leftGroups, IReadOnlyList<UnitGroup> rightGroups)
    {
        var world = CounterReadabilityWorldSetup.CreateCombatWorld(seed: StableSeed(name));
        var left = CounterReadabilityWorldSetup.SpawnSide(world, leftGroups, new OwnerId(1), new Vector2(820, 700), direction: -1);
        var right = CounterReadabilityWorldSetup.SpawnSide(world, rightGroups, new OwnerId(2), new Vector2(1180, 700), direction: 1);

        var commands = new EntityCommandBuffer();
        commands.Enqueue(new GroupAttackEntityCommand(
            new OwnerId(1),
            left.Select(entity => entity.Id).ToArray(),
            Tick: 1,
            Target: right[0].Id,
            TargetKind: CombatTargetKind.Unit));
        commands.Enqueue(new GroupAttackEntityCommand(
            new OwnerId(2),
            right.Select(entity => entity.Id).ToArray(),
            Tick: 1,
            Target: left[0].Id,
            TargetKind: CombatTargetKind.Unit));

        return RunBattle(name, world, left, right, commands);
    }

    public static BattleOutcome RunUnitsVsBuilding(string name, IReadOnlyList<UnitGroup> attackers, string targetKind)
    {
        var world = CounterReadabilityWorldSetup.CreateCombatWorld(seed: StableSeed(name));
        var left = CounterReadabilityWorldSetup.SpawnSide(world, attackers, new OwnerId(1), new Vector2(780, 700), direction: -1);
        var target = CounterReadabilityWorldSetup.SpawnBuilding(world, BuildSpecCatalog.For(targetKind), new OwnerId(2), new Vector2(1180, 700), facing: MathF.PI);
        var right = new[] { target };

        var commands = new EntityCommandBuffer();
        commands.Enqueue(new GroupAttackEntityCommand(
            new OwnerId(1),
            left.Select(entity => entity.Id).ToArray(),
            Tick: 1,
            Target: target.Id,
            TargetKind: CombatTargetKind.Building));

        return RunBattle(name, world, left, right, commands);
    }

    public static BattleOutcome RunTurretVsAircraft()
    {
        const string name = "AA turret pressure: Skyguard turret shoots down aircraft";
        var world = CounterReadabilityWorldSetup.CreateCombatWorld(seed: StableSeed(name));
        var turret = CounterReadabilityWorldSetup.SpawnBuilding(world, BuildSpecCatalog.For(BuildingDesignIds.AntiAirTurret), new OwnerId(1), new Vector2(920, 700), facing: 0);
        var left = new[] { turret };
        var right = CounterReadabilityWorldSetup.SpawnSide(world, [new UnitGroup("cat.scout_aircraft", 4)], new OwnerId(2), new Vector2(1220, 700), direction: 1);

        var commands = new EntityCommandBuffer();
        commands.Enqueue(new GroupAttackEntityCommand(
            new OwnerId(2),
            right.Select(entity => entity.Id).ToArray(),
            Tick: 1,
            Target: turret.Id,
            TargetKind: CombatTargetKind.Building));

        return RunBattle(name, world, left, right, commands);
    }

    private static BattleOutcome RunBattle(
        string name,
        EntityWorld world,
        IReadOnlyList<EntityInstance> left,
        IReadOnlyList<EntityInstance> right,
        EntityCommandBuffer commands)
    {
        var lastEvents = new List<SimEvent>();
        for (var tick = 1; tick <= MaxTicks; tick++)
        {
            world.Step(tick, FixedDelta, commands.DrainUpToTick(tick));
            lastEvents.Clear();
            lastEvents.AddRange(world.Events.Drain());

            var leftAlive = CountAlive(world, left);
            var rightAlive = CountAlive(world, right);
            if (leftAlive == 0 || rightAlive == 0)
            {
                return SummarizeOutcome(name, world, left, right, tick);
            }
        }

        return SummarizeOutcome(name, world, left, right, MaxTicks);
    }

    private static BattleOutcome SummarizeOutcome(
        string name,
        EntityWorld world,
        IReadOnlyList<EntityInstance> left,
        IReadOnlyList<EntityInstance> right,
        int ticks)
    {
        var leftAlive = AliveEntities(world, left).ToList();
        var rightAlive = AliveEntities(world, right).ToList();
        var winner = leftAlive.Count == rightAlive.Count
            ? DuelWinner.Draw
            : leftAlive.Count > rightAlive.Count
                ? DuelWinner.Left
                : DuelWinner.Right;

        return new BattleOutcome(
            Name: name,
            Winner: winner,
            Ticks: ticks,
            LeftAlive: leftAlive.Count,
            RightAlive: rightAlive.Count,
            LeftHp: leftAlive.Sum(CurrentHp),
            RightHp: rightAlive.Sum(CurrentHp));
    }

    private static int CountAlive(EntityWorld world, IReadOnlyList<EntityInstance> original)
    {
        return AliveEntities(world, original).Count();
    }

    private static IEnumerable<EntityInstance> AliveEntities(EntityWorld world, IReadOnlyList<EntityInstance> original)
    {
        foreach (var entity in original)
        {
            if (world.TryGet(entity.Id, out var current)
                && current.Components.TryGet<HealthComponentState>(out var health)
                && health.Hp > 0)
            {
                yield return current;
            }
        }
    }

    private static float CurrentHp(EntityInstance entity)
    {
        return entity.Components.TryGet<HealthComponentState>(out var health) ? Math.Max(0, health.Hp) : 0;
    }

    private static ulong StableSeed(string value)
    {
        const ulong offset = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;
        var hash = offset;
        foreach (var ch in value)
        {
            hash ^= ch;
            hash *= prime;
        }

        return hash;
    }
}
