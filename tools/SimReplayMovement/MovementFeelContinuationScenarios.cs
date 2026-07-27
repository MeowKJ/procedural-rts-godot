static partial class Program
{
    private static readonly Vector2 MovementFeelContinuationTarget = new(1480, 820);

    private static void RunMovementFeelAttackMoveContinuationScenario()
    {
        var battlefield = new UnitBattlefield { WorldSize = MovementFeelWorldSize };
        battlefield.Relations.Set(PlayerSlotId.One, PlayerSlotId.Two, PlayerRelation.Hostile);
        var subject = battlefield.Spawn<DogGuardTank>(PlayerSlotId.One, new Vector2(420, 820), 0);
        var hostile = battlefield.Spawn<CatBasic>(PlayerSlotId.Two, new Vector2(690, 820), Mathf.Pi);
        var hostileEntity = battlefield.UnitEntityByInstanceId(hostile.Id)
            ?? throw new InvalidOperationException("movement-feel hostile entity should exist");
        hostileEntity.Components.Set(hostileEntity.Components.Require<HealthComponentState>() with { Hp = 18 });
        battlefield.Update(0);
        var subjects = new[] { subject };

        var selected = battlefield.SelectUnitsByIds(PlayerSlotId.One, new[] { subject.Id }).Count;
        Assert(selected == 1, $"movement-feel continuation selected {selected}/1: {MovementFeelTrace(battlefield, subjects, -1, 0, MovementFeelContinuationTarget, [])}");

        var commandTick = battlefield.AppliedInputCommandCount + 1;
        battlefield.CommandMoveSelected(PlayerSlotId.One, MovementFeelContinuationTarget, MovementFeelWorldSize, MoveCommandMode.Attack);
        Assert(
            battlefield.AppliedInputCommandCount == commandTick,
            $"movement-feel continuation attack-move ignored: commandTick={commandTick}, trace={MovementFeelTrace(battlefield, subjects, commandTick, 0, MovementFeelContinuationTarget, [])}");
        AssertAllSubjectsKeepAttackMoveOrder(battlefield, subjects, commandTick, MovementFeelContinuationTarget, simTick: 0);

        var killedTick = -1;
        var resumedTick = -1;
        var positionAtKill = subject.Position;
        for (var simTick = 1; simTick <= 360; simTick++)
        {
            battlefield.Update(MovementFeelDelta);
            if (killedTick < 0 && !ContainsUnit(battlefield, hostile.Id))
            {
                killedTick = simTick;
                positionAtKill = subject.Position;
            }

            if (killedTick >= 0
                && resumedTick < 0
                && subject.AttackTargetId is null
                && subject.MoveTarget is not null
                && subject.PlayerIntentTarget is { } intent
                && intent.DistanceSquaredTo(MovementFeelContinuationTarget) <= 1f
                && subject.CommandVisualTarget is { } visual
                && visual.DistanceSquaredTo(MovementFeelContinuationTarget) <= 1f)
            {
                resumedTick = simTick;
            }
        }

        Assert(
            killedTick > 0,
            $"movement-feel continuation did not kill transient hostile: commandTick={commandTick}, hostile={hostile.Id}, trace={MovementFeelTrace(battlefield, subjects, commandTick, 360, MovementFeelContinuationTarget, [])}");
        Assert(
            resumedTick >= killedTick && resumedTick <= killedTick + 4,
            $"movement-feel continuation did not resume attack-move promptly after target removal: killedTick={killedTick}, resumedTick={resumedTick}, trace={MovementFeelTrace(battlefield, subjects, commandTick, 360, MovementFeelContinuationTarget, [])}");

        var distanceAtKill = positionAtKill.DistanceTo(MovementFeelContinuationTarget);
        var finalDistance = subject.Position.DistanceTo(MovementFeelContinuationTarget);
        Assert(
            finalDistance <= distanceAtKill - 48f,
            $"movement-feel continuation did not keep moving after target removal: killedTick={killedTick}, resumedTick={resumedTick}, distanceAtKill={distanceAtKill:0.0}, finalDistance={finalDistance:0.0}, trace={MovementFeelTrace(battlefield, subjects, commandTick, 360, MovementFeelContinuationTarget, [])}");
        AssertAllSubjectsKeepAttackMoveIntent(battlefield, subjects, commandTick, MovementFeelContinuationTarget, simTick: 360);

        Console.WriteLine($"OK [movement-feel continuation fixed]: commandTick {commandTick}, hostile {hostile.Id} removed at simTick {killedTick}, resumed at {resumedTick}, progressed {distanceAtKill - finalDistance:0.0}px toward {FormatVector(MovementFeelContinuationTarget)}, trace {MovementFeelTrace(battlefield, subjects, commandTick, 360, MovementFeelContinuationTarget, [])}.");
    }

    private static bool ContainsUnit(UnitBattlefield battlefield, int unitId)
    {
        foreach (var unit in battlefield.Units)
        {
            if (unit.Id == unitId)
            {
                return true;
            }
        }

        return false;
    }
}
