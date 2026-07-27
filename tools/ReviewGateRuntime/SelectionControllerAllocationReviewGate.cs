static class SelectionControllerAllocationReviewGate
{
    public static void Check(string root, GateResult result)
    {
        var controller = ReviewGateEvidence.ReadSourceWithPartials(Path.Combine(root, "scripts", "controllers", "SelectionController.cs"));
        RequireText(controller, "public required UnitBattlefield UnitBattlefield", "SelectionController must require the live UnitBattlefield authority.", result);
        RequireText(controller, "private bool HasSelectedRuntimeHarvester()", "SelectionController harvest affordance must scan runtime harvesters explicitly.", result);
        ForbidText(controller, "GameState", "SelectionController must not depend on the retired GameState compatibility chain.", result);
        ForbidText(controller, "UnitModel", "SelectionController must not retain legacy UnitModel input state.", result);
        ForbidText(controller, "BuildingModel", "SelectionController must not retain legacy BuildingModel input state.", result);
        RequireText(controller, "List<UnitInstance> _runtimeCommandLineUnitBuffer", "SelectionController command-line preview must reuse runtime selected-unit storage.", result);
        RequireText(controller, "List<int> _selectionHotkeyUnitIdBuffer", "SelectionController selection hotkeys must retain reusable unit-id storage.", result);
        RequireText(controller, "Dictionary<(int X, int Y), (Vector2 Position, Color Accent, float Pulse)> _commandLineTargetMarkers", "SelectionController command-line target markers must reuse dictionary storage.", result);
        RequireText(controller, "HandleSelectionHotkey(key)", "SelectionController must route selection hotkeys through the shared hotkey handler.", result);
        RequireText(controller, "UnitBattlefield.SelectArmy(LocalPlayerSlotId)", "Runtime select-all-army hotkey must route through UnitBattlefield selection commands.", result);
        RequireText(controller, "UnitBattlefield.SelectNextIdleHarvester(LocalPlayerSlotId)", "Runtime idle-harvester cycle hotkey must route through UnitBattlefield selection commands.", result);
        RequireText(controller, "CollectRuntimeCommandLineUnits(_runtimeCommandLineUnitBuffer)", "SelectionController runtime command lines must fill reusable unit storage.", result);
        ForbidText(controller, "new Dictionary<string, (Vector2 Position, Color Accent, float Pulse)>()", "SelectionController command-line preview must not allocate marker dictionaries per draw.", result);
        ForbidText(controller, "SelectedUnits(LocalPlayerSlotId).Any", "SelectionController runtime harvester check must use an explicit scan.", result);
        ForbidText(controller, "foreach (var unit in UnitBattlefield.SelectedUnits(LocalPlayerSlotId))", "SelectionController runtime harvester check must not allocate selected-unit iterators.", result);
    }
}
