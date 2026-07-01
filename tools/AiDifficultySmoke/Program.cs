using Godot;
using ProceduralRts.Core;

const double FixedDelta = 1.0 / 30.0;

var pacing = new[]
{
    RunPacingProbe(EnemyDifficulty.Easy),
    RunPacingProbe(EnemyDifficulty.Normal),
    RunPacingProbe(EnemyDifficulty.Hard),
};
foreach (var report in pacing)
{
    PrintPacing(report);
}

var waveDuel = RunWaveDuel(EnemyDifficulty.Easy, EnemyDifficulty.Hard);
PrintWaveDuel(waveDuel);

var failures = new List<string>();
AssertPacing(pacing, failures);
AssertHardWaveBeatsEasy(waveDuel, failures);

if (failures.Count > 0)
{
    Console.Error.WriteLine("AiDifficultySmoke FAILED:");
    foreach (var failure in failures)
    {
        Console.Error.WriteLine($"- {failure}");
    }

    System.Environment.Exit(1);
}

Console.WriteLine("AiDifficultySmoke PASSED.");

static PacingReport RunPacingProbe(EnemyDifficulty difficulty)
{
    var battlefield = new UnitBattlefield { WorldSize = new Vector2(3600, 2400) };
    battlefield.Relations.Set(PlayerSlotId.One, PlayerSlotId.Two, PlayerRelation.Hostile);
    battlefield.SetCredits(PlayerSlotId.Two, 4200);
    BuildBase(battlefield, PlayerSlotId.One, UnitFactionId.Dog, new Vector2(900, 1200), facing: 0, idBase: 100);
    BuildBase(battlefield, PlayerSlotId.Two, UnitFactionId.Dog, new Vector2(2550, 1200), facing: MathF.PI, idBase: 200);
    SpawnCombatGroup(battlefield, PlayerSlotId.Two, new Vector2(2320, 1200), direction: -1, count: 12);

    var profile = EnemyDifficultyProfile.For(difficulty);
    var production = new UnitBattlefieldEnemyProductionAi(profile);
    var waves = new UnitBattlefieldEnemyAttackWaveAi(profile);
    var completions = 0;
    battlefield.ProductionCompleted += (_, _, unit) =>
    {
        if (unit.PlayerSlotId == PlayerSlotId.Two)
        {
            completions++;
        }
    };

    for (var tick = 0; tick < 30 * 45; tick++)
    {
        production.Update(battlefield, PlayerSlotId.Two, FixedDelta);
        waves.Update(battlefield, PlayerSlotId.Two, FixedDelta);
        battlefield.Update(FixedDelta);
    }

    return new PacingReport(
        difficulty,
        production.SuccessfulOrders,
        completions,
        waves.WavesLaunched,
        battlefield.Units.Count(unit => unit.PlayerSlotId == PlayerSlotId.Two && unit.Hp > 0));
}

static WaveDuelReport RunWaveDuel(EnemyDifficulty leftDifficulty, EnemyDifficulty rightDifficulty)
{
    var battlefield = new UnitBattlefield { WorldSize = new Vector2(3600, 2400) };
    battlefield.Relations.Set(PlayerSlotId.One, PlayerSlotId.Two, PlayerRelation.Hostile);
    var leftHq = BuildBase(battlefield, PlayerSlotId.One, UnitFactionId.Dog, new Vector2(900, 1200), facing: 0, idBase: 100).Headquarters;
    var rightHq = BuildBase(battlefield, PlayerSlotId.Two, UnitFactionId.Dog, new Vector2(2700, 1200), facing: MathF.PI, idBase: 200).Headquarters;
    SpawnCombatGroup(
        battlefield,
        PlayerSlotId.One,
        new Vector2(1180, 1200),
        direction: 1,
        count: EnemyDifficultyProfile.For(leftDifficulty).MaximumWaveUnits);
    SpawnCombatGroup(
        battlefield,
        PlayerSlotId.Two,
        new Vector2(2420, 1200),
        direction: -1,
        count: EnemyDifficultyProfile.For(rightDifficulty).MaximumWaveUnits);

    var leftWaves = new UnitBattlefieldEnemyAttackWaveAi(ImmediateWaveProfile(EnemyDifficultyProfile.For(leftDifficulty)));
    var rightWaves = new UnitBattlefieldEnemyAttackWaveAi(ImmediateWaveProfile(EnemyDifficultyProfile.For(rightDifficulty)));
    for (var tick = 0; tick < 30 * 105 && battlefield.Outcome == GameOutcome.InProgress; tick++)
    {
        leftWaves.Update(battlefield, PlayerSlotId.One, FixedDelta);
        rightWaves.Update(battlefield, PlayerSlotId.Two, FixedDelta);
        battlefield.Update(FixedDelta);
    }

    return new WaveDuelReport(
        leftDifficulty,
        rightDifficulty,
        leftWaves.WavesLaunched,
        rightWaves.WavesLaunched,
        battlefield.Units.Count(unit => unit.PlayerSlotId == PlayerSlotId.One && unit.Hp > 0),
        battlefield.Units.Count(unit => unit.PlayerSlotId == PlayerSlotId.Two && unit.Hp > 0),
        Math.Max(0, battlefield.BuildingSnapshot(leftHq.Id)?.Hp ?? 0),
        Math.Max(0, battlefield.BuildingSnapshot(rightHq.Id)?.Hp ?? 0));
}

static BaseRuntime BuildBase(UnitBattlefield battlefield, PlayerSlotId slot, UnitFactionId faction, Vector2 center, float facing, int idBase)
{
    var hq = battlefield.UpsertBuildingTarget(idBase, BuildingDesignIds.Headquarters, slot, faction, center, facing, BuildSpecCatalog.For(BuildingDesignIds.Headquarters).MaxHp);
    battlefield.UpsertBuildingTarget(idBase + 1, BuildingDesignIds.Barracks, slot, faction, center + new Vector2(0, -190), facing, BuildSpecCatalog.For(BuildingDesignIds.Barracks).MaxHp);
    battlefield.UpsertBuildingTarget(idBase + 2, BuildingDesignIds.VehicleFactory, slot, faction, center + new Vector2(0, 190), facing, BuildSpecCatalog.For(BuildingDesignIds.VehicleFactory).MaxHp);
    return new BaseRuntime(hq);
}

static void SpawnCombatGroup(UnitBattlefield battlefield, PlayerSlotId slot, Vector2 center, int direction, int count)
{
    var pattern = new[] { "dog.guard_tank", "dog.guard_tank", "dog.patrol_vehicle", "dog.infantry", "dog.infantry", "dog.rocket" };
    for (var index = 0; index < count; index++)
    {
        var row = index / 4;
        var column = index % 4;
        var position = center + new Vector2(direction * row * 62f, (column - 1.5f) * 52f);
        var facing = direction > 0 ? 0 : MathF.PI;
        battlefield.Spawn(pattern[index % pattern.Length], slot, position, facing);
    }
}

static EnemyDifficultyProfile ImmediateWaveProfile(EnemyDifficultyProfile profile)
{
    return profile with
    {
        AttackInitialDelay = 0.1f,
        AttackWaveInterval = 999f,
        AggressionRadius = float.PositiveInfinity,
    };
}

static void AssertPacing(IReadOnlyList<PacingReport> reports, List<string> failures)
{
    var easy = reports.First(report => report.Difficulty == EnemyDifficulty.Easy);
    var normal = reports.First(report => report.Difficulty == EnemyDifficulty.Normal);
    var hard = reports.First(report => report.Difficulty == EnemyDifficulty.Hard);

    if (easy.ProductionOrders >= normal.ProductionOrders || normal.ProductionOrders > hard.ProductionOrders)
    {
        failures.Add($"production pacing should scale Easy < Normal <= Hard, got {easy.ProductionOrders}/{normal.ProductionOrders}/{hard.ProductionOrders}.");
    }

    if (easy.WavesLaunched >= normal.WavesLaunched || normal.WavesLaunched > hard.WavesLaunched)
    {
        failures.Add($"wave pacing should scale Easy < Normal <= Hard, got {easy.WavesLaunched}/{normal.WavesLaunched}/{hard.WavesLaunched}.");
    }

    if (hard.UnitsAlive < normal.UnitsAlive)
    {
        failures.Add($"hard should not trail normal in active units during pacing probe ({hard.UnitsAlive} < {normal.UnitsAlive}).");
    }
}

static void AssertHardWaveBeatsEasy(WaveDuelReport report, List<string> failures)
{
    if (report.RightDifficulty != EnemyDifficulty.Hard)
    {
        failures.Add("wave duel expects Hard on the right side.");
        return;
    }

    var hqMaxHp = BuildSpecCatalog.For(BuildingDesignIds.Headquarters).MaxHp;
    var hardDamage = hqMaxHp - report.LeftHqHp;
    var easyDamage = hqMaxHp - report.RightHqHp;
    if (hardDamage < easyDamage)
    {
        failures.Add($"Hard wave AI should not deal less HQ pressure than Easy ({hardDamage:0} < {easyDamage:0}).");
    }

    if (report.RightUnitsAlive <= report.LeftUnitsAlive)
    {
        failures.Add($"Hard wave AI should keep more survivors than Easy ({report.RightUnitsAlive} <= {report.LeftUnitsAlive}).");
    }
}

static void PrintPacing(PacingReport report)
{
    Console.WriteLine($"{report.Difficulty} pacing: production {report.ProductionOrders}, completions {report.ProductionCompletions}, waves {report.WavesLaunched}, units {report.UnitsAlive}");
}

static void PrintWaveDuel(WaveDuelReport report)
{
    Console.WriteLine($"{report.LeftDifficulty} wave duel vs {report.RightDifficulty}");
    Console.WriteLine($"  waves L/R {report.LeftWaves}/{report.RightWaves}, units L/R {report.LeftUnitsAlive}/{report.RightUnitsAlive}, HQ HP L/R {report.LeftHqHp:0}/{report.RightHqHp:0}");
}

sealed record BaseRuntime(UnitBattlefieldBuildingSnapshot Headquarters);

sealed record PacingReport(
    EnemyDifficulty Difficulty,
    int ProductionOrders,
    int ProductionCompletions,
    int WavesLaunched,
    int UnitsAlive);

sealed record WaveDuelReport(
    EnemyDifficulty LeftDifficulty,
    EnemyDifficulty RightDifficulty,
    int LeftWaves,
    int RightWaves,
    int LeftUnitsAlive,
    int RightUnitsAlive,
    float LeftHqHp,
    float RightHqHp);
