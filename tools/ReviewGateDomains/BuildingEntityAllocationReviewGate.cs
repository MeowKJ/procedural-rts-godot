static class BuildingEntityAllocationReviewGate
{
    public static void Check(string root, GateResult result)
    {
        var building = ReviewGateSource.Read(root, "scripts", "core", "entities", "BuildingEntityFactory.cs");
        RequireText(building, "CreateWeaponMountStates(weaponId, seed.Facing)", "Building entity routing weapon mounts must use an explicit snapshot helper.", result);
        RequireText(building, "new WeaponMountRuntimeState[1]", "Building entity routing weapon mount helper must allocate the required independent snapshot array explicitly.", result);
        RequireText(building, "CreateProductionQueueItems(productionQueue)", "Building entity routing production queues must use an explicit snapshot helper.", result);
        RequireText(building, "new UnitProductionQueueItem[items.Count]", "Building entity routing production queue helper must allocate the required independent snapshot array explicitly.", result);
        RequireText(building, "new EntityComponentState[BuildingComponentCount", "Building entity routing component snapshots must allocate the required independent array explicitly.", result);
        RequireText(building, "components[index++] = new BuildingIdentityComponentState", "Building entity routing component snapshots must preserve explicit indexed component order.", result);
        ForbidText(building, "new[] { new WeaponMountRuntimeState(\"main\", weaponId, seed.Facing, 0) }", "Building entity routing weapon mount snapshots must not use inline array construction.", result);
        ForbidText(building, "productionQueue.ToArray()", "Building entity routing production queue snapshots must not use ToArray.", result);
        ForbidText(building, "InitialBuildingComponents(", "Building entity routing component snapshots must not use iterator-based component construction.", result);
        var projection = ReviewGateSource.Read(root, "scripts", "core", "sim", "BuildingPresentationProjection.cs");
        var projectionQueue = ReviewGateSource.Read(root, "scripts", "core", "sim", "BuildingPresentationProjector.Queue.cs");
        RequireText(projection, "partial class BuildingPresentationProjector", "Building presentation projector must be partial so queue clone helpers stay in a focused file.", result);
        RequireText(projection, "CloneProductionQueue(production.Items)", "Building presentation production queues must use an explicit clone helper.", result);
        RequireText(projectionQueue, "new UnitProductionQueueItem[items.Count]", "Building presentation queue clone helper must allocate the required independent snapshot array explicitly.", result);
        RequireText(projectionQueue, "for (var index = 0; index < items.Count; index++)", "Building presentation queue clone helper must use an indexed copy loop.", result);
        ForbidText(projection, "production.Items.Select(CloneQueueItem).ToArray()", "Building presentation production queue clones must not allocate LINQ projection iterators.", result);
    }
}
