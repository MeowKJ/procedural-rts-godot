static class EntityWorldStableReadoutReviewGate
{
    public static void Check(string root, GateResult result)
    {
        var world = ReviewGateSource.Read(root, "scripts", "core", "entities", "EntityWorld.cs");
        RequireText(world, "private readonly List<EntityInstance> _orderedEntities = [];", "EntityWorld must maintain one canonical ordered membership index.", result);
        RequireText(world, "public IReadOnlyCollection<EntityInstance> StableEntities => _orderedEntities;", "StableEntities must expose the ordered membership index through a read-only interface.", result);
        RequireText(world, "public IReadOnlyList<EntityInstance> OrderedEntities => _orderedEntities;", "OrderedEntities must expose indexed read access for allocation-free spatial scans.", result);
        RequireText(world, "public IReadOnlyCollection<EntitySpec> StableSpecs => _specs.Values;", "StableSpecs must expose the sorted spec values view without allocating.", result);
        RequireText(world, "_entities.Add(entity.Id.Value, entity);", "Spawn must add membership to the ID lookup authority before publishing it to the ordered index.", result);
        RequireText(world, "_orderedEntities.Add(entity);", "Spawn must append the same monotonically allocated entity reference to the ordered index.", result);
        RequireText(world, "private bool RemoveEntityNow(int id)", "Direct and queued removals must share one membership-index removal authority.", result);
        RequireText(world, "RemoveEntityNow(id);", "Queued removal flushing must use the shared removal authority.", result);
        RequireText(world, "return RemoveEntityNow(id.Value);", "Direct removal must use the shared removal authority.", result);
        RequireText(world, "_orderedEntities.RemoveAt(foundIndex);", "Removal must delete the matching ordered membership entry.", result);
        RequireText(world, "for (var entityIndex = 0; entityIndex < _orderedEntities.Count; entityIndex++)", "Deterministic state hashing must read the canonical ordered index without iterator allocation.", result);
        ForbidText(world, "public List<EntityInstance> OrderedEntities", "OrderedEntities must not expose the mutable membership list.", result);
        ForbidText(world, "public List<EntityInstance> StableEntities", "StableEntities must not expose the mutable membership list.", result);
        ForbidText(world, "_entities.Values", "EntityWorld must not retain a second ordered entity read sequence.", result);
        ForbidText(world, "StableSpecs => _specs.Values.ToList()", "StableSpecs must not allocate list snapshots.", result);
        ForbidText(world, "IReadOnlyList<EntitySpec> StableSpecs", "StableSpecs must not expose an allocating list snapshot contract.", result);
        ForbidText(world, "public IEnumerable<EntityInstance> OrderedEntities", "OrderedEntities must not regress to a boxing interface-enumerator contract.", result);
        ForbidText(world, "OrderedEntities => _orderedEntities.ToList()", "OrderedEntities must not allocate list snapshots.", result);
        ForbidText(world, "OrderedEntities => _orderedEntities.ToArray()", "OrderedEntities must not allocate array snapshots.", result);
        ForbidText(world, "_orderedEntities.Sort(", "Ordered membership must rely only on monotonic EntityId allocation, not query-time sorting.", result);
    }
}
