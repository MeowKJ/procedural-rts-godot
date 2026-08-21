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

        var invalidMove = QaPlayerCommandDriver.MoveSubjects(
            battlefield,
            PlayerSlotId.One,
            [hostileUnit.EntityId, attackerB.EntityId, attackerA.EntityId, attackerA.EntityId],
            new Vector2(240, 240));
        if (invalidMove.AcceptedCount != 0 || invalidMove.Commands[0].Error != CommandGatewayValidationError.InvalidSubject)
        {
            throw new InvalidOperationException("explicit move payload should reject duplicate subjects at the gateway boundary");
        }

        var moveResult = QaPlayerCommandDriver.MoveSubjects(
            battlefield,
            PlayerSlotId.One,
            [attackerB.EntityId, attackerA.EntityId],
            new Vector2(240, 240));
        if (moveResult.AcceptedCount != 1)
        {
            throw new InvalidOperationException($"explicit move should submit two owned subjects through one gateway command, accepted {moveResult.AcceptedCount}");
        }

        RequireCommandDelta(battlefield, 1, () => QaPlayerCommandDriver.AttackSelection(battlefield, PlayerSlotId.One, hostileUnit), "selected unit attack");
        var explicitAttackResult = QaPlayerCommandDriver.AttackSubjects(
            battlefield,
            PlayerSlotId.One,
            [attackerB.EntityId, hostileUnit.EntityId, attackerA.EntityId],
            hostileUnit.EntityId);
        if (explicitAttackResult.AcceptedCount != 1)
        {
            throw new InvalidOperationException($"explicit unit attack should submit through one gateway command, accepted {explicitAttackResult.AcceptedCount}");
        }

        if (QaPlayerCommandDriver.AttackBuildingSelection(battlefield, PlayerSlotId.One, hostileBuilding.Id).AcceptedCount != 1)
        {
            throw new InvalidOperationException("selected building attack should find buffered attackers");
        }

        var explicitBuildingAttackResult = QaPlayerCommandDriver.AttackSubjects(
            battlefield,
            PlayerSlotId.One,
            [attackerA.EntityId, attackerB.EntityId],
            battlefield.BuildingEntityIdByTargetId(hostileBuilding.Id) ?? default,
            CombatTargetKind.Building);
        if (explicitBuildingAttackResult.AcceptedCount != 1)
        {
            throw new InvalidOperationException($"explicit building attack should submit through one gateway command, accepted {explicitBuildingAttackResult.AcceptedCount}");
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
