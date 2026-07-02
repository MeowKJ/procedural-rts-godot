static class EnemyAttackWaveAiReviewGate
{
    public static void Check(string root, GateResult result)
    {
        RequirePartialSplit(root, result);
    }

    private static void RequirePartialSplit(string root, GateResult result)
    {
        var relativeFiles = new[]
        {
            Path.Combine("scripts", "core", "units", "runtime", "UnitBattlefieldEnemyAttackWaveAi.cs"),
            Path.Combine("scripts", "core", "units", "runtime", "UnitBattlefieldEnemyAttackWaveAi.UnitSelection.cs"),
            Path.Combine("scripts", "core", "units", "runtime", "UnitBattlefieldEnemyAttackWaveAi.Targeting.cs"),
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
