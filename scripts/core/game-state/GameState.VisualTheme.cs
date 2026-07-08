using Godot;

namespace ProceduralRts.Core;

public sealed partial class GameState
{
    public void SetVisualTheme(WorldVisualTheme target, string driver = "script", float transitionProgress = 1)
    {
        transitionProgress = Mathf.Clamp(transitionProgress, 0, 1);
        var nextTheme = transitionProgress >= 1
            ? new WorldVisualThemeState(target, target, 1, driver)
            : new WorldVisualThemeState(VisualTheme.Current, target, transitionProgress, driver);
        ApplyVisualThemeState(nextTheme);
    }

    public void ApplySandboxAtmosphere(SandboxAtmospherePreset preset)
    {
        switch (preset)
        {
            case SandboxAtmospherePreset.Daytime:
                SetSignalNetworkPowered(true);
                SetVisualTheme(WorldVisualTheme.DayCommand, "sandbox-daytime", transitionProgress: 1);
                break;
            case SandboxAtmospherePreset.Dusk:
                SetSignalNetworkPowered(true);
                SetVisualTheme(WorldVisualTheme.FogMorning, "sandbox-fog-morning", transitionProgress: 1);
                break;
            case SandboxAtmospherePreset.Night:
                SetSignalNetworkPowered(true);
                SetVisualTheme(WorldVisualTheme.DuskDefense, "sandbox-dusk-defense", transitionProgress: 1);
                break;
            case SandboxAtmospherePreset.SignalRestoration:
                SetSignalNetworkPowered(true);
                SetVisualThemeTransition(WorldVisualTheme.FogMorning, WorldVisualTheme.DayCommand, 0.35f, "sandbox-signal-restoration");
                break;
            case SandboxAtmospherePreset.Corruption:
                SetSignalNetworkPowered(false);
                SetVisualThemeTransition(
                    WorldVisualTheme.DayCommand,
                    WorldVisualTheme.DuskDefense,
                    0.24f,
                    "sandbox-corruption",
                    ResourceAtmosphere.Corruption);
                break;
        }
    }

    private void SetVisualThemeTransition(
        WorldVisualTheme current,
        WorldVisualTheme target,
        float progress,
        string driver,
        ResourceAtmosphere? atmosphereOverride = null)
    {
        ApplyVisualThemeState(
            new WorldVisualThemeState(current, target, Mathf.Clamp(progress, 0, 1), driver),
            atmosphereOverride);
    }

    public void SetSignalNetworkPowered(bool powered)
    {
        for (var index = 0; index < SignalNodes.Count; index++)
        {
            SignalNodes[index] = SignalNodes[index] with { Powered = powered };
        }

        SignalNetworkChanged?.Invoke();
        UpdateFogOfWar();
    }

    public void AdvanceVisualThemeTransition(float amount)
    {
        if (!VisualTheme.IsTransitioning)
        {
            return;
        }

        var progress = Mathf.Clamp(VisualTheme.TransitionProgress + amount, 0, 1);
        VisualTheme = progress >= 1
            ? new WorldVisualThemeState(VisualTheme.Target, VisualTheme.Target, 1, VisualTheme.Driver)
            : VisualTheme with { TransitionProgress = progress };
        ResourceAtmosphere = _visualThemeAtmosphereOverride ?? WorldThemeMath.ResourceAtmosphereFor(VisualTheme);
        VisualThemeChanged?.Invoke(VisualTheme);
    }

    private void ApplyVisualThemeState(
        WorldVisualThemeState visualTheme,
        ResourceAtmosphere? atmosphereOverride = null)
    {
        VisualTheme = visualTheme;
        _visualThemeAtmosphereOverride = atmosphereOverride;
        ResourceAtmosphere = atmosphereOverride ?? WorldThemeMath.ResourceAtmosphereFor(VisualTheme);
        VisualThemeChanged?.Invoke(VisualTheme);
        UpdateFogOfWar();
    }

    private void UpdateVisualThemeTransition(float dt)
    {
        if (VisualTheme.IsTransitioning)
        {
            AdvanceVisualThemeTransition(dt * 0.28f);
        }
    }
}
