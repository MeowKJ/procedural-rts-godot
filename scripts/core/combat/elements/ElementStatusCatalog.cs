namespace ProceduralRts.Core;

public static class ElementStatusCatalog
{
    private static readonly SortedDictionary<string, ElementStatusDefinition> DefinitionsById = new(StringComparer.Ordinal)
    {
        [ElementStatusIds.EnergyCharge] = new(
            ElementStatusIds.EnergyCharge,
            "Energy Charge",
            DamageElementIds.Energy,
            DurationSeconds: 6f,
            Visibility: ElementStatusVisibility.Highlighted),
        [ElementStatusIds.EntropyDecay] = new(
            ElementStatusIds.EntropyDecay,
            "Entropy Decay",
            DamageElementIds.Entropy,
            DurationSeconds: 7f,
            Visibility: ElementStatusVisibility.Visible,
            ModifierPayload: new ElementStatusModifierPayload(IncomingDamageMultiplier: 1.05f)),
        [ElementStatusIds.KineticStress] = new(
            ElementStatusIds.KineticStress,
            "Kinetic Stress",
            DamageElementIds.Kinetic,
            DurationSeconds: 5f,
            StackingMode: ElementStatusStackingMode.StackAndRefresh,
            MaxStacks: 2),
        [ElementStatusIds.MoonshadowMark] = new(
            ElementStatusIds.MoonshadowMark,
            "Moonshadow Mark",
            DamageElementIds.Moonshadow,
            DurationSeconds: 8f,
            Visibility: ElementStatusVisibility.Highlighted),
        [ElementStatusIds.ResonanceTone] = new(
            ElementStatusIds.ResonanceTone,
            "Resonance Tone",
            DamageElementIds.Resonance,
            DurationSeconds: 6f),
        [ElementStatusIds.ThermalHeat] = new(
            ElementStatusIds.ThermalHeat,
            "Thermal Heat",
            DamageElementIds.Thermal,
            DurationSeconds: 5f,
            ModifierPayload: new ElementStatusModifierPayload(MovementSpeedMultiplier: 0.95f)),
    };

    public static IReadOnlyDictionary<string, ElementStatusDefinition> Definitions => DefinitionsById;

    public static ElementStatusDefinition For(string id)
    {
        return DefinitionsById.TryGetValue(id, out var definition)
            ? definition
            : throw new InvalidOperationException($"Unknown element status '{id}'.");
    }
}
