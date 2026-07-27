using Godot;
using ProceduralRts.Controllers;
using ProceduralRts.Core;
using ProceduralRts.Ui;
using ProceduralRts.World;

namespace ProceduralRts;

public partial class BattleRoot
{
    public override void _ExitTree()
    {
        if (_camera is not null)
        {
            _camera.ViewChanged -= RefreshViewCulling;
        }

        if (_hud is not null)
        {
            _presentationEnvironment.VisualThemeChanged -= OnVisualThemeChanged;
        }

        _presentationEnvironment.SignalNetworkChanged -= OnSignalNetworkChanged;
        _audio?.ReleaseManagedResources();
        _presentationEnvironment.ReleaseManagedResources();
        ManagedGodotResourceCleanup.ReleaseTree(this);
    }

    public override void _Ready()
    {
        DisplayAudioSettings.LoadAndApply();

        _grid = new GridLayer
        {
            Name = "Grid",
            WorldSize = _worldSize,
            VisualThemeProvider = () => _presentationEnvironment.VisualTheme,
        };
        AddChild(_grid);

        _signalNetwork = new SignalNetworkLayer
        {
            Name = "SignalNetwork",
            Nodes = _presentationEnvironment.SignalNodes,
            VisualThemeProvider = () => _presentationEnvironment.VisualTheme,
        };
        AddChild(_signalNetwork);

        var resourceRoot = new Node2D { Name = "Resources" };
        AddChild(resourceRoot);

        foreach (var field in _unitBattlefield.ResourceFields)
        {
            var view = new ResourceFieldView { Name = $"ResourceField_{field.Id}", Field = field };
            resourceRoot.AddChild(view);
            _resourceViews[field.Id] = view;
        }

        _footprints = new FootprintLayer
        {
            Name = "Footprints",
            UnitBattlefield = _unitBattlefield,
            IsVisibleToPlayer = _presentationEnvironment.IsVisible,
        };
        AddChild(_footprints);

        _buildingRoot = new Node2D { Name = "Buildings" };
        AddChild(_buildingRoot);

        _unitBodyBatchLayer = new UnitBodyBatchLayer
        {
            Name = "UnitBodyBatch",
            Units = _unitBattlefield.Units,
            Viewer = PlayerSlotId.One,
            Relations = _unitBattlefield.Relations,
            PresentationProvider = id => _unitBattlefield.UnitPresentationProjection(id),
            VisualThemeProvider = () => _presentationEnvironment.VisualTheme,
        };
        AddChild(_unitBodyBatchLayer);

        _unitInstanceRoot = new Node2D { Name = "UnitInstances" };
        AddChild(_unitInstanceRoot);
        ConfigureUnitBattlefield();
        ConfigureEntityWorld();
        SyncUnitBattlefieldBuildingRuntimeState();
        _presentationEnvironment.Update(0, _unitBattlefield, PlayerSlotId.One);

        _unitBattlefield.UnitsRemoved += OnUnitInstancesRemoved;
        _unitBattlefield.WeaponFired += OnWeaponFired;
        _unitBattlefield.ProjectileImpacted += OnProjectileImpacted;
        _unitBattlefield.UnitAttacked += OnUnitInstanceAttacked;
        _unitBattlefield.UnitAttackedByBuilding += OnUnitInstanceAttackedByBuilding;
        _unitBattlefield.BuildingAttacked += OnUnitBattlefieldBuildingAttacked;
        _unitBattlefield.BuildingsRemoved += OnUnitBattlefieldBuildingsRemoved;
        _unitBattlefield.OutcomeChanged += OnUnitBattlefieldOutcomeChanged;
        _unitBattlefield.ResourceInventoryChanged += OnUnitBattlefieldResourceInventoryChanged;
        _unitBattlefield.ProductionCompleted += OnUnitBattlefieldProductionCompleted;

        _fogOfWar = new FogOfWarLayer
        {
            Name = "FogOfWar",
            FogOfWar = _presentationEnvironment.FogOfWar,
            WorldSize = _worldSize,
            Quality = _presentationEnvironment.FogQuality,
        };
        AddChild(_fogOfWar);

        _combatEffects = new CombatEffectsLayer
        {
            Name = "CombatEffects",
            UnitBattlefield = _unitBattlefield,
            IsVisibleToPlayer = _presentationEnvironment.IsVisible,
            IsExploredByPlayer = _presentationEnvironment.IsExplored,
        };
        AddChild(_combatEffects);

        _commandAcknowledgements = new CommandAcknowledgementLayer { Name = "CommandAcknowledgements" };
        AddChild(_commandAcknowledgements);

        _camera = new CameraController { Name = "Camera", WorldSize = _worldSize };
        _camera.ViewChanged += RefreshViewCulling;
        AddChild(_camera);

        _buildPlacement = new BuildPlacementController
        {
            Name = "BuildPlacement",
            UnitBattlefield = _unitBattlefield,
            Camera = _camera,
            LocalPlayerSlotId = PlayerSlotId.One,
            LocalFaction = ToUnitFaction(_matchConfig.PlayerFaction),
            StatusChanged = OnBuildPlacementStatusChanged,
            CommandAcknowledged = QueueCommandAcknowledgementEvent,
        };
        AddChild(_buildPlacement);

        _selection = new SelectionController
        {
            Name = "Selection",
            Camera = _camera,
            UnitBattlefield = _unitBattlefield,
            LocalPlayerSlotId = PlayerSlotId.One,
            SelectionChanged = OnSelectionChanged,
            StatusChanged = OnStatusChanged,
            AudioCueRequested = cue => PlayAudioCue(cue),
            CommandAcknowledged = QueueCommandAcknowledgementEvent,
            MoveModeRequested = OnMoveModeRequested,
            UnitStanceRequested = OnUnitStanceRequested,
            MouseInputBlocked = () => _buildPlacement.IsActive,
        };
        AddChild(_selection);

        _controlGroups = new ControlGroupController
        {
            Name = "ControlGroups",
            UnitBattlefield = _unitBattlefield,
            LocalPlayerSlotId = PlayerSlotId.One,
            SelectionChanged = OnSelectionChanged,
            FocusRequested = _camera.FocusOnWorldPoint,
            StatusChanged = OnStatusChanged,
        };
        AddChild(_controlGroups);

        var production = new ProductionController
        {
            Name = "Production",
            LocalFaction = ToUnitFaction(_matchConfig.PlayerFaction),
            ProductionDesignRequested = OnProductionHotkeyRequested,
            CancelProductionRequested = OnCancelProductionRequested,
            StatusChanged = OnStatusChanged,
            ProductionStatusChanged = OnProductionStatusChanged,
        };
        AddChild(production);

        {
            AddChild(new EnemyUnitBattlefieldProductionController
            {
                Name = "EnemyProduction",
                Battlefield = _unitBattlefield,
                EnemyPlayerSlotId = PlayerSlotId.Two,
                DifficultyProfile = EnemyDifficultyProfile.For(_matchConfig.EnemyDifficulty),
            });

            AddChild(new EnemyUnitBattlefieldAttackWaveController
            {
                Name = "EnemyAttackWaves",
                Battlefield = _unitBattlefield,
                EnemyPlayerSlotId = PlayerSlotId.Two,
                DifficultyProfile = EnemyDifficultyProfile.For(_matchConfig.EnemyDifficulty),
            });
        }


        _hud = new HudLayer
        {
            Name = "Hud",
            ProductionDesignRequested = OnProductionDesignRequested,
            ProductionRepeatRequested = OnProductionRepeatRequested,
            CancelProductionRequested = OnCancelProductionRequested,
            SellOrCancelRequested = OnSellOrCancelRequested,
            RallyRequested = OnRallyRequested,
            RepairRequested = OnRepairRequested,
            AbilityRequested = OnAbilityRequested,
            BuildKindRequested = OnBuildKindRequested,
            CatalogInspectorIntentRequested = OnCatalogInspectorIntentRequested,
            MinimapJumpRequested = OnMinimapJumpRequested,
            MoveModeRequested = OnMoveModeRequested,
            UnitStanceRequested = OnUnitStanceRequested,
            SettingsRequested = OnSettingsRequested,
            SandboxDeveloperContextRequested = OnSandboxDeveloperContextRequested,
            SandboxStressRequested = OnSandboxStressRequested,
            ViewerFaction = _matchConfig.PlayerFaction,
        };
        AddChild(_hud);
        _hud.SetVisualTheme(_presentationEnvironment.VisualTheme);
        _hud.SetSandboxDeveloperControlsVisible(_matchConfig.LaunchMode == LaunchMode.Sandbox);
        _hud.SetSandboxDeveloperContext(_sandboxContext);
        _presentationEnvironment.VisualThemeChanged += OnVisualThemeChanged;
        _presentationEnvironment.SignalNetworkChanged += OnSignalNetworkChanged;

        _perfHud = new PerfHudLayer
        {
            Name = "PerfHud",
            SnapshotProvider = () => _presentationMetrics.Snapshot(),
            CountsProvider = PerfHudCounts,
        };
        AddChild(_perfHud);

        _audio = new TacticalAudioLayer { Name = "TacticalAudio" };
        AddChild(_audio);

        AddChild(new HotkeyLegendLayer { Name = "HotkeyLegend" });

        _pauseMenu = new PauseMenuLayer { Name = "PauseMenu" };
        AddChild(_pauseMenu);

        _outcomeScreen = new OutcomeScreenLayer { Name = "OutcomeScreen" };
        AddChild(_outcomeScreen);

        _hud.SetResourceCredits(_unitBattlefield.Credits(PlayerSlotId.One));
        _hud.SetMoveCommandMode(MoveCommandMode.Direct);
        RefreshCommandCard();
        RefreshMinimap();
        RefreshControlGroups();
        RefreshAlerts(0);
        RefreshCommandPreview();
        RefreshViewCulling();
        ApplySandboxLaunchState();
    }

    private BuildingView CreateBuildingView(int buildingId)
    {
        return new BuildingView
        {
            Name = $"Building_{buildingId}",
            ViewProjectionProvider = () => _unitBattlefield.BuildingViewProjection(buildingId),
            ExploredProvider = _presentationEnvironment.FogOfWar.AnyExplored,
            VisualThemeProvider = () => _presentationEnvironment.VisualTheme,
            ViewerFaction = _matchConfig.PlayerFaction,
        };
    }

    private void OnVisualThemeChanged(WorldVisualThemeState theme)
    {
        _hud.SetVisualTheme(theme);
        _grid.QueueRedraw();
        _signalNetwork.QueueRedraw();
        SyncEntityWorldResourceAtmosphere(theme);
    }

    private void OnSignalNetworkChanged()
    {
        _signalNetwork.QueueRedraw();
    }
}
