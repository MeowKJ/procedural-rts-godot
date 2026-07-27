static class ReviewGateRunner
{
    public static int Run(string[] args, string startDirectory)
    {
        var root = CoreProjectRoot.FindProjectRoot(startDirectory);
        var mode = args.Length == 0 || args[0].StartsWith("--", StringComparison.Ordinal)
            ? "all"
            : args[0].Trim().ToLowerInvariant();
        var failOnWarnings = args.Any(arg => arg.Equals("--fail-on-warnings", StringComparison.OrdinalIgnoreCase));
        var maxWarnings = CoreArgumentParsing.ParseMaxWarnings(args);

        var result = new GateResult();
        if (!ReviewGateRegistry.IsKnownMode(mode, root))
        {
            result.Error($"Unknown mode '{mode}'. {ReviewGateRegistry.DescribeKnownModes(root)}.");
            result.Print();
            return 1;
        }

        ReviewGateRegistry.Run(mode, new ReviewGateContext(root, result));
        result.Print();

        if (maxWarnings is not null && result.Warnings.Count > maxWarnings)
        {
            Console.WriteLine($"ERROR: warning count {result.Warnings.Count} exceeds --max-warnings={maxWarnings.Value}.");
            return 1;
        }

        return result.Errors.Count > 0 || (failOnWarnings && result.Warnings.Count > 0) ? 1 : 0;
    }
}
