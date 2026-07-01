public sealed class GateResult
{
    public List<string> Errors { get; } = [];
    public List<string> Warnings { get; } = [];

    public void Error(string message) => Errors.Add(message);

    public void Warning(string message) => Warnings.Add(message);

    public void Print()
    {
        Console.WriteLine("ReviewGate");
        Console.WriteLine($"Errors: {Errors.Count}");
        foreach (var error in Errors)
        {
            Console.WriteLine($"ERROR: {error}");
        }

        Console.WriteLine($"Warnings: {Warnings.Count}");
        foreach (var warning in Warnings)
        {
            Console.WriteLine($"WARN: {warning}");
        }

        if (Errors.Count == 0)
        {
            Console.WriteLine("ReviewGate passed.");
        }
    }
}

