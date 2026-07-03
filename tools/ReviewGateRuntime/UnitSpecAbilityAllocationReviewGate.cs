static class UnitSpecAbilityAllocationReviewGate
{
    public static void Check(string root, GateResult result)
    {
        var unitSpec = ReviewGateSource.Read(root, "scripts", "core", "units", "UnitSpec.cs");
        RequireText(unitSpec, "public bool HasAbility(AbilityKind kind)", "UnitSpec ability checks must expose an explicit no-LINQ scan.", result);
        RequireText(unitSpec, "public bool TryGetAbility(AbilityKind kind, out AbilitySpec ability)", "UnitSpec ability lookup must expose an explicit no-LINQ scan.", result);

        var gameState = ReviewGateSource.Read(root, "scripts", "core", "GameState.cs");
        var battleRootSelection = ReviewGateSource.Read(root, "scripts", "BattleRoot.Selection.cs");
        var selectionController = ReviewGateSource.Read(root, "scripts", "controllers", "SelectionController.Utilities.cs");
        var commandBridge = ReviewGateSource.Read(root, "scripts", "core", "units", "runtime", "battlefield", "UnitBattlefield.CommandBridge.cs");
        var syncRuntime = ReviewGateSource.Read(root, "scripts", "core", "units", "runtime", "battlefield", "UnitBattlefield.SyncRuntime.cs");
        var entityBridge = ReviewGateSource.Read(root, "scripts", "core", "entities", "UnitSpecEntityBridge.cs");
        var runtimeSources = gameState
            + battleRootSelection
            + selectionController
            + commandBridge
            + syncRuntime
            + entityBridge;

        RequireText(runtimeSources, "spec.HasAbility(AbilityKind.Harvest)", "Legacy/BattleRoot/controller harvester checks must use UnitSpec.HasAbility.", result);
        RequireText(commandBridge, "unit.Spec.HasAbility(AbilityKind.RepairField)", "UnitBattlefield repair checks must use UnitSpec.HasAbility.", result);
        RequireText(entityBridge, "unitSpec.TryGetAbility(AbilityKind.Build, out var build)", "Entity bridge build-radius lookup must use UnitSpec.TryGetAbility.", result);
        RequireText(entityBridge, "ActiveAbilityCount(unitSpec)", "Entity bridge active ability runtime state must avoid LINQ ability projections.", result);
        RequireText(entityBridge, "CreateWeaponMountStates(unitSpec, facing)", "Entity bridge weapon mount snapshots must use an explicit array-copy helper.", result);

        ForbidText(runtimeSources, "Abilities.Any(", "Runtime/controller UnitSpec ability-kind checks must not allocate LINQ Any predicates.", result);
        ForbidText(entityBridge, "Abilities.FirstOrDefault(", "Entity bridge build ability lookup must not allocate LINQ FirstOrDefault predicates.", result);
        ForbidText(entityBridge, ".Where(ability => ability.Kind is not AbilityKind.Harvest and not AbilityKind.Build)", "Entity bridge active ability projection must not allocate LINQ filters.", result);
        ForbidText(entityBridge, ".Select(mount => new WeaponMountRuntimeState", "Entity bridge weapon mount snapshots must not allocate LINQ projection iterators.", result);
    }
}
