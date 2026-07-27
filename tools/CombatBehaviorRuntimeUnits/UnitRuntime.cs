static partial class Program
{
    private static void AssertUnitBattlefieldCommandsAndCombat()
    {
        var dogT1Roster = UnitDesignCatalog.ForRoster(UnitRosters.DogT1);

        var newUnitBattlefield = new UnitBattlefield();
        var playerOneGuard = newUnitBattlefield.Spawn<DogGuardTank>(PlayerSlotId.One, new Vector2(120, 160), 0.25f);
        var playerTwoGuard = newUnitBattlefield.Spawn<DogGuardTank>(PlayerSlotId.Two, new Vector2(260, 160), 0.25f);
        var sandboxRosterSpawn = newUnitBattlefield.SpawnRoster(UnitRosters.DogT1, PlayerSlotId.One, new Vector2(120, 240), new Vector2(44, 0));
        playerOneGuard.Selected = true;
        playerTwoGuard.AlertPulse = 0.75f;
        if (playerOneGuard.Id == playerTwoGuard.Id
            || !playerOneGuard.EntityId.IsValid
            || !playerTwoGuard.EntityId.IsValid
            || playerOneGuard.Spec.Id != "dog.guard_tank"
            || playerOneGuard.Spec.Faction != UnitFactionId.Dog
            || playerOneGuard.PlayerSlotId != PlayerSlotId.One
            || playerOneGuard.Hp != playerOneGuard.Spec.Stats.MaxHp
            || playerOneGuard.WeaponMounts.Count != playerOneGuard.Spec.Weapons.Count
            || sandboxRosterSpawn.Count != dogT1Roster.Count)
        {
            throw new InvalidOperationException("new unit battlefield should spawn inherited unit designs as clean UnitInstance records owned by a player slot");
        }

        var playerOneGuardEntity = newUnitBattlefield.UnitEntityByInstanceId(playerOneGuard.Id);
        var playerOneGuardProjection = newUnitBattlefield.UnitProjection(playerOneGuard.Id);
        if (playerOneGuardEntity is null
            || playerOneGuardProjection is null
            || playerOneGuardProjection.Value.Id != playerOneGuard.EntityId
            || playerOneGuardProjection.Value.SpecId != "dog.guard_tank"
            || playerOneGuardProjection.Value.Owner != OwnerId.FromPlayerSlot(PlayerSlotId.One)
            || playerOneGuardProjection.Value.Position != playerOneGuard.Position
            || MathF.Abs(playerOneGuardProjection.Value.Hp - playerOneGuard.Hp) > 0.001f)
        {
            throw new InvalidOperationException("UnitBattlefield should mirror UnitInstance spawns into EntityWorld and expose projection snapshots for views");
        }

        playerOneGuard.Position += new Vector2(17, -9);
        playerOneGuard.Facing += 0.42f;
        var driftBeforeProjectionSync = newUnitBattlefield.UnitProjectionDrift();
        if (driftBeforeProjectionSync.UnitCount < 2
            || driftBeforeProjectionSync.MissingMirrors != 0
            || driftBeforeProjectionSync.MaxPositionDrift < 19f
            || driftBeforeProjectionSync.MaxFacingDrift < 0.4f)
        {
            throw new InvalidOperationException("UnitBattlefield projection drift QA should compare retired UnitInstance state against EntityWorld mirrors before the flag flip");
        }

        var resyncedGuardProjection = newUnitBattlefield.UnitProjection(playerOneGuard.Id);
        var driftAfterProjectionSync = newUnitBattlefield.UnitProjectionDrift();
        if (resyncedGuardProjection is null
            || driftAfterProjectionSync.MissingMirrors != 0
            || driftAfterProjectionSync.MaxPositionDrift > 0.01f
            || driftAfterProjectionSync.MaxFacingDrift > 0.01f)
        {
            throw new InvalidOperationException("UnitBattlefield projection drift QA should fall back to zero after projection sync");
        }

        var newSelectionSummary = newUnitBattlefield.SelectionSummary();
        if (newSelectionSummary.Count == 0
            || newSelectionSummary.All(item => item.DesignId != "dog.guard_tank")
            || newSelectionSummary.Any(item => item.PlayerSlotId != PlayerSlotId.One))
        {
            throw new InvalidOperationException("new unit battlefield should provide selection summaries directly from UnitSpec data");
        }

        newUnitBattlefield.ClearSelection(PlayerSlotId.One);
        var pickedNewUnitCount = newUnitBattlefield.SelectSingleAt(PlayerSlotId.One, playerOneGuard.Position, additive: false, pickPadding: 4);
        var selectedEntity = newUnitBattlefield.UnitEntityByInstanceId(playerOneGuard.Id);
        if (pickedNewUnitCount != 1
            || newUnitBattlefield.AppliedInputCommandCount != 2
            || !playerOneGuard.Selected
            || playerTwoGuard.Selected
            || selectedEntity is null
            || !selectedEntity.Components.TryGet<SelectableComponentState>(out var bufferedSelection)
            || !bufferedSelection.Selected)
        {
            throw new InvalidOperationException("unit battlefield should route single-click selection through EntityCommandBuffer");
        }

        var selectedProjection = newUnitBattlefield.UnitProjection(playerOneGuard.Id);
        if (selectedProjection is null || !selectedProjection.Value.Selected)
        {
            throw new InvalidOperationException("UnitBattlefield projection should reflect UnitInstance selection state for presentation");
        }

        var doubleClickCount = newUnitBattlefield.SelectSameUnitsAt(PlayerSlotId.One, playerOneGuard.Position, new Rect2(80, 120, 260, 180), additive: false, pickPadding: 4);
        if (doubleClickCount < 1
            || newUnitBattlefield.SelectedUnits(PlayerSlotId.One).Any(unit => unit.Spec.Id != playerOneGuard.Spec.Id)
            || newUnitBattlefield.SelectedUnits(PlayerSlotId.One).Any(unit => unit.PlayerSlotId != PlayerSlotId.One))
        {
            throw new InvalidOperationException("new unit battlefield should double-click select same UnitDesign ids for only the local player slot");
        }

        var rectSelected = newUnitBattlefield.SelectRect(PlayerSlotId.One, new Rect2(90, 220, 260, 80), additive: false);
        if (rectSelected == 0
            || newUnitBattlefield.SelectedUnits(PlayerSlotId.One).Any(unit => unit.PlayerSlotId != PlayerSlotId.One)
            || newUnitBattlefield.SelectedUnits(PlayerSlotId.One).All(unit => unit.FormationSlot is not null))
        {
            throw new InvalidOperationException("new unit battlefield should box-select UnitInstance records by collision radius before movement assigns formation slots");
        }

        var selectionHotkeyBattlefield = new UnitBattlefield();
        var armyHotkeyGuard = selectionHotkeyBattlefield.Spawn("dog.guard_tank", PlayerSlotId.One, new Vector2(100, 100));
        var armyHotkeyRocket = selectionHotkeyBattlefield.Spawn("dog.rocket", PlayerSlotId.One, new Vector2(140, 100));
        var armyHotkeyIdleHarvesterA = selectionHotkeyBattlefield.Spawn("dog.harvester", PlayerSlotId.One, new Vector2(180, 100));
        var armyHotkeyIdleHarvesterB = selectionHotkeyBattlefield.Spawn("dog.harvester", PlayerSlotId.One, new Vector2(220, 100));
        var armyHotkeyBusyHarvester = selectionHotkeyBattlefield.Spawn("dog.harvester", PlayerSlotId.One, new Vector2(260, 100));
        var armyHotkeyEnemy = selectionHotkeyBattlefield.Spawn("cat.tank", PlayerSlotId.Two, new Vector2(320, 100));
        armyHotkeyBusyHarvester.HarvesterMode = HarvesterMode.Gathering;
        var selectArmyCommandsBefore = selectionHotkeyBattlefield.AppliedInputCommandCount;
        var selectedArmyCount = selectionHotkeyBattlefield.SelectArmy(PlayerSlotId.One);
        if (selectedArmyCount != 2
            || selectionHotkeyBattlefield.AppliedInputCommandCount != selectArmyCommandsBefore + 1
            || !armyHotkeyGuard.Selected
            || !armyHotkeyRocket.Selected
            || armyHotkeyIdleHarvesterA.Selected
            || armyHotkeyBusyHarvester.Selected
            || armyHotkeyEnemy.Selected)
        {
            throw new InvalidOperationException("select-all-army hotkey should route through UnitBattlefield selection commands while excluding harvesters and enemy units");
        }

        var idleHarvesterCommandsBefore = selectionHotkeyBattlefield.AppliedInputCommandCount;
        var selectedIdleHarvesterA = selectionHotkeyBattlefield.SelectNextIdleHarvester(PlayerSlotId.One);
        var selectedIdleHarvesterB = selectionHotkeyBattlefield.SelectNextIdleHarvester(PlayerSlotId.One);
        if (selectedIdleHarvesterA?.Id != armyHotkeyIdleHarvesterA.Id
            || selectedIdleHarvesterB?.Id != armyHotkeyIdleHarvesterB.Id
            || selectionHotkeyBattlefield.AppliedInputCommandCount != idleHarvesterCommandsBefore + 2
            || armyHotkeyGuard.Selected
            || armyHotkeyRocket.Selected
            || armyHotkeyIdleHarvesterA.Selected
            || !armyHotkeyIdleHarvesterB.Selected
            || armyHotkeyBusyHarvester.Selected
            || armyHotkeyEnemy.Selected)
        {
            throw new InvalidOperationException("idle-harvester cycle hotkey should select only the next idle local harvester through EntityWorld selection commands");
        }

        var selectedBeforeMove = newUnitBattlefield.SelectedUnits(PlayerSlotId.One).ToList();
        var firstBeforeMove = selectedBeforeMove[0].Position;
        newUnitBattlefield.Relations.Set(PlayerSlotId.One, PlayerSlotId.Two, PlayerRelation.Allied);
        var commandsBeforeMove = newUnitBattlefield.AppliedInputCommandCount;
        newUnitBattlefield.CommandMoveSelected(PlayerSlotId.One, new Vector2(440, 300), new Vector2(900, 700), MoveCommandMode.Direct);
        var movedEntityBeforeTick = newUnitBattlefield.UnitEntityByInstanceId(selectedBeforeMove[0].Id);
        if (newUnitBattlefield.AppliedInputCommandCount != commandsBeforeMove + 1
            || movedEntityBeforeTick is null
            || !movedEntityBeforeTick.Components.TryGet<MovementComponentState>(out var bufferedMove)
            || !movedEntityBeforeTick.Components.TryGet<CommandableComponentState>(out var bufferedMoveCommand)
            || bufferedMove.MoveTarget != selectedBeforeMove[0].MoveTarget
            || bufferedMove.FormationSlot != selectedBeforeMove[0].FormationSlot
            || bufferedMoveCommand.CommandVisualTarget != new Vector2(440, 300))
        {
            throw new InvalidOperationException("UnitBattlefield selected move input should route through EntityCommandBuffer before updating UnitInstance command state");
        }

        for (var step = 0; step < 6; step++)
        {
            newUnitBattlefield.Update(1 / 30.0);
        }

        if (selectedBeforeMove.Any(unit => unit.FormationSlot is null || unit.CommandVisualTarget != new Vector2(440, 300) || unit.CommandPulse <= 0)
            || selectedBeforeMove[0].Position.DistanceTo(firstBeforeMove) < 8)
        {
            throw new InvalidOperationException("unit battlefield should assign compact formation move slots and advance selected UnitInstance movement");
        }

        var movedProjection = newUnitBattlefield.UnitProjection(selectedBeforeMove[0].Id);
        var movedEntityAfterTick = newUnitBattlefield.UnitEntityByInstanceId(selectedBeforeMove[0].Id);
        if (movedProjection is null
            || movedEntityAfterTick is null
            || movedEntityAfterTick.Transform.Position.DistanceTo(firstBeforeMove) < 8
            || movedEntityAfterTick.Transform.Position.DistanceTo(selectedBeforeMove[0].Position) > 0.001f
            || movedProjection.Value.Position.DistanceTo(movedEntityAfterTick.Transform.Position) > 0.001f
            || movedProjection.Value.Facing != movedEntityAfterTick.Transform.Facing)
        {
            throw new InvalidOperationException("UnitBattlefield runtime movement should advance through EntityWorld MovementSystem and sync UnitInstance/projection positions from the entity transform");
        }

        var runtimeSeparationBattlefield = new UnitBattlefield();
        var separatedLeft = runtimeSeparationBattlefield.Spawn<DogGuardTank>(PlayerSlotId.One, new Vector2(300, 300));
        var separatedRight = runtimeSeparationBattlefield.Spawn<DogGuardTank>(PlayerSlotId.One, new Vector2(305, 300));
        var initialSeparationDistance = separatedLeft.Position.DistanceTo(separatedRight.Position);
        runtimeSeparationBattlefield.Update(1 / 30.0);
        var separatedLeftEntity = runtimeSeparationBattlefield.UnitEntityByInstanceId(separatedLeft.Id);
        var separatedRightEntity = runtimeSeparationBattlefield.UnitEntityByInstanceId(separatedRight.Id);
        if (separatedLeftEntity is null
            || separatedRightEntity is null
            || separatedLeftEntity.Transform.Position.DistanceTo(separatedRightEntity.Transform.Position) <= initialSeparationDistance
            || separatedLeft.Position.DistanceTo(separatedLeftEntity.Transform.Position) > 0.001f
            || separatedRight.Position.DistanceTo(separatedRightEntity.Transform.Position) > 0.001f)
        {
            throw new InvalidOperationException("UnitBattlefield runtime separation should advance through EntityWorld SeparationSystem and sync separated UnitInstance positions from entity transforms");
        }

        var commandsBeforeStop = newUnitBattlefield.AppliedInputCommandCount;
        newUnitBattlefield.CommandStopSelected(PlayerSlotId.One);
        var stoppedEntity = newUnitBattlefield.UnitEntityByInstanceId(selectedBeforeMove[0].Id);
        if (newUnitBattlefield.AppliedInputCommandCount != commandsBeforeStop + 1
            || stoppedEntity is null
            || !stoppedEntity.Components.TryGet<MovementComponentState>(out var stoppedMove)
            || stoppedMove.MoveTarget is not null
            || selectedBeforeMove[0].MoveTarget is not null
            || selectedBeforeMove[0].AttackTargetId is not null)
        {
            throw new InvalidOperationException("UnitBattlefield stop input should route through EntityCommandBuffer and clear movement/attack command state");
        }

        var commandsBeforeStance = newUnitBattlefield.AppliedInputCommandCount;
        var stanceChanged = newUnitBattlefield.CommandSetSelectedStance(PlayerSlotId.One, UnitStance.Hold);
        var stanceEntity = newUnitBattlefield.UnitEntityByInstanceId(selectedBeforeMove[0].Id);
        if (stanceChanged != selectedBeforeMove.Count
            || newUnitBattlefield.AppliedInputCommandCount != commandsBeforeStance + 1
            || stanceEntity is null
            || !stanceEntity.Components.TryGet<StanceComponentState>(out var bufferedStance)
            || bufferedStance.Stance != UnitStance.Hold
            || selectedBeforeMove.Any(unit => unit.Stance != UnitStance.Hold))
        {
            throw new InvalidOperationException("UnitBattlefield stance input should route through EntityCommandBuffer and update EntityWorld stance state");
        }
        newUnitBattlefield.Relations.Set(PlayerSlotId.One, PlayerSlotId.Two, PlayerRelation.Hostile);

        var newCombatBattlefield = new UnitBattlefield();
        var newCombatAttacker = newCombatBattlefield.Spawn<DogGuardTank>(PlayerSlotId.One, new Vector2(300, 300));
        var newCombatTarget = newCombatBattlefield.Spawn("cat.tank", PlayerSlotId.Two, new Vector2(430, 300), Mathf.Pi);
        newCombatBattlefield.SelectUnitsByIds(PlayerSlotId.One, [newCombatAttacker.Id]);
        var commandsBeforeAttack = newCombatBattlefield.AppliedInputCommandCount;
        newCombatBattlefield.CommandAttackSelected(PlayerSlotId.One, newCombatTarget);
        var attackEntityBeforeTick = newCombatBattlefield.UnitEntityByInstanceId(newCombatAttacker.Id);
        if (newCombatBattlefield.AppliedInputCommandCount != commandsBeforeAttack + 1
            || attackEntityBeforeTick is null
            || !attackEntityBeforeTick.Components.TryGet<WeaponUserComponentState>(out var bufferedAttack)
            || bufferedAttack.AttackTarget != newCombatTarget.EntityId
            || !bufferedAttack.AttackTargetIsManual
            || newCombatAttacker.AttackTargetId != newCombatTarget.Id)
        {
            throw new InvalidOperationException("UnitBattlefield selected attack input should route through EntityCommandBuffer before updating UnitInstance attack state");
        }

        for (var step = 0; step < 90; step++)
        {
            newCombatBattlefield.Update(1 / 30.0);
        }

        if (newCombatAttacker.AttackTargetId != newCombatTarget.Id || newCombatTarget.Hp >= newCombatTarget.Spec.Stats.MaxHp)
        {
            throw new InvalidOperationException("unit battlefield should support manual UnitInstance attacks and apply UnitSpec weapon damage");
        }

        var explicitAttackBattlefield = new UnitBattlefield();
        var explicitAttacker = explicitAttackBattlefield.Spawn<DogGuardTank>(PlayerSlotId.One, new Vector2(300, 330));
        var explicitTarget = explicitAttackBattlefield.Spawn("cat.tank", PlayerSlotId.Two, new Vector2(455, 330), Mathf.Pi);
        var explicitCommandsBefore = explicitAttackBattlefield.AppliedInputCommandCount;
        var explicitCommanded = explicitAttackBattlefield.CommandAttackUnits(PlayerSlotId.One, [explicitAttacker.Id], explicitTarget);
        var explicitAttackerEntity = explicitAttackBattlefield.UnitEntityByInstanceId(explicitAttacker.Id);
        if (explicitCommanded != 1
            || explicitAttackBattlefield.AppliedInputCommandCount != explicitCommandsBefore + 1
            || explicitAttackerEntity is null
            || !explicitAttackerEntity.Components.TryGet<WeaponUserComponentState>(out var explicitBufferedAttack)
            || explicitBufferedAttack.AttackTarget != explicitTarget.EntityId
            || explicitAttacker.AttackTargetId != explicitTarget.Id)
        {
            throw new InvalidOperationException("UnitBattlefield explicit attack-units API should route through EntityCommandBuffer instead of directly mutating UnitInstance attack state");
        }

        var unitInstanceDeathBattlefield = new UnitBattlefield();
        var unitInstanceDeathAttacker = unitInstanceDeathBattlefield.Spawn<DogGuardTank>(PlayerSlotId.One, new Vector2(300, 360));
        var unitInstanceDeathTarget = unitInstanceDeathBattlefield.Spawn("cat.basic", PlayerSlotId.Two, new Vector2(425, 360), Mathf.Pi);
        var unitInstanceDeathEvents = new List<UnitInstanceDeathInfo>();
        unitInstanceDeathBattlefield.UnitsRemoved += deaths => unitInstanceDeathEvents.AddRange(deaths);
        unitInstanceDeathTarget.Hp = 2;
        unitInstanceDeathBattlefield.SelectUnitsByIds(PlayerSlotId.One, [unitInstanceDeathAttacker.Id]);
        unitInstanceDeathBattlefield.CommandAttackSelected(PlayerSlotId.One, unitInstanceDeathTarget);
        for (var step = 0; step < 90; step++)
        {
            unitInstanceDeathBattlefield.Update(1 / 30.0);
        }

        if (unitInstanceDeathBattlefield.Units.Any(unit => unit.Id == unitInstanceDeathTarget.Id)
            || unitInstanceDeathEvents.Count != 1
            || unitInstanceDeathEvents[0].DesignId != "cat.basic"
            || unitInstanceDeathEvents[0].KillingAmmoId is null
            || unitInstanceDeathAttacker.AttackTargetId is not null
            || unitInstanceDeathBattlefield.UnitEntityByInstanceId(unitInstanceDeathTarget.Id) is not null
            || unitInstanceDeathBattlefield.UnitProjection(unitInstanceDeathTarget.Id) is not null)
        {
            throw new InvalidOperationException("new unit battlefield should remove dead UnitInstance targets, emit UnitInstanceDeathInfo, and clear destroyed attack targets");
        }

        var buildingTargetBattlefield = new UnitBattlefield();
        var buildingTargetAttacker = buildingTargetBattlefield.Spawn<DogGuardTank>(PlayerSlotId.One, new Vector2(300, 420));
        var buildingTarget = buildingTargetBattlefield.UpsertBuildingTarget(
            77,
            BuildingDesignIds.Headquarters,
            PlayerSlotId.Two,
            UnitFactionId.Cat,
            new Vector2(520, 420),
            Mathf.Pi,
            120);
        var buildingDamageEvents = 0;
        var buildingAttackEventTargets = new List<UnitBattlefieldBuildingSnapshot>();
        var buildingDeathEvents = new List<UnitBattlefieldBuildingDeathInfo>();
        var unitBattlefieldOutcomeEvents = new List<GameOutcome>();
        buildingTargetBattlefield.BuildingAttacked += (target, _) =>
        {
            buildingDamageEvents++;
            buildingAttackEventTargets.Add(target);
        };
        buildingTargetBattlefield.BuildingsRemoved += deaths => buildingDeathEvents.AddRange(deaths);
        buildingTargetBattlefield.OutcomeChanged += outcome => unitBattlefieldOutcomeEvents.Add(outcome);
        buildingTargetBattlefield.SelectUnitsByIds(PlayerSlotId.One, [buildingTargetAttacker.Id]);
        buildingTargetBattlefield.CommandAttackSelected(PlayerSlotId.One, buildingTarget.Id);
        for (var step = 0; step < 180; step++)
        {
            buildingTargetBattlefield.Update(1 / 30.0);
        }

        var destroyedBuildingSnapshot = buildingTargetBattlefield.BuildingSnapshot(buildingTarget.Id);
        var destroyedBuildingEntityId = buildingTargetBattlefield.BuildingEntityIdByTargetId(buildingTarget.Id);
        if (buildingDamageEvents == 0
            || (destroyedBuildingSnapshot?.Hp ?? 0) >= BuildSpecCatalog.For(buildingTarget.Kind).MaxHp
            || destroyedBuildingSnapshot is not null
            || destroyedBuildingEntityId is not null
            || buildingTargetAttacker.AttackTargetId is not null
            || buildingAttackEventTargets.Count == 0
            || buildingAttackEventTargets[0].Kind != BuildingDesignIds.Headquarters
            || buildingAttackEventTargets[0].PlayerSlotId != PlayerSlotId.Two
            || buildingAttackEventTargets[0].Position != new Vector2(520, 420)
            || buildingDeathEvents.Count != 1
            || buildingDeathEvents[0].Kind != BuildingDesignIds.Headquarters
            || buildingDeathEvents[0].PlayerSlotId != PlayerSlotId.Two
            || buildingDeathEvents[0].Position != new Vector2(520, 420)
            || unitBattlefieldOutcomeEvents.Count != 1
            || unitBattlefieldOutcomeEvents[0] != GameOutcome.Victory
            || buildingTargetBattlefield.Outcome != GameOutcome.Victory)
        {
            throw new InvalidOperationException($"new unit battlefield should route building target damage through EntityWorld health, emit self-contained building combat/death alert data, remove destroyed building mirrors, and resolve HQ victory; damageEvents={buildingDamageEvents}, snapshotExists={destroyedBuildingSnapshot is not null}, snapshotHp={destroyedBuildingSnapshot?.Hp}, entityExists={destroyedBuildingEntityId is not null}, attackerTarget={buildingTargetAttacker.AttackTargetId}, attackEvents={buildingAttackEventTargets.Count}, deathEvents={buildingDeathEvents.Count}, outcomeEvents={unitBattlefieldOutcomeEvents.Count}, outcome={buildingTargetBattlefield.Outcome}");
        }

        AssertUnitBattlefieldProjectilePresentation();

        var hostilePips = newUnitBattlefield.MinimapPips(PlayerSlotId.One);
        if (hostilePips.Count != newUnitBattlefield.Units.Count
            || hostilePips.All(pip => pip.Relation != PlayerRelation.Hostile)
            || hostilePips.All(pip => pip.Faction != UnitFactionId.Dog))
        {
            throw new InvalidOperationException("new unit battlefield should provide minimap pips from player relation and UnitSpec data");
        }

        if (!newUnitBattlefield.Relations.CanAttack(PlayerSlotId.One, PlayerSlotId.Two))
        {
            throw new InvalidOperationException("new battle relation table should treat different players as hostile by default outside the unit instance");
        }

        newUnitBattlefield.Relations.Set(PlayerSlotId.One, PlayerSlotId.Two, PlayerRelation.Allied);
        if (newUnitBattlefield.Relations.CanAttack(PlayerSlotId.One, PlayerSlotId.Two)
            || newUnitBattlefield.Relations.Relation(PlayerSlotId.Two, PlayerSlotId.One) != PlayerRelation.Allied
            || newUnitBattlefield.Relations.Relation(PlayerSlotId.One, PlayerSlotId.One) != PlayerRelation.Self)
        {
            throw new InvalidOperationException("new battle relation table should keep alliances outside unit instance ownership data");
        }

        var alliedPips = newUnitBattlefield.MinimapPips(PlayerSlotId.One);
    }
}
