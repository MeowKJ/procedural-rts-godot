using Godot;

namespace ProceduralRts.Core;

public static partial class SimInvariants
{
    private static void ValidateEntity(
        EntityWorld world,
        EntityInstance entity,
        List<SimInvariantViolation> violations,
        Dictionary<int, EntityId> dockReservations)
    {
        CheckFinite(entity, "Transform.Position", entity.Transform.Position, violations);
        CheckFinite(entity, "Transform.Facing", entity.Transform.Facing, violations);

        if (entity.Components.TryGet<HealthComponentState>(out var health))
        {
            if (!IsFinite(health.Hp) || !IsFinite(health.MaxHp))
            {
                Add(entity, "Health", "hp and max hp must be finite", violations);
            }

            if (health.MaxHp <= 0)
            {
                Add(entity, "Health", $"max hp must be positive, got {health.MaxHp}", violations);
            }

            if (health.Hp < 0 || health.Hp > health.MaxHp)
            {
                Add(entity, "Health", $"hp must stay within [0,max], got {health.Hp}/{health.MaxHp}", violations);
            }
        }

        if (entity.Components.TryGet<MovementComponentState>(out var movement))
        {
            CheckFinite(entity, "Movement.Velocity", movement.Velocity, violations);
            CheckFinite(entity, "Movement.MoveTarget", movement.MoveTarget, violations);
            CheckFinite(entity, "Movement.FormationSlot", movement.FormationSlot, violations);
            CheckFinite(entity, "Movement.FireAnchorRemaining", movement.FireAnchorRemaining, violations);
            if (movement.FireAnchorRemaining < 0)
            {
                Add(entity, "Movement", "fire anchor remaining time must be non-negative", violations);
            }
        }

        if (entity.Components.TryGet<PatrolOrderComponentState>(out var patrol))
        {
            ValidatePatrolOrder(entity, patrol, violations);
        }

        if (entity.Components.TryGet<GuardOrderComponentState>(out var guard))
        {
            ValidateGuardOrder(entity, guard, violations);
        }

        if (entity.Components.TryGet<PathfindingComponentState>(out var pathfinding))
        {
            ValidatePathfinding(entity, pathfinding, violations);
        }

        if (entity.Components.TryGet<MovementProfileComponentState>(out var profile))
        {
            CheckFinite(entity, "MovementProfile.MaxSpeed", profile.MaxSpeed, violations);
            CheckFinite(entity, "MovementProfile.ArriveRadius", profile.ArriveRadius, violations);
            CheckFinite(entity, "MovementProfile.TurnRate", profile.TurnRate, violations);
            if (profile.MaxSpeed < 0 || profile.ArriveRadius < 0 || profile.TurnRate < 0)
            {
                Add(entity, "MovementProfile", "speed, arrive radius, and turn rate must be non-negative", violations);
            }
        }

        if (entity.Components.TryGet<CollisionComponentState>(out var collision))
        {
            CheckFinite(entity, "Collision.Radius", collision.Radius, violations);
            CheckFinite(entity, "Collision.Mass", collision.Mass, violations);
            if (collision.Radius < 0 || collision.Mass < 0)
            {
                Add(entity, "Collision", "radius and mass must be non-negative", violations);
            }
        }

        if (entity.Components.TryGet<VisionComponentState>(out var vision))
        {
            CheckFinite(entity, "Vision.SightRange", vision.SightRange, violations);
            if (vision.SightRange < 0)
            {
                Add(entity, "Vision", "sight range must be non-negative", violations);
            }
        }

        if (entity.Components.TryGet<AutonomyComponentState>(out var autonomy))
        {
            CheckFinite(entity, "Autonomy.AcquireRange", autonomy.AcquireRange, violations);
            CheckFinite(entity, "Autonomy.LeashRange", autonomy.LeashRange, violations);
            CheckFinite(entity, "Autonomy.AnchorPosition", autonomy.AnchorPosition, violations);
            if (autonomy.AcquireRange < 0 || autonomy.LeashRange < 0)
            {
                Add(entity, "Autonomy", "acquire and leash ranges must be non-negative", violations);
            }
        }

        if (entity.Components.TryGet<RetaliationComponentState>(out var retaliation))
        {
            ValidateRetaliation(world, entity, retaliation, violations);
        }

        if (entity.Components.TryGet<WeaponUserComponentState>(out var weapon))
        {
            ValidateWeaponUser(world, entity, weapon, violations);
        }

        if (entity.Components.TryGet<ProjectileComponentState>(out var projectile))
        {
            ValidateProjectile(world, entity, projectile, violations);
        }

        if (entity.Components.TryGet<VeterancyComponentState>(out var veterancy))
        {
            ValidateVeterancy(entity, veterancy, violations);
        }

        if (entity.Components.TryGet<RegenerationComponentState>(out var regen))
        {
            ValidateRegeneration(entity, regen, violations);
        }

        if (entity.Components.TryGet<HarvesterComponentState>(out var harvester))
        {
            CheckFinite(entity, "Harvester.HarvestPulse", harvester.HarvestPulse, violations);
            CheckEntityReference(world, entity, "Harvester.FieldId", harvester.FieldId, violations);
            CheckEntityReference(world, entity, "Harvester.RefineryId", harvester.RefineryId, violations);
        }

        if (entity.Components.TryGet<ResourceCargoComponentState>(out var cargo))
        {
            if (cargo.Capacity < 0 || cargo.Cargo < 0 || cargo.Cargo > cargo.Capacity)
            {
                Add(entity, "ResourceCargo", $"cargo must stay within [0,capacity], got {cargo.Cargo}/{cargo.Capacity}", violations);
            }
        }

        if (entity.Components.TryGet<ResourceNodeComponentState>(out var resourceNode))
        {
            if (resourceNode.Amount < 0 || resourceNode.MaxAmount < 0 || resourceNode.Amount > resourceNode.MaxAmount)
            {
                Add(entity, "ResourceNode", $"amount must stay within [0,max], got {resourceNode.Amount}/{resourceNode.MaxAmount}", violations);
            }

            CheckFinite(entity, "ResourceNode.GatherRateModifier", resourceNode.GatherRateModifier, violations);
            CheckFinite(entity, "ResourceNode.RegenerationProgress", resourceNode.RegenerationProgress, violations);
            if (resourceNode.GatherRateModifier < 0)
            {
                Add(entity, "ResourceNode", $"gather rate modifier must be non-negative, got {resourceNode.GatherRateModifier}", violations);
            }

            if (resourceNode.RegenerationProgress < 0)
            {
                Add(entity, "ResourceNode", $"regeneration progress must be non-negative, got {resourceNode.RegenerationProgress}", violations);
            }
        }

        if (entity.Components.TryGet<ResourceRegenerationAuraComponentState>(out var regenerationAura))
        {
            CheckFinite(entity, "ResourceRegenerationAura.Radius", regenerationAura.Radius, violations);
            CheckFinite(entity, "ResourceRegenerationAura.Multiplier", regenerationAura.Multiplier, violations);
            if (regenerationAura.Radius < 0 || regenerationAura.Multiplier < 0)
            {
                Add(entity, "ResourceRegenerationAura", "radius and multiplier must be non-negative", violations);
            }
        }

        if (entity.Components.TryGet<SignalNetworkComponentState>(out var signal))
        {
            CheckFinite(entity, "SignalNetwork.DayControlRadius", signal.DayControlRadius, violations);
            CheckFinite(entity, "SignalNetwork.NightVisionRadius", signal.NightVisionRadius, violations);
            CheckFinite(entity, "SignalNetwork.SafetyAuraMultiplier", signal.SafetyAuraMultiplier, violations);
            if (signal.DayControlRadius < 0
                || signal.NightVisionRadius < 0
                || signal.SafetyAuraMultiplier < 0)
            {
                Add(entity, "SignalNetwork", "radii and safety aura multiplier must be non-negative", violations);
            }
        }

        if (entity.Components.TryGet<ProductionQueueComponentState>(out var production))
        {
            ValidateProductionQueue(entity, production, violations);
        }

        if (entity.Components.TryGet<AbilityRuntimeComponentState>(out var abilityRuntime))
        {
            ValidateAbilityRuntime(entity, abilityRuntime, violations);
        }

        if (entity.Components.TryGet<ShieldComponentState>(out var shield))
        {
            CheckFinite(entity, "Shield.AbsorbRemaining", shield.AbsorbRemaining, violations);
            CheckFinite(entity, "Shield.DurationRemaining", shield.DurationRemaining, violations);
            if (shield.AbsorbRemaining < 0 || shield.DurationRemaining < 0)
            {
                Add(entity, "Shield", "absorb and duration must be non-negative", violations);
            }
        }

        if (entity.Components.TryGet<ScanRevealComponentState>(out var scanReveal))
        {
            CheckFinite(entity, "ScanReveal.Radius", scanReveal.Radius, violations);
            CheckFinite(entity, "ScanReveal.DurationRemaining", scanReveal.DurationRemaining, violations);
            if (scanReveal.Radius < 0 || scanReveal.DurationRemaining < 0)
            {
                Add(entity, "ScanReveal", "radius and duration must be non-negative", violations);
            }
        }

        if (entity.Components.TryGet<DeployComponentState>(out var deploy))
        {
            CheckFinite(entity, "Deploy.SetupRemaining", deploy.SetupRemaining, violations);
            CheckFinite(entity, "Deploy.RangeMultiplier", deploy.RangeMultiplier, violations);
            if (deploy.SetupRemaining < 0 || deploy.RangeMultiplier < 0)
            {
                Add(entity, "Deploy", "setup remaining and range multiplier must be non-negative", violations);
            }
        }

        if (entity.Components.TryGet<RepairOrderComponentState>(out var repairOrder))
        {
            ValidateRepairOrder(world, entity, repairOrder, violations);
        }

        if (entity.Components.TryGet<CommandQueueComponentState>(out var commandQueue))
        {
            ValidateCommandQueue(entity, commandQueue, violations);
        }

        if (entity.Components.TryGet<FootprintComponentState>(out var footprint))
        {
            CheckFinite(entity, "Footprint.Size", footprint.Size, violations);
            if (footprint.Size.X < 0 || footprint.Size.Y < 0)
            {
                Add(entity, "Footprint", "size must be non-negative", violations);
            }
        }

        if (entity.Components.TryGet<ConstructionComponentState>(out var construction))
        {
            ValidateConstruction(entity, construction, violations);
        }

        if (entity.Components.TryGet<PowerComponentState>(out var power))
        {
            if (power.Provided < 0 || power.Used < 0)
            {
                Add(entity, "Power", "provided and used power must be non-negative", violations);
            }
        }

        if (entity.Components.TryGet<RallyPointComponentState>(out var rally))
        {
            CheckFinite(entity, "RallyPoint.Target", rally.Target, violations);
            CheckEntityReference(world, entity, "RallyPoint.TargetEntityId", rally.TargetEntityId, violations);
        }

        if (entity.Components.TryGet<DockComponentState>(out var dock))
        {
            ValidateDock(world, entity, dock, violations, dockReservations);
        }

        if (entity.Components.TryGet<BuildRadiusComponentState>(out var buildRadius))
        {
            CheckFinite(entity, "BuildRadius.Radius", buildRadius.Radius, violations);
            if (buildRadius.Radius < 0)
            {
                Add(entity, "BuildRadius", "radius must be non-negative", violations);
            }
        }

        if (entity.Components.TryGet<PresentationPulseComponentState>(out var pulse))
        {
            CheckFinite(entity, "PresentationPulse.CommandPulse", pulse.CommandPulse, violations);
            CheckFinite(entity, "PresentationPulse.AlertPulse", pulse.AlertPulse, violations);
            CheckFinite(entity, "PresentationPulse.HitPulse", pulse.HitPulse, violations);
        }
    }
}
