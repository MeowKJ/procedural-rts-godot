using ProceduralRts.Core;

static class ArtifactFixtureMap
{
    public static MapSpec Create(float firstFacing = 0f)
    {
        return new MapSpec
        {
            Id = "qa.hand-designed", Seed = 20260701, WorldSize = new MapSize(1600, 1000),
            OwnerStarts =
            [
                new(new OwnerId(1), FactionId.Dog, new MapPoint(260, 320), firstFacing, 2400),
                new(new OwnerId(2), FactionId.Cat, new MapPoint(1260, 680), 3.14159f, 2400),
            ],
            TerrainCells =
            [
                new("SoftRoad", new MapRect(540, 500, 500, 140), "soft-road", 0.85f),
                new("CatBasePad", new MapRect(1152, 608, 192, 160), "base-ground"),
            ],
            Resources = [new("NorthField", new MapPoint(780, 230), 130, 2800, new MapColor("#8fffe1"))],
            Obstacles = [new("CourtyardBlock", new MapRect(690, 450, 220, 120))],
            Buildings =
            [
                new("building.headquarters", new OwnerId(1), FactionId.Dog, new MapPoint(256, 304)),
                new("building.headquarters", new OwnerId(2), FactionId.Cat, new MapPoint(1248, 688), 3.1415927f),
            ],
            Units =
            [
                new("dog.guard_tank", new OwnerId(1), new MapPoint(380, 320)),
                new("cat.tank", new OwnerId(2), new MapPoint(1140, 680), 3.14159f),
            ],
            Triggers = [new("GateTrigger", new MapRect(720, 420, 180, 180), "chapter0.gate_contact")],
            Objectives = [new("SignalObjective", new MapPoint(840, 420), "objective.restore_signal")],
            NarrativeNodes = [new("FirstMark", new MapPoint(650, 380), "narrative.safe_mark", "GateTrigger")],
        };
    }
}
