using System.Text;

static partial class Program
{
    private const int MovementFeelSubjectCount = 3;
    private const double MovementFeelDelta = 1.0 / 30.0;
    private static readonly Vector2 MovementFeelWorldSize = new(2400, 1600);
    private static readonly Vector2 MovementFeelAttackMoveTarget = new(1680, 820);
    private static readonly Vector2 MovementFeelReplacementFinalTarget = new(520, 1320);

    static void RunMovementFeelReproductionScenario()
    {
        RunMovementFeelAttackMoveScenario();
        RunMovementFeelReplacementScenario();
    }

    private static void RunMovementFeelAttackMoveScenario()
    {
        var battlefield = BuildMovementFeelBattlefield(out var subjects, out var hostile);
        var subjectIds = UnitIds(subjects);
        var selected = battlefield.SelectUnitsByIds(PlayerSlotId.One, subjectIds).Count;
        Assert(selected == MovementFeelSubjectCount, $"movement-feel attack-move selected {selected}/{MovementFeelSubjectCount}: {MovementFeelTrace(battlefield, subjects, -1, 0, MovementFeelAttackMoveTarget, [])}");

        var commandTick = battlefield.AppliedInputCommandCount + 1;
        battlefield.CommandMoveSelected(PlayerSlotId.One, MovementFeelAttackMoveTarget, MovementFeelWorldSize, MoveCommandMode.Attack);
        Assert(
            battlefield.AppliedInputCommandCount == commandTick,
            $"movement-feel attack-move command was not accepted: expected commandTick {commandTick}, applied {battlefield.AppliedInputCommandCount}, target={FormatVector(MovementFeelAttackMoveTarget)}, trace={MovementFeelTrace(battlefield, subjects, commandTick, 0, MovementFeelAttackMoveTarget, [])}");
        AssertAllSubjectsKeepAttackMoveOrder(battlefield, subjects, commandTick, MovementFeelAttackMoveTarget, simTick: 0);

        var previousFacings = InitialFacings(subjects);
        var maxFacingDeltas = new float[subjects.Count];
        var firstAcquireTick = -1;
        var firstFireTick = -1;
        var fireCount = 0;
        var simTick = 0;
        battlefield.WeaponFired += fired =>
        {
            if (IsSubjectEntity(subjects, fired.Source))
            {
                fireCount++;
                firstFireTick = firstFireTick < 0 ? simTick : firstFireTick;
            }
        };

        for (simTick = 1; simTick <= 360; simTick++)
        {
            battlefield.Update(MovementFeelDelta);
            UpdateFacingDeltas(subjects, previousFacings, maxFacingDeltas);
            if (firstAcquireTick < 0 && AnySubjectTargets(subjects, hostile.Id))
            {
                firstAcquireTick = simTick;
            }
        }

        var maxFacingDelta = Max(maxFacingDeltas);
        Assert(
            firstAcquireTick > 0,
            $"movement-feel attack-move did not acquire nearby hostile: commandTick={commandTick}, target={FormatVector(MovementFeelAttackMoveTarget)}, hostile={hostile.Id}, trace={MovementFeelTrace(battlefield, subjects, commandTick, simTick, MovementFeelAttackMoveTarget, maxFacingDeltas)}");
        Assert(
            fireCount > 0,
            $"movement-feel attack-move did not fire after acquire: commandTick={commandTick}, acquireSimTick={firstAcquireTick}, hostile={hostile.Id}, trace={MovementFeelTrace(battlefield, subjects, commandTick, simTick, MovementFeelAttackMoveTarget, maxFacingDeltas)}");

        Console.WriteLine($"OK [movement-feel attack-move repro]: commandTick {commandTick}, target {FormatVector(MovementFeelAttackMoveTarget)}, hostile {hostile.Id}, acquireSimTick {firstAcquireTick}, firstFireSimTick {firstFireTick}, fires {fireCount}, maxFacingDelta {maxFacingDelta:0.000}rad, trace {MovementFeelTrace(battlefield, subjects, commandTick, simTick, MovementFeelAttackMoveTarget, maxFacingDeltas)}.");
    }

    private static void RunMovementFeelReplacementScenario()
    {
        var battlefield = BuildMovementFeelBattlefield(out var subjects, out var hostile);
        var subjectIds = UnitIds(subjects);
        var selected = battlefield.SelectUnitsByIds(PlayerSlotId.One, subjectIds).Count;
        Assert(selected == MovementFeelSubjectCount, $"movement-feel replacement selected {selected}/{MovementFeelSubjectCount}: {MovementFeelTrace(battlefield, subjects, -1, 0, MovementFeelReplacementFinalTarget, [])}");

        var firstMoveTarget = new Vector2(900, 760);
        var firstMoveTick = battlefield.AppliedInputCommandCount + 1;
        battlefield.CommandMoveSelected(PlayerSlotId.One, firstMoveTarget, MovementFeelWorldSize, MoveCommandMode.Direct);
        Assert(battlefield.AppliedInputCommandCount == firstMoveTick, $"movement-feel replacement first move ignored: commandTick={firstMoveTick}, trace={MovementFeelTrace(battlefield, subjects, firstMoveTick, 0, firstMoveTarget, [])}");
        StepMovementFeelBattlefield(battlefield, 18);

        var attackMoveTick = battlefield.AppliedInputCommandCount + 1;
        battlefield.CommandMoveSelected(PlayerSlotId.One, MovementFeelAttackMoveTarget, MovementFeelWorldSize, MoveCommandMode.Attack);
        Assert(battlefield.AppliedInputCommandCount == attackMoveTick, $"movement-feel replacement attack-move ignored: commandTick={attackMoveTick}, trace={MovementFeelTrace(battlefield, subjects, attackMoveTick, 18, MovementFeelAttackMoveTarget, [])}");
        StepMovementFeelBattlefield(battlefield, 72);

        var finalMoveTick = battlefield.AppliedInputCommandCount + 1;
        battlefield.CommandMoveSelected(PlayerSlotId.One, MovementFeelReplacementFinalTarget, MovementFeelWorldSize, MoveCommandMode.Direct);
        Assert(
            battlefield.AppliedInputCommandCount == finalMoveTick,
            $"movement-feel replacement final move ignored: commandTick={finalMoveTick}, trace={MovementFeelTrace(battlefield, subjects, finalMoveTick, 90, MovementFeelReplacementFinalTarget, [])}");

        var previousFacings = InitialFacings(subjects);
        var maxFacingDeltas = new float[subjects.Count];
        var stalePathGoals = new[] { firstMoveTarget, MovementFeelAttackMoveTarget };
        AssertNoSubjectEntityTargets(battlefield, subjects, hostile.EntityId, finalMoveTick, simTick: 0, MovementFeelReplacementFinalTarget, maxFacingDeltas);
        AssertNoSubjectStalePathGoal(battlefield, subjects, stalePathGoals, finalMoveTick, simTick: 0, MovementFeelReplacementFinalTarget, maxFacingDeltas);
        for (var simTick = 1; simTick <= 90; simTick++)
        {
            battlefield.Update(MovementFeelDelta);
            UpdateFacingDeltas(subjects, previousFacings, maxFacingDeltas);
            AssertNoSubjectEntityTargets(battlefield, subjects, hostile.EntityId, finalMoveTick, simTick, MovementFeelReplacementFinalTarget, maxFacingDeltas);
            AssertNoSubjectStalePathGoal(battlefield, subjects, stalePathGoals, finalMoveTick, simTick, MovementFeelReplacementFinalTarget, maxFacingDeltas);
        }

        AssertAllSubjectsKeepDirectReplacementOrder(
            battlefield,
            subjects,
            finalMoveTick,
            MovementFeelReplacementFinalTarget,
            simTick: 90,
            maxFacingDeltas);

        Console.WriteLine($"OK [movement-feel replacement fixed]: commandTicks {firstMoveTick}->{attackMoveTick}->{finalMoveTick}, finalTarget {FormatVector(MovementFeelReplacementFinalTarget)}, stale target/path cleared for hostile {hostile.Id}, trace {MovementFeelTrace(battlefield, subjects, finalMoveTick, 90, MovementFeelReplacementFinalTarget, maxFacingDeltas)}.");
    }

    private static UnitBattlefield BuildMovementFeelBattlefield(out IReadOnlyList<UnitInstance> subjects, out UnitInstance hostile)
    {
        var battlefield = new UnitBattlefield { WorldSize = MovementFeelWorldSize };
        battlefield.Relations.Set(PlayerSlotId.One, PlayerSlotId.Two, PlayerRelation.Hostile);
        subjects =
        [
            battlefield.Spawn<DogGuardTank>(PlayerSlotId.One, new Vector2(420, 760), 0),
            battlefield.Spawn<DogRocket>(PlayerSlotId.One, new Vector2(380, 820), 0),
            battlefield.Spawn<DogPatrolVehicle>(PlayerSlotId.One, new Vector2(430, 880), 0),
        ];
        hostile = battlefield.Spawn<CatTank>(PlayerSlotId.Two, new Vector2(1010, 820), Mathf.Pi);
        battlefield.Spawn<CatScoutCar>(PlayerSlotId.Two, new Vector2(1100, 910), Mathf.Pi);
        return battlefield;
    }

    private static int[] UnitIds(IReadOnlyList<UnitInstance> units)
    {
        var ids = new int[units.Count];
        for (var index = 0; index < units.Count; index++)
        {
            ids[index] = units[index].Id;
        }

        return ids;
    }

    private static float[] InitialFacings(IReadOnlyList<UnitInstance> units)
    {
        var facings = new float[units.Count];
        for (var index = 0; index < units.Count; index++)
        {
            facings[index] = units[index].Facing;
        }

        return facings;
    }

    private static void UpdateFacingDeltas(IReadOnlyList<UnitInstance> units, float[] previousFacings, float[] maxFacingDeltas)
    {
        for (var index = 0; index < units.Count; index++)
        {
            var delta = MathF.Abs(Mathf.AngleDifference(previousFacings[index], units[index].Facing));
            maxFacingDeltas[index] = MathF.Max(maxFacingDeltas[index], delta);
            previousFacings[index] = units[index].Facing;
        }
    }

    private static float Max(IReadOnlyList<float> values)
    {
        var max = 0f;
        for (var index = 0; index < values.Count; index++)
        {
            max = MathF.Max(max, values[index]);
        }

        return max;
    }

    private static void StepMovementFeelBattlefield(UnitBattlefield battlefield, int ticks)
    {
        for (var tick = 0; tick < ticks; tick++)
        {
            battlefield.Update(MovementFeelDelta);
        }
    }

    private static bool AnySubjectTargets(IReadOnlyList<UnitInstance> subjects, int targetId)
    {
        foreach (var subject in subjects)
        {
            if (subject.AttackTargetId == targetId)
            {
                return true;
            }
        }

        return false;
    }

    private static void AssertNoSubjectEntityTargets(
        UnitBattlefield battlefield,
        IReadOnlyList<UnitInstance> subjects,
        EntityId targetEntityId,
        int commandTick,
        int simTick,
        Vector2 target,
        IReadOnlyList<float> facingDeltas)
    {
        foreach (var subject in subjects)
        {
            if (battlefield.UnitEntityByInstanceId(subject.Id) is { } entity
                && entity.Components.TryGet<WeaponUserComponentState>(out var weapon)
                && weapon.AttackTarget == targetEntityId)
            {
                Fail($"movement-feel replacement retained stale entity attack target after final direct move: commandTick={commandTick}, simTick={simTick}, unit={subject.Id}, staleTarget={targetEntityId.Value}, trace={MovementFeelTrace(battlefield, subjects, commandTick, simTick, target, facingDeltas)}");
            }
        }
    }

    private static void AssertNoSubjectStalePathGoal(
        UnitBattlefield battlefield,
        IReadOnlyList<UnitInstance> subjects,
        IReadOnlyList<Vector2> staleGoals,
        int commandTick,
        int simTick,
        Vector2 target,
        IReadOnlyList<float> facingDeltas)
    {
        foreach (var subject in subjects)
        {
            if (battlefield.UnitEntityByInstanceId(subject.Id) is not { } entity
                || !entity.Components.TryGet<PathfindingComponentState>(out var path))
            {
                continue;
            }

            var goal = new Vector2(path.Goal.X, path.Goal.Y);
            foreach (var staleGoal in staleGoals)
            {
                if (goal.DistanceSquaredTo(staleGoal) <= 1f)
                {
                    Fail($"movement-feel replacement retained stale path goal after final direct move: commandTick={commandTick}, simTick={simTick}, unit={subject.Id}, staleGoal={FormatVector(staleGoal)}, trace={MovementFeelTrace(battlefield, subjects, commandTick, simTick, target, facingDeltas)}");
                }
            }
        }
    }

    private static bool IsSubjectEntity(IReadOnlyList<UnitInstance> subjects, EntityId entityId)
    {
        foreach (var subject in subjects)
        {
            if (subject.EntityId == entityId)
            {
                return true;
            }
        }

        return false;
    }

    private static void AssertAllSubjectsKeepAttackMoveOrder(
        UnitBattlefield battlefield,
        IReadOnlyList<UnitInstance> subjects,
        int commandTick,
        Vector2 target,
        int simTick)
    {
        foreach (var subject in subjects)
        {
            if (subject.MoveMode != MoveCommandMode.Attack
                || subject.CommandVisualTarget is not { } visualTarget
                || visualTarget.DistanceSquaredTo(target) > 1f
                || subject.PlayerIntentTarget is not { } intentTarget
                || intentTarget.DistanceSquaredTo(target) > 1f
                || subject.CommandPulse <= 0)
            {
                Fail($"movement-feel attack-move subject dropped command state: commandTick={commandTick}, target={FormatVector(target)}, unit={subject.Id}, trace={MovementFeelTrace(battlefield, subjects, commandTick, simTick, target, [])}");
            }
        }
    }

    private static void AssertAllSubjectsKeepDirectReplacementOrder(
        UnitBattlefield battlefield,
        IReadOnlyList<UnitInstance> subjects,
        int commandTick,
        Vector2 target,
        int simTick,
        IReadOnlyList<float> facingDeltas)
    {
        foreach (var subject in subjects)
        {
            if (subject.MoveMode != MoveCommandMode.Direct
                || subject.CommandVisualTarget is not { } visualTarget
                || visualTarget.DistanceSquaredTo(target) > 1f
                || subject.PlayerIntentTarget is not { } intentTarget
                || intentTarget.DistanceSquaredTo(target) > 1f)
            {
                Fail($"movement-feel replacement final order not retained: commandTick={commandTick}, target={FormatVector(target)}, unit={subject.Id}, trace={MovementFeelTrace(battlefield, subjects, commandTick, simTick, target, facingDeltas)}");
            }
        }
    }

    private static string MovementFeelTrace(
        UnitBattlefield battlefield,
        IReadOnlyList<UnitInstance> subjects,
        int commandTick,
        int simTick,
        Vector2 target,
        IReadOnlyList<float> facingDeltas)
    {
        var builder = new StringBuilder();
        builder.Append("commandTick=").Append(commandTick);
        builder.Append(" simTick=").Append(simTick);
        builder.Append(" target=").Append(FormatVector(target));
        for (var index = 0; index < subjects.Count; index++)
        {
            var subject = subjects[index];
            var delta = index < facingDeltas.Count ? facingDeltas[index] : 0;
            builder.Append(" | ");
            builder.Append("unit=").Append(subject.Id);
            builder.Append("/").Append(subject.Spec.Id);
            builder.Append(" pos=").Append(FormatVector(subject.Position));
            builder.Append(" vel=").Append(FormatVector(subject.Velocity));
            builder.Append(" moveMode=").Append(subject.MoveMode);
            builder.Append(" move=").Append(FormatVector(subject.MoveTarget));
            builder.Append(" intent=").Append(FormatVector(subject.PlayerIntentTarget));
            builder.Append(" visual=").Append(FormatVector(subject.CommandVisualTarget));
            builder.Append(" attack=").Append(subject.AttackTargetId?.ToString() ?? "none");
            builder.Append("/").Append(subject.AttackTargetKind);
            builder.Append("/manual=").Append(subject.AttackTargetIsManual);
            builder.Append(" facingDelta=").Append(delta.ToString("0.000"));
            builder.Append(" path=").Append(PathState(battlefield, subject));
        }

        return builder.ToString();
    }

    private static string PathState(UnitBattlefield battlefield, UnitInstance unit)
    {
        if (battlefield.UnitEntityByInstanceId(unit.Id) is not { } entity
            || !entity.Components.TryGet<PathfindingComponentState>(out var path))
        {
            return "none";
        }

        return $"{path.NextWaypointIndex}/{path.Waypoints.Count}@{path.Goal.X},{path.Goal.Y}";
    }

    private static string FormatVector(Vector2? vector)
    {
        return vector is { } value ? FormatVector(value) : "none";
    }

    private static string FormatVector(Vector2 vector)
    {
        return $"{vector.X:0.0},{vector.Y:0.0}";
    }
}
