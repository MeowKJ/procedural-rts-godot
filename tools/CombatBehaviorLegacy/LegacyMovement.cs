static partial class Program
{
    private static void AssertLegacyMovementAndAttackTracking()
    {
        var moveModeState = EmptyState();
        var attackMover = Unit(1, UnitDesignIds.GenericLightTank, Owner.Player, new Vector2(500, 500), UnitStance.Hold);
        var directMover = Unit(2, UnitDesignIds.GenericLightTank, Owner.Player, new Vector2(500, 620), UnitStance.Hold);
        var ignoreMover = Unit(3, UnitDesignIds.GenericLightTank, Owner.Player, new Vector2(500, 740), UnitStance.Hold);
        var nearbyEnemy = Unit(4, UnitDesignIds.GenericLightTank, Owner.Enemy, new Vector2(710, 560), UnitStance.Hold);
        moveModeState.Units.AddRange([attackMover, directMover, ignoreMover, nearbyEnemy]);
        moveModeState.SelectUnitsByIds([attackMover.Id]);
        moveModeState.CommandMoveSelected(new Vector2(1160, 500), MoveCommandMode.Attack);
        moveModeState.SelectUnitsByIds([directMover.Id]);
        moveModeState.CommandMoveSelected(new Vector2(1160, 620), MoveCommandMode.Direct);
        moveModeState.SelectUnitsByIds([ignoreMover.Id]);
        moveModeState.CommandMoveSelected(new Vector2(1160, 740), MoveCommandMode.Ignore);

        Advance(moveModeState, 0.1f);

        if (attackMover.AttackTargetId != nearbyEnemy.Id)
        {
            throw new InvalidOperationException("attack advance should acquire enemies while moving");
        }

        if (directMover.AttackTargetId is not null)
        {
            throw new InvalidOperationException("direct advance should keep moving instead of acquiring roadside enemies");
        }

        if (ignoreMover.Stance != UnitStance.Ignore || ignoreMover.AttackTargetId is not null)
        {
            throw new InvalidOperationException("ignore advance should set ignore stance and avoid auto-targeting");
        }

        var formationSlotState = EmptyState();
        var slotUnits = new[]
        {
            Unit(1, UnitDesignIds.GenericLightTank, Owner.Player, new Vector2(460, 500), UnitStance.Hold),
            Unit(2, UnitDesignIds.GenericLightTank, Owner.Player, new Vector2(518, 500), UnitStance.Hold),
            Unit(3, UnitDesignIds.GenericInfantry, Owner.Player, new Vector2(488, 548), UnitStance.Hold),
            Unit(4, UnitDesignIds.GenericInfantry, Owner.Player, new Vector2(546, 548), UnitStance.Hold),
            Unit(5, UnitDesignIds.GenericHarvester, Owner.Player, new Vector2(504, 604), UnitStance.Hold),
        };
        var distantIdle = Unit(6, UnitDesignIds.GenericLightTank, Owner.Player, new Vector2(1800, 1800), UnitStance.Hold);
        formationSlotState.Units.AddRange(slotUnits);
        formationSlotState.Units.Add(distantIdle);
        formationSlotState.SelectUnitsByIds(slotUnits.Select(unit => unit.Id));
        formationSlotState.CommandMoveSelected(new Vector2(980, 760), MoveCommandMode.Direct);

        if (slotUnits.Any(unit => unit.FormationSlot is null || unit.MovementState != UnitMovementState.MovingToSlot))
        {
            throw new InvalidOperationException("formation move should assign each unit a slot and moving-to-slot state");
        }

        var assignedSlots = slotUnits.Select(unit => unit.FormationSlot!.Value).ToList();
        if (assignedSlots.Select(slot => $"{slot.X:0.0},{slot.Y:0.0}").Distinct().Count() != slotUnits.Length)
        {
            throw new InvalidOperationException("formation slots should be distinct per selected unit");
        }

        if (slotUnits.Any(unit => unit.CommandVisualTarget is null || unit.CommandVisualTarget.Value.DistanceTo(new Vector2(980, 760)) > 0.01f))
        {
            throw new InvalidOperationException("formation command visualization should preserve the player's clicked target instead of per-unit slots");
        }

        if (slotUnits.Any(unit => unit.PlayerIntentTarget is null || unit.PlayerIntentTarget.Value.DistanceTo(new Vector2(980, 760)) > 0.01f))
        {
            throw new InvalidOperationException("formation move should keep the player's intent target separate from assigned unit slots");
        }

        if (slotUnits.All(unit => unit.FormationSlot!.Value.DistanceTo(unit.PlayerIntentTarget!.Value) <= 0.01f))
        {
            throw new InvalidOperationException("formation move should assign slots around the player intent target instead of overwriting intent with slots");
        }

        if (slotUnits.Any(unit =>
            unit.GlobalCorridor.Count == 0
            || unit.MoveTarget is null
            || unit.GlobalCorridor[0].DistanceTo(unit.MoveTarget.Value) > 0.01f
            || unit.GlobalCorridor[^1].DistanceTo(unit.FormationSlot!.Value) > 0.01f))
        {
            throw new InvalidOperationException("formation move should expose a global corridor from current waypoint to assigned slot");
        }

        Advance(formationSlotState, 9.0f);

        if (slotUnits.Any(unit => unit.MovementState != UnitMovementState.HoldingSlot || unit.MoveTarget is not null || unit.Position.DistanceTo(unit.FormationSlot!.Value) > 8.1f))
        {
            throw new InvalidOperationException("formation units should settle into holding state near their assigned slots without requiring a visible snap");
        }

        var heldPositions = slotUnits.ToDictionary(unit => unit.Id, unit => unit.Position);
        var distantBefore = distantIdle.Position;
        Advance(formationSlotState, 2.0f);

        if (slotUnits.Any(unit => unit.Position.DistanceTo(heldPositions[unit.Id]) > 0.01f))
        {
            throw new InvalidOperationException("holding formation units should not jitter after reaching their slots");
        }

        if (distantIdle.Position.DistanceTo(distantBefore) > 0.01f)
        {
            throw new InvalidOperationException("local avoidance should not globally push distant idle units");
        }

        var softSettleState = EmptyState();
        var softSettleUnit = Unit(1, UnitDesignIds.GenericLightTank, Owner.Player, new Vector2(642, 500), UnitStance.Hold);
        softSettleState.Units.Add(softSettleUnit);
        softSettleState.SelectUnitsByIds([softSettleUnit.Id]);
        softSettleState.CommandMoveSelected(new Vector2(648, 500), MoveCommandMode.Direct);
        var softSettleSlot = softSettleUnit.FormationSlot ?? throw new InvalidOperationException("soft settle move should assign a final slot");
        Advance(softSettleState, 0.05f);
        if (softSettleUnit.MovementState != UnitMovementState.HoldingSlot
            || softSettleUnit.Position.DistanceTo(softSettleSlot) <= 0.01f
            || softSettleUnit.Position.DistanceTo(softSettleSlot) > 8.1f)
        {
            throw new InvalidOperationException("slot arrival should stop softly near the slot instead of visibly snapping onto it");
        }

        var slotPriorityState = EmptyState();
        var slotPriorityMover = Unit(1, UnitDesignIds.GenericLightTank, Owner.Player, new Vector2(600, 500), UnitStance.Hold);
        var nearbyInterference = Unit(2, UnitDesignIds.GenericLightTank, Owner.Player, new Vector2(610, 540), UnitStance.Hold);
        slotPriorityState.Units.AddRange([slotPriorityMover, nearbyInterference]);
        slotPriorityState.SelectUnitsByIds([slotPriorityMover.Id]);
        slotPriorityState.CommandMoveSelected(new Vector2(648, 500), MoveCommandMode.Direct);
        var slotPriorityDestination = slotPriorityMover.FormationSlot ?? throw new InvalidOperationException("slot priority move should assign a final slot");
        var distanceBeforeSlotPriorityStep = slotPriorityMover.Position.DistanceTo(slotPriorityDestination);
        Advance(slotPriorityState, 0.05f);
        if (slotPriorityMover.Position.DistanceTo(slotPriorityDestination) >= distanceBeforeSlotPriorityStep)
        {
            throw new InvalidOperationException("slot priority steering should keep progress toward the assigned slot despite nearby avoidance");
        }

        if (slotPriorityMover.DebugSteeringVector.LengthSquared() <= 0.001f)
        {
            throw new InvalidOperationException("path debug overlay should expose the active steering vector while units move");
        }

        Advance(slotPriorityState, 2.0f);
        if (slotPriorityMover.MovementState != UnitMovementState.HoldingSlot
            || slotPriorityMover.MoveTarget is not null
            || slotPriorityMover.Position.DistanceTo(slotPriorityDestination) > 8.1f)
        {
            throw new InvalidOperationException("slot priority steering should converge into holding state near local interference");
        }

        if (slotPriorityMover.DebugRawPathCells.Count != 0
            || slotPriorityMover.DebugLocalAvoidanceVector.LengthSquared() > 0.001f
            || slotPriorityMover.DebugSteeringVector.LengthSquared() > 0.001f)
        {
            throw new InvalidOperationException("path debug vectors and raw cells should clear after a unit reaches holding state");
        }

        var anchorObstacleState = EmptyState();
        var pathAnchor = Unit(1, UnitDesignIds.GenericLightTank, Owner.Player, new Vector2(600, 500), UnitStance.Hold);
        pathAnchor.MovementState = UnitMovementState.CombatAnchor;
        pathAnchor.FormationSlot = pathAnchor.Position;
        pathAnchor.AnchorPosition = pathAnchor.Position;
        var anchorAvoider = Unit(2, UnitDesignIds.GenericLightTank, Owner.Player, new Vector2(420, 500), UnitStance.Hold);
        anchorObstacleState.Units.AddRange([pathAnchor, anchorAvoider]);
        anchorObstacleState.SelectUnitsByIds([anchorAvoider.Id]);
        anchorObstacleState.CommandMoveSelected(new Vector2(780, 500), MoveCommandMode.Direct);
        if (anchorAvoider.GlobalCorridor.Count <= 1)
        {
            throw new InvalidOperationException("global pathing should treat combat anchors as temporary blockers instead of assigning a direct path through them");
        }

        if (anchorAvoider.DebugRawPathCells.Count <= anchorAvoider.GlobalCorridor.Count)
        {
            throw new InvalidOperationException("path debug overlay should preserve raw A* cells separately from the smoothed global corridor");
        }

        if (anchorAvoider.GlobalCorridor.Any(point => point.DistanceTo(pathAnchor.Position) < pathAnchor.RuntimeDescriptor.Radius + anchorAvoider.RuntimeDescriptor.Radius + 10))
        {
            throw new InvalidOperationException("global pathing corridor should keep moving units off combat anchor space");
        }

        var denseBlobState = EmptyState();
        var denseAvoider = Unit(1, UnitDesignIds.GenericLightTank, Owner.Player, new Vector2(420, 500), UnitStance.Hold);
        var blobUnits = new[]
        {
            Unit(2, UnitDesignIds.GenericLightTank, Owner.Player, new Vector2(590, 482), UnitStance.Hold),
            Unit(3, UnitDesignIds.GenericInfantry, Owner.Player, new Vector2(615, 508), UnitStance.Hold),
            Unit(4, UnitDesignIds.GenericInfantry, Owner.Player, new Vector2(585, 502), UnitStance.Hold),
        };
        denseBlobState.Units.Add(denseAvoider);
        denseBlobState.Units.AddRange(blobUnits);
        denseBlobState.SelectUnitsByIds([denseAvoider.Id]);
        denseBlobState.CommandMoveSelected(new Vector2(780, 500), MoveCommandMode.Direct);
        if (denseAvoider.GlobalCorridor.Count <= 1)
        {
            throw new InvalidOperationException("global pathing should route around dense idle unit blobs instead of planning through them");
        }

        var throttledRepathState = EmptyState();
        var throttledMover = Unit(1, UnitDesignIds.GenericLightTank, Owner.Player, new Vector2(420, 500), UnitStance.Hold);
        throttledRepathState.Units.Add(throttledMover);
        throttledRepathState.SelectUnitsByIds([throttledMover.Id]);
        throttledRepathState.CommandMoveSelected(new Vector2(780, 500), MoveCommandMode.Direct);
        if (throttledMover.GlobalCorridor.Count != 1)
        {
            throw new InvalidOperationException("clear initial move should start with a direct corridor before dynamic blockers appear");
        }

        throttledRepathState.Units.AddRange([
            Unit(2, UnitDesignIds.GenericLightTank, Owner.Player, new Vector2(590, 482), UnitStance.Hold),
            Unit(3, UnitDesignIds.GenericInfantry, Owner.Player, new Vector2(615, 508), UnitStance.Hold),
            Unit(4, UnitDesignIds.GenericInfantry, Owner.Player, new Vector2(585, 502), UnitStance.Hold),
        ]);
        throttledMover.PathStallSeconds = 0.69f;
        throttledMover.LastMoveTargetDistance = throttledMover.Position.DistanceTo(throttledMover.MoveTarget!.Value);
        Advance(throttledRepathState, 0.05f);
        if (throttledMover.GlobalCorridor.Count <= 1 || throttledMover.RepathCooldownRemaining <= 0)
        {
            throw new InvalidOperationException("stalled movers should repath around newly dense blobs and start a repath cooldown");
        }

        var cooldownMover = Unit(10, UnitDesignIds.GenericLightTank, Owner.Player, new Vector2(420, 700), UnitStance.Hold);
        var cooldownState = EmptyState();
        cooldownState.Units.Add(cooldownMover);
        cooldownState.SelectUnitsByIds([cooldownMover.Id]);
        cooldownState.CommandMoveSelected(new Vector2(780, 700), MoveCommandMode.Direct);
        cooldownState.Units.AddRange([
            Unit(11, UnitDesignIds.GenericLightTank, Owner.Player, new Vector2(590, 682), UnitStance.Hold),
            Unit(12, UnitDesignIds.GenericInfantry, Owner.Player, new Vector2(615, 708), UnitStance.Hold),
            Unit(13, UnitDesignIds.GenericInfantry, Owner.Player, new Vector2(585, 702), UnitStance.Hold),
        ]);
        cooldownMover.PathStallSeconds = 1.0f;
        cooldownMover.RepathCooldownRemaining = 1.0f;
        cooldownMover.LastMoveTargetDistance = cooldownMover.Position.DistanceTo(cooldownMover.MoveTarget!.Value);
        Advance(cooldownState, 0.05f);
        if (cooldownMover.GlobalCorridor.Count != 1)
        {
            throw new InvalidOperationException("repath cooldown should prevent dense-blob repath from running every frame");
        }

        var combatAnchorState = EmptyState();
        var attackTarget = combatAnchorState.PlaceBuilding(BuildingDesignIds.Headquarters, Owner.Enemy, new Vector2(920, 500))
            ?? throw new InvalidOperationException("combat anchor test should place an enemy target building");
        var firingFront = Unit(1, UnitDesignIds.GenericLightTank, Owner.Player, new Vector2(660, 500), UnitStance.Hold);
        var rearAttackers = new[]
        {
            Unit(2, UnitDesignIds.GenericLightTank, Owner.Player, new Vector2(430, 460), UnitStance.Hold),
            Unit(3, UnitDesignIds.GenericLightTank, Owner.Player, new Vector2(430, 540), UnitStance.Hold),
            Unit(4, UnitDesignIds.GenericLightTank, Owner.Player, new Vector2(380, 500), UnitStance.Hold),
            Unit(5, UnitDesignIds.GenericInfantry, Owner.Player, new Vector2(350, 455), UnitStance.Hold),
            Unit(6, UnitDesignIds.GenericInfantry, Owner.Player, new Vector2(350, 545), UnitStance.Hold),
        };
        combatAnchorState.Units.Add(firingFront);
        combatAnchorState.Units.AddRange(rearAttackers);
        combatAnchorState.SelectUnitsByIds(combatAnchorState.Units.Select(unit => unit.Id));
        combatAnchorState.CommandAttackSelected(attackTarget);

        if (firingFront.MovementState != UnitMovementState.CombatAnchor || firingFront.MoveTarget is not null)
        {
            throw new InvalidOperationException("in-range front attackers should immediately become combat anchors");
        }

        if (rearAttackers.Any(unit => unit.FormationSlot is null || unit.MoveTarget is null))
        {
            throw new InvalidOperationException("rear attackers should receive attack formation slots instead of moving to the target center");
        }

        var firingFrontPosition = firingFront.Position;
        Advance(combatAnchorState, 4.0f);
        if (firingFront.Position.DistanceTo(firingFrontPosition) > 0.01f)
        {
            throw new InvalidOperationException("combat anchors should not be displaced by rear moving attackers");
        }

        if (rearAttackers.Any(unit => unit.Position.DistanceTo(firingFront.Position) < unit.RuntimeDescriptor.Radius + firingFront.RuntimeDescriptor.Radius - 2))
        {
            throw new InvalidOperationException("rear attackers should route around combat anchors instead of overlapping them");
        }

        var anchoredAttackers = combatAnchorState.Units
            .Where(unit => unit.Owner == Owner.Player && unit.MovementState == UnitMovementState.CombatAnchor)
            .ToDictionary(unit => unit.Id, unit => unit.Position);
        if (anchoredAttackers.Count < 2)
        {
            throw new InvalidOperationException("group attack should let later attackers stop and anchor once they can fire");
        }

        Advance(combatAnchorState, 1.0f);
        if (combatAnchorState.Units
            .Where(unit => anchoredAttackers.ContainsKey(unit.Id))
            .Any(unit => unit.Position.DistanceTo(anchoredAttackers[unit.Id]) > 0.01f))
        {
            throw new InvalidOperationException("combat anchors should remain stable after reaching firing positions");
        }

        var movingTargetState = EmptyState();
        var trackingAttacker = Unit(1, UnitDesignIds.GenericLightTank, Owner.Player, new Vector2(500, 500), UnitStance.Hold);
        var trackingSpotter = Unit(2, UnitDesignIds.GenericInfantry, Owner.Player, new Vector2(900, 500), UnitStance.Hold);
        var movingTarget = Unit(3, UnitDesignIds.GenericLightTank, Owner.Enemy, new Vector2(850, 500), UnitStance.Hold);
        movingTargetState.Units.AddRange([trackingAttacker, trackingSpotter, movingTarget]);
        movingTargetState.SelectUnitsByIds([trackingAttacker.Id]);
        movingTargetState.CommandAttackSelected(movingTarget);
        var initialAttackSlot = trackingAttacker.FormationSlot ?? throw new InvalidOperationException("manual unit attack should assign an initial attack slot");
        Advance(movingTargetState, 0.2f);
        movingTarget.Position = new Vector2(1060, 500);
        Advance(movingTargetState, 0.2f);
        if (trackingAttacker.AttackTargetLastKnownPosition is null
            || trackingAttacker.AttackTargetLastKnownPosition.Value.DistanceTo(movingTarget.Position) > 0.01f)
        {
            throw new InvalidOperationException("unit attack tracking should update the target's last known position while visible");
        }

        if (trackingAttacker.CommandVisualTarget is null || trackingAttacker.CommandVisualTarget.Value.DistanceTo(movingTarget.Position) > 0.01f)
        {
            throw new InvalidOperationException("unit attack visualization should follow the tracked unit target, not the assigned attack slot");
        }

        if (trackingAttacker.FormationSlot is null || trackingAttacker.FormationSlot.Value.DistanceTo(initialAttackSlot) <= 46)
        {
            throw new InvalidOperationException("unit attack tracking should repath attack slots when the target unit moves");
        }

        var lostTargetState = EmptyState();
        var lostTracker = Unit(1, UnitDesignIds.GenericLightTank, Owner.Player, new Vector2(500, 900), UnitStance.Hold);
        var lostTarget = Unit(2, UnitDesignIds.GenericLightTank, Owner.Enemy, new Vector2(850, 900), UnitStance.Hold);
        lostTargetState.Units.AddRange([lostTracker, lostTarget]);
        lostTargetState.SelectUnitsByIds([lostTracker.Id]);
        lostTargetState.CommandAttackSelected(lostTarget);
        lostTarget.Position = new Vector2(1700, 900);
        lostTargetState.FogOfWar.ClearMemory();
        Advance(lostTargetState, 3.0f);
        if (lostTracker.AttackTargetId is not null)
        {
            throw new InvalidOperationException("unit attack tracking should stop after briefly pursuing a lost target's last known direction");
        }
    }
}
