using Godot;

namespace ProceduralRts.Core;

public static class DisplayAudioSettings
{
    private const string SettingsPath = "user://settings.cfg";
    private const string DisplaySection = "display";
    private const string AudioSection = "audio";
    private const string UiSection = "ui";
    private const string FeedbackSection = "feedback";

    public static readonly Vector2I[] SupportedResolutions =
    [
        new(1280, 720),
        new(1600, 900),
        new(1920, 1080),
        new(2560, 1440),
    ];

    public static int ResolutionIndex { get; private set; }
    public static bool Fullscreen { get; private set; }
    public static FrameRateMode FrameRate { get; private set; } = FrameRateMode.VSync;
    public static OwnerColorPaletteMode OwnerColors { get; private set; } = OwnerColorPaletteMode.Standard;
    public static bool ImpactScreenShake { get; private set; } = true;
    public static float MasterVolume { get; private set; } = 0.75f;
    public static GameLanguage Language { get; private set; } = GameLanguage.English;
    private static bool _loaded;

    public static void LoadAndApply()
    {
        if (_loaded)
        {
            return;
        }

        _loaded = true;
        using var config = new ConfigFile();
        if (config.Load(SettingsPath) == Error.Ok)
        {
            Fullscreen = config.GetValue(DisplaySection, "fullscreen", false).AsBool();
            ResolutionIndex = Mathf.Clamp(
                config.GetValue(DisplaySection, "resolution_index", 0).AsInt32(),
                0,
                SupportedResolutions.Length - 1);
            FrameRate = FrameRateFromIndex(config.GetValue(DisplaySection, "frame_rate", (int)FrameRateMode.VSync).AsInt32());
            OwnerColors = OwnerColorPaletteFromIndex(config.GetValue(UiSection, "owner_colors", (int)OwnerColorPaletteMode.Standard).AsInt32());
            ImpactScreenShake = config.GetValue(FeedbackSection, "impact_screen_shake", true).AsBool();
            MasterVolume = Mathf.Clamp(config.GetValue(AudioSection, "master_volume", 0.75f).AsSingle(), 0, 1);
            Language = LanguageFromIndex(config.GetValue(UiSection, "language", (int)GameLanguage.English).AsInt32());
        }

        ApplyLanguage(Language, persist: false);
        ApplyResolution(ResolutionIndex, persist: false);
        ApplyFullscreen(Fullscreen, persist: false);
        ApplyFrameRateMode(FrameRate, persist: false);
        ApplyOwnerColorPalette(OwnerColors, persist: false);
        ApplyImpactScreenShake(ImpactScreenShake, persist: false);
        ApplyMasterVolume(MasterVolume, persist: false);
    }

    public static void ApplyFullscreen(bool fullscreen, bool persist = true)
    {
        Fullscreen = fullscreen;
        DisplayServer.WindowSetMode(fullscreen
            ? DisplayServer.WindowMode.Fullscreen
            : DisplayServer.WindowMode.Windowed);

        if (!fullscreen)
        {
            ApplyResolution(ResolutionIndex, persist: false);
        }

        if (persist)
        {
            Save();
        }
    }

    public static void ApplyResolution(int index, bool persist = true)
    {
        ResolutionIndex = Mathf.Clamp(index, 0, SupportedResolutions.Length - 1);
        if (!Fullscreen)
        {
            var resolution = SupportedResolutions[ResolutionIndex];
            DisplayServer.WindowSetSize(resolution);
            CenterWindow(resolution);
        }

        if (persist)
        {
            Save();
        }
    }

    public static void ApplyMasterVolume(float volume, bool persist = true)
    {
        MasterVolume = Mathf.Clamp(volume, 0, 1);
        var bus = AudioServer.GetBusIndex("Master");
        if (bus >= 0)
        {
            AudioServer.SetBusMute(bus, MasterVolume <= 0.001f);
            AudioServer.SetBusVolumeDb(bus, MasterVolume <= 0.001f
                ? -80
                : Mathf.LinearToDb(MasterVolume));
        }

        if (persist)
        {
            Save();
        }
    }

    public static void ApplyFrameRateMode(FrameRateMode mode, bool persist = true)
    {
        FrameRate = mode;
        Engine.PhysicsTicksPerSecond = 60;
        switch (mode)
        {
            case FrameRateMode.Off:
                DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
                Engine.MaxFps = 0;
                break;
            case FrameRateMode.Fps60:
                DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
                Engine.MaxFps = 60;
                break;
            case FrameRateMode.Fps144:
                DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
                Engine.MaxFps = 144;
                break;
            default:
                DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Enabled);
                Engine.MaxFps = 0;
                break;
        }

        if (persist)
        {
            Save();
        }
    }

    public static void ApplyLanguage(GameLanguage language, bool persist = true)
    {
        Language = language;
        GameText.CurrentLanguage = language;
        if (persist)
        {
            Save();
        }
    }

    public static void ApplyOwnerColorPalette(OwnerColorPaletteMode mode, bool persist = true)
    {
        OwnerColors = mode;
        if (persist)
        {
            Save();
        }
    }

    public static void ApplyImpactScreenShake(bool enabled, bool persist = true)
    {
        ImpactScreenShake = enabled;
        if (persist)
        {
            Save();
        }
    }

    public static string ResolutionLabel(int index)
    {
        var resolution = SupportedResolutions[Mathf.Clamp(index, 0, SupportedResolutions.Length - 1)];
        return $"{resolution.X} x {resolution.Y}";
    }

    public static string LanguageLabel(GameLanguage language)
    {
        return language switch
        {
            GameLanguage.ChineseSimplified => GameText.T("settings.language.zh"),
            _ => GameText.T("settings.language.en"),
        };
    }

    public static string FrameRateLabel(FrameRateMode mode)
    {
        return mode switch
        {
            FrameRateMode.Off => GameText.T("settings.frameRate.off"),
            FrameRateMode.Fps60 => GameText.T("settings.frameRate.60"),
            FrameRateMode.Fps144 => GameText.T("settings.frameRate.144"),
            _ => GameText.T("settings.frameRate.vsync"),
        };
    }

    public static string OwnerColorPaletteLabel(OwnerColorPaletteMode mode)
    {
        return mode switch
        {
            OwnerColorPaletteMode.ColorblindSafe => GameText.T("settings.ownerColors.colorblind"),
            _ => GameText.T("settings.ownerColors.standard"),
        };
    }

    private static GameLanguage LanguageFromIndex(int index)
    {
        return Enum.IsDefined(typeof(GameLanguage), index)
            ? (GameLanguage)index
            : GameLanguage.English;
    }

    private static FrameRateMode FrameRateFromIndex(int index)
    {
        return Enum.IsDefined(typeof(FrameRateMode), index)
            ? (FrameRateMode)index
            : FrameRateMode.VSync;
    }

    private static OwnerColorPaletteMode OwnerColorPaletteFromIndex(int index)
    {
        return Enum.IsDefined(typeof(OwnerColorPaletteMode), index)
            ? (OwnerColorPaletteMode)index
            : OwnerColorPaletteMode.Standard;
    }

    private static void CenterWindow(Vector2I resolution)
    {
        var screen = DisplayServer.WindowGetCurrentScreen();
        var screenSize = DisplayServer.ScreenGetSize(screen);
        var position = (screenSize - resolution) / 2;
        DisplayServer.WindowSetPosition(new Vector2I(Mathf.Max(0, position.X), Mathf.Max(0, position.Y)));
    }

    private static void Save()
    {
        using var config = new ConfigFile();
        config.SetValue(DisplaySection, "fullscreen", Fullscreen);
        config.SetValue(DisplaySection, "resolution_index", ResolutionIndex);
        config.SetValue(DisplaySection, "frame_rate", (int)FrameRate);
        config.SetValue(AudioSection, "master_volume", MasterVolume);
        config.SetValue(UiSection, "language", (int)Language);
        config.SetValue(UiSection, "owner_colors", (int)OwnerColors);
        config.SetValue(FeedbackSection, "impact_screen_shake", ImpactScreenShake);
        config.Save(SettingsPath);
    }
}
