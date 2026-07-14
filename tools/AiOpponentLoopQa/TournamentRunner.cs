using System.Text.Json;
using System.Text.Json.Serialization;
using ProceduralRts.Core;

namespace ProceduralRts.Tools.AiOpponentLoopQa;

internal static partial class AiOpponentLoopQaProgram
{
    private static readonly JsonSerializerOptions ArtifactJsonOptions = CreateJsonOptions();

    private static int RunTournament(TournamentOptions options)
    {
        var cases = TournamentCases()
            .Where(config => options.Seed is null || config.Seed == options.Seed)
            .Where(config => options.Mapping is null || config.Mapping == options.Mapping)
            .ToArray();
        if (cases.Length == 0)
        {
            throw new ArgumentException("No tournament case matches the requested --seed/--mapping filters.");
        }

        var build = RunBuildCommandProbe();
        PrintBuildProbe(build);
        var failures = new List<string>();
        AssertBuildCommandProbe(build, failures);
        var results = new List<TournamentCaseResult>(cases.Length);
        foreach (var config in cases)
        {
            var result = RunTournamentCase(config);
            results.Add(result);
            PrintTournamentCase(result);
            foreach (var failure in result.Failures)
            {
                failures.Add($"seed={config.Seed} mapping={config.Mapping}: {failure} Reproduce: {config.ReproductionCommand}");
            }
        }

        AssertSeedDrivenSetups(options, results, failures);
        var passed = results.Count(result => result.Failures.Count == 0);
        var report = new TournamentReport(
            SchemaVersion: "schema-v1",
            TournamentVersion: "tournament-v1",
            FixedDelta,
            SimulationTicks,
            TournamentSeeds,
            TournamentMappings,
            options.Filters,
            PassedCaseCount: passed,
            FailedCaseCount: results.Count - passed,
            results,
            build,
            failures);
        WriteArtifact(options.OutputPath, report);

        if (failures.Count > 0)
        {
            Console.Error.WriteLine("AiOpponentLoopQa FAILED:");
            foreach (var failure in failures)
            {
                Console.Error.WriteLine($"- {failure}");
            }

            Console.Error.WriteLine($"Artifact: {Path.GetFullPath(options.OutputPath)}");
            return 1;
        }

        Console.WriteLine($"AiOpponentLoopQa PASSED: {results.Count} case(s), artifact {Path.GetFullPath(options.OutputPath)}");
        return 0;
    }

    private static TournamentCaseResult RunTournamentCase(TournamentCaseConfig config)
    {
        OpponentLoopReport? first = null;
        try
        {
            first = RunOpponentLoop(config);
            var second = RunOpponentLoop(config);
            var canonicalConfig = new CanonicalCaseConfig(config.Seed, config.Mapping, config.LeftFaction, config.RightFaction);
            var firstHash = CanonicalObjectSha256(new CanonicalCase(canonicalConfig, first));
            var secondHash = CanonicalObjectSha256(new CanonicalCase(canonicalConfig, second));
            var failures = new List<string>();
            AssertOpponentLoop(first, failures);
            if (!string.Equals(firstHash, secondHash, StringComparison.Ordinal))
            {
                failures.Add($"deterministic summary mismatch {firstHash} != {secondHash}.");
            }

            return new TournamentCaseResult(
                config,
                first,
                firstHash,
                secondHash,
                firstHash == secondHash,
                first.Termination,
                first.FinalTick,
                first.OutcomeTick,
                failures,
                config.ReproductionCommand);
        }
        catch (Exception ex)
        {
            return new TournamentCaseResult(
                config,
                first,
                first is null ? null : CanonicalObjectSha256(new CanonicalCase(
                    new CanonicalCaseConfig(config.Seed, config.Mapping, config.LeftFaction, config.RightFaction),
                    first)),
                null,
                false,
                "exception",
                first?.FinalTick ?? 0,
                first?.OutcomeTick,
                [$"{ex.GetType().Name}: {ex.Message}"],
                config.ReproductionCommand);
        }
    }

    private static IEnumerable<TournamentCaseConfig> TournamentCases()
    {
        foreach (var seed in TournamentSeeds)
        {
            yield return new TournamentCaseConfig(seed, "dog-left", FactionId.Dog, FactionId.Cat);
            yield return new TournamentCaseConfig(seed, "cat-left", FactionId.Cat, FactionId.Dog);
        }
    }

    private static void AssertSeedDrivenSetups(
        TournamentOptions options,
        IReadOnlyList<TournamentCaseResult> results,
        List<string> failures)
    {
        if (options.Seed is not null)
        {
            return;
        }

        foreach (var mapping in TournamentMappings.Where(mapping => options.Mapping is null || options.Mapping == mapping))
        {
            var mappingCases = results
                .Where(result => result.Config.Mapping == mapping && result.Metrics is not null)
                .ToArray();
            var distinctFingerprints = mappingCases
                .Select(result => result.Metrics!.SetupFingerprint)
                .Distinct(StringComparer.Ordinal)
                .Count();
            if (mappingCases.Length != TournamentSeeds.Length || distinctFingerprints != TournamentSeeds.Length)
            {
                failures.Add(
                    $"mapping {mapping} should produce exactly four cases with four distinct setup fingerprints; "
                    + $"cases/distinct {mappingCases.Length}/{distinctFingerprints}.");
            }
        }
    }

    private static TournamentReport EmptyFailureReport(TournamentSelectedFilters filters, string failure)
    {
        return new TournamentReport(
            "schema-v1",
            "tournament-v1",
            FixedDelta,
            SimulationTicks,
            TournamentSeeds,
            TournamentMappings,
            filters,
            PassedCaseCount: 0,
            FailedCaseCount: 1,
            Cases: [],
            BuildProbe: null,
            Failures: [failure]);
    }

    internal static void WriteArtifact(string outputPath, TournamentReport report)
    {
        var fullPath = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, JsonSerializer.Serialize(report, ArtifactJsonOptions));
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private sealed record CanonicalCaseConfig(
        int Seed,
        string Mapping,
        FactionId LeftFaction,
        FactionId RightFaction);

    private sealed record CanonicalCase(CanonicalCaseConfig Config, OpponentLoopReport Metrics);
}
