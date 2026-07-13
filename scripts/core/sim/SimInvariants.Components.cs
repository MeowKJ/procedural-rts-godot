using Godot;

namespace ProceduralRts.Core;

public static partial class SimInvariants
{
    private static void ValidateRetaliation(
        EntityWorld world,
        EntityInstance entity,
        RetaliationComponentState retaliation,
        List<SimInvariantViolation> violations)
    {
        CheckEntityReference(world, entity, "Retaliation.Target", retaliation.Target.Value, violations);
        if (retaliation.LastThreatTick < 0)
        {
            Add(entity, "Retaliation", "last threat tick must be non-negative", violations);
        }
    }

    private static void ValidateAttackGroundOrder(
        EntityInstance entity,
        AttackGroundOrderComponentState attackGround,
        List<SimInvariantViolation> violations)
    {
        CheckFinite(entity, "AttackGround.Target", attackGround.Target, violations);
    }

    private static void ValidateWeaponUser(
        EntityWorld world,
        EntityInstance entity,
        WeaponUserComponentState weapon,
        List<SimInvariantViolation> violations)
    {
        if (weapon.Mounts is null)
        {
            Add(entity, "WeaponUser", "mount list must not be null", violations);
            return;
        }

        foreach (var mount in weapon.Mounts)
        {
            if (string.IsNullOrWhiteSpace(mount.MountId))
            {
                Add(entity, "WeaponUser", "mount id must not be empty", violations);
            }

            CheckFinite(entity, $"WeaponUser.{mount.MountId}.Facing", mount.Facing, violations);
            CheckFinite(entity, $"WeaponUser.{mount.MountId}.Cooldown", mount.CooldownRemaining, violations);
            if (mount.CooldownRemaining < 0)
            {
                Add(entity, "WeaponUser", $"mount '{mount.MountId}' cooldown must be non-negative", violations);
            }
        }

        CheckFinite(entity, "WeaponUser.AutoReacquireCooldown", weapon.AutoReacquireCooldownRemaining, violations);
        if (weapon.AutoReacquireCooldownRemaining < 0)
        {
            Add(entity, "WeaponUser", "auto re-acquire cooldown must be non-negative", violations);
        }

        CheckFinite(entity, "WeaponUser.LastKnownTargetPosition", weapon.LastKnownTargetPosition, violations);
        CheckFinite(entity, "WeaponUser.LastKnownTargetRemaining", weapon.LastKnownTargetRemaining, violations);
        if (weapon.LastKnownTargetRemaining < 0)
        {
            Add(entity, "WeaponUser", "last-known target memory must be non-negative", violations);
        }

        if (weapon.LastKnownTargetRemaining > 0 && weapon.LastKnownTargetPosition is null)
        {
            Add(entity, "WeaponUser", "active last-known target memory must include a position", violations);
        }

        if (weapon.LastKnownTargetRemaining <= 0 && weapon.LastKnownTargetPosition is not null)
        {
            Add(entity, "WeaponUser", "expired last-known target memory must clear its position", violations);
        }

        if (!weapon.AttackTarget.IsValid)
        {
            return;
        }

        if (!world.TryGet(weapon.AttackTarget, out var target))
        {
            Add(entity, "WeaponUser", $"attack target {weapon.AttackTarget.Value} does not exist", violations);
            return;
        }

        if (target.Components.TryGet<HealthComponentState>(out var targetHealth) && targetHealth.Hp <= 0)
        {
            Add(entity, "WeaponUser", $"attack target {weapon.AttackTarget.Value} is dead", violations);
        }
    }

    private static void ValidateProjectile(
        EntityWorld world,
        EntityInstance entity,
        ProjectileComponentState projectile,
        List<SimInvariantViolation> violations)
    {
        if (!projectile.Source.IsValid)
        {
            Add(entity, "Projectile.Source", "source id must identify the original shooter", violations);
        }

        if (!world.TryGetWeaponDefinition(projectile.WeaponId, out _))
        {
            Add(entity, "Projectile.WeaponId", $"weapon definition '{projectile.WeaponId}' must exist", violations);
        }

        if (!world.TryGetAmmoDefinition(projectile.AmmoId, out var ammo))
        {
            Add(entity, "Projectile.AmmoId", $"ammo definition '{projectile.AmmoId}' must exist", violations);
        }
        else if (projectile.Behavior != ammo.Behavior || projectile.HitRule != ammo.HitRule)
        {
            Add(entity, "Projectile", $"state behavior/hit rule {projectile.Behavior}/{projectile.HitRule} must match ammo {ammo.Behavior}/{ammo.HitRule}", violations);
        }

        if (!Enum.IsDefined(projectile.Behavior) || projectile.Behavior == ProjectileBehavior.Beam)
        {
            Add(entity, "Projectile.Behavior", $"projectile behavior must be a non-beam value, got {projectile.Behavior}", violations);
        }

        if (!Enum.IsDefined(projectile.HitRule))
        {
            Add(entity, "Projectile.HitRule", $"hit rule must be valid, got {projectile.HitRule}", violations);
        }

        CheckFinite(entity, "Projectile.Origin", projectile.Origin, violations);
        CheckFinite(entity, "Projectile.AimPoint", projectile.AimPoint, violations);
        CheckFinite(entity, "Projectile.Damage", projectile.Damage, violations);
        CheckFinite(entity, "Projectile.Velocity", projectile.Velocity, violations);
        CheckFinite(entity, "Projectile.Speed", projectile.Speed, violations);
        CheckFinite(entity, "Projectile.TrackingStrength", projectile.TrackingStrength, violations);
        CheckFinite(entity, "Projectile.HitRadius", projectile.HitRadius, violations);
        CheckFinite(entity, "Projectile.Age", projectile.Age, violations);
        CheckFinite(entity, "Projectile.FlightDuration", projectile.FlightDuration, violations);
        CheckFinite(entity, "Projectile.LifetimeRemaining", projectile.LifetimeRemaining, violations);
        if (projectile.Damage < 0
            || projectile.Speed <= 0
            || projectile.TrackingStrength < 0
            || projectile.HitRadius < 0
            || projectile.Age < 0
            || projectile.FlightDuration <= 0
            || projectile.LifetimeRemaining < 0)
        {
            Add(entity, "Projectile", "damage, speed, hit radius, age, flight duration, and lifetime must stay valid", violations);
        }
    }

    private static void ValidateVeterancy(
        EntityInstance entity,
        VeterancyComponentState veterancy,
        List<SimInvariantViolation> violations)
    {
        CheckFinite(entity, "Veterancy.Experience", veterancy.Experience, violations);
        if (veterancy.Kills < 0 || veterancy.Experience < 0 || veterancy.Rank < 0 || veterancy.Rank > VeterancyRules.MaxRank)
        {
            Add(entity, "Veterancy", "kills, experience, and rank must stay within valid bounds", violations);
        }
    }

    private static void ValidateRegeneration(
        EntityInstance entity,
        RegenerationComponentState regen,
        List<SimInvariantViolation> violations)
    {
        CheckFinite(entity, "Regeneration.HpPerSecond", regen.HpPerSecond, violations);
        CheckFinite(entity, "Regeneration.Progress", regen.Progress, violations);
        if (regen.HpPerSecond < 0 || regen.Progress < 0)
        {
            Add(entity, "Regeneration", "rate and progress must be non-negative", violations);
        }
    }

    private static void ValidateProductionQueue(
        EntityInstance entity,
        ProductionQueueComponentState production,
        List<SimInvariantViolation> violations)
    {
        if (production.Items is null)
        {
            Add(entity, "ProductionQueue", "items must not be null", violations);
            return;
        }

        if (production.Items.Count > MaxProductionQueueItems)
        {
            Add(entity, "ProductionQueue", $"queue length {production.Items.Count} exceeds {MaxProductionQueueItems}", violations);
        }

        if (production.RepeatOutputSpecId is not null && string.IsNullOrWhiteSpace(production.RepeatOutputSpecId))
        {
            Add(entity, "ProductionQueue", "repeat output spec id must be null or non-empty", violations);
        }

        foreach (var item in production.Items)
        {
            if (item.Id <= 0)
            {
                Add(entity, "ProductionQueue", "item id must be positive", violations);
            }

            if (string.IsNullOrWhiteSpace(item.DesignId))
            {
                Add(entity, "ProductionQueue", "item design id must not be empty", violations);
            }

            CheckFinite(entity, "ProductionQueue.Progress", item.Progress, violations);
            if (item.Progress < 0)
            {
                Add(entity, "ProductionQueue", "item progress must be non-negative", violations);
            }
        }
    }

    private static void ValidateAbilityRuntime(
        EntityInstance entity,
        AbilityRuntimeComponentState abilityRuntime,
        List<SimInvariantViolation> violations)
    {
        if (abilityRuntime.Cooldowns is null)
        {
            Add(entity, "AbilityRuntime", "cooldowns must not be null", violations);
            return;
        }

        var seen = new HashSet<AbilityKind>();
        foreach (var cooldown in abilityRuntime.Cooldowns)
        {
            if (!seen.Add(cooldown.Kind))
            {
                Add(entity, "AbilityRuntime", $"duplicate cooldown for {cooldown.Kind}", violations);
            }

            CheckFinite(entity, $"AbilityRuntime.{cooldown.Kind}.Cooldown", cooldown.CooldownRemaining, violations);
            if (cooldown.CooldownRemaining < 0)
            {
                Add(entity, "AbilityRuntime", $"cooldown for {cooldown.Kind} must be non-negative", violations);
            }
        }
    }


    private static void ValidatePathfinding(
        EntityInstance entity,
        PathfindingComponentState pathfinding,
        List<SimInvariantViolation> violations)
    {
        CheckFinite(entity, "Pathfinding.Goal.X", pathfinding.Goal.X, violations);
        CheckFinite(entity, "Pathfinding.Goal.Y", pathfinding.Goal.Y, violations);
        if (pathfinding.Waypoints is null)
        {
            Add(entity, "Pathfinding", "waypoints must not be null", violations);
            return;
        }

        if (pathfinding.NextWaypointIndex < 0 || pathfinding.NextWaypointIndex > pathfinding.Waypoints.Count)
        {
            Add(entity, "Pathfinding", $"next waypoint index {pathfinding.NextWaypointIndex} must stay within [0,{pathfinding.Waypoints.Count}]", violations);
        }

        for (var index = 0; index < pathfinding.Waypoints.Count; index++)
        {
            var waypoint = pathfinding.Waypoints[index];
            CheckFinite(entity, $"Pathfinding.Waypoints[{index}].X", waypoint.X, violations);
            CheckFinite(entity, $"Pathfinding.Waypoints[{index}].Y", waypoint.Y, violations);
        }
    }

    private static void ValidateConstruction(
        EntityInstance entity,
        ConstructionComponentState construction,
        List<SimInvariantViolation> violations)
    {
        CheckFinite(entity, "Construction.Progress", construction.Progress, violations);
        CheckFinite(entity, "Construction.BuildTime", construction.BuildTime, violations);
        CheckFinite(entity, "Construction.RefundRatio", construction.RefundRatio, violations);

        if (construction.Progress < 0 || construction.Progress > 1)
        {
            Add(entity, "Construction", $"progress must stay within [0,1], got {construction.Progress}", violations);
        }

        if (construction.BuildTime < 0 || construction.Cost < 0)
        {
            Add(entity, "Construction", "build time and cost must be non-negative", violations);
        }

        if (construction.RefundRatio < 0 || construction.RefundRatio > 1)
        {
            Add(entity, "Construction", $"refund ratio must stay within [0,1], got {construction.RefundRatio}", violations);
        }

        if (!Enum.IsDefined(construction.PauseReason))
        {
            Add(entity, "Construction", $"pause reason must be valid, got {construction.PauseReason}", violations);
        }

        if (!Enum.IsDefined(construction.Phase))
        {
            Add(entity, "Construction", $"phase must be valid, got {construction.Phase}", violations);
        }

        if (construction.ReadyToPlace && construction.Progress < 1)
        {
            Add(entity, "Construction", "ready-to-place construction must have completed queue progress", violations);
        }

        if (construction.Progress >= 1 && construction.Paused)
        {
            Add(entity, "Construction", $"completed construction must not remain paused, got {construction.PauseReason}", violations);
        }
    }
}
