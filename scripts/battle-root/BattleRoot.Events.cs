using Godot;
using ProceduralRts.Controllers;
using ProceduralRts.Core;
using ProceduralRts.Ui;
using ProceduralRts.World;

namespace ProceduralRts;

public partial class BattleRoot
{
    private void OnStatusChanged(string status)
    {
        _hud.SetStatus(status);
        AddStatusAlert(status);
    }

    private void OnBuildPlacementStatusChanged(string status)
    {
        OnStatusChanged(status);
        _hud.SetCommandPanelResult(status);
    }

    private void OnUnitInstancesRemoved(IReadOnlyList<UnitInstanceDeathInfo> deaths)
    {
        foreach (var death in deaths)
        {
            if (_unitInstanceViews.Remove(death.Id, out var view))
            {
                view.QueueFree();
            }

            _combatEffects.AddUnitDeath(death, UnitFactionAccent(death.Faction, death.PlayerSlotId));

            if (death.PlayerSlotId == PlayerSlotId.One)
            {
                AddAlert(AlertKind.Combat, GameText.Format("ui.unit.destroyed", UnitDesignCatalog.Spec(death.DesignId).Label), death.Position);
            }
        }

        PlayDeathCue(deaths);
    }

    private void OnUnitInstanceAttacked(UnitInstance target, UnitInstance attacker)
    {
        AddBeamIfNeeded(
            attacker.Position,
            target.Position,
            target.LastDamageAmmoKind,
            attacker.Spec.Faction,
            attacker.PlayerSlotId);

        var impactStyle = _combatEffects.AddImpactFlash(
            target.Position,
            target.Spec.Collision.Radius,
            UnitFactionAccent(target.Spec.Faction, target.PlayerSlotId),
            target.Spec.Stats.WeightClass,
            target.Spec.Movement.Domain,
            target.LastDamageAmount,
            target.LastDamageAmmoKind,
            DamageElementIdForAmmoKind(target.LastDamageAmmoKind));
        RequestImpactShake(target.Position, impactStyle);

        if (target.PlayerSlotId != PlayerSlotId.One || !TryUseAlertCooldown($"unit-attack:{target.Id}", CombatAlertCooldown))
        {
            return;
        }

        AddAlert(AlertKind.Combat, GameText.Format("ui.alert.underAttack", target.Spec.Label), target.Position);
        PlayAudioCue(TacticalAudioCue.Alert, target.Position);
    }

    private void OnUnitInstanceAttackedByBuilding(UnitInstance target, UnitBattlefieldBuildingSnapshot attacker)
    {
        AddBeamIfNeeded(
            attacker.Position,
            target.Position,
            target.LastDamageAmmoKind,
            attacker.Faction,
            attacker.PlayerSlotId);

        var impactStyle = _combatEffects.AddImpactFlash(
            target.Position,
            target.Spec.Collision.Radius,
            UnitFactionAccent(target.Spec.Faction, target.PlayerSlotId),
            target.Spec.Stats.WeightClass,
            target.Spec.Movement.Domain,
            target.LastDamageAmount,
            target.LastDamageAmmoKind,
            DamageElementIdForAmmoKind(target.LastDamageAmmoKind));
        RequestImpactShake(target.Position, impactStyle);

        if (target.PlayerSlotId != PlayerSlotId.One || !TryUseAlertCooldown($"unit-attack:{target.Id}", CombatAlertCooldown))
        {
            return;
        }

        AddAlert(AlertKind.Combat, GameText.Format("ui.alert.underAttack", target.Spec.Label), target.Position);
        PlayAudioCue(TacticalAudioCue.Alert, target.Position);
    }

    private void OnUnitBattlefieldBuildingAttacked(UnitBattlefieldBuildingSnapshot target, UnitInstance attacker)
    {
        var spec = BuildSpecCatalog.For(target.Kind);
        AddBeamIfNeeded(
            attacker.Position,
            target.Position,
            AmmoKindForPrimaryWeapon(attacker),
            attacker.Spec.Faction,
            attacker.PlayerSlotId);

        if (_state.BuildingById(target.Id) is { } building)
        {
            building.Hp = target.Hp;
            building.HitPulse = 1;
        }

        var ammoKind = AmmoKindForPrimaryWeapon(attacker);
        var damage = DamageForPrimaryWeapon(attacker, spec);
        var impactStyle = _combatEffects.AddImpactFlash(
            target.Position,
            Mathf.Max(spec.Footprint.X, spec.Footprint.Y) * 0.5f,
            UnitFactionAccent(target.Faction, target.PlayerSlotId),
            UnitWeightClass.Heavy,
            MovementDomain.Land,
            damage,
            ammoKind,
            DamageElementIdForAmmoKind(ammoKind));
        RequestImpactShake(target.Position, impactStyle);
        if (target.PlayerSlotId != PlayerSlotId.One || !TryUseAlertCooldown($"building-attack:{target.Id}", CombatAlertCooldown))
        {
            return;
        }

        AddAlert(AlertKind.Combat, GameText.Format("ui.alert.underAttack", BuildSpecCatalog.For(target.Kind).Label), target.Position);
        PlayAudioCue(TacticalAudioCue.Alert, target.Position);
    }

    private void OnWeaponFired(WeaponFiredEvent fired)
    {
        var accent = WeaponCatalog.WeaponDefinitions.TryGetValue(fired.WeaponId, out var weapon)
            && WeaponCatalog.AmmoDefinitions.TryGetValue(weapon.AmmoId, out var ammo)
            ? ammo.Accent
            : new Color("#f6c55c");
        _combatEffects.AddMuzzleFlash(fired.Muzzle, fired.TargetPosition, accent, fired.LegacyWeaponKind);
    }

    private void OnProjectileImpacted(ProjectileImpactEvent impact)
    {
        if (!WeaponCatalog.AmmoDefinitions.TryGetValue(impact.AmmoId, out var ammo))
        {
            return;
        }

        var radius = ammo.SplashRadius > 0
            ? Mathf.Clamp(ammo.SplashRadius * 0.42f, 14f, 32f)
            : 12f;
        var style = _combatEffects.AddImpactFlash(
            impact.Position,
            radius,
            ElementPresentationCatalog.ProjectileAccentFor(ammo.DamageElementId, ammo.Accent),
            ammo.Behavior == ProjectileBehavior.Ballistic ? UnitWeightClass.Heavy : UnitWeightClass.Light,
            MovementDomain.Land,
            ammo.BaseDamage,
            ammo.LegacyKind,
            ammo.DamageElementId);
        if (ammo.Behavior == ProjectileBehavior.Ballistic || ammo.SplashRadius > 0)
        {
            RequestImpactShake(impact.Position, style);
        }
    }

    private void OnUnitBattlefieldOutcomeChanged(GameOutcome outcome)
    {
        OnOutcomeChanged(outcome);
    }

    private void OnBuildingsRemoved(IReadOnlyList<int> buildingIds)
    {
        Vector2? deathCuePosition = null;
        foreach (var id in buildingIds)
        {
            _unitBattlefield.RemoveBuildingTarget(id);
            if (!_buildingViews.Remove(id, out var view))
            {
                continue;
            }

            deathCuePosition ??= view.Building.Position;
            if (view.Building.Owner == ProceduralRts.Core.Owner.Player)
            {
                AddAlert(AlertKind.Building, GameText.Format("ui.building.destroyed", BuildSpecCatalog.For(view.Building.Kind).Label), view.Building.Position);
                if (view.Building.Kind == BuildingDesignIds.PowerPlant)
                {
                    UpdatePowerAlert(true);
                }
            }

            view.QueueFree();
        }

        PlayDeathCue(deathCuePosition);
    }

    private void OnProductionCompleted(BuildingModel building, CompletedProductionItem completed)
    {
        if (UseUnitDesignRuntime)
        {
            return;
        }

        var spec = UnitDesignCatalog.Spec(completed.DesignId);
        _hud.SetStatus(GameText.Format("ui.production.deployedFrom", spec.Label, BuildSpecCatalog.For(building.Kind).Label));
        _hud.SetProductionStatus(GameText.Format("ui.production.deployed", spec.Label));
        AddProductionCompleteAlert(completed.DesignId, spec.Label, building.Position);
        PlayAudioCue(TacticalAudioCue.Production, building.Position);
        RefreshCommandCard();
    }

    private void OnUnitBattlefieldProductionCompleted(UnitBattlefieldBuildingSnapshot building, UnitProductionQueueItem item, UnitInstance unit)
    {
        AddUnitInstanceView(unit);
        var spec = UnitDesignCatalog.Spec(item.DesignId);
        _hud.SetStatus(GameText.Format("ui.production.deployedFrom", spec.Label, BuildSpecCatalog.For(building.Kind).Label));
        _hud.SetProductionStatus(GameText.Format("ui.production.deployed", spec.Label));
        AddProductionCompleteAlert(item.DesignId, spec.Label, building.Position);
        PlayAudioCue(TacticalAudioCue.Production, building.Position);
        RefreshCommandCard();
    }

    private void OnProductionRequested(ProductionKind productionKind, int requestCount = 1)
    {
        var status = "";
        var queued = 0;
        var attempts = Math.Max(1, requestCount);
        for (var attempt = 0; attempt < attempts; attempt++)
        {
            if (!_unitBattlefield.TryCreateProductionPayload(productionKind, PlayerSlotId.One, out var payload, out status))
            {
                break;
            }

            var result = _unitBattlefield.SubmitLiveLocalPlayerCommand(PlayerSlotId.One, PlayerCommandKind.Produce, payload);
            status = GatewayStatus(result, status);
            if (result.AcceptedCount == 0)
            {
                break;
            }

            queued++;
        }

        status = ProductionBatchStatus(queued, attempts, status);
        _hud.SetStatus(status);
        _hud.SetProductionStatus(status);
        AddStatusAlert(status);
        RefreshCommandCard();
    }

    private void OnProductionDesignRequested(string designId, Func<int?> providerIdSelector, int requestCount = 1)
    {
        var status = "";
        var queued = 0;
        var attempts = Math.Max(1, requestCount);
        for (var attempt = 0; attempt < attempts; attempt++)
        {
            if (!TrySubmitProductionDesignRequest(designId, providerIdSelector(), out status))
            {
                break;
            }

            queued++;
        }

        status = ProductionBatchStatus(queued, attempts, status);
        _hud.SetStatus(status);
        _hud.SetProductionStatus(status);
        AddStatusAlert(status);
        RefreshCommandCard();
    }

    private bool TrySubmitProductionDesignRequest(string designId, int? providerId, out string status)
    {
        PlayerCommandPayload payload;
        bool canSubmit;
        if (providerId is { } specificProviderId)
        {
            canSubmit = _unitBattlefield.TryCreateProductionDesignPayloadForProvider(
                designId,
                PlayerSlotId.One,
                specificProviderId,
                out payload,
                out status);
        }
        else
        {
            canSubmit = _unitBattlefield.TryCreateProductionDesignPayload(
                designId,
                PlayerSlotId.One,
                out payload,
                out status);
        }

        if (canSubmit)
        {
            var result = _unitBattlefield.SubmitLiveLocalPlayerCommand(PlayerSlotId.One, PlayerCommandKind.Produce, payload);
            status = GatewayStatus(result, status);
            return result.AcceptedCount > 0;
        }

        return false;
    }

    private void OnProductionRepeatRequested(string designId, int providerId)
    {
        var accepted = _unitBattlefield.ToggleRepeatProductionForProvider(designId, PlayerSlotId.One, providerId, out var status);
        _hud.SetStatus(status);
        _hud.SetProductionStatus(status);
        if (accepted)
        {
            AddStatusAlert(status);
        }

        RefreshCommandCard();
    }

    private static string ProductionBatchStatus(int queued, int attempts, string status)
    {
        return queued > 1
            ? GameText.Format("production.batchQueued", queued, attempts, status)
            : status;
    }

    private void OnProductionStatusChanged(string status)
    {
        _hud.SetProductionStatus(status);
        AddStatusAlert(status);
    }

    private void OnResourceInventoryChanged(ProceduralRts.Core.Owner owner, ResourceInventory inventory)
    {
        if (UseUnitDesignRuntime && !_syncingResourceInventories)
        {
            _syncingResourceInventories = true;
            _unitBattlefield.SetCredits(PlayerSlotForOwner(owner), inventory.Credits);
            _syncingResourceInventories = false;
        }

        if (owner != ProceduralRts.Core.Owner.Player)
        {
            return;
        }

        _hud.SetResourceCredits(inventory.Credits);
        RefreshCommandCard();
    }

    private void OnUnitBattlefieldResourceInventoryChanged(PlayerSlotId playerSlotId, ResourceInventory inventory)
    {
        if (UseUnitDesignRuntime && !_syncingResourceInventories && OwnerForPlayerSlot(playerSlotId) is { } owner)
        {
            _syncingResourceInventories = true;
            _state.SetCredits(owner, inventory.Credits);
            _syncingResourceInventories = false;
        }

        if (playerSlotId != PlayerSlotId.One)
        {
            return;
        }

        _hud.SetResourceCredits(inventory.Credits);
        RefreshCommandCard();
    }

    private void OnMinimapJumpRequested(Vector2 worldPoint)
    {
        _camera.FocusOnWorldPoint(worldPoint);
        RefreshViewCulling();
        _hud.SetStatus(GameText.Format("ui.camera.moved", worldPoint.X, worldPoint.Y));
    }

    private void OnMoveModeRequested(MoveCommandMode mode)
    {
        _selection.SetMoveCommandMode(mode);
        _hud.SetMoveCommandMode(mode);
        _hud.SetStatus(CommandRibbonContextResolver.MoveModeLabel(mode));
        PlayAudioCue(mode == MoveCommandMode.Attack ? TacticalAudioCue.Attack : TacticalAudioCue.Move);
    }

    private void OnUnitStanceRequested(UnitStance stance)
    {
        if (UseUnitDesignRuntime)
        {
            var selectedCount = _unitBattlefield.SelectedCount(PlayerSlotId.One);
            if (selectedCount == 0)
            {
                _hud.SetStatus(GameText.T("stance.selectRequired"));
                PlayAudioCue(TacticalAudioCue.Invalid);
                return;
            }

            var subjects = _unitBattlefield.SelectedUnitEntityIds(PlayerSlotId.One);
            var payload = PlayerCommandPayload.ForSubjects(subjects) with { Stance = stance };
            var result = _unitBattlefield.SubmitLiveLocalPlayerCommand(PlayerSlotId.One, PlayerCommandKind.SetStance, payload);
            var changed = result.AcceptedCount > 0 ? selectedCount : 0;
            if (changed == 0)
            {
                _hud.SetStatus(GatewayStatus(result, GameText.T("stance.selectRequired")));
                PlayAudioCue(TacticalAudioCue.Invalid);
                return;
            }

            RefreshSelectionInfo();
            _hud.SetStatus(GameText.Format("stance.changed", changed, UnitStancePresentationCatalog.DefinitionFor(stance).Label));
            PlayAudioCue(TacticalAudioCue.Selection);
            return;
        }

        var legacySelectedCount = _state.SelectedUnitCount();
        if (legacySelectedCount == 0)
        {
            _hud.SetStatus(GameText.T("stance.selectRequired"));
            PlayAudioCue(TacticalAudioCue.Invalid);
            return;
        }

        _state.SetSelectedStance(stance);
        _hud.SetSelectedUnitStance(stance, legacySelectedCount);
        _hud.SetStatus(GameText.Format("stance.changed", legacySelectedCount, UnitStancePresentationCatalog.DefinitionFor(stance).Label));
        PlayAudioCue(TacticalAudioCue.Selection);
    }

    private static string GatewayStatus(CommandGatewayResult result, string acceptedStatus)
    {
        return CommandGatewayFeedback.Status(result, acceptedStatus);
    }

    private void OnSettingsRequested()
    {
        _pauseMenu.SetPaused(true);
        _pauseMenu.OpenSettings();
    }

    private void OnEntityAttacked(ProceduralRts.Core.Owner owner, FactionId factionId, Vector2 position, string label)
    {
        var impactStyle = _combatEffects.AddImpactFlash(
            position,
            24,
            _state.VisualAccent(owner, factionId, FactionCatalog.For(factionId).Accent),
            UnitWeightClass.Medium,
            MovementDomain.Land);
        RequestImpactShake(position, impactStyle);

        if (!FactionRelations.IsAllied(ProceduralRts.Core.Owner.Player, _state.MatchConfig.PlayerFaction, owner, factionId)
            || !TryUseAlertCooldown($"attack:{label}", CombatAlertCooldown))
        {
            return;
        }

        AddAlert(AlertKind.Combat, GameText.Format("ui.alert.underAttack", label), position);
        PlayAudioCue(TacticalAudioCue.Alert, position);
    }

    private void OnOutcomeChanged(GameOutcome outcome)
    {
        if (outcome == GameOutcome.InProgress || _displayedOutcome != GameOutcome.InProgress)
        {
            return;
        }

        _displayedOutcome = outcome;
        var detail = outcome == GameOutcome.Victory
            ? GameText.T("ui.outcome.enemyHqDestroyed")
            : GameText.T("ui.outcome.playerHqDestroyed");
        _hud.SetOutcomeBanner(outcome, detail);
        _hud.SetStatus(outcome == GameOutcome.Victory ? GameText.T("ui.status.victory") : GameText.T("ui.status.defeat"));
        _pauseMenu.InputEnabled = false;
        _outcomeScreen.ShowOutcome(outcome, detail);
        PlayAudioCue(outcome == GameOutcome.Victory ? TacticalAudioCue.OutcomeVictory : TacticalAudioCue.OutcomeDefeat);
        AddAlert(outcome == GameOutcome.Victory ? AlertKind.Production : AlertKind.Combat, detail);
    }

}
