static class EnemyAttackWaveAiReviewGate
{
    public static void Check(string root, GateResult result)
    {
        RequirePartialSplit(root, result);
        RequireUnitSelectionBuffers(root, result);
        RequireTargetScanLoops(root, result);
        RequireLegacyAttackWaveScans(root, result);
    }

    private static void RequirePartialSplit(string root, GateResult result)
    {
        var relativeFiles = new[]
        {
            Path.Combine("scripts", "core", "units", "runtime", "UnitBattlefieldEnemyAttackWaveAi.cs"),
            Path.Combine("scripts", "core", "units", "runtime", "UnitBattlefieldEnemyAttackWaveAi.UnitSelection.cs"),
            Path.Combine("scripts", "core", "units", "runtime", "UnitBattlefieldEnemyAttackWaveAi.Targeting.cs"),
            Path.Combine("scripts", "core", "units", "runtime", "UnitBattlefieldEnemyAttackWaveAi.TargetScans.cs"),
            Path.Combine("scripts", "core", "units", "runtime", "UnitBattlefieldEnemyAttackWaveAi.Geometry.cs"),
        };

        foreach (var relativeFile in relativeFiles)
        {
            RequireFileUnderLineBudget(root, result, relativeFile);
        }

        var main = ReviewGateSource.Read(root, "scripts", "core", "units", "runtime", "UnitBattlefieldEnemyAttackWaveAi.cs");
        RequireText(main, "public sealed partial class UnitBattlefieldEnemyAttackWaveAi", "Enemy attack wave AI main file must be partial.", result);
        ForbidText(main, "private static IEnumerable<UnitInstance> AvailableWaveUnits", "Enemy attack wave AI unit selection must stay in its partial file.", result);
        ForbidText(main, "private bool TryFindTarget", "Enemy attack wave AI target search must stay in its partial file.", result);
        ForbidText(main, "private static Vector2 EnemyCenter", "Enemy attack wave AI geometry helpers must stay in their partial file.", result);
    }

    private static void RequireUnitSelectionBuffers(string root, GateResult result)
    {
        var ai = ReviewGateEvidence.ReadSourceWithPartials(
            Path.Combine(root, "scripts", "core", "units", "runtime", "UnitBattlefieldEnemyAttackWaveAi.cs"));
        RequireText(ai, "List<UnitInstance> _waveCandidateUnits", "Enemy attack wave AI must reuse wave candidate storage.", result);
        RequireText(ai, "List<UnitInstance> _waveUnits", "Enemy attack wave AI must reuse wave unit storage.", result);
        RequireText(ai, "List<int> _waveUnitIds", "Enemy attack wave AI must reuse wave command id storage.", result);
        RequireText(ai, "List<UnitInstance> _defenseUnits", "Enemy attack wave AI must reuse defense unit storage.", result);
        RequireText(ai, "List<int> _defenseUnitIds", "Enemy attack wave AI must reuse defense command id storage.", result);
        RequireText(ai, "UnitDistanceComparer _unitDistanceComparer", "Enemy attack wave AI must reuse distance sort comparison state.", result);

        var main = ReviewGateSource.Read(root, "scripts", "core", "units", "runtime", "UnitBattlefieldEnemyAttackWaveAi.cs");
        RequireText(main, "CollectAvailableWaveUnits(", "Enemy wave selection must fill the reusable wave unit buffer.", result);
        RequireText(main, "CollectUnitIds(_waveUnits, _waveUnitIds)", "Enemy wave attack commands must fill reusable id storage.", result);
        RequireText(main, "TryIssueScoutWave(battlefield, enemyPlayerSlotId, _waveUnits, _waveUnitIds", "Enemy scout waves must reuse the wave id storage.", result);
        RequireText(main, "CollectAvailableDefenseUnits(", "Enemy defense selection must fill reusable defender storage.", result);
        RequireText(main, "CollectUnitIds(_defenseUnits, _defenseUnitIds)", "Enemy defense commands must fill reusable id storage.", result);
        ForbidText(main, "var waveUnits = AvailableWaveUnits(", "Enemy wave selection must not allocate via enumerable ToList.", result);
        ForbidText(main, "var defenders = AvailableDefenseUnits(", "Enemy defense selection must not allocate via enumerable Take/ToList.", result);
        ForbidText(main, ".Select(unit => unit.Id).ToList()", "Enemy attack wave AI must not allocate command id lists.", result);
        ForbidText(main, "waveUnits.Where(unit => unit.AttackTargetId is not null)", "Enemy wave pulse updates must use an explicit loop.", result);

        var selection = ReviewGateSource.Read(root, "scripts", "core", "units", "runtime", "UnitBattlefieldEnemyAttackWaveAi.UnitSelection.cs");
        RequireText(selection, "CollectAvailableWaveUnits(", "Enemy wave unit selection must use a caller-owned buffer helper.", result);
        RequireText(selection, "CollectAvailableDefenseUnits(", "Enemy defense unit selection must use a caller-owned buffer helper.", result);
        RequireText(selection, "CollectUnitIds(IReadOnlyList<UnitInstance> units, List<int> result)", "Enemy attack wave AI must centralize id buffer fills.", result);
        ForbidText(selection, ".ToList()", "Enemy unit selection must not materialize LINQ lists.", result);
        ForbidText(selection, ".ToHashSet()", "Enemy wave reserve must not allocate HashSets.", result);
        ForbidText(selection, "IEnumerable<UnitInstance> Available", "Enemy unit selection helpers must not return allocating enumerables.", result);

        var targeting = ReviewGateSource.Read(root, "scripts", "core", "units", "runtime", "UnitBattlefieldEnemyAttackWaveAi.Targeting.cs");
        RequireText(targeting, "CollectUnitIds(waveUnits, unitIds)", "Enemy scout waves must fill the reusable id buffer.", result);
        ForbidText(targeting, "waveUnits.Select(unit => unit.Id)", "Enemy scout waves must not allocate id enumerables.", result);
    }

    private static void RequireTargetScanLoops(string root, GateResult result)
    {
        var targeting = ReviewGateSource.Read(root, "scripts", "core", "units", "runtime", "UnitBattlefieldEnemyAttackWaveAi.Targeting.cs");
        var targetScans = ReviewGateSource.Read(root, "scripts", "core", "units", "runtime", "UnitBattlefieldEnemyAttackWaveAi.TargetScans.cs");
        RequireText(targeting, "NearestVisibleAttackableBuilding(", "Enemy attack target building selection must use an explicit nearest scan.", result);
        RequireText(targeting, "NearestVisibleAttackableUnit(", "Enemy attack target unit selection must use an explicit nearest scan.", result);
        RequireText(targeting, "NearestVisibleDefenseThreatUnit(", "Enemy defense target unit selection must use an explicit nearest scan.", result);
        RequireText(targeting, "NearestVisibleDefenseThreatBuilding(", "Enemy defense target building selection must use an explicit nearest scan.", result);
        RequireText(targetScans, "IsVisibleAttackableBuilding(", "Enemy target scans must share the visible attackable building predicate.", result);
        RequireText(targetScans, "IsVisibleAttackableUnit(", "Enemy target scans must share the visible attackable unit predicate.", result);
        ForbidText(targeting, ".Where(", "Enemy target scans must not allocate LINQ filter chains.", result);
        ForbidText(targeting, ".OrderBy(", "Enemy target scans must not allocate ordered LINQ chains.", result);
        ForbidText(targeting, ".ThenBy(", "Enemy target scans must not allocate secondary ordered LINQ chains.", result);
        ForbidText(targeting, ".FirstOrDefault()", "Enemy target scans must not allocate LINQ first queries.", result);
        ForbidText(targeting, ".Select(building => (UnitBattlefieldBuildingSnapshot?)building)", "Enemy target scans must not allocate nullable building projections.", result);
        ForbidText(targetScans, ".Where(", "Enemy target scan helpers must not allocate LINQ filter chains.", result);
        ForbidText(targetScans, ".OrderBy(", "Enemy target scan helpers must not allocate ordered LINQ chains.", result);
        ForbidText(targetScans, ".ThenBy(", "Enemy target scan helpers must not allocate secondary ordered LINQ chains.", result);
        ForbidText(targetScans, ".FirstOrDefault()", "Enemy target scan helpers must not allocate LINQ first queries.", result);
        ForbidText(targetScans, ".Select(building => (UnitBattlefieldBuildingSnapshot?)building)", "Enemy target scan helpers must not allocate nullable building projections.", result);

        var geometry = ReviewGateSource.Read(root, "scripts", "core", "units", "runtime", "UnitBattlefieldEnemyAttackWaveAi.Geometry.cs");
        RequireText(geometry, "foreach (var building in battlefield.BuildingSnapshots())", "Enemy base center and owned-building checks must scan buildings explicitly.", result);
        RequireText(geometry, "foreach (var unit in battlefield.Units)", "Enemy army center must scan units explicitly.", result);
        RequireText(geometry, "return sum / count;", "Enemy center calculations must average explicit scan sums.", result);
        ForbidText(geometry, ".Where(", "Enemy geometry helpers must not allocate LINQ filter chains.", result);
        ForbidText(geometry, ".Any(", "Enemy near-owned-building checks must not allocate LINQ Any queries.", result);
        ForbidText(geometry, ".Select(", "Enemy center calculations must not allocate LINQ projection chains.", result);
        ForbidText(geometry, ".ToList()", "Enemy center calculations must not materialize temporary lists.", result);
        ForbidText(geometry, ".Aggregate(", "Enemy center calculations must not allocate aggregate delegates.", result);
    }

    private static void RequireLegacyAttackWaveScans(string root, GateResult result)
    {
        var legacy = ReviewGateSource.Read(root, "scripts", "core", "ai", "EnemyAttackWaveAi.cs");
        RequireText(legacy, "List<UnitModel> _waveUnits", "Legacy EnemyAttackWaveAi must reuse wave-unit storage.", result);
        RequireText(legacy, "CollectAvailableCombatUnits(state, _profile.MaximumWaveUnits, _waveUnits)", "Legacy EnemyAttackWaveAi must fill a caller-owned wave-unit buffer.", result);
        RequireText(legacy, "result.Sort(CompareWaveUnits)", "Legacy EnemyAttackWaveAi must sort wave candidates in reusable storage.", result);
        RequireText(legacy, "BuildingModel? buildingTarget = null;", "Legacy EnemyAttackWaveAi must scan nearest building targets explicitly.", result);
        RequireText(legacy, "UnitModel? unitTarget = null;", "Legacy EnemyAttackWaveAi must scan nearest unit targets explicitly.", result);
        RequireText(legacy, "sum += unit.Position;", "Legacy EnemyAttackWaveAi center calculation must use an explicit position sum.", result);
        ForbidText(legacy, ".Where(", "Legacy EnemyAttackWaveAi must not allocate LINQ filter chains.", result);
        ForbidText(legacy, ".OrderBy(", "Legacy EnemyAttackWaveAi must not allocate ordered LINQ chains.", result);
        ForbidText(legacy, ".ThenBy(", "Legacy EnemyAttackWaveAi must not allocate secondary ordered LINQ chains.", result);
        ForbidText(legacy, ".Select(", "Legacy EnemyAttackWaveAi must not allocate projection chains.", result);
        ForbidText(legacy, ".ToList()", "Legacy EnemyAttackWaveAi must not materialize temporary lists.", result);
        ForbidText(legacy, ".FirstOrDefault(", "Legacy EnemyAttackWaveAi must not allocate LINQ first queries.", result);
        ForbidText(legacy, ".Aggregate(", "Legacy EnemyAttackWaveAi must not allocate aggregate delegates.", result);
        ForbidText(legacy, "IEnumerable<UnitModel>", "Legacy EnemyAttackWaveAi selection helpers must not return allocating enumerables.", result);
    }

    private static void RequireFileUnderLineBudget(string root, GateResult result, string relativeFile)
    {
        var path = Path.Combine(root, relativeFile);
        var normalized = relativeFile.Replace(Path.DirectorySeparatorChar, '/');
        if (!File.Exists(path))
        {
            result.Error($"Enemy attack wave AI partial is missing: {normalized}.");
            return;
        }

        var lineCount = File.ReadAllLines(path).Length;
        if (lineCount > 200)
        {
            result.Error($"Enemy attack wave AI partial exceeds 200 lines: {normalized} has {lineCount} lines.");
        }
    }
}
