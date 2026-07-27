using Godot;
using ProceduralRts.Core;

internal static partial class SelectionStressSuite
{
    private static int RunUnitBattlefieldSelectionCommandScenarios()
    {
        var battlefield = new UnitBattlefield();
        var infantry = battlefield.Spawn<DogInfantry>(PlayerSlotId.One, new Vector2(100, 100));
        var harvester = battlefield.Spawn<DogHarvester>(PlayerSlotId.One, new Vector2(140, 100));
        var distantInfantry = battlefield.Spawn<DogInfantry>(PlayerSlotId.One, new Vector2(320, 100));
        var hostile = battlefield.Spawn<CatTank>(PlayerSlotId.Two, new Vector2(500, 100));

        var mixedPreviewCount = battlefield.CountSelectionRectCandidates(PlayerSlotId.One, new Rect2(80, 80, 100, 80));
        var mixedRectCount = battlefield.SelectRect(PlayerSlotId.One, new Rect2(80, 80, 100, 80), additive: false);
        if (mixedPreviewCount != mixedRectCount
            || mixedRectCount != 2
            || !infantry.Selected
            || !harvester.Selected
            || distantInfantry.Selected
            || hostile.Selected)
        {
            throw new InvalidOperationException("selection preview and commit should preserve focused economy inclusion without selecting hostile units");
        }

        AssertRuntimeRectParity(battlefield, new Rect2(132, 92, 16, 16), 1, "economy-only", harvester);
        AssertRuntimeRectParity(battlefield, new Rect2(312, 92, 16, 16), 1, "combat-only", distantInfantry);
        AssertRuntimeRectParity(battlefield, new Rect2(600, 600, 20, 20), 0, "empty");
        AssertRuntimeRectParity(battlefield, new Rect2(492, 92, 16, 16), 0, "hostile-only");

        var selectedByIds = battlefield.SelectUnitsByIds(PlayerSlotId.One, [distantInfantry.Id, hostile.Id, infantry.Id, infantry.Id]);
        if (selectedByIds.Count != 2
            || selectedByIds[0].Id != infantry.Id
            || selectedByIds[1].Id != distantInfantry.Id
            || !infantry.Selected
            || harvester.Selected
            || !distantInfantry.Selected
            || hostile.Selected)
        {
            throw new InvalidOperationException("id selection should filter by player slot, de-duplicate, and preserve stable id order");
        }

        var additiveCount = battlefield.SelectRect(PlayerSlotId.One, new Rect2(130, 88, 24, 24), additive: true);
        if (additiveCount != 3 || !infantry.Selected || !harvester.Selected || !distantInfantry.Selected || hostile.Selected)
        {
            throw new InvalidOperationException("additive selection rect should reuse existing selection and add matching units");
        }

        return 7;
    }

    private static void AssertRuntimeRectParity(
        UnitBattlefield battlefield,
        Rect2 rect,
        int expectedCount,
        string label,
        params UnitInstance[] expectedUnits)
    {
        var previewCount = battlefield.CountSelectionRectCandidates(PlayerSlotId.One, rect);
        var selectedCount = battlefield.SelectRect(PlayerSlotId.One, rect, additive: false);
        if (previewCount != expectedCount || selectedCount != previewCount)
        {
            throw new InvalidOperationException($"{label} selection preview/commit mismatch: preview {previewCount}, selected {selectedCount}, expected {expectedCount}");
        }

        foreach (var unit in expectedUnits)
        {
            if (!unit.Selected)
            {
                throw new InvalidOperationException($"{label} should select expected unit {unit.Id}");
            }
        }
    }

}
