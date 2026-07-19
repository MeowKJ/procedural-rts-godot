using Godot;
using ProceduralRts.Core;

static class UnitStanceGatewayProjectionQa
{
    public static void Assert()
    {
        AssertAcceptedProjection();
        AssertTooManySubjectsPreservesProjection();
        Require(ProjectSelectedUnitStance(NewBattlefield(), PlayerSlotId.One) == UnitStanceStripProjection.None,
            "an empty UnitDesign battlefield must project zero selected stance state");
    }

    private static void AssertAcceptedProjection()
    {
        var battlefield = NewBattlefield();
        var units = new[]
        {
            battlefield.Spawn("dog.guard_tank", PlayerSlotId.One, new Vector2(420, 420)),
            battlefield.Spawn("dog.rocket", PlayerSlotId.One, new Vector2(480, 420)),
        };
        battlefield.SelectUnitsByIds(PlayerSlotId.One, units.Select(unit => unit.Id));
        var aggressiveProjection = ProjectSelectedUnitStance(battlefield, PlayerSlotId.One);
        Require(aggressiveProjection.State == UnitStanceStripSelectionState.Uniform
            && aggressiveProjection.IsSelected(UnitStance.Aggressive),
            "live stance projection should start from the selected entities' Aggressive authority state");

        var subjects = battlefield.SelectedUnitEntityIds(PlayerSlotId.One);
        var commandsBefore = battlefield.AppliedInputCommandCount;
        var result = battlefield.SubmitLiveLocalPlayerCommand(
            PlayerSlotId.One,
            PlayerCommandKind.SetStance,
            PlayerCommandPayload.ForSubjects(subjects) with { Stance = UnitStance.Hold });
        Require(result.AcceptedCount == 1,
            "live SetStance should pass through the default gateway and real UnitBattlefield sink");
        Require(battlefield.AppliedInputCommandCount == commandsBefore + 1,
            "accepted live SetStance should apply exactly one authoritative input command");
        Require(units.All(unit => unit.Stance == UnitStance.Hold),
            "accepted live SetStance should update every selected entity stance");

        var holdProjection = ProjectSelectedUnitStance(battlefield, PlayerSlotId.One);
        Require(holdProjection.State == UnitStanceStripSelectionState.Uniform
            && holdProjection.SelectedUnitCount == units.Length
            && holdProjection.IsSelected(UnitStance.Hold),
            "accepted live SetStance should rebuild a uniform Hold projection from entities");
    }

    private static void AssertTooManySubjectsPreservesProjection()
    {
        const int subjectCount = 257;
        var battlefield = NewBattlefield();
        var units = new List<UnitInstance>(subjectCount);
        for (var index = 0; index < subjectCount; index++)
        {
            units.Add(battlefield.Spawn(
                "dog.guard_tank",
                PlayerSlotId.One,
                new Vector2(160 + index % 32 * 24, 160 + index / 32 * 24)));
        }

        battlefield.SelectUnitsByIds(PlayerSlotId.One, units.Select(unit => unit.Id));
        var subjects = battlefield.SelectedUnitEntityIds(PlayerSlotId.One);
        Require(subjects.Count == subjectCount, "TooManySubjects scenario must use 257 real selected entity ids");
        var projectionBefore = ProjectSelectedUnitStance(battlefield, PlayerSlotId.One);
        var commandsBefore = battlefield.AppliedInputCommandCount;
        var result = battlefield.SubmitLiveLocalPlayerCommand(
            PlayerSlotId.One,
            PlayerCommandKind.SetStance,
            PlayerCommandPayload.ForSubjects(subjects) with { Stance = UnitStance.Hold });

        RequireRejected(result, CommandGatewayValidationError.TooManySubjects,
            "257-subject live stance intent should reject at the default gateway limit");
        Require(battlefield.AppliedInputCommandCount == commandsBefore,
            "TooManySubjects must reject before the real UnitBattlefield sink applies a command");
        Require(units.All(unit => unit.Stance == UnitStance.Aggressive),
            "TooManySubjects must leave every real entity stance unchanged");
        var projectionAfter = ProjectSelectedUnitStance(battlefield, PlayerSlotId.One);
        Require(projectionAfter == projectionBefore && projectionAfter.IsSelected(UnitStance.Aggressive),
            "TooManySubjects must preserve the projection rebuilt from unchanged entities");
    }

    private static UnitBattlefield NewBattlefield()
    {
        var battlefield = new UnitBattlefield
        {
            WorldSize = MatchConfig.DefaultWorldSize,
        };
        battlefield.Relations.Set(PlayerSlotId.One, PlayerSlotId.Two, PlayerRelation.Hostile);
        battlefield.SetCredits(PlayerSlotId.One, 12000);
        battlefield.SetCredits(PlayerSlotId.Two, 12000);
        return battlefield;
    }

    private static UnitStanceStripProjection ProjectSelectedUnitStance(UnitBattlefield battlefield, PlayerSlotId playerSlotId)
    {
        var selectedCount = 0;
        UnitStance? uniformStance = null;
        var mixed = false;
        foreach (var unit in battlefield.Units)
        {
            if (unit.PlayerSlotId != playerSlotId || !unit.Selected || unit.Hp <= 0)
            {
                continue;
            }

            selectedCount++;
            if (uniformStance is null)
            {
                uniformStance = unit.Stance;
            }
            else if (uniformStance.Value != unit.Stance)
            {
                mixed = true;
            }
        }

        return UnitStanceStripProjection.FromSelection(mixed ? null : uniformStance, selectedCount);
    }

    private static void RequireRejected(CommandGatewayResult result, CommandGatewayValidationError expected, string message)
    {
        Require(result.RejectedCount == 1, message);
        Require(result.Commands[0].Error == expected, $"{message}: expected {expected}, got {result.Commands[0].Error}");
        Require(!string.IsNullOrWhiteSpace(result.Commands[0].Message),
            $"{message}: rejection should carry structured feedback");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
