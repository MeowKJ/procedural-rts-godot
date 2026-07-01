var failures = new List<string>();

Console.WriteLine("CounterReadabilityQa");
Console.WriteLine($"Max readable window: {CounterReadabilitySimulation.MaxTicks} ticks / 60 seconds");
Console.WriteLine();

CounterReadabilityAssertions.CheckDataRules(failures);

foreach (var scenario in CounterReadabilityCaseSpec.Cases)
{
    var outcome = scenario.Run();
    CounterReadabilityReport.PrintOutcome(outcome);
    CounterReadabilityReport.RequireLeftWin(outcome, failures);
}

CounterReadabilityReport.ExitIfFailed(failures);

Console.WriteLine();
Console.WriteLine("CounterReadabilityQa PASSED.");
