static class ControlGroupAllocationReviewGate
{
    public static void Check(string root, GateResult result)
    {
        ReviewGateSource.RequireFile(root, result, "scripts", "controllers", "ControlGroupController.Groups.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "controllers", "ControlGroupController.Snapshots.cs");
        var controller = ReviewGateEvidence.ReadSourceWithPartials(
            Path.Combine(root, "scripts", "controllers", "ControlGroupController.cs"));

        RequireText(controller, "private void CollectSelectedUnitIds(List<int> result)", "ControlGroup save must fill a reusable selected-id list.", result);
        RequireText(controller, "CollectSelectedUnitIds(selectedIds)", "SaveGroup must reuse the stored group list instead of materializing selection ids.", result);
        RequireText(controller, "private int SelectUnitsByIds(IReadOnlyList<int> unitIds)", "ControlGroup recall must pass read-only id storage through selection.", result);
        RequireText(controller, "private Vector2? GroupCenter(IReadOnlyList<int> unitIds)", "ControlGroup double-tap center must scan group ids without a position list.", result);
        RequireText(controller, "private static bool ContainsUnitId(IReadOnlyList<int> unitIds, int unitId)", "ControlGroup id matching must use an explicit no-allocation scan.", result);
        RequireText(controller, "List<ControlGroupSnapshot> _snapshotBuffer", "ControlGroup HUD snapshots must reuse snapshot storage.", result);
        RequireText(controller, "HashSet<int> _snapshotSelectedIds", "ControlGroup HUD snapshots must reuse selected-id storage.", result);
        RequireText(controller, "CollectSnapshotSelectedIds();", "ControlGroup HUD snapshots must fill selected ids explicitly.", result);
        RequireText(controller, "private void CollectUnitBattlefieldSnapshots()", "Runtime control-group snapshots must use an explicit no-allocation scan.", result);
        RequireText(controller, "private void AddSnapshot(", "ControlGroup snapshot construction must use a shared append helper.", result);
        ForbidText(controller, "State.", "ControlGroup runtime must not read retired state fields.", result);

        ForbidText(controller, "var selectedIds = SelectedUnitIds().ToList()", "ControlGroup save must not allocate selected ids through LINQ.", result);
        ForbidText(controller, "private IEnumerable<int> SelectedUnitIds()", "ControlGroup selected-id collection must not expose a LINQ iterator helper.", result);
        ForbidText(controller, "var requestedIds = unitIds.ToHashSet()", "ControlGroup recall center must not allocate a requested-id HashSet.", result);
        ForbidText(controller, ".Select(unit => unit.Position)\n                .ToList()", "ControlGroup recall center must not materialize temporary position lists.", result);
        ForbidText(controller, "new List<ControlGroupSnapshot>(9)", "ControlGroup HUD snapshots must not allocate a snapshot list per refresh.", result);
        ForbidText(controller, ".ToHashSet()", "ControlGroup HUD snapshots and recall paths must not allocate HashSets through LINQ.", result);
        ForbidText(controller, ".ToList()", "ControlGroup HUD snapshots must not materialize live-unit lists.", result);
        ForbidText(controller, ".Select(", "ControlGroup HUD snapshots must not allocate LINQ projection chains.", result);
        ForbidText(controller, ".Where(", "ControlGroup HUD snapshots must not allocate LINQ filter chains.", result);
        ForbidText(controller, ".Any(", "ControlGroup HUD snapshots must not allocate LINQ ability checks.", result);
    }
}
