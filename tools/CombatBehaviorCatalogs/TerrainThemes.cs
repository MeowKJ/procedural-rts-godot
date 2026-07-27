static partial class Program
{
    private static void AssertTerrainThemesAndSignals()
    {
        var floorTiles = TerrainFloorMath.CreateTiles(new Vector2(3600, 2400));
        if (floorTiles.Count < 300)
        {
            throw new InvalidOperationException("procedural terrain floor should generate enough panels for readable battlefield scale");
        }

        var requiredFloorKinds = new[]
        {
            TerrainFloorKind.Ground,
            TerrainFloorKind.NavigationLane,
            TerrainFloorKind.Coast,
            TerrainFloorKind.Water,
        };
        foreach (var floorKind in requiredFloorKinds)
        {
            if (!floorTiles.Any(tile => tile.Kind == floorKind))
            {
                throw new InvalidOperationException($"procedural terrain floor should include {floorKind} tiles");
            }
        }

        if (floorTiles.Any(tile => tile.Kind == TerrainFloorKind.CommandPlate))
        {
            throw new InvalidOperationException("procedural terrain floor should keep CommandPlate out of cached terrain tiles; GridLayer draws it as a rounded field");
        }

        if (!TerrainFloorMath.IsNavigationLane(new Vector2(1700, 1368), new Vector2(3600, 2400)))
        {
            throw new InvalidOperationException("procedural terrain floor should expose stable navigation lane hints");
        }

        var dayPalette = WorldThemeMath.Palette(WorldVisualTheme.DayCommand);
        var nightPalette = WorldThemeMath.Palette(WorldVisualTheme.NightRadar);
        if (dayPalette.Background == nightPalette.Background
            || dayPalette.GroundFill == nightPalette.GroundFill
            || dayPalette.NavigationLine == nightPalette.NavigationLine)
        {
            throw new InvalidOperationException("world visual themes should expose distinct day-command and night-radar palettes");
        }

        var transitionPalette = WorldThemeMath.Palette(new WorldVisualThemeState(WorldVisualTheme.DayCommand, WorldVisualTheme.NightRadar, 0.5f, "test"));
        if (transitionPalette.Background == dayPalette.Background || transitionPalette.Background == nightPalette.Background)
        {
            throw new InvalidOperationException("world visual theme transitions should blend between day and night palettes");
        }

        var dayProfile = WorldThemeMath.Profile(WorldVisualTheme.DayCommand);
        var nightProfile = WorldThemeMath.Profile(WorldVisualTheme.NightRadar);
        if (dayProfile.PlanningClarity <= nightProfile.PlanningClarity
            || dayProfile.RebuildingFocus <= nightProfile.RebuildingFocus
            || dayProfile.RepairFocus <= nightProfile.RepairFocus
            || dayProfile.ResourceReadability <= nightProfile.ResourceReadability
            || dayProfile.TerrainReadability <= nightProfile.TerrainReadability
            || nightProfile.Pressure <= dayProfile.Pressure
            || nightProfile.FogProminence <= dayProfile.FogProminence
            || nightProfile.LightNetworkSafety <= dayProfile.LightNetworkSafety
            || nightProfile.SignalNoise <= dayProfile.SignalNoise
            || nightProfile.DefensiveCaution <= dayProfile.DefensiveCaution)
        {
            throw new InvalidOperationException("world tactical theme profiles should make daytime read as planning/rebuilding/repair/resource mode and nighttime read as fog-pressure/signal-safety/defensive-caution mode");
        }

        var transitionProfile = WorldThemeMath.Profile(new WorldVisualThemeState(WorldVisualTheme.DayCommand, WorldVisualTheme.NightRadar, 0.5f, "test"));
        if (transitionProfile.PlanningClarity >= dayProfile.PlanningClarity
            || transitionProfile.PlanningClarity <= nightProfile.PlanningClarity
            || transitionProfile.Pressure <= dayProfile.Pressure
            || transitionProfile.Pressure >= nightProfile.Pressure)
        {
            throw new InvalidOperationException("world tactical theme profile transitions should blend semantic emphasis values between day and night");
        }

        if (WorldThemeMath.Palette(new WorldVisualThemeState(WorldVisualTheme.DayCommand, WorldVisualTheme.NightRadar, 1, "test")).Background != nightPalette.Background
            || WorldThemeMath.Profile(new WorldVisualThemeState(WorldVisualTheme.DayCommand, WorldVisualTheme.NightRadar, 1, "test")).Pressure != nightProfile.Pressure)
        {
            throw new InvalidOperationException("completed visual theme transitions should resolve to the target theme palette and tactical profile");
        }

        if (WorldThemeMath.ResourceAtmosphereFor(WorldVisualTheme.DayCommand) != ResourceAtmosphere.Day
            || WorldThemeMath.ResourceAtmosphereFor(WorldVisualTheme.FogMorning) != ResourceAtmosphere.Fog
            || WorldThemeMath.ResourceAtmosphereFor(WorldVisualTheme.DuskDefense) != ResourceAtmosphere.Night
            || WorldThemeMath.ResourceAtmosphereFor(WorldVisualTheme.NightRadar) != ResourceAtmosphere.Night)
        {
            throw new InvalidOperationException("world visual themes should map explicitly to the sim resource atmosphere used by live signal systems");
        }

        var themeBattlefield = new UnitBattlefield();
        var themedState = new WorldPresentationEnvironment(new Vector2(3600, 2400));
        var observedThemes = new List<WorldVisualThemeState>();
        themedState.VisualThemeChanged += observedThemes.Add;
        themedState.SetVisualTheme(WorldVisualTheme.DayCommand, "objective-phase", transitionProgress: 1);
        themedState.SetVisualTheme(WorldVisualTheme.NightRadar, "signal-loss", transitionProgress: 0);
        themedState.Update(0.5f, themeBattlefield, PlayerSlotId.One);
        if (themedState.VisualTheme.Driver != "signal-loss"
            || themedState.VisualTheme.Target != WorldVisualTheme.NightRadar
            || themedState.VisualTheme.TransitionProgress <= 0
            || observedThemes.Count < 3)
        {
            throw new InvalidOperationException("game state should expose scriptable visual theme transition hooks for mission events");
        }

        if (themedState.ResourceAtmosphere != ResourceAtmosphere.Day)
        {
            throw new InvalidOperationException("in-progress day-to-night visual theme transitions should keep the current sim atmosphere until the transition completes");
        }

        for (var step = 0; step < 100; step++)
        {
            themedState.Update(0.05f, themeBattlefield, PlayerSlotId.One);
        }
        if (themedState.ResourceAtmosphere != ResourceAtmosphere.Night)
        {
            throw new InvalidOperationException("completed visual theme transitions should update presentation resource atmosphere to the target theme");
        }

        var signalNodes = SignalNetworkMath.CreateDefaultNetwork(new Vector2(3600, 2400));
        if (signalNodes.Count < 16
            || !signalNodes.Any(node => node.Kind == SignalNodeKind.RoadLight)
            || !signalNodes.Any(node => node.Kind == SignalNodeKind.SignalTower)
            || !signalNodes.Any(node => node.Kind == SignalNodeKind.SafeZone))
        {
            throw new InvalidOperationException("signal network should include road lights, signal towers, and safe-zone nodes");
        }

        if (SignalNetworkMath.ThemeGlowStrength(new WorldVisualThemeState(WorldVisualTheme.NightRadar, WorldVisualTheme.NightRadar, 1, "test"))
            <= SignalNetworkMath.ThemeGlowStrength(new WorldVisualThemeState(WorldVisualTheme.DayCommand, WorldVisualTheme.DayCommand, 1, "test")))
        {
            throw new InvalidOperationException("signal nodes should glow more strongly at night than during daytime planning mode");
        }

        var signalProbeState = new WorldPresentationEnvironment(new Vector2(3600, 2400));
        signalProbeState.FogOfWar.ClearMemory();
        var probeNode = signalProbeState.SignalNodes.First(node => node.Kind == SignalNodeKind.SignalTower);
        signalProbeState.SetVisualTheme(WorldVisualTheme.DayCommand, "test-day", transitionProgress: 1);
        signalProbeState.Update(0.2f, themeBattlefield, PlayerSlotId.One);
        if (signalProbeState.FogOfWar.IsVisible(probeNode.Position))
        {
            throw new InvalidOperationException("daytime signal nodes should read as engineering/control infrastructure without acting as night vision sources");
        }

        signalProbeState.SetVisualTheme(WorldVisualTheme.NightRadar, "test-night", transitionProgress: 1);
        signalProbeState.Update(0.2f, themeBattlefield, PlayerSlotId.One);
        if (!signalProbeState.FogOfWar.IsVisible(probeNode.Position))
        {
            throw new InvalidOperationException("nighttime signal nodes should contribute safety/vision to the fog network");
        }

        var dayTiles = TerrainFloorMath.CreateTiles(new Vector2(3600, 2400), dayPalette);
        var nightTiles = TerrainFloorMath.CreateTiles(new Vector2(3600, 2400), nightPalette);
        if (dayTiles.Count != nightTiles.Count
            || dayTiles.Select(tile => tile.Kind).SequenceEqual(nightTiles.Select(tile => tile.Kind)) == false
            || dayTiles[0].Fill == nightTiles[0].Fill)
        {
            throw new InvalidOperationException("terrain floor should preserve procedural layout while adapting colors to the active visual theme");
        }
    }
}
