namespace ProceduralRts;

public sealed record AuthoredMapPreviewRequest(string AbsoluteArtifactPath, string Sha256)
{
    private const string PathFlag = "--authored-map-preview";
    private const string HashFlag = "--authored-map-sha256";

    public static AuthoredMapPreviewRequest Parse(IReadOnlyList<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        string? path = null;
        string? hash = null;
        for (var index = 0; index < arguments.Count; index += 2)
        {
            if (index + 1 >= arguments.Count)
                throw new InvalidOperationException("Preview flags require one value each.");
            var flag = arguments[index];
            var value = arguments[index + 1];
            switch (flag)
            {
                case PathFlag when path is null: path = value; break;
                case HashFlag when hash is null: hash = value; break;
                case PathFlag or HashFlag: throw new InvalidOperationException($"Duplicate preview flag: {flag}.");
                default: throw new InvalidOperationException($"Unknown preview flag: {flag}.");
            }
        }

        if (path is null || hash is null)
            throw new InvalidOperationException("Both authored preview flags are required.");
        if (hash.Length != 64 || hash != hash.ToLowerInvariant() || !hash.All(Uri.IsHexDigit))
            throw new InvalidOperationException("Authored preview SHA-256 must be 64 lowercase hexadecimal characters.");
        return new AuthoredMapPreviewRequest(Path.GetFullPath(path), hash);
    }
}
