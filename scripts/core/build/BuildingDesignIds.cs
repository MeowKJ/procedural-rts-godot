namespace ProceduralRts.Core;

public static class BuildingDesignIds
{
    public const string Headquarters = "building.headquarters";
    public const string PowerPlant = "building.powerplant";
    public const string Barracks = "building.barracks";
    public const string VehicleFactory = "building.vehiclefactory";
    public const string Refinery = "building.refinery";
    public const string Airfield = "building.airfield";
    public const string GroundTurret = "building.groundturret";
    public const string AntiAirTurret = "building.antiairturret";

    public static IReadOnlyList<string> All => BuildSpecCatalog.Definitions.Keys.ToArray();

    public static string NameKey(string id)
    {
        return id switch
        {
            Headquarters => "building.headquarters.name",
            PowerPlant => "building.powerPlant.name",
            Barracks => "building.barracks.name",
            VehicleFactory => "building.vehicleFactory.name",
            Refinery => "building.refinery.name",
            Airfield => "building.airfield.name",
            GroundTurret => "building.groundTurret.name",
            AntiAirTurret => "building.antiAirTurret.name",
            _ => $"{id}.name",
        };
    }

    public static string ShortCode(string id)
    {
        return id switch
        {
            Headquarters => "HQ",
            PowerPlant => "PWR",
            Barracks => "BAR",
            VehicleFactory => "FAC",
            Refinery => "REF",
            Airfield => "AIR",
            GroundTurret => "GUN",
            AntiAirTurret => "AA",
            _ => id.ToUpperInvariant(),
        };
    }
}
