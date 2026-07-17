var failures = new List<string>();
MapAuthoringCatalogScenarios.Run(failures);
MapAuthoringExportBoundaryScenarios.Run(args, failures);
if (failures.Count > 0)
{
    Console.Error.WriteLine("MapAuthoringCatalogQa FAILED");
    foreach (var failure in failures)
    {
        Console.Error.WriteLine($"- {failure}");
    }

    Environment.Exit(1);
}

Console.WriteLine("MapAuthoringCatalogQa PASSED: authoritative stable options, strict authoring keys, and Debug-only editor type boundary.");
