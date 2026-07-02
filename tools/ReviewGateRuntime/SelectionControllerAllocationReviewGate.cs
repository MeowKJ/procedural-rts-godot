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
        RequireText(controller, "State.SelectedUnitCount()", "SelectionController legacy selected-unit readouts must use explicit selected-unit counts.", result);
        RequireText(controller, "List<UnitModel> _legacyCommandLineUnitBuffer", "SelectionController command-line preview must reuse legacy selected-unit storage.", result);
        RequireText(controller, "List<UnitInstance> _runtimeCommandLineUnitBuffer", "SelectionController command-line preview must reuse runtime selected-unit storage.", result);
        RequireText(controller, "Dictionary<(int X, int Y), (Vector2 Position, Color Accent, float Pulse)> _commandLineTargetMarkers", "SelectionController command-line target markers must reuse dictionary storage.", result);
        RequireText(controller, "CollectLegacyCommandLineUnits(_legacyCommandLineUnitBuffer)", "SelectionController legacy command lines must fill reusable unit storage.", result);
        RequireText(controller, "CollectRuntimeCommandLineUnits(_runtimeCommandLineUnitBuffer)", "SelectionController runtime command lines must fill reusable unit storage.", result);

        ForbidText(controller, "State.SelectedUnits().ToList()", "SelectionController must not materialize selected legacy units for right-click commands.", result);
        ForbidText(controller, "State.SelectedUnits().Any()", "SelectionController preview must not allocate selected-unit LINQ queries.", result);
        ForbidText(controller, "State.SelectedUnits().Count()", "SelectionController selected-unit readouts must not allocate LINQ Count queries.", result);
        ForbidText(controller, "State.SelectedUnits()\n            .Where(unit => unit.CommandVisualTarget is not null || unit.FormationSlot is not null)\n            .ToList()", "SelectionController legacy command-line preview must not materialize selected-unit lists.", result);
        ForbidText(controller, "UnitBattlefield!.SelectedUnits(LocalPlayerSlotId)\n            .Where(unit => unit.CommandVisualTarget is not null || unit.FormationSlot is not null)\n            .ToList()", "SelectionController runtime command-line preview must not materialize selected-unit lists.", result);
        ForbidText(controller, "new Dictionary<string, (Vector2 Position, Color Accent, float Pulse)>()", "SelectionController command-line preview must not allocate marker dictionaries per draw.", result);
        ForbidText(controller, "State.SelectedBuildings().Any()", "SelectionController preview must not allocate selected-building LINQ queries.", result);
        ForbidText(controller, "UnitBattlefield!.SelectedUnits(LocalPlayerSlotId).Any(IsHarvester)", "SelectionController runtime harvester check must use an explicit scan.", result);
    }
}
