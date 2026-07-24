using Godot;
using ProceduralRts.Core;

const int Trials = 24;
const int MaxTicks = 1800;
const float FixedDelta = 1f / 30f;

var scenarios = new[]
{
    BattleScenario.OneVsOne(
        Name: "light parity: dog infantry vs cat basic",
        LeftSpecId: "dog.infantry",
        LeftCount: 8,
        RightSpecId: "cat.basic",
        RightCount: 8,
        Expected: ExpectedOutcome.Balanced),
    BattleScenario.OneVsOne(
        Name: "vehicle parity: dog guard tanks vs cat tanks",
        LeftSpecId: "dog.guard_tank",
        LeftCount: 4,
        RightSpecId: "cat.tank",
        RightCount: 4,
        Expected: ExpectedOutcome.Balanced),
    BattleScenario.OneVsOne(
        Name: "anti-vehicle check: rocket dogs vs cat tanks",
        LeftSpecId: "dog.rocket",
        LeftCount: 6,
        RightSpecId: "cat.tank",
        RightCount: 3,
        Expected: ExpectedOutcome.LeftShouldWin),
    BattleScenario.OneVsOne(
        Name: "anti-light screen: dog patrol vehicles vs cat basic",
        LeftSpecId: "dog.patrol_vehicle",
        LeftCount: 4,
        RightSpecId: "cat.basic",
        RightCount: 8,
        Expected: ExpectedOutcome.LeftShouldWin),
    BattleScenario.OneVsOne(
        Name: "air pressure: cat scout aircraft vs dog guard tanks",
        LeftSpecId: "cat.scout_aircraft",
        LeftCount: 8,
        RightSpecId: "dog.guard_tank",
        RightCount: 3,
        Expected: ExpectedOutcome.LeftShouldWin),
    BattleScenario.OneVsOne(
        Name: "anti-air check: dog rocket dogs vs cat scout aircraft",
        LeftSpecId: "dog.rocket",
        LeftCount: 6,
        RightSpecId: "cat.scout_aircraft",
        RightCount: 6,
        Expected: ExpectedOutcome.LeftShouldWin),
    new BattleScenario(
        Name: "mixed-force pressure: cat air-supported push vs dog ground screen",
        Left: [new UnitGroup("dog.infantry", 6), new UnitGroup("dog.rocket", 4), new UnitGroup("dog.guard_tank", 3)],
        Right: [new UnitGroup("cat.basic", 6), new UnitGroup("cat.scout_car", 2), new UnitGroup("cat.tank", 3), new UnitGroup("cat.scout_aircraft", 1)],
        Expected: ExpectedOutcome.RightShouldWin),
};

Console.WriteLine("BalanceReport");
Console.WriteLine($"Trials/scenario: {Trials}, max ticks: {MaxTicks}");
Console.WriteLine();

var failures = new List<string>();
PrintCombatChemistryCoverage(failures);
Console.WriteLine();
PrintUnitProductionCoverage(failures);
Console.WriteLine();

foreach (var scenario in scenarios)
{
    var report = RunScenario(scenario);
    PrintReport(report);
    Validate(report, failures);
}

if (failures.Count > 0)
{
    Console.Error.WriteLine("BalanceReport FAILED:");
    foreach (var failure in failures)
    {
        Console.Error.WriteLine($"- {failure}");
    }

    System.Environment.Exit(1);
}

Console.WriteLine("BalanceReport PASSED.");

void PrintCombatChemistryCoverage(List<string> failures)
{
    var ammoByElement = WeaponCatalog.AmmoDefinitions.Values
        .GroupBy(ammo => ammo.DamageElementId)
        .OrderBy(group => group.Key, StringComparer.Ordinal)
        .Select(group => $"{group.Key}={group.Count()}")
        .ToArray();
    var authoredCounterAmmo = WeaponCatalog.AmmoDefinitions.Values
        .Where(ammo => ammo.CounterRules.Rules.Count > 0)
        .OrderBy(ammo => ammo.Id, StringComparer.Ordinal)
        .Select(ammo => $"{ammo.Id}({ammo.CounterRules.Rules.Count})")
        .ToArray();
    var authoredDefenses = UnitDesignCatalog.Designs.Values
        .Select(design => design.ToSpec())
        .SelectMany(spec => DefenseEntries(spec.Id, spec.Stats.ElementDefense))
        .Concat(BuildSpecCatalog.Definitions.Values.SelectMany(spec => DefenseEntries(spec.Kind, spec.ElementDefense)))
        .OrderBy(entry => entry, StringComparer.Ordinal)
        .ToArray();
    var counterProbe = CombatProfileDesign.CounterRules(
        new CounterRule(1.35f, Trait: TargetTrait.Shielded),
        new CounterRule(1.1f, Role: UnitRoleTag.Vehicle));
    var shieldedVehicle = CombatProfileDesign.TargetTraits([TargetTrait.Shielded], [UnitRoleTag.Vehicle]);
    var resistanceProbe = CombatProfileDesign.ElementDefense(new() { [DamageElementIds.Energy] = 0.75f });
    var overload = ElementReactionCatalog.Match(ElementStatusIds.EnergyCharge, DamageElementIds.Explosive);

    Console.WriteLine("Combat chemistry coverage");
    Console.WriteLine($"  elements {DamageElementCatalog.Definitions.Count}/{DamageElementIds.All.Count}; ammo elements {string.Join(", ", ammoByElement)}");
    Console.WriteLine($"  authored counter ammo: {(authoredCounterAmmo.Length == 0 ? "none authored; QA probe covers trait/role counters" : string.Join(", ", authoredCounterAmmo))}");
    Console.WriteLine($"  non-neutral defenses: {(authoredDefenses.Length == 0 ? "none authored; QA probe covers sparse resistance" : string.Join(", ", authoredDefenses))}");
    Console.WriteLine($"  reactions {ElementReactionCatalog.Definitions.Count}/{ElementReactionIds.All.Count}; overload {(overload is null ? "missing" : overload.ReactionId)}");
    Console.WriteLine($"  presentations {ElementPresentationCatalog.Definitions.Count}/{DamageElementIds.All.Count}; badges {ElementPresentationCatalog.Definitions.Values.Select(style => style.Badge.ShortCode).Distinct(StringComparer.Ordinal).Count()} unique");

    RequireCoverage(WeaponCatalog.AmmoDefinitions.Values.All(ammo => DamageElementCatalog.Definitions.ContainsKey(ammo.DamageElementId)), "Every authored ammo entry must reference a known damage element.", failures);
    RequireCoverage(counterProbe.MultiplierFor(shieldedVehicle, UnitWeightClass.Medium, MovementDomain.Land, ArmorTag.Vehicle) > 1.4f, "Counter rule probe must cover special target-trait and role multipliers.", failures);
    RequireCoverage(Nearly(resistanceProbe.MultiplierFor(DamageElementIds.Energy), 0.75f), "Element defense probe must cover sparse resistance multipliers.", failures);
    RequireCoverage(overload?.ReactionId == ElementReactionIds.Overload, "Element reaction coverage must include EnergyCharge + Explosive -> Overload.", failures);
    RequireCoverage(ElementPresentationCatalog.Definitions.Count == DamageElementIds.All.Count, "Every damage element must have presentation metadata.", failures);
}

void PrintUnitProductionCoverage(List<string> failures)
{
    var productionDesignIds = UnitDesignCatalog.Designs.Values
        .Where(design => design.Production is not null)
        .Select(design => design.Id)
        .ToHashSet(StringComparer.Ordinal);
    var descriptors = UnitDesignDefinitionCatalog.RuntimeDescriptors.Values
        .OrderBy(descriptor => descriptor.Faction)
        .ThenBy(descriptor => descriptor.TechTier)
        .ThenBy(descriptor => descriptor.DesignId, StringComparer.Ordinal)
        .ToArray();
    var productionDescriptors = descriptors
        .Where(descriptor => productionDesignIds.Contains(descriptor.DesignId))
        .ToArray();
    var nonProductionDescriptors = descriptors
        .Where(descriptor => !productionDesignIds.Contains(descriptor.DesignId))
        .ToArray();
    var byFaction = productionDescriptors
        .GroupBy(descriptor => descriptor.Faction)
        .OrderBy(group => group.Key)
        .Select(group => $"{group.Key}={group.Count()}")
        .ToArray();
    var byCategory = productionDescriptors
        .Where(descriptor => descriptor.ProductionCategory is not null)
        .GroupBy(descriptor => descriptor.ProductionCategory!.Value)
        .OrderBy(group => group.Key)
        .Select(group => $"{group.Key}={group.Count()}")
        .ToArray();
    var byTier = productionDescriptors
        .GroupBy(descriptor => descriptor.TechTier)
        .OrderBy(group => group.Key)
        .Select(group => $"T{group.Key}={group.Count()}")
        .ToArray();

    Console.WriteLine("Unit production tuning coverage");
    Console.WriteLine($"  production units {productionDescriptors.Length}/{descriptors.Length}; by faction {string.Join(", ", byFaction)}");
    Console.WriteLine($"  by category {string.Join(", ", byCategory)}; by tier {string.Join(", ", byTier)}");
    Console.WriteLine($"  non-production units: {FormatNonProductionDescriptors(nonProductionDescriptors)}");

    foreach (var descriptor in productionDescriptors)
    {
        RequireCoverage(descriptor.Cost > 0, $"{descriptor.DesignId}: production unit must have positive cost.", failures);
        RequireCoverage(descriptor.TechTier >= 0, $"{descriptor.DesignId}: production unit must have non-negative tech tier.", failures);
        RequireCoverage(descriptor.ProductionCategory is not null, $"{descriptor.DesignId}: production category must be authored.", failures);
        RequireCoverage(descriptor.ProductionDuration is > 0, $"{descriptor.DesignId}: production duration must be positive.", failures);
        RequireCoverage(descriptor.ProductionLaneIndex is >= 0, $"{descriptor.DesignId}: production lane index must be non-negative.", failures);
        RequireCoverage(!string.IsNullOrWhiteSpace(descriptor.ProductionLaneKey), $"{descriptor.DesignId}: production lane key must be authored.", failures);
        RequireCoverage(!string.IsNullOrWhiteSpace(descriptor.ProducerKind), $"{descriptor.DesignId}: producer kind must be authored.", failures);
    }

    RequireCoverage(descriptors.Length == UnitDesignCatalog.Designs.Count, "Runtime descriptor coverage must match the discovered unit design catalog.", failures);
    RequireCoverage(productionDescriptors.Length > 0, "Unit production tuning coverage must include at least one production unit.", failures);
    RequireCoverage(nonProductionDescriptors.Length > 0, "Unit production tuning coverage must report at least one non-production unit separately.", failures);
    RequireCoverage(byFaction.Length > 0, "Unit production tuning coverage must group production units by faction.", failures);
    RequireCoverage(byCategory.Length > 0, "Unit production tuning coverage must group production units by category.", failures);
    RequireCoverage(byTier.Length > 0, "Unit production tuning coverage must group production units by tech tier.", failures);
}

string FormatNonProductionDescriptors(IReadOnlyList<UnitSpecRuntimeDescriptor> descriptors)
{
    return descriptors.Count == 0
        ? "none"
        : string.Join(", ", descriptors.Select(descriptor => $"{descriptor.DesignId}(T{descriptor.TechTier})"));
}

IEnumerable<string> DefenseEntries(string owner, ElementDefenseProfile? defense)
{
    if (defense is null)
    {
        yield break;
    }

    foreach (var pair in defense.ElementMultipliers)
    {
        if (!Nearly(pair.Value, 1f))
        {
            yield return $"{owner}:{pair.Key}={pair.Value:0.00}";
        }
    }
}

DuelReport RunScenario(BattleScenario scenario)
{
    var outcomes = new List<DuelOutcome>(Trials);
    for (var trial = 0; trial < Trials; trial++)
    {
        outcomes.Add(RunTrial(scenario, seed: (ulong)(9000 + trial)));
    }

    return new DuelReport(scenario, outcomes);
}

DuelOutcome RunTrial(BattleScenario scenario, ulong seed)
{
    var world = new EntityWorld(seed)
    {
        WorldWidth = 2200,
        WorldHeight = 1400,
    };
    world.AddSystem(new CommandSystem());
    world.AddSystem(new VisionSystem());
    world.AddSystem(new CombatSystem());
    world.AddSystem(new ProjectileSystem());
    world.AddSystem(new MovementSystem());
    world.AddSystem(new SeparationSystem());

    var left = SpawnSide(world, scenario.Left, new OwnerId(1), new Vector2(850, 700), direction: -1);
    var right = SpawnSide(world, scenario.Right, new OwnerId(2), new Vector2(1150, 700), direction: 1);

    var commands = new EntityCommandBuffer();
    // Symmetric attack-move avoids single-target manual-focus overkill from
    // dominating roster balance after damage moved to projectile impact time.
    commands.Enqueue(new AttackMoveEntityCommand(
        new OwnerId(1),
        left.Select(entity => entity.Id).ToArray(),
        Tick: 1,
        Target: new Vector2(1150, 700),
        Mode: MoveCommandMode.Attack));
    commands.Enqueue(new AttackMoveEntityCommand(
        new OwnerId(2),
        right.Select(entity => entity.Id).ToArray(),
        Tick: 1,
        Target: new Vector2(850, 700),
        Mode: MoveCommandMode.Attack));

    for (var tick = 1; tick <= MaxTicks; tick++)
    {
        var due = commands.DrainUpToTick(tick);
        world.Step(tick, FixedDelta, due);
        world.Events.Drain();

        var leftAlive = CountAlive(world, left);
        var rightAlive = CountAlive(world, right);
        // Let already-fired rounds settle before deciding the winner.
        var projectilesInFlight = world.OrderedEntities.Any(entity => entity.Components.Has<ProjectileComponentState>());
        if ((leftAlive == 0 || rightAlive == 0) && !projectilesInFlight)
        {
            return SummarizeOutcome(world, left, right, tick);
        }
    }

    return SummarizeOutcome(world, left, right, MaxTicks);
}

IReadOnlyList<EntityInstance> SpawnSide(EntityWorld world, IReadOnlyList<UnitGroup> groups, OwnerId owner, Vector2 center, int direction)
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

IReadOnlyList<EntityInstance> SpawnLine(EntityWorld world, UnitSpec spec, OwnerId owner, int count, Vector2 center, int direction)
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

DuelOutcome SummarizeOutcome(EntityWorld world, IReadOnlyList<EntityInstance> left, IReadOnlyList<EntityInstance> right, int ticks)
{
    var leftAlive = AliveEntities(world, left).ToList();
    var rightAlive = AliveEntities(world, right).ToList();
    var winner = leftAlive.Count == rightAlive.Count
        ? DuelWinner.Draw
        : leftAlive.Count > rightAlive.Count
            ? DuelWinner.Left
            : DuelWinner.Right;

    return new DuelOutcome(
        Winner: winner,
        Ticks: ticks,
        LeftAlive: leftAlive.Count,
        RightAlive: rightAlive.Count,
        LeftHp: leftAlive.Sum(CurrentHp),
        RightHp: rightAlive.Sum(CurrentHp));
}

int CountAlive(EntityWorld world, IReadOnlyList<EntityInstance> original)
{
    return AliveEntities(world, original).Count();
}

IEnumerable<EntityInstance> AliveEntities(EntityWorld world, IReadOnlyList<EntityInstance> original)
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

float CurrentHp(EntityInstance entity)
{
    return entity.Components.TryGet<HealthComponentState>(out var health) ? Math.Max(0, health.Hp) : 0;
}

void PrintReport(DuelReport report)
{
    Console.WriteLine(report.Scenario.Name);
    Console.WriteLine($"  {SideLabel(report.Scenario.Left)} vs {SideLabel(report.Scenario.Right)}");
    Console.WriteLine($"  left {report.LeftWinRate:P0}, right {report.RightWinRate:P0}, draw {report.DrawRate:P0}, avg ticks {report.AverageTicks:0}");
    Console.WriteLine($"  avg survivors L/R {report.AverageLeftAlive:0.0}/{report.AverageRightAlive:0.0}, avg HP L/R {report.AverageLeftHp:0.0}/{report.AverageRightHp:0.0}");
}

string SideLabel(IReadOnlyList<UnitGroup> groups)
{
    return string.Join(" + ", groups.Select(group => $"{group.SpecId} x{group.Count}"));
}

void Validate(DuelReport report, List<string> failures)
{
    if (report.DrawRate > 0.25f)
    {
        failures.Add($"{report.Scenario.Name}: draw rate {report.DrawRate:P0} is too high for a canonical duel.");
    }

    switch (report.Scenario.Expected)
    {
        case ExpectedOutcome.LeftShouldWin when report.LeftWinRate < 0.60f:
            failures.Add($"{report.Scenario.Name}: expected left to win at least 60%, got {report.LeftWinRate:P0}.");
            break;
        case ExpectedOutcome.RightShouldWin when report.RightWinRate < 0.60f:
            failures.Add($"{report.Scenario.Name}: expected right to win at least 60%, got {report.RightWinRate:P0}.");
            break;
        case ExpectedOutcome.Balanced when report.LeftWinRate is < 0.15f or > 0.85f:
            failures.Add($"{report.Scenario.Name}: parity matchup is outside 15%-85% win-rate band; left={report.LeftWinRate:P0}.");
            break;
    }
}

void RequireCoverage(bool condition, string message, List<string> failures)
{
    if (!condition)
    {
        failures.Add(message);
    }
}

bool Nearly(float actual, float expected)
{
    return MathF.Abs(actual - expected) < 0.001f;
}

enum ExpectedOutcome
{
    Balanced,
    LeftShouldWin,
    RightShouldWin,
}

enum DuelWinner
{
    Draw,
    Left,
    Right,
}

sealed record UnitGroup(string SpecId, int Count);

sealed record BattleScenario(
    string Name,
    IReadOnlyList<UnitGroup> Left,
    IReadOnlyList<UnitGroup> Right,
    ExpectedOutcome Expected)
{
    public static BattleScenario OneVsOne(
        string Name,
        string LeftSpecId,
        int LeftCount,
        string RightSpecId,
        int RightCount,
        ExpectedOutcome Expected)
    {
        return new BattleScenario(
            Name,
            [new UnitGroup(LeftSpecId, LeftCount)],
            [new UnitGroup(RightSpecId, RightCount)],
            Expected);
    }
}

sealed record DuelOutcome(
    DuelWinner Winner,
    int Ticks,
    int LeftAlive,
    int RightAlive,
    float LeftHp,
    float RightHp);

sealed class DuelReport
{
    public DuelReport(BattleScenario scenario, IReadOnlyList<DuelOutcome> outcomes)
    {
        Scenario = scenario;
        Outcomes = outcomes;
    }

    public BattleScenario Scenario { get; }
    public IReadOnlyList<DuelOutcome> Outcomes { get; }
    public float LeftWinRate => Outcomes.Count(outcome => outcome.Winner == DuelWinner.Left) / (float)Outcomes.Count;
    public float RightWinRate => Outcomes.Count(outcome => outcome.Winner == DuelWinner.Right) / (float)Outcomes.Count;
    public float DrawRate => Outcomes.Count(outcome => outcome.Winner == DuelWinner.Draw) / (float)Outcomes.Count;
    public float AverageTicks => (float)Outcomes.Average(outcome => outcome.Ticks);
    public float AverageLeftAlive => (float)Outcomes.Average(outcome => outcome.LeftAlive);
    public float AverageRightAlive => (float)Outcomes.Average(outcome => outcome.RightAlive);
    public float AverageLeftHp => (float)Outcomes.Average(outcome => outcome.LeftHp);
    public float AverageRightHp => (float)Outcomes.Average(outcome => outcome.RightHp);
}
