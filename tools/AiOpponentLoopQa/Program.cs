namespace ProceduralRts.Tools.AiOpponentLoopQa;

internal static class Program
{
    public static int Main()
    {
        return AiOpponentLoopQaProgram.Run();
    }
}

internal static partial class AiOpponentLoopQaProgram
{
    private const double FixedDelta = 1.0 / 30.0;
    private const int SimulationTicks = 30 * 96;

    public static int Run()
    {
        try
        {
            var loop = RunOpponentLoop();
            var build = RunBuildCommandProbe();

            PrintLoop(loop);
            PrintBuildProbe(build);

            var failures = new List<string>();
            AssertOpponentLoop(loop, failures);
            AssertBuildCommandProbe(build, failures);

            if (failures.Count > 0)
            {
                Console.Error.WriteLine("AiOpponentLoopQa FAILED:");
                foreach (var failure in failures)
                {
                    Console.Error.WriteLine($"- {failure}");
                }

                return 1;
            }

            Console.WriteLine("AiOpponentLoopQa PASSED.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("AiOpponentLoopQa FAILED:");
            Console.Error.WriteLine($"- {ex.GetType().Name}: {ex.Message}");
            return 1;
        }
    }
}
