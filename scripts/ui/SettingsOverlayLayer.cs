using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public partial class SettingsOverlayLayer : CanvasLayer
{
    private static readonly Color Ink = new("#d8f7ff");
    private static readonly Color InkMuted = new("#8095aa");
    private static readonly Color Cyan = new("#59f1ff");
    private static readonly Color Mint = new("#8fffe1");
    private static readonly Color Amber = new("#f6c55c");
    private static readonly Color Danger = new("#ff5d75");

    private Control _root = null!;
    private SettingsBackdrop _backdrop = null!;
    private Panel _panel = null!;
    private Label _title = null!;
    private Label _meta = null!;
    private CheckButton _fullscreen = null!;
    private Label _resolutionLabel = null!;
    private OptionButton _resolution = null!;
    private Label _languageLabel = null!;
    private OptionButton _language = null!;
    private Label _frameRateLabel = null!;
    private OptionButton _frameRate = null!;
    private Label _ownerColorsLabel = null!;
    private OptionButton _ownerColors = null!;
    private CheckButton _impactShake = null!;
    private Label _controlsLabel = null!;
    private Label _controlsOverview = null!;
    private Label _volumeLabel = null!;
    private HSlider _volume = null!;
    private Label _volumeValue = null!;
    private Button _close = null!;
    private Label _status = null!;
    private float _elapsed;
    public bool IsOpen => _root is not null && _root.Visible;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        Layer = 65;

        _root = new Control
        {
            Name = "SettingsRoot",
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Stop,
            ProcessMode = ProcessModeEnum.Always,
        };
        _root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(_root);

        _backdrop = new SettingsBackdrop { Name = "Backdrop" };
        _backdrop.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _root.AddChild(_backdrop);

        BuildPanel();
        RefreshControls();
    }

    public override void _Process(double delta)
    {
        if (!_root.Visible)
        {
            return;
        }

        _elapsed += (float)delta;
        _backdrop.Elapsed = _elapsed;
        _backdrop.QueueRedraw();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_root.Visible || @event is not InputEventKey key || !key.Pressed || key.Echo || key.Keycode != Key.Escape)
        {
            return;
        }

        Close();
        GetViewport().SetInputAsHandled();
    }

    public void Open()
    {
        RefreshControls();
        _root.Visible = true;
        _status.Text = GameText.T("settings.ready");
        _fullscreen.GrabFocus();
    }

    public void Close()
    {
        _root.Visible = false;
    }

    private void BuildPanel()
    {
        _panel = UiFactory.MakePanel("Panel", new Color("#071019", 0.97f), new Color("#8fffe1", 0.52f));
        _panel.SetAnchorsPreset(Control.LayoutPreset.Center);
        _panel.OffsetLeft = -264;
        _panel.OffsetTop = -332;
        _panel.OffsetRight = 264;
        _panel.OffsetBottom = 332;
        _root.AddChild(_panel);

        _title = UiFactory.MakeLabel(GameText.T("settings.title"), 24, Mint);
        _title.Position = new Vector2(28, 24);
        _title.CustomMinimumSize = new Vector2(320, 34);
        _panel.AddChild(_title);

        _meta = UiFactory.MakeLabel(GameText.T("settings.meta"), 12, InkMuted);
        _meta.Position = new Vector2(30, 62);
        _meta.CustomMinimumSize = new Vector2(420, 22);
        _panel.AddChild(_meta);

        _fullscreen = new CheckButton
        {
            Text = GameText.T("settings.fullscreen"),
            Position = new Vector2(28, 108),
            CustomMinimumSize = new Vector2(420, 36),
            FocusMode = Control.FocusModeEnum.All,
            TooltipText = GameText.T("settings.fullscreen.tooltip"),
        };
        UiFactory.StyleButton(_fullscreen, Cyan);
        _fullscreen.Toggled += OnFullscreenToggled;
        _panel.AddChild(_fullscreen);

        _resolutionLabel = UiFactory.MakeLabel(GameText.T("settings.resolution"), 12, InkMuted);
        _resolutionLabel.Position = new Vector2(32, 160);
        _resolutionLabel.CustomMinimumSize = new Vector2(160, 22);
        _panel.AddChild(_resolutionLabel);

        _resolution = new OptionButton
        {
            Position = new Vector2(204, 152),
            CustomMinimumSize = new Vector2(274, 38),
            FocusMode = Control.FocusModeEnum.All,
            TooltipText = GameText.T("settings.resolution.tooltip"),
        };
        for (var index = 0; index < DisplayAudioSettings.SupportedResolutions.Length; index++)
        {
            _resolution.AddItem(DisplayAudioSettings.ResolutionLabel(index), index);
        }
        UiFactory.StyleButton(_resolution, Amber);
        _resolution.ItemSelected += OnResolutionSelected;
        _panel.AddChild(_resolution);

        _languageLabel = UiFactory.MakeLabel(GameText.T("settings.language"), 12, InkMuted);
        _languageLabel.Position = new Vector2(32, 216);
        _languageLabel.CustomMinimumSize = new Vector2(160, 22);
        _panel.AddChild(_languageLabel);

        _language = new OptionButton
        {
            Position = new Vector2(204, 208),
            CustomMinimumSize = new Vector2(274, 38),
            FocusMode = Control.FocusModeEnum.All,
            TooltipText = GameText.T("settings.language.tooltip"),
        };
        _language.AddItem(GameText.T("settings.language.en"), (int)GameLanguage.English);
        _language.AddItem(GameText.T("settings.language.zh"), (int)GameLanguage.ChineseSimplified);
        UiFactory.StyleButton(_language, Mint);
        _language.ItemSelected += OnLanguageSelected;
        _panel.AddChild(_language);

        _frameRateLabel = UiFactory.MakeLabel(GameText.T("settings.frameRate"), 12, InkMuted);
        _frameRateLabel.Position = new Vector2(32, 264);
        _frameRateLabel.CustomMinimumSize = new Vector2(160, 22);
        _panel.AddChild(_frameRateLabel);

        _frameRate = new OptionButton
        {
            Position = new Vector2(204, 256),
            CustomMinimumSize = new Vector2(274, 38),
            FocusMode = Control.FocusModeEnum.All,
            TooltipText = GameText.T("settings.frameRate.tooltip"),
        };
        _frameRate.AddItem(DisplayAudioSettings.FrameRateLabel(FrameRateMode.Off), (int)FrameRateMode.Off);
        _frameRate.AddItem(DisplayAudioSettings.FrameRateLabel(FrameRateMode.VSync), (int)FrameRateMode.VSync);
        _frameRate.AddItem(DisplayAudioSettings.FrameRateLabel(FrameRateMode.Fps60), (int)FrameRateMode.Fps60);
        _frameRate.AddItem(DisplayAudioSettings.FrameRateLabel(FrameRateMode.Fps144), (int)FrameRateMode.Fps144);
        UiFactory.StyleButton(_frameRate, Cyan);
        _frameRate.ItemSelected += OnFrameRateSelected;
        _panel.AddChild(_frameRate);

        _ownerColorsLabel = UiFactory.MakeLabel(GameText.T("settings.ownerColors"), 12, InkMuted);
        _ownerColorsLabel.Position = new Vector2(32, 318);
        _ownerColorsLabel.CustomMinimumSize = new Vector2(160, 22);
        _panel.AddChild(_ownerColorsLabel);

        _ownerColors = new OptionButton
        {
            Position = new Vector2(204, 310),
            CustomMinimumSize = new Vector2(274, 38),
            FocusMode = Control.FocusModeEnum.All,
            TooltipText = GameText.T("settings.ownerColors.tooltip"),
        };
        _ownerColors.AddItem(DisplayAudioSettings.OwnerColorPaletteLabel(OwnerColorPaletteMode.Standard), (int)OwnerColorPaletteMode.Standard);
        _ownerColors.AddItem(DisplayAudioSettings.OwnerColorPaletteLabel(OwnerColorPaletteMode.ColorblindSafe), (int)OwnerColorPaletteMode.ColorblindSafe);
        UiFactory.StyleButton(_ownerColors, Mint);
        _ownerColors.ItemSelected += OnOwnerColorsSelected;
        _panel.AddChild(_ownerColors);

        _impactShake = new CheckButton
        {
            Text = GameText.T("settings.impactShake"),
            Position = new Vector2(28, 366),
            CustomMinimumSize = new Vector2(450, 36),
            FocusMode = Control.FocusModeEnum.All,
            TooltipText = GameText.T("settings.impactShake.tooltip"),
        };
        UiFactory.StyleButton(_impactShake, Cyan);
        _impactShake.Toggled += OnImpactShakeToggled;
        _panel.AddChild(_impactShake);

        _controlsLabel = UiFactory.MakeLabel(GameText.T("settings.controls"), 12, InkMuted);
        _controlsLabel.Position = new Vector2(32, 420);
        _controlsLabel.CustomMinimumSize = new Vector2(160, 22);
        _panel.AddChild(_controlsLabel);

        _controlsOverview = UiFactory.MakeLabel(SettingsControlsOverviewText(), 11, Ink);
        _controlsOverview.Name = "ControlsBindingOverview";
        _controlsOverview.Position = new Vector2(204, 408);
        _controlsOverview.CustomMinimumSize = new Vector2(274, 64);
        _controlsOverview.TooltipText = GameText.T("settings.controls.tooltip");
        _panel.AddChild(_controlsOverview);

        _volumeLabel = UiFactory.MakeLabel(GameText.T("settings.masterAudio"), 12, InkMuted);
        _volumeLabel.Position = new Vector2(32, 492);
        _volumeLabel.CustomMinimumSize = new Vector2(160, 22);
        _panel.AddChild(_volumeLabel);

        _volume = new HSlider
        {
            Position = new Vector2(204, 486),
            CustomMinimumSize = new Vector2(208, 36),
            MinValue = 0,
            MaxValue = 100,
            Step = 1,
            FocusMode = Control.FocusModeEnum.All,
            TooltipText = GameText.T("settings.masterAudio.tooltip"),
        };
        _volume.ValueChanged += OnVolumeChanged;
        _panel.AddChild(_volume);

        _volumeValue = UiFactory.MakeLabel("0%", 13, Ink);
        _volumeValue.HorizontalAlignment = HorizontalAlignment.Right;
        _volumeValue.Position = new Vector2(420, 492);
        _volumeValue.CustomMinimumSize = new Vector2(58, 22);
        _panel.AddChild(_volumeValue);

        _close = UiFactory.MakeButton(GameText.T("settings.close"), Danger);
        _close.Position = new Vector2(28, 548);
        _close.CustomMinimumSize = new Vector2(450, 44);
        _close.Pressed += Close;
        _panel.AddChild(_close);

        _status = UiFactory.MakeLabel(GameText.T("settings.hint"), 11, InkMuted);
        _status.HorizontalAlignment = HorizontalAlignment.Center;
        _status.Position = new Vector2(28, 612);
        _status.CustomMinimumSize = new Vector2(450, 20);
        _panel.AddChild(_status);
    }

    private void RefreshControls()
    {
        if (_fullscreen is null)
        {
            return;
        }

        _fullscreen.ButtonPressed = DisplayAudioSettings.Fullscreen;
        _resolution.Select(DisplayAudioSettings.ResolutionIndex);
        _resolution.Disabled = DisplayAudioSettings.Fullscreen;
        _language.Select((int)DisplayAudioSettings.Language);
        _frameRate.Select((int)DisplayAudioSettings.FrameRate);
        _ownerColors.Select((int)DisplayAudioSettings.OwnerColors);
        _impactShake.ButtonPressed = DisplayAudioSettings.ImpactScreenShake;
        _volume.Value = Mathf.RoundToInt(DisplayAudioSettings.MasterVolume * 100);
        _volumeValue.Text = $"{Mathf.RoundToInt(DisplayAudioSettings.MasterVolume * 100)}%";
        RefreshText();
    }

    private void OnFullscreenToggled(bool enabled)
    {
        DisplayAudioSettings.ApplyFullscreen(enabled);
        _resolution.Disabled = enabled;
        _status.Text = enabled
            ? GameText.T("settings.fullscreenEnabled")
            : GameText.Format("settings.windowed", DisplayAudioSettings.ResolutionLabel(DisplayAudioSettings.ResolutionIndex));
    }

    private void OnResolutionSelected(long index)
    {
        DisplayAudioSettings.ApplyResolution((int)index);
        _status.Text = GameText.Format("settings.resolutionStatus", DisplayAudioSettings.ResolutionLabel((int)index));
    }

    private void OnVolumeChanged(double value)
    {
        DisplayAudioSettings.ApplyMasterVolume((float)value / 100f);
        _volumeValue.Text = $"{Mathf.RoundToInt((float)value)}%";
        _status.Text = GameText.Format("settings.masterAudioStatus", _volumeValue.Text);
    }

    private void OnFrameRateSelected(long index)
    {
        var mode = Enum.IsDefined(typeof(FrameRateMode), (int)index)
            ? (FrameRateMode)(int)index
            : FrameRateMode.VSync;
        DisplayAudioSettings.ApplyFrameRateMode(mode);
        _status.Text = GameText.Format("settings.frameRateStatus", DisplayAudioSettings.FrameRateLabel(mode));
    }

    private void OnOwnerColorsSelected(long index)
    {
        var mode = Enum.IsDefined(typeof(OwnerColorPaletteMode), (int)index)
            ? (OwnerColorPaletteMode)(int)index
            : OwnerColorPaletteMode.Standard;
        DisplayAudioSettings.ApplyOwnerColorPalette(mode);
        _status.Text = GameText.Format("settings.ownerColorsStatus", DisplayAudioSettings.OwnerColorPaletteLabel(mode));
    }

    private void OnImpactShakeToggled(bool enabled)
    {
        DisplayAudioSettings.ApplyImpactScreenShake(enabled);
        _status.Text = GameText.T(enabled ? "settings.impactShakeEnabled" : "settings.impactShakeDisabled");
    }

    private void OnLanguageSelected(long index)
    {
        var language = Enum.IsDefined(typeof(GameLanguage), (int)index)
            ? (GameLanguage)(int)index
            : GameLanguage.English;
        DisplayAudioSettings.ApplyLanguage(language);
        RefreshText();
        _status.Text = GameText.Format("settings.languageStatus", DisplayAudioSettings.LanguageLabel(language));
    }

    private void RefreshText()
    {
        _title.Text = GameText.T("settings.title");
        _meta.Text = GameText.T("settings.meta");
        _fullscreen.Text = GameText.T("settings.fullscreen");
        _fullscreen.TooltipText = GameText.T("settings.fullscreen.tooltip");
        _resolutionLabel.Text = GameText.T("settings.resolution");
        _resolution.TooltipText = GameText.T("settings.resolution.tooltip");
        _languageLabel.Text = GameText.T("settings.language");
        _language.TooltipText = GameText.T("settings.language.tooltip");
        _language.SetItemText(0, GameText.T("settings.language.en"));
        _language.SetItemText(1, GameText.T("settings.language.zh"));
        _frameRateLabel.Text = GameText.T("settings.frameRate");
        _frameRate.TooltipText = GameText.T("settings.frameRate.tooltip");
        _frameRate.SetItemText((int)FrameRateMode.Off, DisplayAudioSettings.FrameRateLabel(FrameRateMode.Off));
        _frameRate.SetItemText((int)FrameRateMode.VSync, DisplayAudioSettings.FrameRateLabel(FrameRateMode.VSync));
        _frameRate.SetItemText((int)FrameRateMode.Fps60, DisplayAudioSettings.FrameRateLabel(FrameRateMode.Fps60));
        _frameRate.SetItemText((int)FrameRateMode.Fps144, DisplayAudioSettings.FrameRateLabel(FrameRateMode.Fps144));
        _ownerColorsLabel.Text = GameText.T("settings.ownerColors");
        _ownerColors.TooltipText = GameText.T("settings.ownerColors.tooltip");
        _ownerColors.SetItemText((int)OwnerColorPaletteMode.Standard, DisplayAudioSettings.OwnerColorPaletteLabel(OwnerColorPaletteMode.Standard));
        _ownerColors.SetItemText((int)OwnerColorPaletteMode.ColorblindSafe, DisplayAudioSettings.OwnerColorPaletteLabel(OwnerColorPaletteMode.ColorblindSafe));
        _impactShake.Text = GameText.T("settings.impactShake");
        _impactShake.TooltipText = GameText.T("settings.impactShake.tooltip");
        _controlsLabel.Text = GameText.T("settings.controls");
        _controlsOverview.Text = SettingsControlsOverviewText();
        _controlsOverview.TooltipText = GameText.T("settings.controls.tooltip");
        _volumeLabel.Text = GameText.T("settings.masterAudio");
        _volume.TooltipText = GameText.T("settings.masterAudio.tooltip");
        _close.Text = GameText.T("settings.close");
        _close.TooltipText = GameText.T("settings.close");
    }

    private static string SettingsControlsOverviewText()
    {
        var rowKeys = ControlBindingCatalog.SettingsOverviewRowKeys;
        var rows = new string[rowKeys.Count];
        for (var index = 0; index < rowKeys.Count; index++)
        {
            rows[index] = GameText.T(rowKeys[index]);
        }

        return string.Join('\n', rows);
    }

    private partial class SettingsBackdrop : Control
    {
        public float Elapsed { get; set; }

        public override void _Draw()
        {
            DrawRect(new Rect2(Vector2.Zero, Size), new Color("#02060a", 0.76f));
            var drift = (Elapsed * 10) % 40;
            for (var x = -40 + drift; x <= Size.X + 40; x += 40)
            {
                DrawLine(new Vector2(x, 0), new Vector2(x, Size.Y), new Color("#8fffe1", 0.055f), 1, true);
            }

            var center = Size * 0.5f;
            DrawArc(center, 238, 0, Mathf.Tau, 128, new Color(Mint, 0.15f), 1.6f, true);
            DrawArc(center, 286, 0, Mathf.Tau, 128, new Color(Cyan, 0.10f), 1.3f, true);
        }
    }
}
