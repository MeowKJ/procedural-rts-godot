using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public partial class HudLayer : CanvasLayer
{
    public Action<ProductionKind>? ProductionRequested { get; init; }
    public Action<string>? ProductionDesignRequested { get; init; }
    public Action? CancelProductionRequested { get; init; }
    public Action<Vector2>? MinimapJumpRequested { get; init; }
    public Action<MoveCommandMode>? MoveModeRequested { get; init; }
    public Action<UnitStance>? UnitStanceRequested { get; init; }
    public Action? SettingsRequested { get; init; }
    public Action<SandboxDeveloperContextRequest>? SandboxDeveloperContextRequested { get; init; }
    public Action? SandboxStressRequested { get; init; }
    public FactionId ViewerFaction { get; init; } = FactionId.Dog;

    public void SetVisualTheme(WorldVisualThemeState state)
    {
        CurrentPalette = SoftOldCityTheme.For(state);
        ApplySoftOldCityPanelStyles();
    }

    public void SetSandboxDeveloperControlsVisible(bool visible)
    {
        if (_sandboxDeveloperPanel is not null)
        {
            _sandboxDeveloperPanel.Visible = visible;
        }
    }
    public override void _Ready()
    {
        var root = new Control
        {
            Name = "HudRoot",
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);

        BuildResourceStrip(root);
        BuildGlobalSkillPanel(root);
        BuildSandboxDeveloperPanel(root);
        BuildMinimapCluster(root);
        BuildSelectionCluster(root);
        BuildCommandRibbon(root);
        BuildAlertChips(root);
        BuildRightRail(root);
        BuildRightDrawer(root);
        BuildOutcomeBanner(root);
        BuildCommandPreview(root);
        ApplySoftOldCityPanelStyles();
    }

    public override void _Process(double delta)
    {
        var dt = (float)delta;
        var viewport = GetViewport().GetVisibleRect().Size;
        var mouse = GetViewport().GetMousePosition();
        var pointerNearRail = mouse.X >= viewport.X - 72;
        var productionForcedOpen = _hasBuildingSelection || _buildModeActive || pointerNearRail;

        if (productionForcedOpen)
        {
            _drawerInactivity = 0;
        }
        else
        {
            _drawerInactivity += dt;
        }

        if (_drawerInactivity > 2.4f)
        {
            _manualDrawerOpen = false;
        }

        var holdAfterActivity = _productionDrawerProgress > 0.02f && _drawerInactivity <= 2.4f;
        var productionTarget = productionForcedOpen || _manualDrawerOpen || holdAfterActivity ? 1f : 0f;
        var detailTarget = _hasSelection ? 1f : 0f;
        _productionDrawerProgress = Mathf.MoveToward(_productionDrawerProgress, productionTarget, dt * 5.5f);
        _detailDrawerProgress = Mathf.MoveToward(_detailDrawerProgress, detailTarget, dt * 9.5f);
        LayoutDynamicHud(viewport);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Tab })
        {
            _manualDrawerOpen = !_manualDrawerOpen;
            _drawerInactivity = _manualDrawerOpen ? 0 : 3;
            GetViewport().SetInputAsHandled();
        }
    }

    public void ReleaseManagedResources()
    {
        if (_minimapSurface is not null)
        {
            _minimapSurface.FogMask = null;
        }

        foreach (var texture in IconTextureCache.Values)
        {
            ManagedGodotResourceCleanup.DisposeGodotObject(texture);
        }

        IconTextureCache.Clear();
    }
}
