static class EntityWorldStableReadoutReviewGate
{
    public static void Check(string root, GateResult result)
    {
        var world = ReviewGateSource.Read(root, "scripts", "core", "entities", "EntityWorld.cs");
        RequireText(world, "public IReadOnlyCollection<EntityInstance> StableEntities => _entities.Values;", "StableEntities must expose the sorted entity values view without allocating.", result);
        RequireText(world, "public IReadOnlyCollection<EntitySpec> StableSpecs => _specs.Values;", "StableSpecs must expose the sorted spec values view without allocating.", result);
        ForbidText(world, "StableEntities => _entities.Values.ToList()", "StableEntities must not allocate list snapshots.", result);
        ForbidText(world, "StableSpecs => _specs.Values.ToList()", "StableSpecs must not allocate list snapshots.", result);
        ForbidText(world, "IReadOnlyList<EntityInstance> StableEntities", "StableEntities must not expose an allocating list snapshot contract.", result);
        ForbidText(world, "IReadOnlyList<EntitySpec> StableSpecs", "StableSpecs must not expose an allocating list snapshot contract.", result);
    }
}
