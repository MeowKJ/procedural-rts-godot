static partial class Program
{
    static void RunElementReactionScenario()
    {
        var first = ElementReactionSignature();
        var second = ElementReactionSignature();
        Assert(first == second, $"element reaction replay must be deterministic, got {first} then {second}.");
        Console.WriteLine($"OK [element-reaction]: {first}.");
    }

    private static string ElementReactionSignature()
    {
        var active = ElementReactionResolver.ApplyPrimer(
            Array.Empty<ElementStatusInstance>(),
            ElementStatusCatalog.For(ElementStatusIds.EnergyCharge));

        Assert(active.Count == 1, "energy primer should add one active status.");
        Assert(active[0].StatusId == ElementStatusIds.EnergyCharge, "energy primer should add the energy charge status.");

        var undefined = ElementReactionResolver.Resolve(active, DamageElementIds.Kinetic);
        Assert(!undefined.Triggered, "energy primer plus kinetic trigger should be an undefined no-op.");
        Assert(undefined.ActiveStatuses.Count == 1, "undefined reaction should not consume or add statuses.");
        Assert(undefined.ActiveStatuses[0] == active[0], "undefined reaction should not mutate the active status payload.");

        var overload = ElementReactionResolver.Resolve(active, DamageElementIds.Explosive);
        Assert(overload.Triggered, "energy primer plus explosive trigger should produce a reaction.");
        Assert(overload.Reaction is not null, "triggered reaction must carry its reaction definition.");
        var reaction = overload.Reaction ?? throw new InvalidOperationException("Triggered reaction had no definition.");
        Assert(reaction.ReactionId == ElementReactionIds.Overload, $"expected overload, got {reaction.ReactionId}.");
        Assert(overload.ConsumedStatus is not null && overload.ConsumedStatus.StatusId == ElementStatusIds.EnergyCharge, "overload should consume the energy primer status.");
        Assert(overload.ActiveStatuses.Count == 0, "overload should consume the single primer status.");

        return string.Join(
            '|',
            reaction.ReactionId,
            reaction.EffectPayload.DamageMultiplier.ToString("0.00"),
            reaction.EffectPayload.SplashRadius.ToString("0.0"),
            reaction.PresentationStyle.ToString());
    }
}
