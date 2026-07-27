static class UnitSpecAbilityAllocationReviewGate
{
    public static void Check(string root, GateResult result)
    {
        var unitSpec = ReviewGateSource.Read(root, "scripts", "core", "units", "UnitSpec.cs");
        RequireText(unitSpec, "public bool HasAbility(AbilityKind kind)", "UnitSpec ability checks must expose an explicit no-LINQ scan.", result);
        RequireText(unitSpec, "public bool TryGetAbility(AbilityKind kind, out AbilitySpec ability)", "UnitSpec ability lookup must expose an explicit no-LINQ scan.", result);
        var battleRootSelection = ReviewGateSource.Read(root, "scripts", "battle-root", "BattleRoot.Selection.cs");
        var selectionController = ReviewGateSource.Read(root, "scripts", "controllers", "SelectionController.Utilities.cs");
        var commandRouting = ReviewGateSource.Read(root, "scripts", "core", "units", "runtime", "battlefield", "UnitBattlefield.CommandRouting.cs");
        var syncRuntime = ReviewGateSource.Read(root, "scripts", "core", "units", "runtime", "battlefield", "UnitBattlefield.SyncRuntime.cs");
        var entityFactory = ReviewGateSource.Read(root, "scripts", "core", "entities", "UnitEntityFactory.cs");
        var runtimeSources = battleRootSelection
            + selectionController
            + commandRouting
            + syncRuntime
            + entityFactory;

        RequireText(runtimeSources, "spec.HasAbility(AbilityKind.Harvest)", "Runtime and controller harvester checks must use UnitSpec.HasAbility.", result);
        RequireText(commandRouting, "unit.Spec.HasAbility(AbilityKind.RepairField)", "UnitBattlefield repair checks must use UnitSpec.HasAbility.", result);
        RequireText(entityFactory, "unitSpec.TryGetAbility(AbilityKind.Build, out var build)", "Entity routing build-radius lookup must use UnitSpec.TryGetAbility.", result);
        RequireText(entityFactory, "ActiveAbilityCount(unitSpec)", "Entity routing active ability runtime state must avoid LINQ ability projections.", result);
        RequireText(entityFactory, "CreateWeaponMountStates(unitSpec, facing)", "Entity routing weapon mount snapshots must use an explicit array-copy helper.", result);
        RequireText(entityFactory, "CreateTags(unitSpec)", "Entity routing tag snapshots must use an explicit HashSet fill helper.", result);
        RequireText(entityFactory, "foreach (var tag in unitSpec.RoleTags)", "Entity routing tag snapshots must scan role tags explicitly.", result);

        ForbidText(runtimeSources, "Abilities.Any(", "Runtime/controller UnitSpec ability-kind checks must not allocate LINQ Any predicates.", result);
        ForbidText(entityFactory, "Abilities.FirstOrDefault(", "Entity routing build ability lookup must not allocate LINQ FirstOrDefault predicates.", result);
        ForbidText(entityFactory, ".Where(ability => ability.Kind is not AbilityKind.Harvest and not AbilityKind.Build)", "Entity routing active ability projection must not allocate LINQ filters.", result);
        ForbidText(entityFactory, ".Select(mount => new WeaponMountRuntimeState", "Entity routing weapon mount snapshots must not allocate LINQ projection iterators.", result);
        ForbidText(entityFactory, ".Select(tag => tag.ToString())", "Entity routing tag snapshots must not allocate LINQ projection iterators.", result);
        ForbidText(entityFactory, ".Append(unitSpec.Archetype.ToString())", "Entity routing tag snapshots must not allocate LINQ append iterators.", result);
        ForbidText(entityFactory, ".ToHashSet()", "Entity routing tag snapshots must not materialize LINQ hash sets.", result);
    }
}
