using Godot;

static partial class Program
{
    static void AssertPlacementGridFootprints()
    {
        var footprint = new PlacementGridFootprint(4, 3);
        Assert(footprint.IsValid, "4x3 placement footprint should be valid");
        Assert(footprint.WorldSize == new Vector2(128, 96), $"4x3 footprint should resolve to 128x96, got {footprint.WorldSize}");

        var rotated = footprint.Rotated(Mathf.Pi * 0.5f);
        Assert(rotated == new PlacementGridFootprint(3, 4), $"quarter turn should swap 4x3 to 3x4, got {rotated}");
        Assert(footprint.Rotated(Mathf.Pi) == footprint, "half turn should preserve footprint dimensions");

        var evenOdd = PlacementMath.ValidateBuildableArea(
            101,
            101,
            1,
            1,
            640,
            480,
            MovementDomain.Land,
            [],
            [],
            requiresBuildAuthority: false,
            logicalFootprint: footprint);
        Assert(evenOdd.IsValid && evenOdd.X == 96 && evenOdd.Y == 112,
            $"4x3 anchor parity should snap to 96,112; got {evenOdd.X},{evenOdd.Y}");

        var oddEven = PlacementMath.ValidateBuildableArea(
            101,
            101,
            1,
            1,
            640,
            480,
            MovementDomain.Land,
            [],
            [],
            requiresBuildAuthority: false,
            logicalFootprint: footprint,
            facing: Mathf.Pi * 0.5f);
        Assert(oddEven.IsValid && oddEven.X == 112 && oddEven.Y == 96,
            $"rotated 3x4 anchor parity should snap to 112,96; got {oddEven.X},{oddEven.Y}");

        var rotationDiscriminator = new PlacementObstacle(32, 64, 32, 32);
        var unrotatedBlocked = PlacementMath.ValidateBuildableArea(
            101,
            101,
            1,
            1,
            640,
            480,
            MovementDomain.Land,
            [],
            [rotationDiscriminator],
            requiresBuildAuthority: false,
            logicalFootprint: footprint);
        var rotatedClear = PlacementMath.ValidateBuildableArea(
            101,
            101,
            1,
            1,
            640,
            480,
            MovementDomain.Land,
            [],
            [rotationDiscriminator],
            requiresBuildAuthority: false,
            logicalFootprint: footprint,
            facing: Mathf.Pi * 0.5f);
        Assert(!unrotatedBlocked.IsValid && unrotatedBlocked.Reason == "placement.blocked",
            "unrotated 4x3 footprint should overlap the rotation discriminator");
        Assert(rotatedClear.IsValid,
            "rotated 3x4 footprint should clear an obstacle touching only its left edge");

        var firstRect = PlacementMath.RectFromCenter(evenOdd.X, evenOdd.Y, footprint.WorldSize.X, footprint.WorldSize.Y);
        var obstacle = new PlacementObstacle(firstRect.X, firstRect.Y, firstRect.Width, firstRect.Height);
        var adjacent = PlacementMath.ValidateBuildableArea(
            224,
            112,
            1,
            1,
            640,
            480,
            MovementDomain.Land,
            [],
            [obstacle],
            requiresBuildAuthority: false,
            logicalFootprint: footprint);
        var overlapping = PlacementMath.ValidateBuildableArea(
            192,
            112,
            1,
            1,
            640,
            480,
            MovementDomain.Land,
            [],
            [obstacle],
            requiresBuildAuthority: false,
            logicalFootprint: footprint);
        Assert(adjacent.IsValid, "cell footprints that only share an edge should be valid");
        Assert(!overlapping.IsValid && overlapping.Reason == "placement.blocked", "overlapping logical cells should be rejected");

        foreach (var spec in BuildSpecCatalog.Definitions.Values)
        {
            Assert(spec.FootprintCells.IsValid, $"{spec.Kind} should declare positive FootprintCells");
            var logical = spec.LogicalFootprint();
            Assert(logical.X >= spec.Footprint.X && logical.Y >= spec.Footprint.Y,
                $"{spec.Kind} logical footprint should contain its visual geometry");
        }

        AssertRotatedBuildingFootprintLifecycle();

        Console.WriteLine("OK [placement-grid]: parity, quarter-turn validation, preview/runtime footprint, adjacency, overlap, and 8 authored footprints.");
    }

    private static void AssertRotatedBuildingFootprintLifecycle()
    {
        var owner = new OwnerId(1);
        var facing = Mathf.Pi * 0.5f;
        var world = new EntityWorld(seed: 528) { WorldWidth = 800, WorldHeight = 600 };
        world.AddSystem(new ConstructionSystem());
        world.ResourceInventory(owner).Credits = 900;

        var hqSpec = BuildSpecCatalog.For(BuildingDesignIds.Headquarters);
        var hq = world.Spawn(
            hqSpec.ToEntitySpec(),
            owner,
            EntityTransform.At(new Vector2(320, 240)),
            new EntityComponentState[]
            {
                new ConstructionIdentityComponentState(BuildingDesignIds.Headquarters),
                new HealthComponentState(hqSpec.MaxHp, hqSpec.MaxHp),
                new VisionComponentState(hqSpec.SightRange),
                new FootprintComponentState(hqSpec.LogicalFootprint(), hqSpec.PlacementDomain),
                new ConstructionComponentState(Progress: 1, BuildTime: hqSpec.BuildTime, Cost: hqSpec.Cost, RefundRatio: hqSpec.RefundRatio),
                new PowerComponentState(hqSpec.PowerProvided, hqSpec.PowerUsed, Powered: true),
                new BuildRadiusComponentState(hqSpec.BuildRadius),
            });

        var powerSpec = BuildSpecCatalog.For(BuildingDesignIds.PowerPlant);
        var previewFootprint = powerSpec.LogicalFootprint(facing);
        Assert(previewFootprint == new Vector2(96, 128),
            $"rotated placement preview should use 96x128 logical footprint, got {previewFootprint}");

        var command = new StartConstructionEntityCommand(
            owner,
            [hq.Id],
            1,
            BuildingDesignIds.PowerPlant,
            new Vector2(520, 248),
            facing);
        var buffer = new EntityCommandBuffer();
        buffer.Enqueue(command);
        var clock = new SimClock();
        world.Step(1, clock.FixedDelta, buffer.DrainUpToTick(1));

        var rejection = world.Events.Drain()
            .OfType<ConstructionRejectedEvent>()
            .FirstOrDefault();
        Assert(rejection is null, $"rotated building placement should succeed, got {rejection?.Reason}");

        var spawned = world.OrderedEntities.Single(entity => entity.Id.Value != hq.Id.Value);
        var runtimeFootprint = spawned.Components.Require<FootprintComponentState>();
        Assert(spawned.Transform.Position == new Vector2(528, 256),
            $"rotated building should use 3x4 anchor parity, got {spawned.Transform.Position}");
        Assert(Math.Abs(spawned.Transform.Facing - facing) < 0.0001f,
            $"rotated building should preserve quarter-turn facing, got {spawned.Transform.Facing}");
        Assert(runtimeFootprint.Size == previewFootprint,
            $"runtime footprint {runtimeFootprint.Size} should match preview/validation footprint {previewFootprint}");
    }
}
