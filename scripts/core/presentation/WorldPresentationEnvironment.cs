using System.Diagnostics;
using Godot;

namespace ProceduralRts.Core;

/// <summary>
/// Owns world presentation state that is intentionally outside simulation authority.
/// Live units, buildings, resources, commands, and outcomes remain in UnitBattlefield/EntityWorld.
/// </summary>
public sealed class WorldPresentationEnvironment
{
    private readonly Stopwatch _fogUpdateStopwatch = new();
    private readonly List<(Vector2 Position, float SightRange)> _visionSources = [];
    private float _fogRefreshTimer;
    private ResourceAtmosphere? _atmosphereOverride;

    public WorldPresentationEnvironment(Vector2 worldSize, FogQualityTier fogQuality = FogQualityTier.Medium)
    {
        WorldSize = worldSize;
        FogQuality = fogQuality;
        FogOfWar = new FogOfWarMap(fogQuality);
        SignalNodes = SignalNetworkMath.CreateDefaultNetwork(worldSize).ToList();
    }

    public Vector2 WorldSize { get; }
    public FogQualityTier FogQuality { get; }
    public FogOfWarMap FogOfWar { get; }
    public List<SignalNetworkNode> SignalNodes { get; }
    public double LastFogUpdateMs { get; private set; }
    public WorldVisualThemeState VisualTheme { get; private set; } = new(
        WorldVisualTheme.DayCommand,
        WorldVisualTheme.DayCommand,
        1,
        "default");
    public ResourceAtmosphere ResourceAtmosphere { get; private set; } = ResourceAtmosphere.Day;

    public event Action<WorldVisualThemeState>? VisualThemeChanged;
    public event Action? SignalNetworkChanged;

    public void Update(double delta, UnitBattlefield battlefield, PlayerSlotId viewer)
    {
        var dt = (float)Math.Min(delta, 0.05);
        if (VisualTheme.IsTransitioning)
        {
            AdvanceVisualThemeTransition(dt * 0.28f);
        }

        _fogRefreshTimer -= dt;
        if (_fogRefreshTimer > 0)
        {
            return;
        }

        _fogRefreshTimer = FogOfWarVisualPolicy.WorldRedrawIntervalFor(FogQuality);
        CollectVisionSources(battlefield, viewer);
        _fogUpdateStopwatch.Restart();
        FogOfWar.Update(WorldSize, _visionSources);
        _fogUpdateStopwatch.Stop();
        LastFogUpdateMs = _fogUpdateStopwatch.Elapsed.TotalMilliseconds;
    }

    public bool IsVisible(Vector2 worldPosition)
    {
        return FogOfWar.IsVisible(worldPosition);
    }

    public bool IsExplored(Vector2 worldPosition)
    {
        return FogOfWar.IsExplored(worldPosition);
    }

    public void ApplySandboxAtmosphere(SandboxAtmospherePreset preset)
    {
        switch (preset)
        {
            case SandboxAtmospherePreset.Daytime:
                SetSignalNetworkPowered(true);
                SetVisualTheme(WorldVisualTheme.DayCommand, "sandbox-daytime");
                break;
            case SandboxAtmospherePreset.Dusk:
                SetSignalNetworkPowered(true);
                SetVisualTheme(WorldVisualTheme.FogMorning, "sandbox-fog-morning");
                break;
            case SandboxAtmospherePreset.Night:
                SetSignalNetworkPowered(true);
                SetVisualTheme(WorldVisualTheme.DuskDefense, "sandbox-dusk-defense");
                break;
            case SandboxAtmospherePreset.SignalRestoration:
                SetSignalNetworkPowered(true);
                SetVisualThemeTransition(
                    WorldVisualTheme.FogMorning,
                    WorldVisualTheme.DayCommand,
                    0.35f,
                    "sandbox-signal-restoration");
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

    public void ReleaseManagedResources()
    {
        FogOfWar.ReleaseManagedResources();
    }

    private void CollectVisionSources(UnitBattlefield battlefield, PlayerSlotId viewer)
    {
        _visionSources.Clear();
        foreach (var source in battlefield.VisionSources(viewer))
        {
            _visionSources.Add((source.Position, source.SightRange));
        }

        foreach (var building in battlefield.BuildingSnapshots())
        {
            if (building.Hp <= 0
                || battlefield.Relations.Relation(viewer, building.PlayerSlotId) is not (PlayerRelation.Self or PlayerRelation.Allied)
                || battlefield.BuildingPresentationProjection(building.Id) is not { BuildProgress: >= 1 })
            {
                continue;
            }

            _visionSources.Add((building.Position, BuildSpecCatalog.For(building.Kind).SightRange));
        }

        foreach (var node in SignalNodes)
        {
            if (SignalNetworkMath.EmitsNightVision(node, VisualTheme))
            {
                _visionSources.Add((node.Position, node.NightVisionRadius));
            }
        }
    }

    public void SetVisualTheme(
        WorldVisualTheme target,
        string driver = "script",
        float transitionProgress = 1)
    {
        transitionProgress = Mathf.Clamp(transitionProgress, 0, 1);
        ApplyVisualThemeState(transitionProgress >= 1
            ? new WorldVisualThemeState(target, target, 1, driver)
            : new WorldVisualThemeState(VisualTheme.Current, target, transitionProgress, driver));
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

    private void SetSignalNetworkPowered(bool powered)
    {
        for (var index = 0; index < SignalNodes.Count; index++)
        {
            SignalNodes[index] = SignalNodes[index] with { Powered = powered };
        }

        SignalNetworkChanged?.Invoke();
        _fogRefreshTimer = 0;
    }

    private void AdvanceVisualThemeTransition(float amount)
    {
        var progress = Mathf.Clamp(VisualTheme.TransitionProgress + amount, 0, 1);
        VisualTheme = progress >= 1
            ? new WorldVisualThemeState(VisualTheme.Target, VisualTheme.Target, 1, VisualTheme.Driver)
            : VisualTheme with { TransitionProgress = progress };
        ResourceAtmosphere = _atmosphereOverride ?? WorldThemeMath.ResourceAtmosphereFor(VisualTheme);
        VisualThemeChanged?.Invoke(VisualTheme);
    }

    private void ApplyVisualThemeState(
        WorldVisualThemeState visualTheme,
        ResourceAtmosphere? atmosphereOverride = null)
    {
        VisualTheme = visualTheme;
        _atmosphereOverride = atmosphereOverride;
        ResourceAtmosphere = atmosphereOverride ?? WorldThemeMath.ResourceAtmosphereFor(VisualTheme);
        VisualThemeChanged?.Invoke(VisualTheme);
        _fogRefreshTimer = 0;
    }
}
