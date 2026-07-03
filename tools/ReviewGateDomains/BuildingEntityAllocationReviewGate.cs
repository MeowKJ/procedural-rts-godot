static class BuildingEntityAllocationReviewGate
{
    public static void Check(string root, GateResult result)
    {
        var building = ReviewGateSource.Read(root, "scripts", "core", "entities", "BuildingTargetEntityBridge.cs");
        RequireText(building, "CreateWeaponMountStates(weaponKind, seed.Facing)", "Building entity bridge weapon mounts must use an explicit snapshot helper.", result);
        RequireText(building, "new WeaponMountRuntimeState[1]", "Building entity bridge weapon mount helper must allocate the required independent snapshot array explicitly.", result);
        RequireText(building, "CreateProductionQueueItems(productionQueue)", "Building entity bridge production queues must use an explicit snapshot helper.", result);
        RequireText(building, "new UnitProductionQueueItem[items.Count]", "Building entity bridge production queue helper must allocate the required independent snapshot array explicitly.", result);
        ForbidText(building, "new[] { new WeaponMountRuntimeState(\"main\", weaponKind, seed.Facing, 0) }", "Building entity bridge weapon mount snapshots must not use inline array construction.", result);
        ForbidText(building, "productionQueue.ToArray()", "Building entity bridge production queue snapshots must not use ToArray.", result);

        var projection = ReviewGateSource.Read(root, "scripts", "core", "sim", "BuildingPresentationProjection.cs");
        var projectionQueue = ReviewGateSource.Read(root, "scripts", "core", "sim", "BuildingPresentationProjector.Queue.cs");
        RequireText(projection, "partial class BuildingPresentationProjector", "Building presentation projector must be partial so queue clone helpers stay in a focused file.", result);
        RequireText(projection, "CloneProductionQueue(production.Items)", "Building presentation production queues must use an explicit clone helper.", result);
        RequireText(projectionQueue, "new UnitProductionQueueItem[items.Count]", "Building presentation queue clone helper must allocate the required independent snapshot array explicitly.", result);
        RequireText(projectionQueue, "for (var index = 0; index < items.Count; index++)", "Building presentation queue clone helper must use an indexed copy loop.", result);
        ForbidText(projection, "production.Items.Select(CloneQueueItem).ToArray()", "Building presentation production queue clones must not allocate LINQ projection iterators.", result);
    }
}
