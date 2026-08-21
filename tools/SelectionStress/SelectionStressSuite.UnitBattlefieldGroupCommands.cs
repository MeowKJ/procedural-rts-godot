using Godot;
using ProceduralRts.Core;
using ProceduralRts.Tools.Qa;

internal static partial class SelectionStressSuite
{
    private static int RunUnitBattlefieldGroupCommandSubjectScenarios()
    {
        var battlefield = new UnitBattlefield { WorldSize = new Vector2(1000, 1000) };
        var attackerA = battlefield.Spawn<DogInfantry>(PlayerSlotId.One, new Vector2(120, 120));
        var attackerB = battlefield.Spawn<DogInfantry>(PlayerSlotId.One, new Vector2(150, 120));
        var hostileUnit = battlefield.Spawn<CatTank>(PlayerSlotId.Two, new Vector2(340, 120));
        var hostileBuilding = battlefield.UpsertBuildingTarget(
            20,
            BuildingDesignIds.Headquarters,
            PlayerSlotId.Two,
            UnitFactionId.Cat,
            new Vector2(390, 130),
            0,
            500);

        battlefield.SelectUnitsByIds(PlayerSlotId.One, [attackerB.Id, hostileUnit.Id, attackerA.Id, attackerA.Id]);
        RequireCommandDelta(battlefield, 1, () => QaPlayerCommandDriver.MoveSelection(battlefield, PlayerSlotId.One, new Vector2(220, 220)), "selected move");

        var movedCount = battlefield.CommandMoveUnits(
            PlayerSlotId.One,
            [hostileUnit.Id, attackerB.Id, attackerA.Id, attackerA.Id],
            new Vector2(240, 240),
            new Vector2(1000, 1000));
        if (movedCount != 2)
        {
            throw new InvalidOperationException($"explicit move should submit two owned subjects, got {movedCount}");
        }

        RequireCommandDelta(battlefield, 1, () => QaPlayerCommandDriver.AttackSelection(battlefield, PlayerSlotId.One, hostileUnit), "selected unit attack");
        var explicitAttackCount = battlefield.CommandAttackUnits(PlayerSlotId.One, [attackerB.Id, hostileUnit.Id, attackerA.Id], hostileUnit);
        if (explicitAttackCount != 2)
        {
            throw new InvalidOperationException($"explicit unit attack should submit two owned attackers, got {explicitAttackCount}");
        }

        if (QaPlayerCommandDriver.AttackBuildingSelection(battlefield, PlayerSlotId.One, hostileBuilding.Id).AcceptedCount != 1)
        {
            throw new InvalidOperationException("selected building attack should find buffered attackers");
        }

        var explicitBuildingAttackCount = battlefield.CommandAttackUnits(PlayerSlotId.One, [attackerA.Id, attackerB.Id], hostileBuilding.Id);
        if (explicitBuildingAttackCount != 2)
        {
            throw new InvalidOperationException($"explicit building attack should submit two owned attackers, got {explicitBuildingAttackCount}");
        }

        RequireCommandDelta(battlefield, 1, () => QaPlayerCommandDriver.StopSelection(battlefield, PlayerSlotId.One), "selected stop");
        var stanceResult = QaPlayerCommandDriver.SetSelectionStance(battlefield, PlayerSlotId.One, UnitStance.Hold);
        if (stanceResult.AcceptedCount != 1 || attackerA.Stance != UnitStance.Hold || attackerB.Stance != UnitStance.Hold)
        {
            throw new InvalidOperationException($"selected stance should submit two armed units through one gateway command, accepted {stanceResult.AcceptedCount}");
        }

        return 8;
    }

    private static void RequireCommandDelta(UnitBattlefield battlefield, int expectedDelta, Action command, string label)
    {
        var before = battlefield.AppliedInputCommandCount;
        command();
        var delta = battlefield.AppliedInputCommandCount - before;
        if (delta != expectedDelta)
        {
            throw new InvalidOperationException($"{label} expected command delta {expectedDelta}, got {delta}");
        }
    }
}
