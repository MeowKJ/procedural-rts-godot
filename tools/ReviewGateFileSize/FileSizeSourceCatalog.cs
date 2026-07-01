static class FileSizeSourceCatalog
{
    public static IEnumerable<string> EnumerateSourceFiles(string root)
    {
        return Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsGeneratedOrBuildOutput(root, path));
    }

    public static string RelativePath(string root, string path)
    {
        return Path.GetRelativePath(root, path)
            .Replace(Path.DirectorySeparatorChar, '/')
            .Replace(Path.AltDirectorySeparatorChar, '/');
    }

    public static string DirectoryName(string relativePath)
    {
        return Path.GetDirectoryName(relativePath)?.Replace('\\', '/') ?? ".";
    }

    private static bool IsGeneratedOrBuildOutput(string root, string path)
    {
        var relative = RelativePath(root, path);
        return relative.StartsWith(".godot/", StringComparison.OrdinalIgnoreCase)
            || relative.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
            || relative.Contains("/obj/", StringComparison.OrdinalIgnoreCase);
    }
}

sealed record FileSizeSourceFile(string Path, int Lines);
