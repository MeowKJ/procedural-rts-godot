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
            _state.VisualThemeChanged -= _hud.SetVisualTheme;
            _hud.ReleaseManagedResources();
        }

        _audio?.ReleaseManagedResources();
        _state.FogOfWar.ReleaseManagedResources();
        ManagedGodotResourceCleanup.ReleaseTree(this);
    }

    public override void _Ready()
    {
        DisplayAudioSettings.LoadAndApply();

        _grid = new GridLayer { Name = "Grid", WorldSize = _state.WorldSize, State = _state };
        AddChild(_grid);

        AddChild(new SignalNetworkLayer { Name = "SignalNetwork", State = _state });

        var resourceRoot = new Node2D { Name = "Resources" };
        AddChild(resourceRoot);

        foreach (var field in _state.ResourceFields)
        {
            var view = new ResourceFieldView { Name = $"ResourceField_{field.Id}", Field = field };
            resourceRoot.AddChild(view);
            _resourceViews[field.Id] = view;
        }

        _footprints = new FootprintLayer { Name = "Footprints", State = _state };
        AddChild(_footprints);

        _buildingRoot = new Node2D { Name = "Buildings" };
        AddChild(_buildingRoot);

        foreach (var building in _state.Buildings)
        {
            var view = CreateBuildingView(building);
            _buildingRoot.AddChild(view);
            _buildingViews[building.Id] = view;
            UpsertBuildingTarget(building);
        }

        _state.BuildingAdded += building =>
        {
            var view = CreateBuildingView(building);
            _buildingRoot.AddChild(view);
            _buildingViews[building.Id] = view;
            UpsertBuildingTarget(building);
            if (building.Owner == ProceduralRts.Core.Owner.Player)
            {
                AddAlert(AlertKind.Building, GameText.Format("ui.building.online", BuildSpecCatalog.For(building.Kind).Label), building.Position);
                if (building.Kind == BuildingDesignIds.PowerPlant || !_powerStable)
                {
                    UpdatePowerAlert(true);
                }
            }
        };

        var unitRoot = new Node2D { Name = "Units" };
        AddChild(unitRoot);

        if (!UseUnitDesignRuntime)
        {
            foreach (var unit in _state.Units)
            {
                var view = new UnitView { Name = $"Unit_{unit.Id}", State = _state, Unit = unit };
                unitRoot.AddChild(view);
                _unitViews[unit.Id] = view;
            }
        }

        _state.UnitAdded += unit =>
        {
            if (UseUnitDesignRuntime)
            {
                return;
            }

            var view = new UnitView { Name = $"Unit_{unit.Id}", State = _state, Unit = unit };
            unitRoot.AddChild(view);
            _unitViews[unit.Id] = view;
        };

        _unitInstanceRoot = new Node2D { Name = "UnitInstances" };
        AddChild(_unitInstanceRoot);
        ConfigureUnitBattlefield();
        ConfigureEntityWorld();

        _state.UnitsRemoved += OnUnitsRemoved;
        _state.BuildingsRemoved += OnBuildingsRemoved;
        _unitBattlefield.UnitsRemoved += OnUnitInstancesRemoved;
        _unitBattlefield.UnitAttacked += OnUnitInstanceAttacked;
        _unitBattlefield.UnitAttackedByBuilding += OnUnitInstanceAttackedByBuilding;
        _unitBattlefield.BuildingAttacked += OnUnitBattlefieldBuildingAttacked;
        _unitBattlefield.BuildingsRemoved += OnUnitBattlefieldBuildingsRemoved;
        _unitBattlefield.OutcomeChanged += OnUnitBattlefieldOutcomeChanged;
        _unitBattlefield.ResourceInventoryChanged += OnUnitBattlefieldResourceInventoryChanged;
        _unitBattlefield.ProductionCompleted += OnUnitBattlefieldProductionCompleted;

        _fogOfWar = new FogOfWarLayer { Name = "FogOfWar", State = _state, Quality = _state.FogQuality };
        AddChild(_fogOfWar);

        _combatEffects = new CombatEffectsLayer { Name = "CombatEffects", State = _state, UnitBattlefield = _unitBattlefield };
        AddChild(_combatEffects);

        _commandAcknowledgements = new CommandAcknowledgementLayer { Name = "CommandAcknowledgements" };
        AddChild(_commandAcknowledgements);

        AddChild(new PathDebugLayer
        {
            Name = "PathDebug",
            State = _state,
            StatusChanged = OnStatusChanged,
        });

        _camera = new CameraController { Name = "Camera", WorldSize = _state.WorldSize };
        _camera.ViewChanged += RefreshViewCulling;
        AddChild(_camera);

        _buildPlacement = new BuildPlacementController
        {
            Name = "BuildPlacement",
            State = _state,
            UnitBattlefield = _unitBattlefield,
            Camera = _camera,
            LocalPlayerSlotId = PlayerSlotId.One,
            LocalFaction = ToUnitFaction(_state.Options.PlayerFaction),
            StatusChanged = OnStatusChanged,
            CommandAcknowledged = AddCommandAcknowledgement,
        };
        AddChild(_buildPlacement);

        _selection = new SelectionController
        {
            Name = "Selection",
            State = _state,
            Camera = _camera,
            UnitBattlefield = _unitBattlefield,
            LocalPlayerSlotId = PlayerSlotId.One,
            SelectionChanged = OnSelectionChanged,
            StatusChanged = OnStatusChanged,
            AudioCueRequested = PlayAudioCue,
            CommandAcknowledged = AddCommandAcknowledgement,
            MoveModeRequested = OnMoveModeRequested,
            UnitStanceRequested = OnUnitStanceRequested,
            MouseInputBlocked = () => _buildPlacement.IsActive,
        };
        AddChild(_selection);

        _controlGroups = new ControlGroupController
        {
            Name = "ControlGroups",
            State = _state,
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
            State = _state,
            ProductionRequested = OnProductionRequested,
            CancelProductionRequested = OnCancelProductionRequested,
            StatusChanged = OnStatusChanged,
            ProductionStatusChanged = OnProductionStatusChanged,
        };
        AddChild(production);

        if (UseUnitDesignRuntime)
        {
            AddChild(new EnemyUnitBattlefieldProductionController
            {
                Name = "EnemyProduction",
                Battlefield = _unitBattlefield,
                EnemyPlayerSlotId = PlayerSlotId.Two,
                DifficultyProfile = EnemyDifficultyProfile.For(_state.Options.EnemyDifficulty),
            });

            AddChild(new EnemyUnitBattlefieldAttackWaveController
            {
                Name = "EnemyAttackWaves",
                Battlefield = _unitBattlefield,
                EnemyPlayerSlotId = PlayerSlotId.Two,
                DifficultyProfile = EnemyDifficultyProfile.For(_state.Options.EnemyDifficulty),
            });
        }
        else
        {
            AddChild(new EnemyProductionController
            {
                Name = "EnemyProduction",
                State = _state,
                DifficultyProfile = EnemyDifficultyProfile.For(_state.Options.EnemyDifficulty),
            });

            AddChild(new EnemyAttackWaveController
            {
                Name = "EnemyAttackWaves",
                State = _state,
                DifficultyProfile = EnemyDifficultyProfile.For(_state.Options.EnemyDifficulty),
            });
        }

        _state.ProductionCompleted += OnProductionCompleted;
        _state.ResourceInventoryChanged += OnResourceInventoryChanged;
        _state.EntityAttacked += OnEntityAttacked;
        _state.OutcomeChanged += OnOutcomeChanged;

        _hud = new HudLayer
        {
            Name = "Hud",
            ProductionRequested = OnProductionRequested,
            ProductionDesignRequested = OnProductionDesignRequested,
            CancelProductionRequested = OnCancelProductionRequested,
            MinimapJumpRequested = OnMinimapJumpRequested,
            MoveModeRequested = OnMoveModeRequested,
            UnitStanceRequested = OnUnitStanceRequested,
            SettingsRequested = OnSettingsRequested,
            SandboxDeveloperContextRequested = OnSandboxDeveloperContextRequested,
            SandboxStressRequested = OnSandboxStressRequested,
            ViewerFaction = _state.MatchConfig.PlayerFaction,
        };
        AddChild(_hud);
        _hud.SetVisualTheme(_state.VisualTheme);
        _hud.SetSandboxDeveloperControlsVisible(_state.Options.LaunchMode == LaunchMode.Sandbox);
        _hud.SetSandboxDeveloperContext(_sandboxContext);
        _state.VisualThemeChanged += _hud.SetVisualTheme;

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

    private BuildingView CreateBuildingView(BuildingModel building)
    {
        return new BuildingView
        {
            Name = $"Building_{building.Id}",
            State = _state,
            Building = building,
            ProjectionProvider = () => _unitBattlefield.BuildingProjection(building.Id),
            BuildingProjectionProvider = () => _unitBattlefield.BuildingPresentationProjection(building.Id),
            ViewProjectionProvider = () => _unitBattlefield.BuildingViewProjection(building.Id),
            ExploredProvider = rect => _state.FogOfWar.AnyExplored(rect),
            VisualThemeProvider = () => _state.VisualTheme,
            ViewerFaction = _state.MatchConfig.PlayerFaction,
        };
    }
}
