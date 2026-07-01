static class CoreProjectRoot
{
    public static string FindProjectRoot(string start)
    {
        var current = new DirectoryInfo(start);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "ProceduralRts.csproj"))
                && File.Exists(Path.Combine(current.FullName, "TODO.md")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not find ProceduralRts project root.");
    }
}
