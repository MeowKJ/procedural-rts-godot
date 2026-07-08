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

var scoutPolicy = RunScoutPolicyProbe();
PrintScoutPolicy(scoutPolicy);

var defensePolicy = RunDefensePolicyProbe();
PrintDefensePolicy(defensePolicy);

var failures = new List<string>();
AssertPacing(pacing, failures);
AssertHardWaveBeatsEasy(waveDuel, failures);
AssertEasyKeepsScoutingLimited(scoutPolicy, defensePolicy, failures);

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

static ScoutPolicyReport RunScoutPolicyProbe()
{
    return new ScoutPolicyReport(
        ProbeScoutPolicy(EnemyDifficulty.Easy),
        ProbeScoutPolicy(EnemyDifficulty.Normal));
}

static DefensePolicyReport RunDefensePolicyProbe()
{
    return new DefensePolicyReport(
        ProbeDefensePolicy(EnemyDifficulty.Easy, threatOffsetFromBase: 760f),
        ProbeDefensePolicy(EnemyDifficulty.Normal, threatOffsetFromBase: 760f),
        ProbeDefensePolicy(EnemyDifficulty.Easy, threatOffsetFromBase: 500f));
}

static ScoutProbe ProbeScoutPolicy(EnemyDifficulty difficulty)
{
    var battlefield = new UnitBattlefield { WorldSize = new Vector2(3600, 2400) };
    battlefield.Relations.Set(PlayerSlotId.One, PlayerSlotId.Two, PlayerRelation.Hostile);
    BuildBase(battlefield, PlayerSlotId.Two, UnitFactionId.Dog, new Vector2(2700, 1200), facing: MathF.PI, idBase: 200);
    SpawnCombatGroup(
        battlefield,
        PlayerSlotId.Two,
        new Vector2(2460, 1200),
        direction: -1,
        count: EnemyDifficultyProfile.For(difficulty).MinimumWaveUnits + 2);

    var waves = new UnitBattlefieldEnemyAttackWaveAi(ImmediateWaveProfile(EnemyDifficultyProfile.For(difficulty)));
    waves.Update(battlefield, PlayerSlotId.Two, 0.2);
    var attackMoveOrders = battlefield.Units.Count(unit =>
        unit.PlayerSlotId == PlayerSlotId.Two
        && unit.MoveMode == MoveCommandMode.Attack
        && unit.MoveTarget is not null);
    return new ScoutProbe(difficulty, waves.WavesLaunched, attackMoveOrders, waves.LastStatus);
}

static DefenseProbe ProbeDefensePolicy(EnemyDifficulty difficulty, float threatOffsetFromBase)
{
    var battlefield = new UnitBattlefield { WorldSize = new Vector2(3600, 2400) };
    battlefield.Relations.Set(PlayerSlotId.One, PlayerSlotId.Two, PlayerRelation.Hostile);
    var baseCenter = new Vector2(2700, 1200);
    BuildBase(battlefield, PlayerSlotId.Two, UnitFactionId.Dog, baseCenter, facing: MathF.PI, idBase: 200);
    SpawnCombatGroup(
        battlefield,
        PlayerSlotId.Two,
        baseCenter + new Vector2(-420, 0),
        direction: -1,
        count: 8);
    var threat = battlefield.Spawn("dog.infantry", PlayerSlotId.One, baseCenter + new Vector2(-threatOffsetFromBase, 0), facing: 0);

    var profile = EnemyDifficultyProfile.For(difficulty) with { AttackInitialDelay = 999f };
    var waves = new UnitBattlefieldEnemyAttackWaveAi(profile);
    waves.Update(battlefield, PlayerSlotId.Two, 0.2);
    var assignedDefenders = battlefield.Units.Count(unit =>
        unit.PlayerSlotId == PlayerSlotId.Two
        && unit.AttackTargetIsManual
        && unit.AttackTargetKind == CombatTargetKind.Unit
        && unit.AttackTargetId == threat.Id);
    return new DefenseProbe(difficulty, threatOffsetFromBase, waves.DefenseOrders, assignedDefenders, waves.LastStatus);
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

static void AssertEasyKeepsScoutingLimited(ScoutPolicyReport scoutPolicy, DefensePolicyReport defensePolicy, List<string> failures)
{
    if (EnemyDifficultyProfile.Easy.ScoutWavesEnabled)
    {
        failures.Add("easy difficulty should disable blind scout waves.");
    }

    if (EnemyDifficultyProfile.Easy.DefenseRadius >= EnemyDifficultyProfile.Normal.DefenseRadius
        || EnemyDifficultyProfile.Easy.MaximumDefenseUnits >= EnemyDifficultyProfile.Normal.MaximumDefenseUnits)
    {
        failures.Add("easy difficulty should keep a smaller defense radius and fewer defenders than normal.");
    }

    if (scoutPolicy.Easy.WavesLaunched != 0 || scoutPolicy.Easy.AttackMoveOrders != 0)
    {
        failures.Add($"easy should hold blind scouting, got waves/orders {scoutPolicy.Easy.WavesLaunched}/{scoutPolicy.Easy.AttackMoveOrders}.");
    }

    if (scoutPolicy.Normal.WavesLaunched == 0 || scoutPolicy.Normal.AttackMoveOrders == 0)
    {
        failures.Add($"normal should still scout when no target is visible, got waves/orders {scoutPolicy.Normal.WavesLaunched}/{scoutPolicy.Normal.AttackMoveOrders}.");
    }

    if (defensePolicy.EasyOuter.DefenseOrders != 0 || defensePolicy.EasyOuter.AssignedDefenders != 0)
    {
        failures.Add($"easy should ignore defense threats outside its smaller radius, got orders/defenders {defensePolicy.EasyOuter.DefenseOrders}/{defensePolicy.EasyOuter.AssignedDefenders}.");
    }

    if (defensePolicy.NormalOuter.DefenseOrders == 0 || defensePolicy.NormalOuter.AssignedDefenders == 0)
    {
        failures.Add($"normal should answer the same outer defense threat, got orders/defenders {defensePolicy.NormalOuter.DefenseOrders}/{defensePolicy.NormalOuter.AssignedDefenders}.");
    }

    if (defensePolicy.EasyInner.AssignedDefenders > EnemyDifficultyProfile.Easy.MaximumDefenseUnits)
    {
        failures.Add($"easy should cap defenders at {EnemyDifficultyProfile.Easy.MaximumDefenseUnits}, got {defensePolicy.EasyInner.AssignedDefenders}.");
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

static void PrintScoutPolicy(ScoutPolicyReport report)
{
    Console.WriteLine("scout policy:");
    Console.WriteLine($"  {report.Easy.Difficulty}: waves {report.Easy.WavesLaunched}, attack-move orders {report.Easy.AttackMoveOrders}, status {report.Easy.LastStatus}");
    Console.WriteLine($"  {report.Normal.Difficulty}: waves {report.Normal.WavesLaunched}, attack-move orders {report.Normal.AttackMoveOrders}, status {report.Normal.LastStatus}");
}

static void PrintDefensePolicy(DefensePolicyReport report)
{
    Console.WriteLine("defense policy:");
    PrintDefenseProbe(report.EasyOuter);
    PrintDefenseProbe(report.NormalOuter);
    PrintDefenseProbe(report.EasyInner);
}

static void PrintDefenseProbe(DefenseProbe probe)
{
    Console.WriteLine($"  {probe.Difficulty} threat {probe.ThreatOffsetFromBase:0}px: orders {probe.DefenseOrders}, defenders {probe.AssignedDefenders}, status {probe.LastStatus}");
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

sealed record ScoutPolicyReport(ScoutProbe Easy, ScoutProbe Normal);

sealed record ScoutProbe(
    EnemyDifficulty Difficulty,
    int WavesLaunched,
    int AttackMoveOrders,
    string LastStatus);

sealed record DefensePolicyReport(
    DefenseProbe EasyOuter,
    DefenseProbe NormalOuter,
    DefenseProbe EasyInner);

sealed record DefenseProbe(
    EnemyDifficulty Difficulty,
    float ThreatOffsetFromBase,
    int DefenseOrders,
    int AssignedDefenders,
    string LastStatus);
