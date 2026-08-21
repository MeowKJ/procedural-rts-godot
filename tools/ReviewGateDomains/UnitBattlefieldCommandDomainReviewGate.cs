static class UnitBattlefieldCommandDomainReviewGate
{
    public static void Check(string root, GateResult result)
    {
        foreach (var module in new[]
        {
            "UnitBattlefield.CommandRouting.cs",
            "UnitBattlefield.Commands.cs",
            "UnitBattlefield.DamageRemoval.cs",
            "UnitBattlefield.EntityIdLookup.cs",
            "UnitBattlefield.PlayerCommandGateway.cs",
            "UnitBattlefield.PlayerCommandPayloads.cs",
        })
        {
            ReviewGateSource.RequireFile(root, result, "scripts", "core", "units", "runtime", "battlefield", "command", module);
        }

        foreach (var retiredFlatModule in new[]
        {
            "CommandRouting",
            "Commands",
            "PlayerCommandGateway",
            "PlayerCommandPayloads",
        })
        {
            ReviewGateSource.ForbidFile(root, result, "scripts", "core", "units", "runtime", "battlefield", $"UnitBattlefield.{retiredFlatModule}.cs");
        }
    }
}
