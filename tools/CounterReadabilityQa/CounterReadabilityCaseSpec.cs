using ProceduralRts.Core;

internal static class CounterReadabilityCaseSpec
{
    public static IReadOnlyList<CounterReadabilityCase> Cases { get; } =
    [
        new CounterReadabilityCase(
            "light pressure: dog infantry suppress generic infantry",
            () => CounterReadabilitySimulation.RunUnitDuel(
                "light pressure: dog infantry suppress generic infantry",
                [new UnitGroup("dog.infantry", 6)],
                [new UnitGroup("generic.infantry", 6)])),

        new CounterReadabilityCase(
            "tank pressure: dog guard tanks beat patrol vehicles",
            () => CounterReadabilitySimulation.RunUnitDuel(
                "tank pressure: dog guard tanks beat patrol vehicles",
                [new UnitGroup("dog.guard_tank", 3)],
                [new UnitGroup("dog.patrol_vehicle", 3)])),

        new CounterReadabilityCase(
            "tank pressure: dog guard tanks crack a structure",
            () => CounterReadabilitySimulation.RunUnitsVsBuilding(
                "tank pressure: dog guard tanks crack a structure",
                [new UnitGroup("dog.guard_tank", 4)],
                BuildingDesignIds.PowerPlant)),

        new CounterReadabilityCase(
            "anti-vehicle pressure: rocket dogs beat cat tanks",
            () => CounterReadabilitySimulation.RunUnitDuel(
                "anti-vehicle pressure: rocket dogs beat cat tanks",
                [new UnitGroup("dog.rocket", 6)],
                [new UnitGroup("cat.tank", 3)])),

        new CounterReadabilityCase(
            "air pressure: cat aircraft beat ground tanks with no AA",
            () => CounterReadabilitySimulation.RunUnitDuel(
                "air pressure: cat aircraft beat ground tanks with no AA",
                [new UnitGroup("cat.scout_aircraft", 8)],
                [new UnitGroup("dog.guard_tank", 3)])),

        new CounterReadabilityCase(
            "AA unit pressure: rocket dogs shoot down aircraft",
            () => CounterReadabilitySimulation.RunUnitDuel(
                "AA unit pressure: rocket dogs shoot down aircraft",
                [new UnitGroup("dog.rocket", 6)],
                [new UnitGroup("cat.scout_aircraft", 6)])),

        new CounterReadabilityCase(
            "AA turret pressure: Skyguard turret shoots down aircraft",
            CounterReadabilitySimulation.RunTurretVsAircraft),
    ];
}
