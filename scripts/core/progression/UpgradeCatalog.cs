namespace ProceduralRts.Core;

public static class UpgradeCatalog
{
    public static readonly IReadOnlyDictionary<string, UpgradeDefinition> Definitions =
        new Dictionary<string, UpgradeDefinition>(StringComparer.Ordinal)
        {
            [UpgradeIds.FocusedMunitions] = new(
                UpgradeIds.FocusedMunitions,
                "Focused Munitions",
                new UpgradeModifier(DamageMultiplier: 1.25f)),
            [UpgradeIds.ExtendedBarrels] = new(
                UpgradeIds.ExtendedBarrels,
                "Extended Barrels",
                new UpgradeModifier(WeaponRangeMultiplier: 1.15f)),
            [UpgradeIds.OpticArray] = new(
                UpgradeIds.OpticArray,
                "Optic Array",
                new UpgradeModifier(SightRangeMultiplier: 1.2f)),
            [UpgradeIds.ServoTuning] = new(
                UpgradeIds.ServoTuning,
                "Servo Tuning",
                new UpgradeModifier(MoveSpeedMultiplier: 1.18f)),
            [UpgradeIds.FieldRepairs] = new(
                UpgradeIds.FieldRepairs,
                "Field Repairs",
                new UpgradeModifier(HealthRegenMultiplier: 1.75f)),
            [UpgradeIds.EnergyCapacitors] = new(
                UpgradeIds.EnergyCapacitors,
                "Energy Capacitors",
                new UpgradeModifier(
                    OutgoingElementDamageMultipliers: new SortedDictionary<string, float>(StringComparer.Ordinal)
                    {
                        [DamageElementIds.Energy] = 1.18f,
                    },
                    IncomingElementDamageMultipliers: new SortedDictionary<string, float>(StringComparer.Ordinal)
                    {
                        [DamageElementIds.Explosive] = 0.9f,
                    },
                    VisualDeltaIds: new[]
                    {
                        "visual.delta.energy_capacitors",
                    })),
        };
}
