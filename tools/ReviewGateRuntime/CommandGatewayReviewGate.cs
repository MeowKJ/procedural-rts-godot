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
        var payloadValidation = ReviewGateSource.Read(root, "scripts", "core", "players", "CommandGateway.PayloadValidation.cs");
        RequireText(payloadValidation, "ContainsInvalidSubject(subjects)", "CommandGateway payload validation must use an explicit subject scan.", result);
        RequireText(payloadValidation, "private static bool ContainsInvalidSubject(IReadOnlyList<EntityId> subjects)", "CommandGateway invalid-subject scan must be reusable and allocation-free.", result);
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
        RequireText(buildPath, "new StartConstructionEntityCommand(", "Build gateway must continue bridging accepted player intent to the construction command.", result);
        ForbidText(buildPath, "ClampInsideWorld(", "Build gateway must not preprocess or drift the desired placement point.", result);
    }
}
