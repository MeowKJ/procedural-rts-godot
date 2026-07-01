namespace ProceduralRts.Core;

public sealed record WorldVisualThemeState(
    WorldVisualTheme Current,
    WorldVisualTheme Target,
    float TransitionProgress,
    string Driver)
{
    public bool IsTransitioning => Current != Target && TransitionProgress < 1;
}
