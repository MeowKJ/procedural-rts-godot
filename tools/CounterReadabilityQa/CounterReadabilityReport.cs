internal static class CounterReadabilityReport
{
    public static void PrintOutcome(BattleOutcome outcome)
    {
        Console.WriteLine($"CASE [{outcome.Name}] winner={outcome.Winner}, ticks={outcome.Ticks}, alive L/R={outcome.LeftAlive}/{outcome.RightAlive}, hp L/R={outcome.LeftHp:0.0}/{outcome.RightHp:0.0}");
    }

    public static void RequireLeftWin(BattleOutcome outcome, List<string> failures)
    {
        CounterReadabilityAssertions.Require(outcome.Winner == DuelWinner.Left, $"{outcome.Name}: expected left to win, got {outcome.Winner}.", failures);
        CounterReadabilityAssertions.Require(outcome.RightAlive == 0, $"{outcome.Name}: expected right side to be eliminated within 60 seconds.", failures);
        CounterReadabilityAssertions.Require(outcome.LeftAlive > 0, $"{outcome.Name}: expected at least one left-side survivor.", failures);
        CounterReadabilityAssertions.Require(outcome.Ticks <= CounterReadabilitySimulation.MaxTicks, $"{outcome.Name}: expected resolution within 60 seconds.", failures);
    }

    public static void ExitIfFailed(IReadOnlyList<string> failures)
    {
        if (failures.Count == 0)
        {
            return;
        }

        Console.Error.WriteLine();
        Console.Error.WriteLine("CounterReadabilityQa FAILED:");
        foreach (var failure in failures)
        {
            Console.Error.WriteLine($"- {failure}");
        }

        Environment.Exit(1);
    }
}
