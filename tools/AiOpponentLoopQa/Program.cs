namespace ProceduralRts.Tools.AiOpponentLoopQa;

internal static class Program
{
    public static int Main(string[] args)
    {
        return AiOpponentLoopQaProgram.Run(args);
    }
}

internal static partial class AiOpponentLoopQaProgram
{
    private const double FixedDelta = 1.0 / 30.0;
    private const int SimulationTicks = 30 * 96;
    internal const string DefaultOutputPath = "artifacts/ai-opponent-loop/tournament-v1.json";
    private static readonly int[] TournamentSeeds = [1729, 535, 10535, 424242];
    private static readonly string[] TournamentMappings = ["dog-left", "cat-left"];

    public static int Run(string[] args)
    {
        var hint = TournamentOptionParser.Infer(args);
        try
        {
            return RunTournament(TournamentOptionParser.Parse(args));
        }
        catch (Exception ex)
        {
            var failure = $"{ex.GetType().Name}: {ex.Message}";
            var report = EmptyFailureReport(hint.Filters, failure);
            try
            {
                WriteArtifact(hint.OutputPath, report);
            }
            catch (Exception artifactException)
            {
                Console.Error.WriteLine($"Artifact write failed: {artifactException.GetType().Name}: {artifactException.Message}");
            }

            Console.Error.WriteLine("AiOpponentLoopQa FAILED:");
            Console.Error.WriteLine($"- {failure}");
            Console.Error.WriteLine($"Artifact: {Path.GetFullPath(hint.OutputPath)}");
            return 1;
        }
    }
}
