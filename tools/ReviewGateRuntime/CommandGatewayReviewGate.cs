static class CommandGatewayReviewGate
{
    public static void Check(string root, GateResult result)
    {
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "players", "CommandGateway.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "players", "CommandGatewayResults.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "players", "PlayerCommandPayload.cs");

        var gateway = ReviewGateSource.Read(root, "scripts", "core", "players", "CommandGateway.cs");
        var results = ReviewGateSource.Read(root, "scripts", "core", "players", "CommandGatewayResults.cs");
        var payload = ReviewGateSource.Read(root, "scripts", "core", "players", "PlayerCommandPayload.cs");
        var contracts = ReviewGateSource.Read(root, "scripts", "core", "players", "PlayerControllerContracts.cs");

        RequireText(contracts, "PlayerCommandPayload Payload", "PlayerCommand must carry a value payload for gateway shape validation.", result);
        RequireText(gateway, "public sealed partial class CommandGateway", "CommandGateway shell must exist as a core type.", result);
        RequireText(gateway, "ICommandGatewayEntityCommandSink", "CommandGateway must reserve an EntityCommandBuffer sink boundary.", result);
        RequireText(gateway, "SandboxCommandsEnabled", "CommandGateway must gate sandbox-only commands.", result);
        RequireText(gateway, "_lastSequenceByController", "CommandGateway must track per-controller client sequence.", result);
        RequireText(gateway, "ControllerDoesNotOwnSlot", "CommandGateway must validate controller slot rights.", result);
        RequireText(gateway, "private static bool ControlsSlot(IReadOnlyList<PlayerSlotId> slots, PlayerSlotId slot)", "CommandGateway slot ownership must keep the explicit scan helper.", result);
        RequireText(results, "public sealed record PlayerCommandResult", "Gateway must return structured per-command results.", result);
        RequireText(results, "CommandGatewayValidationError", "Gateway must expose structured validation errors.", result);
        RequireText(results, "private readonly int _acceptedCount = CountAccepted(Commands);", "CommandGatewayResult must cache accepted counts once.", result);
        RequireText(results, "private static int CountAccepted(IReadOnlyList<PlayerCommandResult> commands)", "CommandGatewayResult accepted counts must scan explicitly.", result);
        RequireText(payload, "PlayerCommandPoint", "Gateway payload must use Godot-free point data.", result);
        RequireText(payload, "IReadOnlyList<EntityId>?", "Gateway payload subjects must be read-only entity ids.", result);
        RequireText(payload, "PlayerCommandBuildFacing(int Version, int QuarterTurns)", "Build facing must use the bounded versioned quarter-turn value object.", result);
        RequireText(payload, "TryResolveCanonicalRadians(out float radians)", "Build facing validation and canonical-radian mapping must share one resolver.", result);
        RequireText(payload, "return QuarterTurns == 0;", "Shared Build-facing resolver must accept legacy v0 only at quarter-turn zero.", result);
        RequireText(payload, "Version != 1 || QuarterTurns is < 0 or > 3", "Shared Build-facing resolver must reject unknown versions and non-cardinal schema v1 values.", result);
        RequireText(payload, "PlayerCommandBuildFacing BuildFacing = default", "Build facing must remain an optional trailing payload field for legacy source compatibility.", result);
        RequireText(payload, "PlayerCommandPayload ForBuild(string specId, float x, float y, int quarterTurns)", "Build payloads must expose a focused schema v1 writer factory.", result);
        ForbidText(payload, "CanonicalRadians =>", "Build facing must not expose an unconditional radians conversion that bypasses schema validation.", result);
        var payloadValidation = ReviewGateSource.Read(root, "scripts", "core", "players", "CommandGateway.PayloadValidation.cs");
        RequireText(payloadValidation, "ContainsInvalidSubject(subjects)", "CommandGateway payload validation must use an explicit subject scan.", result);
        RequireText(payloadValidation, "private static bool ContainsInvalidSubject(IReadOnlyList<EntityId> subjects)", "CommandGateway invalid-subject scan must be reusable and allocation-free.", result);
        RequireText(payloadValidation, "command.Kind != PlayerCommandKind.Build && payload.BuildFacing != default", "Non-Build commands must reject non-default Build-facing pollution.", result);
        RequireText(payloadValidation, "PlayerCommandKind.Build => RequireBuild(payload", "Build payloads must use the ordered spec-point-facing validator.", result);
        RequireText(payloadValidation, "facing.TryResolveCanonicalRadians(out _)", "Gateway Build-facing validation must use the shared canonical resolver.", result);
        ForbidText(payloadValidation, "subjects.Any(subject => !subject.IsValid)", "CommandGateway payload validation must not allocate invalid-subject LINQ predicates.", result);
        ForbidText(gateway, "slots.Any(candidate => candidate == slot)", "CommandGateway slot ownership must not allocate LINQ predicates.", result);
        ForbidText(results, "Commands.Count(command => command.Accepted)", "CommandGatewayResult must not allocate predicate Count iterators.", result);

        var battlefieldGateway = ReviewGateSource.Read(
            root,
            "scripts",
            "core",
            "units",
            "runtime",
            "battlefield",
            "UnitBattlefield.PlayerCommandGateway.cs");
        var buildStart = battlefieldGateway.IndexOf("private bool TryApplyBuildCommand(", StringComparison.Ordinal);
        var buildEnd = battlefieldGateway.IndexOf("private static bool TryGetProductionSpec(", buildStart, StringComparison.Ordinal);
        if (buildStart < 0 || buildEnd <= buildStart)
        {
            result.Errors.Add("UnitBattlefield PlayerCommand gateway must retain a focused TryApplyBuildCommand method.");
            return;
        }

        var buildPath = battlefieldGateway[buildStart..buildEnd];
        RequireText(buildPath, "ToVector2(command.Payload.TargetPoint)", "Build gateway must submit the original desired point for simulation placement authority.", result);
        RequireText(buildPath, "command.Payload.BuildFacing.TryResolveCanonicalRadians(out var facing)", "Build sink must defensively resolve and reject malformed facing before mutation.", result);
        RequireText(buildPath, "CommandGatewayValidationError.InvalidPayloadShape", "Build sink must preserve the structured payload-shape rejection.", result);
        RequireText(buildPath, "PlayerCommandBuildFacing.InvalidPayloadMessage", "Build sink must preserve the stable malformed-facing message.", result);
        RequireText(buildPath, "new StartConstructionEntityCommand(", "Build gateway must continue bridging accepted player intent to the construction command.", result);
        ForbidText(buildPath, "ClampInsideWorld(", "Build gateway must not preprocess or drift the desired placement point.", result);

        var resolveFacing = buildPath.IndexOf("TryResolveCanonicalRadians(out var facing)", StringComparison.Ordinal);
        var collectSubjects = buildPath.IndexOf("ConstructionSubjectEntities(", StringComparison.Ordinal);
        var nextTick = buildPath.IndexOf("NextInputCommandTick()", StringComparison.Ordinal);
        if (resolveFacing < 0 || collectSubjects <= resolveFacing || nextTick <= resolveFacing)
        {
            result.Errors.Add("Build sink must reject malformed facing before collecting subjects or advancing the command tick.");
        }
    }
}
