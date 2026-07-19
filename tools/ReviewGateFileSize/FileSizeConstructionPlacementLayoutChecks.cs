static class FileSizeConstructionPlacementLayoutChecks
{
    private const int QueryFileMaxLines = 400;

    public static void Check(string root, GateResult result)
    {
        var constructionPath = Path.Combine(root, "scripts", "core", "sim", "systems", "construction");
        var queryPath = Path.Combine(constructionPath, "ConstructionSystem.PlacementQueries.cs");
        var environmentPath = Path.Combine(constructionPath, "ConstructionSystem.PlacementEnvironment.cs");
        var obstaclesPath = Path.Combine(constructionPath, "ConstructionSystem.PlacementObstacles.cs");
        if (!File.Exists(queryPath))
        {
            result.Error("Construction placement query orchestration source is missing.");
            return;
        }

        if (!File.Exists(environmentPath))
        {
            result.Error("Construction placement environment helper source is missing.");
            return;
        }

        if (!File.Exists(obstaclesPath))
        {
            result.Error("Construction placement obstacle helper source is missing.");
            return;
        }

        var queryLines = File.ReadLines(queryPath).Count();
        if (queryLines >= QueryFileMaxLines)
        {
            result.Error($"Construction placement query orchestration must stay below {QueryFileMaxLines} lines, found {queryLines}.");
        }

        var environment = File.ReadAllText(environmentPath);
        var obstacles = File.ReadAllText(obstaclesPath);
        if (!environment.Contains("CollectPlacementSnapshot(", StringComparison.Ordinal)
            || !environment.Contains("IsTerrainPassable(", StringComparison.Ordinal)
            || !environment.Contains("HasBuildVisibility(", StringComparison.Ordinal)
            || !environment.Contains("EnvironmentPlacementRejectionReason(", StringComparison.Ordinal)
            || !obstacles.Contains("ObstacleAndReservationRejectionReason(", StringComparison.Ordinal))
        {
            result.Error("Construction placement helpers must own snapshot, terrain, visibility, and obstacle/reservation queries.");
        }
    }
}
