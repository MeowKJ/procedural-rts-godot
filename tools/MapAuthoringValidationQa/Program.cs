using System.Text.Json;

var failures = new List<string>();
var diagnostics = MapValidationDiagnosticScenarios.Run(failures);
MapValidationGeometryScenarios.Run(failures);
if (failures.Count > 0)
{
    Console.Error.WriteLine("MapAuthoringValidationQa FAILED");
    foreach (var failure in failures) Console.Error.WriteLine($"- {failure}");
    Environment.Exit(1);
}

if (args is ["--diagnostics-json", var output])
{
    if (diagnostics.Count != 24) throw new InvalidOperationException("Evidence requires all 24 diagnostics.");
    var directory = Path.GetDirectoryName(output);
    if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
    File.WriteAllText(output, JsonSerializer.Serialize(diagnostics.Select(value => new
    {
        Severity = value.Severity.ToString(),
        value.Code,
        Phase = value.Phase.ToString(),
        Source = new { Kind = value.Source.Kind.ToString(), value.Source.Index, value.Source.Id },
        Conflict = value.Conflict is null ? null : new
        {
            Kind = value.Conflict.Kind.ToString(), value.Conflict.Index, value.Conflict.Id,
        },
        value.Message,
    }), new JsonSerializerOptions { WriteIndented = true }));
}
else if (args.Length != 0)
{
    throw new ArgumentException("usage: MapAuthoringValidationQa [--diagnostics-json path]");
}

Console.WriteLine("MapAuthoringValidationQa PASSED: 24 codes, stable ordering, read-only diagnostics, shared geometry/reservations, and runtime reachability.");
