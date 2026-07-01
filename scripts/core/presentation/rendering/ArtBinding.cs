namespace ProceduralRts.Core;

public enum ArtBindingKind
{
    Body,
    Mount,
    RuntimePulse
}

public sealed record ArtBinding(ArtBindingKind Kind, string Id)
{
    public static readonly ArtBinding Body = new(ArtBindingKind.Body, string.Empty);

    public static ArtBinding Mount(string mountId)
    {
        return new ArtBinding(ArtBindingKind.Mount, mountId);
    }

    public static ArtBinding RuntimePulse(string pulseId)
    {
        return new ArtBinding(ArtBindingKind.RuntimePulse, pulseId);
    }
}
