using ProceduralRts.Core;

internal static class MapPreflightAtomicScenarios
{
    public static void Run(MapSpec valid, List<string> failures)
    {
        AssertAtomic("missing-owner", valid, valid with
        {
            Id = "qa.preflight.missing-owner",
            OwnerStarts = valid.OwnerStarts.Where(start => start.OwnerId.Value != 2).ToArray(),
        }, failures);
        AssertAtomic("duplicate-owner", valid, valid with
        {
            Id = "qa.preflight.duplicate-owner",
            OwnerStarts = [.. valid.OwnerStarts, valid.OwnerStarts[0]],
        }, failures);
        AssertAtomic("extra-owner", valid, valid with
        {
            Id = "qa.preflight.extra-owner",
            OwnerStarts =
            [
                .. valid.OwnerStarts,
                new(new OwnerId(3), FactionId.Dog, new MapPoint(800, 500), 0, 100),
            ],
        }, failures);
        AssertAtomic("entity-owner", valid, valid with
        {
            Id = "qa.preflight.entity-owner",
            Units = [valid.Units[0] with { OwnerId = new OwnerId(3) }, .. valid.Units.Skip(1)],
        }, failures);
        AssertAtomic("unknown-building", valid, valid with
        {
            Id = "qa.preflight.unknown-building",
            Buildings = [valid.Buildings[0] with { Kind = "building.unknown" }, .. valid.Buildings.Skip(1)],
        }, failures);
        AssertAtomic("unknown-unit", valid, valid with
        {
            Id = "qa.preflight.unknown-unit",
            Units = [valid.Units[0] with { DesignId = "unit.unknown" }, .. valid.Units.Skip(1)],
        }, failures);
        AssertAtomic("duplicate-semantic-id", valid, valid with
        {
            Id = "qa.preflight.duplicate-semantic-id",
            Obstacles =
            [
                valid.Obstacles[0] with { Id = valid.Resources[0].Id },
                .. valid.Obstacles.Skip(1),
            ],
        }, failures);
        AssertAtomic("missing-trigger-reference", valid, valid with
        {
            Id = "qa.preflight.missing-trigger-reference",
            NarrativeNodes =
            [
                valid.NarrativeNodes[0] with { TriggerId = "trigger.missing" },
                .. valid.NarrativeNodes.Skip(1),
            ],
        }, failures);
        AssertAtomic("nonpositive-runtime-id", valid, valid with
        {
            Id = "qa.preflight.nonpositive-runtime-id",
            Buildings = [valid.Buildings[0] with { RuntimeId = 0 }, .. valid.Buildings.Skip(1)],
        }, failures);
        AssertAtomic("duplicate-runtime-id", valid, valid with
        {
            Id = "qa.preflight.duplicate-runtime-id",
            Buildings =
            [
                valid.Buildings[0] with { RuntimeId = 7 },
                valid.Buildings[1] with { RuntimeId = 7 },
                .. valid.Buildings.Skip(2),
            ],
        }, failures);
        AssertAtomic("placement", valid, valid with
        {
            Id = "qa.preflight.placement",
            Buildings =
            [
                valid.Buildings[0] with { Position = new MapPoint(valid.WorldSize.Width - 1, valid.WorldSize.Height - 1) },
                .. valid.Buildings.Skip(1),
            ],
        }, failures);
        AssertAutoIdsSkipReserved(valid, failures);
    }

    private static void AssertAtomic(string label, MapSpec valid, MapSpec invalid, List<string> failures)
    {
        var previous = SkirmishSetupState.PendingMatchConfig;
        var existing = MapLoader.Load(valid);
        var environmentBefore = existing.MapEnvironment;
        var hashBefore = existing.DeterministicStateHash();
        var countBefore = existing.Count;
        InvalidOperationException? stagedFailure = null;
        InvalidOperationException? loadedFailure = null;
        try
        {
            try
            {
                SkirmishSetupState.StageAuthoredMap(invalid);
            }
            catch (InvalidOperationException exception)
            {
                stagedFailure = exception;
            }

            try
            {
                MapLoader.LoadInto(existing, invalid, new MapLoadOptions(ConfigureLiveSystems: true));
            }
            catch (InvalidOperationException exception)
            {
                loadedFailure = exception;
            }

            Require(stagedFailure is not null
                && loadedFailure is not null
                && stagedFailure.GetType() == loadedFailure.GetType()
                && stagedFailure.Message == loadedFailure.Message,
                $"{label} should fail staging and loading through the same deterministic preflight.", failures);
            Require(SkirmishSetupState.PendingMatchConfig == previous,
                $"{label} should not replace pending match state.", failures);
            Require(existing.DeterministicStateHash() == hashBefore
                && ReferenceEquals(existing.MapEnvironment, environmentBefore)
                && existing.Count == countBefore
                && MathF.Abs(existing.WorldWidth - valid.WorldSize.Width) < 0.001f
                && MathF.Abs(existing.WorldHeight - valid.WorldSize.Height) < 0.001f,
                $"{label} should not mutate an existing EntityWorld.", failures);
        }
        finally
        {
            SkirmishSetupState.StageMatchConfig(previous);
        }
    }

    private static void AssertAutoIdsSkipReserved(MapSpec valid, List<string> failures)
    {
        var map = valid with
        {
            Id = "qa.preflight.reserved-auto-id",
            Buildings =
            [
                valid.Buildings[0] with { RuntimeId = null },
                valid.Buildings[1] with { RuntimeId = 1 },
                .. valid.Buildings.Skip(2).Select(building => building with { RuntimeId = null }),
            ],
        };
        var world = MapLoader.Load(map);
        var ids = world.OrderedEntities
            .Select(entity => entity.Components.TryGet<BuildingIdentityComponentState>(out var identity)
                ? identity.BuildingId
                : (int?)null)
            .Where(id => id is not null)
            .Select(id => id!.Value)
            .ToArray();
        Require(ids.Length == map.Buildings.Count
            && ids.Distinct().Count() == ids.Length
            && ids[0] != 1
            && ids[1] == 1,
            "auto building ids should skip every explicit id reserved later in authored order.", failures);
    }

    private static void Require(bool condition, string message, List<string> failures)
    {
        if (!condition)
        {
            failures.Add(message);
        }
    }
}
