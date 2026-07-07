static partial class Program
{
    private static void AssertRuntimeSnapshotsSmartClickAndBuildingWeapons()
    {
        var worldOnlyState = new GameState();
        worldOnlyState.SetCredits(Owner.Player, 10000);
        var worldOnlyUnit = worldOnlyState.Units.First(unit => unit.Owner == Owner.Player);
        var worldOnlyStart = worldOnlyUnit.Position;
        var worldOnlyUnitCount = worldOnlyState.Units.Count;
        worldOnlyUnit.MoveTarget = worldOnlyStart + new Vector2(240, 0);
        if (!worldOnlyState.EnqueueProduction(ProductionKind.InfantrySquad, Owner.Player, out _))
        {
            throw new InvalidOperationException("world-only update test should be able to queue legacy production before checking no-sim behavior");
        }

        var worldOnlyProducer = worldOnlyState.Buildings.First(building => building.ProductionQueue.Count > 0);
        var extraVisionPosition = new Vector2(worldOnlyState.WorldSize.X - 160, 160);
        worldOnlyState.UpdateWorldOnly(10, [(extraVisionPosition, 140)]);
        if (worldOnlyUnit.Position != worldOnlyStart
            || worldOnlyProducer.ProductionQueue.Count != 1
            || worldOnlyProducer.ProductionQueue[0].Progress > 0
            || worldOnlyState.Units.Count != worldOnlyUnitCount
            || !worldOnlyState.IsExploredByPlayer(extraVisionPosition))
        {
            throw new InvalidOperationException("GameState world-only update should refresh theme/fog without advancing hidden legacy UnitModel movement or production");
        }

        var runtimeSnapshotBattlefield = new UnitBattlefield();
        var runtimeSnapshotSelf = runtimeSnapshotBattlefield.Spawn("dog.infantry", PlayerSlotId.One, new Vector2(140, 140));
        var runtimeSnapshotEnemy = runtimeSnapshotBattlefield.Spawn("cat.basic", PlayerSlotId.Two, new Vector2(420, 140));
        runtimeSnapshotBattlefield.Relations.Set(PlayerSlotId.One, PlayerSlotId.Two, PlayerRelation.Hostile);
        var runtimeSnapshotField = new ResourceFieldModel
        {
            Id = 711,
            Position = new Vector2(260, 220),
            Radius = 64,
            MaxAmount = 400,
            Amount = 240,
            Accent = new Color("#b98232"),
        };
        runtimeSnapshotBattlefield.SetResourceFields([runtimeSnapshotField]);
        var visionSources = runtimeSnapshotBattlefield.VisionSources(PlayerSlotId.One);
        var resourcePips = runtimeSnapshotBattlefield.ResourcePips(position => position.X < 300);
        var hiddenResourcePips = runtimeSnapshotBattlefield.ResourcePips(position => position.X < 100);
        if (visionSources.Count != 1
            || visionSources[0].Position != runtimeSnapshotSelf.Position
            || visionSources.Any(source => source.Position == runtimeSnapshotEnemy.Position)
            || resourcePips.Count != 1
            || Mathf.Abs(resourcePips[0].RemainingRatio - 0.6f) > 0.001f
            || hiddenResourcePips.Count != 0)
        {
            throw new InvalidOperationException("new unit battlefield should expose direct vision and resource minimap snapshots without old GameState resource scans");
        }

        var hiddenLegacyVisionState = new GameState();
        var hiddenLegacyUnit = hiddenLegacyVisionState.Units.First(unit => unit.Owner == Owner.Player);
        hiddenLegacyUnit.Position = new Vector2(hiddenLegacyVisionState.WorldSize.X - 80, hiddenLegacyVisionState.WorldSize.Y - 80);
        var hiddenLegacyOnlyPoint = hiddenLegacyUnit.Position;
        hiddenLegacyVisionState.UpdateWorldOnly(10, [(new Vector2(80, 80), 40f)]);
        if (hiddenLegacyVisionState.IsVisibleToPlayer(hiddenLegacyOnlyPoint))
        {
            throw new InvalidOperationException("world-only fog refresh with UnitBattlefield vision sources should not include hidden legacy UnitModel vision");
        }

        var runtimeHarvestBattlefield = new UnitBattlefield();
        runtimeHarvestBattlefield.WorldSize = new Vector2(900, 700);
        runtimeHarvestBattlefield.SetCredits(PlayerSlotId.One, 0);
        var runtimeResourceField = new ResourceFieldModel
        {
            Id = 701,
            Position = new Vector2(340, 260),
            Radius = 82,
            MaxAmount = 150,
            Amount = 150,
            Accent = new Color("#b98232"),
        };
        runtimeHarvestBattlefield.SetResourceFields([runtimeResourceField]);
        var runtimeRefinery = runtimeHarvestBattlefield.UpsertBuildingTarget(
            702,
            BuildingDesignIds.Refinery,
            PlayerSlotId.One,
            UnitFactionId.Dog,
            new Vector2(520, 260),
            0,
            BuildSpecCatalog.For(BuildingDesignIds.Refinery).MaxHp);
        var runtimeHarvester = runtimeHarvestBattlefield.Spawn("dog.harvester", PlayerSlotId.One, new Vector2(300, 260));
        runtimeHarvestBattlefield.SelectUnitsByIds(PlayerSlotId.One, [runtimeHarvester.Id]);
        var commandsBeforeHarvest = runtimeHarvestBattlefield.AppliedInputCommandCount;
        if (!runtimeHarvestBattlefield.CommandHarvestSelected(PlayerSlotId.One, runtimeResourceField, out _)
            || runtimeHarvester.HarvesterMode != HarvesterMode.MovingToField
            || runtimeHarvester.HarvestFieldId != runtimeResourceField.Id
            || runtimeHarvestBattlefield.BuildingDockReservedByHarvesterId(runtimeRefinery.Id) is not null)
        {
            throw new InvalidOperationException("new unit battlefield should accept harvest commands for selected UnitInstance harvesters without immediately reserving legacy refinery docks");
        }

        var runtimeHarvesterEntity = runtimeHarvestBattlefield.UnitEntityByInstanceId(runtimeHarvester.Id);
        var runtimeResourceEntity = runtimeHarvestBattlefield.ResourceEntityByFieldId(runtimeResourceField.Id);
        if (runtimeHarvestBattlefield.AppliedInputCommandCount != commandsBeforeHarvest + 1
            || runtimeHarvesterEntity is null
            || runtimeResourceEntity is null
            || !runtimeResourceEntity.Components.TryGet<ResourceNodeComponentState>(out var mirroredResourceNode)
            || mirroredResourceNode.Amount != runtimeResourceField.Amount
            || !runtimeHarvesterEntity.Components.TryGet<HarvesterComponentState>(out var mirroredHarvester)
            || mirroredHarvester.Mode != HarvesterMode.MovingToField
            || mirroredHarvester.FieldId != runtimeResourceEntity.Id.Value
            || !runtimeHarvesterEntity.Components.TryGet<MovementComponentState>(out var mirroredHarvestMove)
            || mirroredHarvestMove.MoveTarget != runtimeResourceField.Position)
        {
            throw new InvalidOperationException("UnitBattlefield harvest input should route through EntityCommandBuffer using mirrored ResourceNode entities");
        }

        var smartClickBattlefield = new UnitBattlefield { WorldSize = new Vector2(1200, 900) };
        smartClickBattlefield.Relations.Set(PlayerSlotId.One, PlayerSlotId.Two, PlayerRelation.Hostile);
        var smartClickResource = new ResourceFieldModel
        {
            Id = 733,
            Position = new Vector2(360, 300),
            Radius = 70,
            MaxAmount = 240,
            Amount = 240,
            Accent = new Color("#b98232"),
        };
        smartClickBattlefield.SetResourceFields([smartClickResource]);
        var smartClickRefinery = smartClickBattlefield.UpsertBuildingTarget(
            734,
            BuildingDesignIds.Refinery,
            PlayerSlotId.One,
            UnitFactionId.Dog,
            new Vector2(520, 300),
            0,
            BuildSpecCatalog.For(BuildingDesignIds.Refinery).MaxHp);
        var smartClickBarracks = smartClickBattlefield.UpsertBuildingTarget(
            735,
            BuildingDesignIds.Barracks,
            PlayerSlotId.One,
            UnitFactionId.Dog,
            new Vector2(260, 520),
            0,
            BuildSpecCatalog.For(BuildingDesignIds.Barracks).MaxHp);
        var smartClickHarvester = smartClickBattlefield.Spawn("dog.harvester", PlayerSlotId.One, new Vector2(300, 300));
        var smartClickAttacker = smartClickBattlefield.Spawn("dog.guard_tank", PlayerSlotId.One, new Vector2(300, 380));
        var smartClickEngineer = smartClickBattlefield.Spawn("dog.engineer", PlayerSlotId.One, new Vector2(300, 450));
        var smartClickAlly = smartClickBattlefield.Spawn("dog.guard_tank", PlayerSlotId.One, new Vector2(430, 450));
        var smartClickEnemy = smartClickBattlefield.Spawn("cat.basic", PlayerSlotId.Two, new Vector2(520, 380), Mathf.Pi);
        var smartClickEnemyHq = smartClickBattlefield.UpsertBuildingTarget(
            736,
            BuildingDesignIds.Headquarters,
            PlayerSlotId.Two,
            UnitFactionId.Cat,
            new Vector2(620, 520),
            Mathf.Pi,
            BuildSpecCatalog.For(BuildingDesignIds.Headquarters).MaxHp);
        smartClickAlly.Hp -= 24;
        smartClickBattlefield.UpsertBuildingTarget(
            smartClickRefinery.Id,
            smartClickRefinery.Kind,
            smartClickRefinery.PlayerSlotId,
            smartClickRefinery.Faction,
            smartClickRefinery.Position,
            smartClickRefinery.Facing,
            smartClickRefinery.Hp - 32);

        smartClickBattlefield.SelectUnitsByIds(PlayerSlotId.One, [smartClickHarvester.Id]);
        var smartClickBeforeHarvest = smartClickBattlefield.AppliedInputCommandCount;
        if (!smartClickBattlefield.CommandHarvestSelected(PlayerSlotId.One, smartClickResource, out _)
            || smartClickBattlefield.AppliedInputCommandCount != smartClickBeforeHarvest + 1
            || smartClickBattlefield.UnitEntityByInstanceId(smartClickHarvester.Id)?.Components.Require<HarvesterComponentState>().FieldId != smartClickBattlefield.ResourceEntityByFieldId(smartClickResource.Id)?.Id.Value)
        {
            throw new InvalidOperationException("smart right-click resource branch should route selected harvesters through UnitBattlefield EntityCommandBuffer harvest commands");
        }

        smartClickBattlefield.SelectUnitsByIds(PlayerSlotId.One, [smartClickAttacker.Id]);
        var smartClickBeforeAttack = smartClickBattlefield.AppliedInputCommandCount;
        smartClickBattlefield.CommandAttackSelected(PlayerSlotId.One, smartClickEnemy);
        if (smartClickBattlefield.AppliedInputCommandCount != smartClickBeforeAttack + 1
            || smartClickBattlefield.UnitEntityByInstanceId(smartClickAttacker.Id)?.Components.Require<WeaponUserComponentState>().AttackTarget != smartClickEnemy.EntityId)
        {
            throw new InvalidOperationException("smart right-click enemy branch should route selected attackers through UnitBattlefield EntityCommandBuffer attack commands");
        }

        var smartClickBeforeBuildingAttack = smartClickBattlefield.AppliedInputCommandCount;
        var smartClickEnemyHqProjection = smartClickBattlefield.PickHostileBuildingHoverProjection(smartClickEnemyHq.Position, PlayerSlotId.One, pickPadding: 8);
        if (smartClickEnemyHqProjection is null
            || smartClickEnemyHqProjection.Value.Id != smartClickEnemyHq.Id
            || !smartClickBattlefield.CommandAttackSelected(PlayerSlotId.One, smartClickEnemyHqProjection.Value.Id)
            || smartClickBattlefield.AppliedInputCommandCount != smartClickBeforeBuildingAttack + 1
            || smartClickBattlefield.UnitEntityByInstanceId(smartClickAttacker.Id)?.Components.Require<WeaponUserComponentState>().AttackTarget != smartClickBattlefield.BuildingEntityIdByTargetId(smartClickEnemyHq.Id))
        {
            throw new InvalidOperationException("building public surface should route hostile building attack through hover projection plus building id instead of mutable building target handles");
        }

        var smartClickGround = new Vector2(780, 620);
        var smartClickBeforeMove = smartClickBattlefield.AppliedInputCommandCount;
        smartClickBattlefield.CommandMoveSelected(PlayerSlotId.One, smartClickGround, smartClickBattlefield.WorldSize, MoveCommandMode.Direct);
        if (smartClickBattlefield.AppliedInputCommandCount != smartClickBeforeMove + 1
            || smartClickBattlefield.UnitEntityByInstanceId(smartClickAttacker.Id)?.Components.Require<CommandableComponentState>().CommandVisualTarget != smartClickGround)
        {
            throw new InvalidOperationException("smart right-click ground branch should route selected units through UnitBattlefield EntityCommandBuffer move commands");
        }

        smartClickBattlefield.SelectUnitsByIds(PlayerSlotId.One, [smartClickEngineer.Id]);
        var smartClickBeforeRepair = smartClickBattlefield.AppliedInputCommandCount;
        if (!smartClickBattlefield.CanRepairSelected(PlayerSlotId.One, smartClickAlly)
            || !smartClickBattlefield.CommandRepairSelected(PlayerSlotId.One, smartClickAlly, out _)
            || smartClickBattlefield.AppliedInputCommandCount != smartClickBeforeRepair + 1
            || smartClickBattlefield.UnitEntityByInstanceId(smartClickEngineer.Id)?.Components.Require<RepairOrderComponentState>().TargetId != smartClickAlly.EntityId.Value
            || smartClickBattlefield.UnitEntityByInstanceId(smartClickEngineer.Id)?.Components.Require<CommandableComponentState>().CommandVisualTarget != smartClickAlly.Position)
        {
            throw new InvalidOperationException("smart right-click damaged ally branch should route selected repairers through UnitBattlefield EntityCommandBuffer repair commands");
        }

        var smartClickBeforeBuildingRepair = smartClickBattlefield.AppliedInputCommandCount;
        var smartClickRefineryProjection = smartClickBattlefield.PickAnyBuildingHoverProjection(smartClickRefinery.Position, PlayerSlotId.One, pickPadding: 8);
        if (smartClickRefineryProjection is null
            || smartClickRefineryProjection.Value.Id != smartClickRefinery.Id
            || !smartClickBattlefield.CanRepairSelectedBuilding(PlayerSlotId.One, smartClickRefineryProjection.Value.Id)
            || !smartClickBattlefield.CommandRepairSelectedBuilding(PlayerSlotId.One, smartClickRefineryProjection.Value.Id, out _)
            || smartClickBattlefield.AppliedInputCommandCount != smartClickBeforeBuildingRepair + 1
            || smartClickBattlefield.UnitEntityByInstanceId(smartClickEngineer.Id)?.Components.Require<RepairOrderComponentState>().TargetId != smartClickBattlefield.BuildingEntityIdByTargetId(smartClickRefinery.Id)?.Value
            || smartClickBattlefield.UnitEntityByInstanceId(smartClickEngineer.Id)?.Components.Require<CommandableComponentState>().CommandVisualTarget != smartClickRefinery.Position)
        {
            throw new InvalidOperationException("building public surface should route damaged building repair through hover projection plus building id instead of mutable building target handles");
        }

        smartClickBattlefield.SelectBuildingTargetAt(PlayerSlotId.One, smartClickBarracks.Position, additive: false, pickPadding: 8);
        var smartClickBeforeResourceRally = smartClickBattlefield.AppliedInputCommandCount;
        if (!smartClickBattlefield.SetSelectedBuildingRallyPoints(PlayerSlotId.One, smartClickResource, out _)
            || smartClickBattlefield.AppliedInputCommandCount != smartClickBeforeResourceRally + 1
            || BuildingEntityForTargetId(smartClickBattlefield, smartClickBarracks.Id)?.Components.Require<RallyPointComponentState>().TargetEntityId != smartClickBattlefield.ResourceEntityByFieldId(smartClickResource.Id)?.Id.Value
            || BuildingEntityForTargetId(smartClickBattlefield, smartClickRefinery.Id)?.Components.TryGet<RallyPointComponentState>(out _) == true)
        {
            throw new InvalidOperationException("smart right-click resource rally branch should keep production-building resource rally on SetRallyPointEntityCommand with a ResourceNode target entity");
        }

        var smartClickBeforeFriendlyRally = smartClickBattlefield.AppliedInputCommandCount;
        var smartClickBarracksSubjects = smartClickBattlefield.SelectedBuildingEntityIds(PlayerSlotId.One);
        var hostileRallyPayload = PlayerCommandPayload.ForPoint(smartClickBarracksSubjects, smartClickEnemy.Position.X, smartClickEnemy.Position.Y) with { TargetEntity = smartClickEnemy.EntityId };
        var hostileRallyResult = smartClickBattlefield.SubmitLiveLocalPlayerCommand(PlayerSlotId.One, PlayerCommandKind.Rally, hostileRallyPayload);
        var hostileRallyRejectedWithoutCommand = hostileRallyResult.AcceptedCount == 0
            && smartClickBattlefield.AppliedInputCommandCount == smartClickBeforeFriendlyRally;
        var friendlyRallyAccepted = smartClickBattlefield.SetSelectedBuildingRallyPoints(PlayerSlotId.One, smartClickAlly, out _);
        var friendlyRallyState = BuildingEntityForTargetId(smartClickBattlefield, smartClickBarracks.Id)?.Components.Require<RallyPointComponentState>();
        if (!hostileRallyRejectedWithoutCommand
            || !friendlyRallyAccepted
            || smartClickBattlefield.AppliedInputCommandCount != smartClickBeforeFriendlyRally + 1
            || friendlyRallyState?.TargetEntityId != smartClickAlly.EntityId.Value
            || friendlyRallyState?.Target != smartClickAlly.Position
            || smartClickBattlefield.SetSelectedBuildingRallyPoints(PlayerSlotId.One, smartClickEnemy, out _))
        {
            throw new InvalidOperationException("smart right-click friendly-unit rally branch should accept friendly runtime unit targets and reject hostile unit targets through the live rally gateway");
        }

        for (var step = 0; step < 420; step++)
        {
            runtimeHarvestBattlefield.Update(1 / 30.0);
        }

        if (runtimeResourceField.Amount != 0
            || runtimeHarvestBattlefield.Credits(PlayerSlotId.One) != 150
            || runtimeHarvester.Cargo != 0
            || runtimeHarvester.HarvesterMode != HarvesterMode.Idle
            || runtimeHarvester.HarvestFieldId is not null
            || runtimeHarvester.HarvestRefineryId is not null
            || runtimeHarvestBattlefield.BuildingDockReservedByHarvesterId(runtimeRefinery.Id) is not null
            || runtimeHarvestBattlefield.BuildingDockedHarvesterId(runtimeRefinery.Id) is not null)
        {
            throw new InvalidOperationException($"new unit battlefield should gather resources, unload at refineries, credit the owner, and release refinery dock claims; amount={runtimeResourceField.Amount}, credits={runtimeHarvestBattlefield.Credits(PlayerSlotId.One)}, cargo={runtimeHarvester.Cargo}, mode={runtimeHarvester.HarvesterMode}, field={runtimeHarvester.HarvestFieldId}, refinery={runtimeHarvester.HarvestRefineryId}, reserved={runtimeHarvestBattlefield.BuildingDockReservedByHarvesterId(runtimeRefinery.Id)}, docked={runtimeHarvestBattlefield.BuildingDockedHarvesterId(runtimeRefinery.Id)}, position={runtimeHarvester.Position}, move={runtimeHarvester.MoveTarget}");
        }

        var buildingWeaponBattlefield = new UnitBattlefield();
        var buildingAcquireBattlefield = new UnitBattlefield();
        var acquiringHq = buildingAcquireBattlefield.UpsertBuildingTarget(
            800,
            BuildingDesignIds.Headquarters,
            PlayerSlotId.One,
            UnitFactionId.Dog,
            new Vector2(300, 260),
            0,
            BuildSpecCatalog.For(BuildingDesignIds.Headquarters).MaxHp);
        var acquiringTarget = buildingAcquireBattlefield.Spawn("cat.tank", PlayerSlotId.Two, new Vector2(430, 260), Mathf.Pi);
        buildingAcquireBattlefield.Update(1 / 30.0);
        var acquiringEntity = BuildingEntityForTargetId(buildingAcquireBattlefield, acquiringHq.Id);
        if (acquiringEntity is null
            || !acquiringEntity.Components.TryGet<WeaponUserComponentState>(out var acquiringWeaponState)
            || acquiringWeaponState.AttackTarget != acquiringTarget.EntityId
            || buildingAcquireBattlefield.BuildingAttackTargetId(acquiringHq.Id) != acquiringTarget.Id)
        {
            throw new InvalidOperationException("armed building targets should acquire hostile units through EntityWorld TurretCombatSystem state");
        }

        var defendingHq = buildingWeaponBattlefield.UpsertBuildingTarget(
            801,
            BuildingDesignIds.Headquarters,
            PlayerSlotId.One,
            UnitFactionId.Dog,
            new Vector2(300, 300),
            0,
            BuildSpecCatalog.For(BuildingDesignIds.Headquarters).MaxHp);
        var buildingWeaponTarget = buildingWeaponBattlefield.Spawn("cat.basic", PlayerSlotId.Two, new Vector2(430, 300), Mathf.Pi);
        var buildingWeaponHits = 0;
        var buildingWeaponDeaths = new List<UnitInstanceDeathInfo>();
        buildingWeaponBattlefield.UnitAttackedByBuilding += (_, attacker) =>
        {
            if (attacker.Id == defendingHq.Id)
            {
                buildingWeaponHits++;
            }
        };
        buildingWeaponBattlefield.UnitsRemoved += deaths => buildingWeaponDeaths.AddRange(deaths);
        for (var step = 0; step < 180; step++)
        {
            buildingWeaponBattlefield.Update(1 / 30.0);
        }

        if (buildingWeaponHits == 0
            || buildingWeaponTarget.Hp >= buildingWeaponTarget.Spec.Stats.MaxHp
            || buildingWeaponBattlefield.Units.Any(unit => unit.Id == buildingWeaponTarget.Id)
            || buildingWeaponDeaths.Count != 1
            || buildingWeaponDeaths[0].DesignId != "cat.basic"
            || buildingWeaponBattlefield.BuildingAttackTargetId(defendingHq.Id) is not null)
        {
            throw new InvalidOperationException("new unit battlefield should let armed building targets auto-acquire and destroy hostile UnitInstance units through EntityWorld turret combat");
        }
    }
}
