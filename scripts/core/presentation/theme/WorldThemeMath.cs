using Godot;

namespace ProceduralRts.Core;

public static class WorldThemeMath
{
    public static WorldThemePalette Palette(WorldVisualThemeState state)
    {
        var current = Palette(state.Current);
        if (state.Current != state.Target && state.TransitionProgress >= 1)
        {
            return Palette(state.Target);
        }

        if (!state.IsTransitioning)
        {
            return current;
        }

        return Lerp(current, Palette(state.Target), Mathf.Clamp(state.TransitionProgress, 0, 1));
    }

    public static WorldThemeTacticalProfile Profile(WorldVisualThemeState state)
    {
        var current = Profile(state.Current);
        if (state.Current != state.Target && state.TransitionProgress >= 1)
        {
            return Profile(state.Target);
        }

        if (!state.IsTransitioning)
        {
            return current;
        }

        return Lerp(current, Profile(state.Target), Mathf.Clamp(state.TransitionProgress, 0, 1));
    }

    public static ResourceAtmosphere ResourceAtmosphereFor(WorldVisualThemeState state)
    {
        var effectiveTheme = state.Current != state.Target && state.TransitionProgress >= 1
            ? state.Target
            : state.Current;
        return ResourceAtmosphereFor(effectiveTheme);
    }

    public static ResourceAtmosphere ResourceAtmosphereFor(WorldVisualTheme theme)
    {
        return theme switch
        {
            WorldVisualTheme.DayCommand => ResourceAtmosphere.Day,
            WorldVisualTheme.FogMorning => ResourceAtmosphere.Fog,
            WorldVisualTheme.DuskDefense => ResourceAtmosphere.Night,
            WorldVisualTheme.NightRadar => ResourceAtmosphere.Night,
            _ => ResourceAtmosphere.Day,
        };
    }

    public static WorldThemePalette Palette(WorldVisualTheme theme)
    {
        return theme switch
        {
            WorldVisualTheme.DayCommand => new WorldThemePalette(
                Background: SoftOldCityPalette.Paper,
                GridMinor: new Color(SoftOldCityPalette.Text, 0.050f),
                GridMajor: new Color(SoftOldCityPalette.Border, 0.105f),
                Boundary: new Color(SoftOldCityPalette.Border, 0.26f),
                GroundFill: new Color(SoftOldCityPalette.PaperStrong, 0.48f),
                GroundEdge: new Color(SoftOldCityPalette.TextDim, 0.115f),
                CommandFill: new Color(SoftOldCityPalette.PaperSubtle, 0.56f),
                CommandEdge: new Color(SoftOldCityPalette.WarmCommand, 0.18f),
                NavigationFill: new Color(SoftOldCityPalette.PaperSubtle, 0.52f),
                NavigationEdge: new Color(SoftOldCityPalette.Repair, 0.16f),
                CoastFill: new Color(SoftOldCityPalette.PaperSubtle, 0.48f),
                CoastEdge: new Color(SoftOldCityPalette.Route, 0.14f),
                WaterFill: new Color(SoftOldCityPalette.Water, 0.38f),
                WaterEdge: new Color(SoftOldCityPalette.Repair, 0.16f),
                NavigationLine: new Color(SoftOldCityPalette.Repair, 0.16f),
                StrataLine: new Color(SoftOldCityPalette.Border, 0.052f)),
            WorldVisualTheme.FogMorning => new WorldThemePalette(
                Background: SoftOldCityPalette.FogPaper,
                GridMinor: new Color(SoftOldCityPalette.FogText, 0.040f),
                GridMajor: new Color(SoftOldCityPalette.FogBorder, 0.082f),
                Boundary: new Color(SoftOldCityPalette.FogBorder, 0.22f),
                GroundFill: new Color(SoftOldCityPalette.FogPaperStrong, 0.42f),
                GroundEdge: new Color(SoftOldCityPalette.FogDim, 0.10f),
                CommandFill: new Color(SoftOldCityPalette.FogPaperSubtle, 0.46f),
                CommandEdge: new Color(SoftOldCityPalette.FogCommand, 0.14f),
                NavigationFill: new Color(SoftOldCityPalette.FogPaperSubtle, 0.46f),
                NavigationEdge: new Color(SoftOldCityPalette.FogRoute, 0.12f),
                CoastFill: new Color(SoftOldCityPalette.FogPaperSubtle, 0.40f),
                CoastEdge: new Color(SoftOldCityPalette.FogRoute, 0.11f),
                WaterFill: new Color(SoftOldCityPalette.FogWater, 0.30f),
                WaterEdge: new Color(SoftOldCityPalette.FogBorder, 0.12f),
                NavigationLine: new Color(SoftOldCityPalette.FogRoute, 0.12f),
                StrataLine: new Color(SoftOldCityPalette.FogBorder, 0.040f)),
            WorldVisualTheme.DuskDefense => new WorldThemePalette(
                Background: SoftOldCityPalette.DuskPanel,
                GridMinor: new Color(SoftOldCityPalette.DuskLine, 0.035f),
                GridMajor: new Color(SoftOldCityPalette.DuskCommand, 0.085f),
                Boundary: new Color(SoftOldCityPalette.DuskLine, 0.20f),
                GroundFill: new Color(SoftOldCityPalette.DuskPanelStrong, 0.40f),
                GroundEdge: new Color(SoftOldCityPalette.DuskLine, 0.060f),
                CommandFill: new Color(SoftOldCityPalette.Ink, 0.42f),
                CommandEdge: new Color(SoftOldCityPalette.DuskCommand, 0.11f),
                NavigationFill: new Color(SoftOldCityPalette.Repair, 0.34f),
                NavigationEdge: new Color(SoftOldCityPalette.DuskRepair, 0.10f),
                CoastFill: new Color(SoftOldCityPalette.WarmCommand, 0.32f),
                CoastEdge: new Color(SoftOldCityPalette.DuskRoute, 0.09f),
                WaterFill: new Color(SoftOldCityPalette.NightWater, 0.34f),
                WaterEdge: new Color(SoftOldCityPalette.DuskRepair, 0.10f),
                NavigationLine: new Color(SoftOldCityPalette.DuskCommand, 0.10f),
                StrataLine: new Color(SoftOldCityPalette.DuskLine, 0.030f)),
            WorldVisualTheme.NightRadar => new WorldThemePalette(
                Background: SoftOldCityPalette.NightBackground,
                GridMinor: new Color(SoftOldCityPalette.NightRadarSoft, 0.042f),
                GridMajor: new Color(SoftOldCityPalette.NightRadar, 0.10f),
                Boundary: new Color(SoftOldCityPalette.NightRadarSoft, 0.24f),
                GroundFill: new Color(SoftOldCityPalette.NightGround, 0.28f),
                GroundEdge: new Color(SoftOldCityPalette.NightMuted, 0.045f),
                CommandFill: new Color(SoftOldCityPalette.DuskPanelSubtle, 0.34f),
                CommandEdge: new Color(SoftOldCityPalette.Cargo, 0.09f),
                NavigationFill: new Color(SoftOldCityPalette.Repair, 0.32f),
                NavigationEdge: new Color(SoftOldCityPalette.NightRadar, 0.10f),
                CoastFill: new Color(SoftOldCityPalette.WarmCommand, 0.30f),
                CoastEdge: new Color(SoftOldCityPalette.InnerLight, 0.09f),
                WaterFill: new Color(SoftOldCityPalette.NightWater, 0.36f),
                WaterEdge: new Color(SoftOldCityPalette.NightWaterEdge, 0.11f),
                NavigationLine: new Color(SoftOldCityPalette.NightRadar, 0.105f),
                StrataLine: new Color(SoftOldCityPalette.NightMuted, 0.035f)),
            _ => Palette(WorldVisualTheme.DayCommand),
        };
    }

    public static WorldThemeTacticalProfile Profile(WorldVisualTheme theme)
    {
        return theme switch
        {
            WorldVisualTheme.DayCommand => new WorldThemeTacticalProfile(
                PlanningClarity: 0.96f,
                RebuildingFocus: 0.90f,
                RepairFocus: 0.84f,
                ResourceReadability: 0.92f,
                TerrainReadability: 0.88f,
                Pressure: 0.28f,
                FogProminence: 0.30f,
                LightNetworkSafety: 0.28f,
                SignalNoise: 0.12f,
                DefensiveCaution: 0.38f),
            WorldVisualTheme.FogMorning => new WorldThemeTacticalProfile(
                PlanningClarity: 0.72f,
                RebuildingFocus: 0.58f,
                RepairFocus: 0.60f,
                ResourceReadability: 0.70f,
                TerrainReadability: 0.66f,
                Pressure: 0.46f,
                FogProminence: 0.78f,
                LightNetworkSafety: 0.45f,
                SignalNoise: 0.34f,
                DefensiveCaution: 0.62f),
            WorldVisualTheme.DuskDefense => new WorldThemeTacticalProfile(
                PlanningClarity: 0.58f,
                RebuildingFocus: 0.50f,
                RepairFocus: 0.58f,
                ResourceReadability: 0.62f,
                TerrainReadability: 0.60f,
                Pressure: 0.86f,
                FogProminence: 0.76f,
                LightNetworkSafety: 0.92f,
                SignalNoise: 0.68f,
                DefensiveCaution: 0.84f),
            WorldVisualTheme.NightRadar => new WorldThemeTacticalProfile(
                PlanningClarity: 0.46f,
                RebuildingFocus: 0.34f,
                RepairFocus: 0.36f,
                ResourceReadability: 0.50f,
                TerrainReadability: 0.54f,
                Pressure: 0.94f,
                FogProminence: 0.90f,
                LightNetworkSafety: 1.00f,
                SignalNoise: 0.82f,
                DefensiveCaution: 0.88f),
            _ => Profile(WorldVisualTheme.DayCommand),
        };
    }

    public static WorldThemePalette Lerp(WorldThemePalette from, WorldThemePalette to, float amount)
    {
        return new WorldThemePalette(
            from.Background.Lerp(to.Background, amount),
            from.GridMinor.Lerp(to.GridMinor, amount),
            from.GridMajor.Lerp(to.GridMajor, amount),
            from.Boundary.Lerp(to.Boundary, amount),
            from.GroundFill.Lerp(to.GroundFill, amount),
            from.GroundEdge.Lerp(to.GroundEdge, amount),
            from.CommandFill.Lerp(to.CommandFill, amount),
            from.CommandEdge.Lerp(to.CommandEdge, amount),
            from.NavigationFill.Lerp(to.NavigationFill, amount),
            from.NavigationEdge.Lerp(to.NavigationEdge, amount),
            from.CoastFill.Lerp(to.CoastFill, amount),
            from.CoastEdge.Lerp(to.CoastEdge, amount),
            from.WaterFill.Lerp(to.WaterFill, amount),
            from.WaterEdge.Lerp(to.WaterEdge, amount),
            from.NavigationLine.Lerp(to.NavigationLine, amount),
            from.StrataLine.Lerp(to.StrataLine, amount));
    }

    public static WorldThemeTacticalProfile Lerp(WorldThemeTacticalProfile from, WorldThemeTacticalProfile to, float amount)
    {
        return new WorldThemeTacticalProfile(
            Mathf.Lerp(from.PlanningClarity, to.PlanningClarity, amount),
            Mathf.Lerp(from.RebuildingFocus, to.RebuildingFocus, amount),
            Mathf.Lerp(from.RepairFocus, to.RepairFocus, amount),
            Mathf.Lerp(from.ResourceReadability, to.ResourceReadability, amount),
            Mathf.Lerp(from.TerrainReadability, to.TerrainReadability, amount),
            Mathf.Lerp(from.Pressure, to.Pressure, amount),
            Mathf.Lerp(from.FogProminence, to.FogProminence, amount),
            Mathf.Lerp(from.LightNetworkSafety, to.LightNetworkSafety, amount),
            Mathf.Lerp(from.SignalNoise, to.SignalNoise, amount),
            Mathf.Lerp(from.DefensiveCaution, to.DefensiveCaution, amount));
    }
}
