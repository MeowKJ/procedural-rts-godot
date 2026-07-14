namespace ProceduralRts.Tools.AiOpponentLoopQa;

internal sealed record TournamentOptions(int? Seed, string? Mapping, string OutputPath)
{
    public TournamentSelectedFilters Filters => new(Seed, Mapping);
}

internal sealed record TournamentInvocationHint(
    string OutputPath,
    TournamentSelectedFilters Filters);

internal static class TournamentOptionParser
{
    public static TournamentOptions Parse(IReadOnlyList<string> args)
    {
        int? seed = null;
        string? mapping = null;
        var output = AiOpponentLoopQaProgram.DefaultOutputPath;
        for (var index = 0; index < args.Count; index++)
        {
            var (name, inlineValue) = Split(args[index]);
            if (name is not ("--seed" or "--mapping" or "--output"))
            {
                throw new ArgumentException($"Unknown option '{name}'.");
            }

            var value = inlineValue ?? NextValue(args, ref index, name);
            switch (name)
            {
                case "--seed":
                    seed = int.TryParse(value, out var parsedSeed)
                        ? parsedSeed
                        : throw new ArgumentException($"Invalid --seed '{value}'.");
                    break;
                case "--mapping":
                    mapping = value.ToLowerInvariant();
                    if (mapping is not ("dog-left" or "cat-left"))
                    {
                        throw new ArgumentException($"Invalid --mapping '{value}'. Expected dog-left or cat-left.");
                    }

                    break;
                case "--output":
                    output = value;
                    break;
            }
        }

        return new TournamentOptions(seed, mapping, output);
    }

    public static TournamentInvocationHint Infer(IReadOnlyList<string> args)
    {
        int? seed = null;
        string? mapping = null;
        var output = AiOpponentLoopQaProgram.DefaultOutputPath;
        for (var index = 0; index < args.Count; index++)
        {
            var (name, inlineValue) = Split(args[index]);
            string? value = inlineValue;
            if (value is null && index + 1 < args.Count && !args[index + 1].StartsWith("--", StringComparison.Ordinal))
            {
                value = args[++index];
            }

            if (name == "--output" && !string.IsNullOrWhiteSpace(value))
            {
                output = value;
            }
            else if (name == "--seed" && int.TryParse(value, out var parsedSeed))
            {
                seed = parsedSeed;
            }
            else if (name == "--mapping" && value is not null)
            {
                mapping = value.ToLowerInvariant();
            }
        }

        return new TournamentInvocationHint(output, new TournamentSelectedFilters(seed, mapping));
    }

    private static (string Name, string? Value) Split(string argument)
    {
        var separator = argument.IndexOf('=');
        return separator < 0
            ? (argument, null)
            : (argument[..separator], argument[(separator + 1)..]);
    }

    private static string NextValue(IReadOnlyList<string> args, ref int index, string name)
    {
        if (index + 1 >= args.Count || args[index + 1].StartsWith("--", StringComparison.Ordinal))
        {
            throw new ArgumentException($"Missing value for '{name}'.");
        }

        return args[++index];
    }
}
