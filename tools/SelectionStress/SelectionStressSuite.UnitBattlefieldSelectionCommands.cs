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

    private static int RunSelectionCandidateCountScenarios()
    {
        var legacyState = new GameState();
        legacyState.Units.Clear();
        legacyState.Buildings.Clear();
        legacyState.ResourceFields.Clear();
        legacyState.Projectiles.Clear();
        legacyState.Beams.Clear();
        legacyState.Units.AddRange(
        [
            SelectionStressUnit(1, "generic.light_tank", Owner.Player, new Vector2(100, 100)),
            SelectionStressUnit(2, "generic.harvester", Owner.Player, new Vector2(140, 100)),
            SelectionStressUnit(3, "generic.light_tank", Owner.Enemy, new Vector2(110, 100)),
        ]);
        var focusedRect = new Rect2(80, 80, 100, 80);
        var legacyCandidates = legacyState.CountSelectionRectCandidates(focusedRect);
        var legacySelected = legacyState.SelectRect(focusedRect, additive: false);
        if (legacyCandidates != legacySelected || legacyCandidates != 2)
        {
            throw new InvalidOperationException("legacy drag feedback candidates must mirror filtered SelectRect unit selection");
        }

        var runtimeBattlefield = new UnitBattlefield();
        runtimeBattlefield.Spawn<DogInfantry>(PlayerSlotId.One, new Vector2(100, 100));
        runtimeBattlefield.Spawn<DogHarvester>(PlayerSlotId.One, new Vector2(140, 100));
        runtimeBattlefield.Spawn<CatTank>(PlayerSlotId.Two, new Vector2(110, 100));
        var runtimeCandidates = runtimeBattlefield.CountSelectionRectCandidates(PlayerSlotId.One, focusedRect);
        var runtimeSelected = runtimeBattlefield.SelectRect(PlayerSlotId.One, focusedRect, additive: false);
        if (runtimeCandidates != runtimeSelected || runtimeCandidates != 2)
        {
            throw new InvalidOperationException("runtime drag feedback candidates must mirror filtered SelectRect unit selection");
        }

        return 2;
    }

    private static UnitModel SelectionStressUnit(int id, string designId, Owner owner, Vector2 position)
    {
        return new UnitModel
        {
            Id = id,
            DesignId = designId,
            Owner = owner,
            FactionId = owner == Owner.Player ? FactionId.Dog : FactionId.Cat,
            Position = position,
            AnchorPosition = position,
            Hp = UnitDesignCatalog.Spec(designId).Stats.MaxHp,
        };
    }
}
