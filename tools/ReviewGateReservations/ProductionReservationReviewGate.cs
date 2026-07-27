static class ProductionReservationReviewGate
{
    public static void Check(string root, GateResult result)
    {
        var production = ReviewGateSource.Read(root, "scripts", "core", "sim", "systems", "ProductionSystem.cs");
        RequireText(
            production,
            "if (!TrySpawnProducedUnit(world, producer, unitSpec))\n            {\n                continue;\n            }\n\n            RemoveFirstQueueItem(producer, queue);",
            "ProductionSystem must retain a completed item until fixed-egress spawn succeeds.", result);
        var spawning = ReviewGateSource.Read(root, "scripts", "core", "sim", "systems", "production", "ProductionSystem.Spawning.cs");
        RequireText(spawning, "PlacementReservationMath.TryCenter(", "ECS production must use reservation metadata.", result);
        RequireText(spawning, "PlacementReservationKind.ProductionEgress", "ECS production must consume its egress reservation.", result);
        RequireText(spawning, "ProductionSpawnMath.IsSpawnPointAvailable(", "ECS production must test the fixed egress.", result);
        ForbidText(spawning, "RemoveFirstQueueItem", "The spawn helper must not dequeue blocked production.", result);
        var spawnMath = ReviewGateSource.Read(root, "scripts", "core", "production", "ProductionSpawnMath.cs");
        RequireText(spawnMath, "IsSpawnPointAvailable(", "Spawn math must test only the fixed egress.", result);
        ForbidText(spawnMath, "DirectionOffsets", "Spawn math must not search alternate directions.", result);
        ForbidText(spawnMath, "RingScales", "Spawn math must not search fallback rings.", result);
        ForbidText(spawnMath, "FindSpawnPoint", "The radial spawn API must be removed.", result);
        ForbidText(spawnMath, "ClampToWorld", "Blocked egress must not clamp to a fallback.", result);
        ReviewGateSource.ForbidTextInSources(root, result, "FindSpawnPoint", "scripts", "tools");
        ReviewGateSource.ForbidTextInSources(root, result, "DirectionOffsets", "scripts", "tools");
        ReviewGateSource.ForbidTextInSources(root, result, "RingScales", "scripts", "tools");
    }
}
