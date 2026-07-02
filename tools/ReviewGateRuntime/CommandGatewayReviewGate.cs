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
        RequireText(results, "public sealed record PlayerCommandResult", "Gateway must return structured per-command results.", result);
        RequireText(results, "CommandGatewayValidationError", "Gateway must expose structured validation errors.", result);
        RequireText(payload, "PlayerCommandPoint", "Gateway payload must use Godot-free point data.", result);
        RequireText(payload, "IReadOnlyList<EntityId>?", "Gateway payload subjects must be read-only entity ids.", result);
    }
}
