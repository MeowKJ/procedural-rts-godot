namespace ProceduralRts.Core;

public static class ElementReactionCatalog
{
    private static readonly SortedDictionary<string, ElementReactionDefinition> DefinitionsById = new(StringComparer.Ordinal)
    {
        [ElementReactionIds.DecayBurst] = new(
            ElementReactionIds.DecayBurst,
            "Decay Burst",
            ElementStatusIds.EntropyDecay,
            DamageElementIds.Explosive,
            new ElementReactionEffectPayload(DamageMultiplier: 1.18f, SplashRadius: 42f),
            ElementReactionPresentationStyle.Decay),
        [ElementReactionIds.EclipseDecay] = new(
            ElementReactionIds.EclipseDecay,
            "Eclipse Decay",
            ElementStatusIds.MoonshadowMark,
            DamageElementIds.Entropy,
            new ElementReactionEffectPayload(DamageMultiplier: 1.12f, SplashRadius: 28f, StatusDurationMultiplier: 1.2f),
            ElementReactionPresentationStyle.Eclipse),
        [ElementReactionIds.Fracture] = new(
            ElementReactionIds.Fracture,
            "Fracture",
            ElementStatusIds.KineticStress,
            DamageElementIds.Resonance,
            new ElementReactionEffectPayload(DamageMultiplier: 1.14f),
            ElementReactionPresentationStyle.Shatter),
        [ElementReactionIds.HarmonicOverload] = new(
            ElementReactionIds.HarmonicOverload,
            "Harmonic Overload",
            ElementStatusIds.ResonanceTone,
            DamageElementIds.Energy,
            new ElementReactionEffectPayload(DamageMultiplier: 1.16f, SplashRadius: 36f),
            ElementReactionPresentationStyle.Surge),
        [ElementReactionIds.Meltdown] = new(
            ElementReactionIds.Meltdown,
            "Meltdown",
            ElementStatusIds.ThermalHeat,
            DamageElementIds.Energy,
            new ElementReactionEffectPayload(DamageMultiplier: 1.2f, SplashRadius: 32f),
            ElementReactionPresentationStyle.Meltdown),
        [ElementReactionIds.Moonbreak] = new(
            ElementReactionIds.Moonbreak,
            "Moonbreak",
            ElementStatusIds.MoonshadowMark,
            DamageElementIds.Kinetic,
            new ElementReactionEffectPayload(DamageMultiplier: 1.15f),
            ElementReactionPresentationStyle.Shatter),
        [ElementReactionIds.Overload] = new(
            ElementReactionIds.Overload,
            "Overload",
            ElementStatusIds.EnergyCharge,
            DamageElementIds.Explosive,
            new ElementReactionEffectPayload(DamageMultiplier: 1.2f, SplashRadius: 40f),
            ElementReactionPresentationStyle.Burst),
    };

    public static IReadOnlyDictionary<string, ElementReactionDefinition> Definitions => DefinitionsById;

    public static ElementReactionDefinition For(string id)
    {
        return DefinitionsById.TryGetValue(id, out var definition)
            ? definition
            : throw new InvalidOperationException($"Unknown element reaction '{id}'.");
    }

    public static ElementReactionDefinition? Match(string primerStatusId, string triggerElementId)
    {
        _ = ElementStatusCatalog.For(primerStatusId);
        _ = DamageElementCatalog.For(triggerElementId);

        foreach (var definition in DefinitionsById.Values)
        {
            if (definition.PrimerStatusId == primerStatusId
                && definition.TriggerElementId == triggerElementId)
            {
                return definition;
            }
        }

        return null;
    }
}
