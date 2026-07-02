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
        var hostile = battlefield.Spawn<CatTank>(PlayerSlotId.Two, new Vector2(110, 100));

        var mixedRectCount = battlefield.SelectRect(PlayerSlotId.One, new Rect2(80, 80, 100, 80), additive: false);
        if (mixedRectCount != 2 || !infantry.Selected || !harvester.Selected || distantInfantry.Selected || hostile.Selected)
        {
            throw new InvalidOperationException("selection rect should preserve focused economy inclusion without selecting hostile units");
        }

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

        return 3;
    }
}
