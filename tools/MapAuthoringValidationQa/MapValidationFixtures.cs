using Godot;
using ProceduralRts.Core;

static class MapValidationFixtures
{
    public static MapSpec Valid(string id = "qa.valid")
    {
        return new MapSpec
        {
            Id = id,
            Seed = 568,
            WorldSize = new MapSize(1536, 1024),
            OwnerStarts =
            [
                new(new OwnerId(1), FactionId.Dog, new MapPoint(128, 128), 0, 2400),
                new(new OwnerId(2), FactionId.Cat, new MapPoint(1408, 896), MathF.PI, 2400),
            ],
        };
    }

    public static MapBuildingSeedSpec Building(
        string kind,
        float x,
        float y,
        int owner = 1,
        FactionId? faction = null,
        float facing = 0,
        int? runtimeId = null)
    {
        var spec = BuildSpecCatalog.For(kind);
        var footprint = spec.FootprintCells.Rotated(facing);
        return new MapBuildingSeedSpec(
            kind,
            new OwnerId(owner),
            faction ?? (owner == 2 ? FactionId.Cat : FactionId.Dog),
            new MapPoint(
                PlacementMath.SnapAnchor(x, footprint.WidthCells),
                PlacementMath.SnapAnchor(y, footprint.HeightCells)),
            facing,
            RuntimeId: runtimeId);
    }

    public static MapSpec WithBuildings(params MapBuildingSeedSpec[] buildings)
    {
        return Valid("qa.buildings") with { Buildings = buildings };
    }

    public static MapSpec SolidWall(string id = "qa.wall")
    {
        return Valid(id) with
        {
            Obstacles = [new MapObstacleSpec("wall", new MapRect(704, 0, 128, 1024))],
        };
    }

    public static MapSpec WallWithGap()
    {
        return Valid("qa.gap") with
        {
            Obstacles =
            [
                new MapObstacleSpec("wall.top", new MapRect(704, 0, 128, 384)),
                new MapObstacleSpec("wall.bottom", new MapRect(704, 640, 128, 384)),
            ],
        };
    }

    public static string Fingerprint(MapSpec map)
    {
        return string.Join('|',
            map.Id,
            $"{map.WorldSize.Width:R},{map.WorldSize.Height:R}",
            string.Join(',', map.OwnerStarts.Select(value => $"{value.OwnerId.Value}:{value.Position.X:R}:{value.Position.Y:R}")),
            string.Join(',', map.Buildings.Select(value => $"{value.Kind}:{value.Position.X:R}:{value.Position.Y:R}:{value.Facing:R}:{value.RuntimeId}")),
            string.Join(',', map.Resources.Select(value => $"{value.Id}:{value.Position.X:R}:{value.Position.Y:R}:{value.Radius:R}")),
            string.Join(',', map.Obstacles.Select(value => $"{value.Id}:{value.Bounds.X:R}:{value.Bounds.Y:R}:{value.Bounds.Width:R}:{value.Bounds.Height:R}")),
            string.Join(',', map.TerrainCells.Select(value => $"{value.Id}:{value.Bounds.X:R}:{value.Bounds.Y:R}:{value.Bounds.Width:R}:{value.Bounds.Height:R}:{value.MovementCost:R}")));
    }

    public static void Require(bool condition, string message, List<string> failures)
    {
        if (!condition) failures.Add(message);
    }
}
