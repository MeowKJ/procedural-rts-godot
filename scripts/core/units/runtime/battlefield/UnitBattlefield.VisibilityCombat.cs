using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    public IReadOnlyList<UnitMinimapPip> MinimapPips(PlayerSlotId viewer)
    {
        var result = NextUnitMinimapPipBuffer();
        foreach (var unit in Units)
        {
            result.Add(new UnitMinimapPip(
                unit.Position,
                unit.PlayerSlotId,
                unit.Spec.Faction,
                Relations.Relation(viewer, unit.PlayerSlotId),
                unit.Selected,
                unit.AlertPulse));
        }

        return result;
    }

    private List<UnitMinimapPip> NextUnitMinimapPipBuffer()
    {
        _useSecondaryUnitMinimapPipBuffer = !_useSecondaryUnitMinimapPipBuffer;
        var result = _useSecondaryUnitMinimapPipBuffer ? _unitMinimapPipSecondaryBuffer : _unitMinimapPipBuffer;
        result.Clear();
        return result;
    }

    public void RebuildVisibilityIndex()
    {
        SyncOwnerRelations();
        SyncUnitEntities();
        SyncBuildingTargetEntities();
        SyncResourceFieldEntities();
        _visionSystem.Step(new SimContext(_entityWorld, _inputCommandTick, 0, []));
        MarkVisibleBuildingFootprints();
    }

    public bool IsVisibleTo(PlayerSlotId viewer, UnitInstance unit)
    {
        return _entityWorld.Visibility.IsVisible(OwnerId.FromPlayerSlot(viewer), unit.EntityId);
    }

    public bool IsVisibleTo(PlayerSlotId viewer, int buildingId)
    {
        return IsVisibleToCore(viewer, buildingId);
    }

    private bool IsVisibleToCore(PlayerSlotId viewer, int buildingId)
    {
        return BuildingEntityByTargetId(buildingId) is { } entity
            && _entityWorld.Visibility.IsVisible(OwnerId.FromPlayerSlot(viewer), entity.Id);
    }

    private void MarkVisibleBuildingFootprints()
    {
        foreach (var viewer in Units)
        {
            if (viewer.Hp <= 0)
            {
                continue;
            }

            MarkVisibleBuildingFootprints(
                viewer.PlayerSlotId,
                viewer.Position,
                viewer.Spec.Stats.SightRange);
        }

        CollectBuildingTargetIds(_buildingVisibilityViewerIdBuffer);
        foreach (var buildingId in _buildingVisibilityViewerIdBuffer)
        {
            if (BuildingSnapshot(buildingId) is not { } viewer
                || viewer.Hp <= 0
                || BuildingBuildProgress(viewer.Id) < 1)
            {
                continue;
            }

            MarkVisibleBuildingFootprints(
                viewer.PlayerSlotId,
                viewer.Position,
                BuildSpecCatalog.For(viewer.Kind).SightRange);
        }
    }

    private void MarkVisibleBuildingFootprints(PlayerSlotId viewer, Vector2 viewerPosition, float sightRange)
    {
        if (sightRange <= 0)
        {
            return;
        }

        var owner = OwnerId.FromPlayerSlot(viewer);
        CollectBuildingTargetIds(_buildingVisibilityTargetIdBuffer);
        foreach (var buildingId in _buildingVisibilityTargetIdBuffer)
        {
            if (BuildingSnapshot(buildingId) is not { } building)
            {
                continue;
            }

            if (building.Hp <= 0
                || !Relations.CanAttack(viewer, building.PlayerSlotId)
                || !_buildingTargetEntityIds.TryGetValue(building.Id, out var entityId)
                || _entityWorld.Visibility.IsVisible(owner, entityId))
            {
                continue;
            }

            var visibleRange = sightRange + BuildingTargetRadiusCore(building.Id, building.Kind);
            if (viewerPosition.DistanceSquaredTo(building.Position) <= visibleRange * visibleRange)
            {
                _entityWorld.Visibility.MarkVisible(owner, entityId);
            }
        }
    }

    public IReadOnlyList<UnitSelectionSummaryItem> SelectionSummary()
    {
        _selectionSummaryBuffer.Clear();
        foreach (var unit in Units)
        {
            if (unit.Selected)
            {
                AddSelectionSummaryUnit(unit);
            }
        }

        _selectionSummaryBuffer.Sort(CompareUnitSelectionSummaryItems);
        return _selectionSummaryBuffer;
    }

    private void AddSelectionSummaryUnit(UnitInstance unit)
    {
        for (var index = 0; index < _selectionSummaryBuffer.Count; index++)
        {
            var item = _selectionSummaryBuffer[index];
            if (item.DesignId == unit.Spec.Id && item.PlayerSlotId == unit.PlayerSlotId)
            {
                _selectionSummaryBuffer[index] = item with { Count = item.Count + 1 };
                return;
            }
        }

        _selectionSummaryBuffer.Add(new UnitSelectionSummaryItem(
            unit.Spec.Id,
            unit.PlayerSlotId,
            unit.Spec.Faction,
            unit.Spec.Icon,
            unit.Spec.Label,
            unit.Spec.ShortCode,
            1));
    }

    private static int CompareUnitSelectionSummaryItems(UnitSelectionSummaryItem left, UnitSelectionSummaryItem right)
    {
        return string.Compare(left.DesignId, right.DesignId, StringComparison.Ordinal);
    }

    private static void UpdateMovementIntent(UnitInstance unit, float dt)
    {
        if (unit.MoveTarget is not { } target)
        {
            unit.Velocity = unit.Velocity.MoveToward(Vector2.Zero, unit.Spec.Movement.Speed * DefaultAccelerationMultiplier * dt);
            return;
        }

        var toTarget = target - unit.Position;
        var distance = toTarget.Length();
        var stopDistance = MathF.Max(unit.Spec.Movement.StopDistance, MathF.Max(4f, unit.Spec.Collision.Radius * 0.22f));
        if (distance <= stopDistance)
        {
            unit.Position = target;
            unit.Velocity = Vector2.Zero;
            unit.MoveTarget = null;
            unit.FormationSlot = target;
            return;
        }

        var desiredDirection = toTarget / MathF.Max(distance, 0.001f);
        var slowRadius = MathF.Max(unit.Spec.Collision.Radius * 2.6f, 42);
        var speedScale = Mathf.Clamp(distance / slowRadius, 0.22f, 1f);
        var desiredVelocity = desiredDirection * unit.Spec.Movement.Speed * speedScale;
        var acceleration = unit.Spec.Movement.Acceleration > 0
            ? unit.Spec.Movement.Acceleration
            : unit.Spec.Movement.Speed * DefaultAccelerationMultiplier;
        unit.Velocity = unit.Velocity.MoveToward(desiredVelocity, acceleration * dt);
        unit.Position += unit.Velocity * dt;
        unit.Facing = RotateToward(unit.Facing, unit.Velocity.Angle(), unit.Spec.Movement.TurnRate * dt);

        for (var index = 0; index < unit.WeaponMounts.Count; index++)
        {
            var mount = unit.WeaponMounts[index];
            if (unit.Spec.Weapons[index].FacingMode != WeaponMountFacingMode.Independent)
            {
                unit.WeaponMounts[index] = mount with { Facing = unit.Facing };
            }
        }
    }

    private void AcquireAutoTarget(UnitInstance unit)
    {
        if (unit.AttackTargetId is not null
            || unit.MoveMode == MoveCommandMode.Ignore
            || (unit.MoveTarget is not null && unit.MoveMode != MoveCommandMode.Attack))
        {
            return;
        }

        if (IsHarvester(unit) && unit.HarvesterMode != HarvesterMode.Idle)
        {
            return;
        }

        var weapon = PrimaryWeapon(unit);
        var maxRange = unit.Spec.Stats.SightRange;
        UnitInstance? bestTarget = null;
        var bestPriority = 0f;
        var bestDistanceSquared = float.PositiveInfinity;
        var maxRangeSquared = maxRange * maxRange;
        foreach (var target in Units)
        {
            if (target.Id == unit.Id
                || target.Hp <= 0
                || !Relations.CanAttack(unit.PlayerSlotId, target.PlayerSlotId)
                || !CanWeaponTarget(weapon, target.Spec))
            {
                continue;
            }

            var distanceSquared = unit.Position.DistanceSquaredTo(target.Position);
            if (distanceSquared > maxRangeSquared)
            {
                continue;
            }

            var priority = WeaponTargetPriority(weapon, target.Spec);
            if (priority <= 0
                || priority < bestPriority
                || (priority == bestPriority && distanceSquared >= bestDistanceSquared))
            {
                continue;
            }

            bestTarget = target;
            bestPriority = priority;
            bestDistanceSquared = distanceSquared;
        }

        if (bestTarget is null)
        {
            return;
        }

        unit.AttackTargetId = bestTarget.Id;
        unit.AttackTargetIsManual = false;
    }

    private void UpdateCombat(UnitInstance unit, float dt)
    {
        if (unit.AttackTargetId is not { } targetId)
        {
            return;
        }

        if (unit.MoveTarget is not null && unit.MoveMode != MoveCommandMode.Attack)
        {
            ClearAttackTarget(unit);
            return;
        }

        if (unit.AttackTargetKind == CombatTargetKind.Building)
        {
            return;
        }

        var target = UnitById(targetId);
        if (target is null
            || target.Hp <= 0
            || !Relations.CanAttack(unit.PlayerSlotId, target.PlayerSlotId)
            || !CanUnitTarget(unit, target))
        {
            ClearAttackTarget(unit);
            return;
        }

        var weapon = PrimaryWeapon(unit);
        var toTarget = target.Position - unit.Position;
        var distance = toTarget.Length();
        var range = weapon.Range + target.Spec.Collision.Radius;
        var targetAngle = toTarget.Angle();
        AimWeaponMounts(unit, targetAngle, dt);

        if (distance > range * AutoAcquireRangeMultiplier)
        {
            if (unit.AttackTargetIsManual || unit.MoveMode == MoveCommandMode.Attack)
            {
                var approachDistance = MathF.Max(target.Spec.Collision.Radius + unit.Spec.Collision.Radius + 16, range * ManualAttackRangeMultiplier);
                unit.MoveTarget = target.Position - toTarget.Normalized() * approachDistance;
                unit.CommandVisualTarget = target.Position;
            }

            return;
        }

        unit.MoveTarget = null;
        unit.Velocity = unit.Velocity.MoveToward(Vector2.Zero, unit.Spec.Movement.Speed * DefaultAccelerationMultiplier * dt);
        if (unit.AttackCooldownRemaining > 0 || (!weapon.CanFireWhileMoving && unit.Velocity.LengthSquared() > 1))
        {
            return;
        }

        var primaryMount = unit.WeaponMounts.Count == 0 ? null : unit.WeaponMounts[0];
        if (primaryMount is not null && !WeaponCanFireAt(primaryMount.Facing, targetAngle, weapon))
        {
            return;
        }

        unit.AttackCooldownRemaining = weapon.Cooldown;
        WeaponFired?.Invoke(new WeaponFiredEvent(
            _inputCommandTick,
            unit.EntityId,
            primaryMount?.MountId ?? "main",
            weapon.Id,
            MuzzlePosition(unit, primaryMount),
            target.Position,
            weapon.LegacyKind));
        ApplyDamage(unit, target, weapon);
    }

    private static Vector2 MuzzlePosition(UnitInstance unit, WeaponMountRuntimeState? mount)
    {
        var mountId = mount?.MountId;
        if (!string.IsNullOrWhiteSpace(mountId))
        {
            foreach (var spec in unit.Spec.Weapons)
            {
                if (spec.MountId != mountId)
                {
                    continue;
                }

                return unit.Position
                    + spec.Anchor.Rotated(unit.Facing)
                    + spec.MuzzleOffset.Rotated(mount!.Facing);
            }
        }

        var facing = mount?.Facing ?? unit.Facing;
        return unit.Position + Vector2.FromAngle(facing) * (unit.Spec.Collision.Radius + 12);
    }

}
