static class ControlGroupAllocationReviewGate
{
    public static void Check(string root, GateResult result)
    {
        var controller = ReviewGateSource.Read(root, "scripts", "controllers", "ControlGroupController.cs");

        RequireText(controller, "private void CollectSelectedUnitIds(List<int> result)", "ControlGroup save must fill a reusable selected-id list.", result);
        RequireText(controller, "CollectSelectedUnitIds(selectedIds)", "SaveGroup must reuse the stored group list instead of materializing selection ids.", result);
        RequireText(controller, "private int SelectUnitsByIds(IReadOnlyList<int> unitIds)", "ControlGroup recall must pass read-only id storage through selection.", result);
        RequireText(controller, "private int SelectLegacyUnitsByIds(IReadOnlyList<int> unitIds)", "Legacy control-group recall must scan ids without allocating a HashSet.", result);
        RequireText(controller, "private Vector2? GroupCenter(IReadOnlyList<int> unitIds)", "ControlGroup double-tap center must scan group ids without a position list.", result);
        RequireText(controller, "private static bool ContainsUnitId(IReadOnlyList<int> unitIds, int unitId)", "ControlGroup id matching must use an explicit no-allocation scan.", result);

        ForbidText(controller, "var selectedIds = SelectedUnitIds().ToList()", "ControlGroup save must not allocate selected ids through LINQ.", result);
        ForbidText(controller, "private IEnumerable<int> SelectedUnitIds()", "ControlGroup selected-id collection must not expose a LINQ iterator helper.", result);
        ForbidText(controller, "var requestedIds = unitIds.ToHashSet()", "ControlGroup recall center must not allocate a requested-id HashSet.", result);
        ForbidText(controller, ".Select(unit => unit.Position)\n                .ToList()", "ControlGroup recall center must not materialize temporary position lists.", result);
    }
}
