using Godot;

static partial class Program
{
    private static void AssertPlayerBuildGatewayCardinalReplayHashes()
    {
        var point = new PlayerCommandPoint(559.75f, 320.25f);
        AssertDirectBuildSinkRejectsFacingWithoutMutation(
            point,
            new PlayerCommandBuildFacing(1, 4),
            "schema v1 quarter-turn 4");
        AssertDirectBuildSinkRejectsFacingWithoutMutation(
            point,
            new PlayerCommandBuildFacing(2, 0),
            "unknown schema version");

        var expectedFacings = new[] { 0f, MathF.PI * 0.5f, MathF.PI, MathF.PI * 1.5f };
        for (var quarterTurns = 0; quarterTurns < expectedFacings.Length; quarterTurns++)
        {
            var battlefield = CreatePlayerBuildGatewayBattlefield();
            var command = new PlayerCommand(
                PlayerSlotId.One,
                1,
                1,
                PlayerCommandKind.Build,
                PlayerCommandPayload.ForBuild(
                    BuildingDesignIds.PowerPlant,
                    point.X,
                    point.Y,
                    quarterTurns));
            var enqueued = battlefield.TryEnqueue(command, out var envelope, out var error, out var message);
            Assert(enqueued && error == CommandGatewayValidationError.None,
                $"schema v1 cardinal Build {quarterTurns} should enqueue; got {error}: {message}");
            Assert(envelope?.Command is StartConstructionEntityCommand build
                && build.Position == new Vector2(point.X, point.Y)
                && build.Facing == expectedFacings[quarterTurns],
                $"schema v1 cardinal Build {quarterTurns} should preserve the desired point and map to canonical facing {expectedFacings[quarterTurns]}");
        }

        var legacy = PlayerBuildGatewayCheckpoints(PlayerCommandPayload.ForSpec(BuildingDesignIds.PowerPlant) with
        {
            HasTargetPoint = true,
            TargetPoint = point,
        });
        var versionOneZero = PlayerBuildGatewayCheckpoints(
            PlayerCommandPayload.ForBuild(BuildingDesignIds.PowerPlant, point.X, point.Y, quarterTurns: 0));
        var versionOneQuarterTurnA = PlayerBuildGatewayCheckpoints(
            PlayerCommandPayload.ForBuild(BuildingDesignIds.PowerPlant, point.X, point.Y, quarterTurns: 1));
        var versionOneQuarterTurnB = PlayerBuildGatewayCheckpoints(
            PlayerCommandPayload.ForBuild(BuildingDesignIds.PowerPlant, point.X, point.Y, quarterTurns: 1));

        Assert(legacy.SequenceEqual(versionOneZero),
            "legacy/default Build and schema v1 quarter-turn 0 should retain identical deterministic checkpoints");
        Assert(versionOneQuarterTurnA.SequenceEqual(versionOneQuarterTurnB),
            "identical schema v1 quarter-turn command streams should produce identical deterministic checkpoints");
        Assert(versionOneZero[^1] != versionOneQuarterTurnA[^1],
            "schema v1 0-degree and 90-degree Build outcomes should produce different final state hashes");
    }

    private static void AssertDirectBuildSinkRejectsFacingWithoutMutation(
        PlayerCommandPoint point,
        PlayerCommandBuildFacing invalidFacing,
        string label)
    {
        var battlefield = CreatePlayerBuildGatewayBattlefield();
        var appliedBefore = battlefield.AppliedInputCommandCount;
        var creditsBefore = battlefield.Credits(PlayerSlotId.One);
        var entitiesBefore = battlefield.EntityWorld.Count;
        var hashBefore = battlefield.EntityWorld.DeterministicStateHash();
        var invalidCommand = new PlayerCommand(
            PlayerSlotId.One,
            1,
            1,
            PlayerCommandKind.Build,
            PlayerCommandPayload.ForBuild(BuildingDesignIds.PowerPlant, point.X, point.Y, quarterTurns: 0) with
            {
                BuildFacing = invalidFacing,
            });

        var enqueued = battlefield.TryEnqueue(
            invalidCommand,
            out var envelope,
            out var error,
            out var message);
        Assert(!enqueued
            && envelope is null
            && error == CommandGatewayValidationError.InvalidPayloadShape
            && message == PlayerCommandBuildFacing.InvalidPayloadMessage,
            $"direct Build sink should reject {label} with the stable payload-shape result");
        Assert(battlefield.AppliedInputCommandCount == appliedBefore
            && battlefield.Credits(PlayerSlotId.One) == creditsBefore
            && battlefield.EntityWorld.Count == entitiesBefore
            && battlefield.EntityWorld.DeterministicStateHash() == hashBefore,
            $"direct Build sink rejection for {label} must not mutate tick, credits, entities, or deterministic state");

        var validCommand = invalidCommand with
        {
            Payload = PlayerCommandPayload.ForBuild(
                BuildingDesignIds.PowerPlant,
                point.X,
                point.Y,
                quarterTurns: 1),
        };
        var validEnqueued = battlefield.TryEnqueue(
            validCommand,
            out var validEnvelope,
            out var validError,
            out var validMessage);
        Assert(validEnqueued
            && validError == CommandGatewayValidationError.None
            && validEnvelope?.Command is StartConstructionEntityCommand validBuild
            && validBuild.Tick == 1
            && battlefield.AppliedInputCommandCount == appliedBefore + 1,
            $"a valid Build should still use the first command tick after rejected {label}; got {validError}: {validMessage}");
    }

    private static ulong[] PlayerBuildGatewayCheckpoints(PlayerCommandPayload payload)
    {
        var battlefield = CreatePlayerBuildGatewayBattlefield();
        var checkpoints = new[] { battlefield.EntityWorld.DeterministicStateHash(), 0UL };
        var command = new PlayerCommand(PlayerSlotId.One, 1, 1, PlayerCommandKind.Build, payload);
        var submission = new CommandGatewaySubmission(
            new PlayerControllerId("build-replay"),
            PlayerControllerKind.QaAgent,
            [PlayerSlotId.One],
            CurrentTick: 0);
        var result = new CommandGateway().Submit(submission, [command], battlefield);
        Assert(result.AcceptedCount == 1 && result.RejectedCount == 0,
            $"Build replay command should pass gateway and simulation sink; got {result.Commands[0].Error}: {result.Commands[0].Message}");
        checkpoints[1] = battlefield.EntityWorld.DeterministicStateHash();
        return checkpoints;
    }

    private static UnitBattlefield CreatePlayerBuildGatewayBattlefield()
    {
        var battlefield = new UnitBattlefield
        {
            WorldSize = new Vector2(1200, 900),
        };
        battlefield.EntityWorld.WorldWidth = battlefield.WorldSize.X;
        battlefield.EntityWorld.WorldHeight = battlefield.WorldSize.Y;
        battlefield.SetCredits(PlayerSlotId.One, 5000);
        var hqSpec = BuildSpecCatalog.For(BuildingDesignIds.Headquarters);
        battlefield.UpsertBuildingTarget(
            1,
            hqSpec.Kind,
            PlayerSlotId.One,
            UnitFactionId.Dog,
            new Vector2(320, 320),
            0,
            hqSpec.MaxHp,
            powered: true);
        return battlefield;
    }
}
