static class SelectionControllerAllocationReviewGate
{
    public static void Check(string root, GateResult result)
    {
        var controller = ReviewGateEvidence.ReadSourceWithPartials(Path.Combine(root, "scripts", "controllers", "SelectionController.cs"));
        RequireText(controller, "List<UnitModel> _legacySelectedUnitCommandBuffer", "SelectionController must reuse legacy selected-unit command storage.", result);
        RequireText(controller, "CollectSelectedLegacyUnits(_legacySelectedUnitCommandBuffer)", "SelectionController right-click fallback must fill reusable selected-unit storage.", result);
        RequireText(controller, "private bool HasSelectedLegacyUnits()", "SelectionController preview must scan selected legacy units without LINQ.", result);
        RequireText(controller, "private bool HasSelectedLegacyHarvester()", "SelectionController harvest affordance must scan selected legacy harvesters explicitly.", result);
        RequireText(controller, "private bool HasSelectedRuntimeHarvester()", "SelectionController harvest affordance must scan runtime harvesters explicitly.", result);
        RequireText(controller, "private bool HasSelectedLegacyBuildings()", "SelectionController building preview must scan selected legacy buildings explicitly.", result);

        ForbidText(controller, "State.SelectedUnits().ToList()", "SelectionController must not materialize selected legacy units for right-click commands.", result);
        ForbidText(controller, "State.SelectedUnits().Any()", "SelectionController preview must not allocate selected-unit LINQ queries.", result);
        ForbidText(controller, "State.SelectedBuildings().Any()", "SelectionController preview must not allocate selected-building LINQ queries.", result);
        ForbidText(controller, "UnitBattlefield!.SelectedUnits(LocalPlayerSlotId).Any(IsHarvester)", "SelectionController runtime harvester check must use an explicit scan.", result);
    }
}
