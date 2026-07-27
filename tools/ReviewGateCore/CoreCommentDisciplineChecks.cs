static class CoreCommentDisciplineChecks
{
    public static void CheckCommentDiscipline(string root, GateResult result)
    {
        RequireCommandGatewayComments(root, result);
        RequirePlayerControllerComments(root, result);
        RequirePathfindingWorkspaceComments(root, result);
    }

    private static void RequireCommandGatewayComments(string root, GateResult result)
    {
        var gateway = ReviewGateSource.Read(root, "scripts", "core", "players", "CommandGateway.cs");
        RequireText(
            gateway,
            "Public handoff point between local/remote controllers and deterministic",
            "CommandGateway must document its cross-layer validation responsibility.",
            result);
        RequireText(
            gateway,
            "Narrow adapter boundary from validated player intent into the authoritative",
            "CommandGateway sink boundary must document safe usage.",
            result);
        RequireText(
            gateway,
            "replay, bots, and network clients share the same validation path.",
            "CommandGateway submission context must document its Godot-free alternate boundary.",
            result);
    }

    private static void RequirePlayerControllerComments(string root, GateResult result)
    {
        var contracts = ReviewGateSource.Read(root, "scripts", "core", "players", "PlayerControllerContracts.cs");
        RequireText(
            contracts,
            "Godot-free controller contract for local UI, replay, bots, or external",
            "IPlayerController must document its cross-layer controller boundary.",
            result);
        RequireText(
            contracts,
            "cannot mutate simulation state directly.",
            "IPlayerAgent must document its safe simulation boundary.",
            result);
    }

    private static void RequirePathfindingWorkspaceComments(string root, GateResult result)
    {
        var workspace = ReviewGateSource.Read(root, "scripts", "core", "pathing", "PathfindingWorkspace.cs");
        RequireText(
            workspace,
            "Caller-owned scratch storage for deterministic pathfinding.",
            "PathfindingWorkspace must document its allocation and deterministic reuse contract.",
            result);
        RequireText(
            workspace,
            "the workspace is reused.",
            "PathfindingWorkspace must document safe result ownership.",
            result);
    }
}
