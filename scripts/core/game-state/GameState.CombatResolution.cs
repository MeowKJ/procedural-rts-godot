using Godot;

namespace ProceduralRts.Core;

public sealed partial class GameState
{
    private void UpdateCombat(UnitModel unit)
    {
        if (unit.AttackTargetId is null)
        {
            unit.TurretState = unit.AttackCooldownRemaining > 0 ? TurretState.Reloading : TurretState.Idle;
            return;
        }

        var targetPosition = CombatTargetPosition(unit.AttackTargetKind, unit.AttackTargetId.Value);
        if (targetPosition is null
            || !IsCombatTargetHostile(unit.Owner, unit.AttackTargetKind, unit.AttackTargetId.Value)
            || !CanUnitTarget(unit, unit.AttackTargetKind, unit.AttackTargetId.Value))
        {
            ClearAttackTarget(unit);
            unit.TurretState = TurretState.Idle;
            return;
        }

        if (!CanTrackActualTarget(unit, targetPosition.Value))
        {
            unit.TurretState = unit.AttackCooldownRemaining > 0 ? TurretState.Reloading : TurretState.Tracking;
            return;
        }

        var descriptor = unit.RuntimeDescriptor;
        var weapon = Weapon(unit);
        var toTarget = targetPosition.Value - unit.Position;
        var targetAngle = toTarget.Angle();
        AimUnitWeapon(unit, weapon, targetAngle, descriptor.TurnRate * 0.04f);
        unit.TurretState = unit.AttackCooldownRemaining > 0 ? TurretState.Reloading : TurretState.Tracking;

        if (toTarget.Length() > FireAuthorizationRange(weapon, unit.AttackTargetKind, unit.AttackTargetId.Value) || unit.AttackCooldownRemaining > 0)
        {
            return;
        }

        if (!weapon.CanFireWhileMoving && unit.Velocity.LengthSquared() > 1)
        {
            return;
        }

        if (!WeaponCanFireAt(unit.TurretFacing, targetAngle, weapon))
        {
            return;
        }

        unit.AttackCooldownRemaining = weapon.Cooldown;
        unit.TurretState = TurretState.Firing;
        FireWeapon(CombatSource(unit), weapon, unit.AttackTargetKind, unit.AttackTargetId.Value, targetPosition.Value);
    }

    private void UpdateBuildingCombat(BuildingModel building)
    {
        var weapon = Weapon(building);
        if (weapon is null)
        {
            return;
        }

        if (building.AttackTargetId is null)
        {
            building.TurretState = building.AttackCooldownRemaining > 0 ? TurretState.Reloading : TurretState.Idle;
            return;
        }

        var targetPosition = CombatTargetPosition(building.AttackTargetKind, building.AttackTargetId.Value);
        if (targetPosition is null
            || !IsCombatTargetHostile(building.Owner, building.AttackTargetKind, building.AttackTargetId.Value)
            || !CanWeaponTarget(weapon, building.AttackTargetKind, building.AttackTargetId.Value))
        {
            ClearBuildingAttackTarget(building);
            return;
        }

        var source = CombatSource(building);
        var toTarget = targetPosition.Value - building.Position;
        var targetAngle = toTarget.Angle();
        var desiredWeaponFacing = DesiredWeaponFacing(source, weapon, targetAngle);
        building.TurretFacing = RotateToward(building.TurretFacing, desiredWeaponFacing, source.TurnRate * 0.04f);
        building.TurretState = building.AttackCooldownRemaining > 0 ? TurretState.Reloading : TurretState.Tracking;

        if (toTarget.Length() > FireAuthorizationRange(weapon, building.AttackTargetKind, building.AttackTargetId.Value) || building.AttackCooldownRemaining > 0)
        {
            return;
        }

        if (!WeaponCanFireAt(building.TurretFacing, targetAngle, weapon))
        {
            return;
        }

        building.AttackCooldownRemaining = weapon.Cooldown;
        building.TurretState = TurretState.Firing;
        FireWeapon(CombatSource(building), weapon, building.AttackTargetKind, building.AttackTargetId.Value, targetPosition.Value);
    }

    private CombatSourceModel CombatSource(UnitModel unit)
    {
        var descriptor = unit.RuntimeDescriptor;
        return new CombatSourceModel(
            CombatSourceKind.Unit,
            unit.Id,
            unit.Owner,
            unit.FactionId,
            unit.Position,
            unit.Facing,
            unit.TurretFacing,
            descriptor.Radius,
            descriptor.TurnRate,
            descriptor.WeaponKind,
            VisualAccent(unit.Owner, unit.FactionId, descriptor.Accent));
    }

    private CombatSourceModel CombatSource(BuildingModel building)
    {
        var spec = BuildSpecCatalog.For(building.Kind);
        return new CombatSourceModel(
            CombatSourceKind.Building,
            building.Id,
            building.Owner,
            building.FactionId,
            building.Position,
            building.Facing,
            building.TurretFacing,
            BuildingRadius(building),
            5.4f,
            spec.WeaponKind ?? WeaponKind.IonEmitter,
            VisualAccent(building.Owner, building.FactionId, spec.Accent));
    }

    private static float DesiredWeaponFacing(CombatSourceModel source, WeaponDefinition weapon, float targetAngle)
    {
        return weapon.MountKind switch
        {
            WeaponMountKind.FixedForward => source.BodyFacing,
            _ => targetAngle,
        };
    }

    private static void AimUnitWeapon(UnitModel unit, WeaponDefinition weapon, float targetAngle, float turnStep)
    {
        if (weapon.MountKind == WeaponMountKind.FixedForward)
        {
            if (unit.MoveTarget is null || unit.MovementState is UnitMovementState.HoldingSlot or UnitMovementState.CombatAnchor or UnitMovementState.Idle)
            {
                unit.Facing = RotateToward(unit.Facing, targetAngle, turnStep);
            }

            unit.TurretFacing = unit.Facing;
            return;
        }

        unit.TurretFacing = RotateToward(unit.TurretFacing, targetAngle, turnStep);
    }

    private static bool WeaponCanFireAt(float weaponFacing, float targetAngle, WeaponDefinition weapon)
    {
        if (weapon.MountKind == WeaponMountKind.Special)
        {
            return true;
        }

        var arc = weapon.MountKind == WeaponMountKind.StaticTurret
            ? Mathf.Tau
            : weapon.FireArcRadians;
        return Mathf.Abs(Mathf.AngleDifference(weaponFacing, targetAngle)) <= arc * 0.5f;
    }

    private static Vector2 MuzzlePosition(CombatSourceModel source, WeaponDefinition weapon)
    {
        var facing = weapon.MountKind == WeaponMountKind.FixedForward
            ? source.BodyFacing
            : source.WeaponFacing;
        var offset = source.Radius + (source.Kind == CombatSourceKind.Building ? 18 : 12);
        return source.Position + Vector2.FromAngle(facing) * offset;
    }

    private void FireWeapon(
        CombatSourceModel source,
        WeaponDefinition weapon,
        CombatTargetKind targetKind,
        int targetId,
        Vector2 targetPosition)
    {
        var ammo = AmmoDefinitions[weapon.AmmoKind];
        var damage = DamageForTarget(ammo, targetKind, targetId);
        var muzzle = MuzzlePosition(source, weapon);
        if (ammo.Behavior == ProjectileBehavior.Beam)
        {
            AddBeam(source, ammo, targetKind, targetId, targetPosition, muzzle);
            ApplyDamage(targetKind, targetId, damage, source.Kind, source.Id);
            return;
        }

        Vector2? impactPosition = ammo.HitRule == HitRule.BallisticDeviation
            ? targetPosition + BallisticDeviation(source.Id, targetId, _nextProjectileId, targetKind)
            : null;
        var aimPoint = impactPosition ?? targetPosition;
        var direction = (aimPoint - muzzle).LengthSquared() <= 0.01f
            ? Vector2.FromAngle(source.WeaponFacing)
            : (aimPoint - muzzle).Normalized();

        Projectiles.Add(new ProjectileModel
        {
            Id = _nextProjectileId++,
            SourceKind = source.Kind,
            SourceId = source.Id,
            TargetId = targetId,
            TargetKind = targetKind,
            AmmoKind = ammo.Kind,
            Behavior = ammo.Behavior,
            HitRule = ammo.HitRule,
            Position = muzzle,
            Velocity = direction * ammo.Speed,
            ImpactPosition = impactPosition,
            Speed = ammo.Speed,
            Damage = damage,
            HitRadiusMultiplier = ammo.AccuracyRadiusMultiplier,
            TrackingStrength = ammo.TrackingStrength,
            TrailWidth = ammo.Kind == AmmoKind.NeedleDart ? 3.2f : ammo.Kind == AmmoKind.SeekerRocket ? 7.2f : 8.4f,
            CoreWidth = ammo.Kind == AmmoKind.NeedleDart ? 1.1f : ammo.Kind == AmmoKind.SeekerRocket ? 2.4f : 2.8f,
            HeadRadius = ammo.Kind == AmmoKind.NeedleDart ? 2.8f : ammo.Kind == AmmoKind.SeekerRocket ? 5.6f : 4.6f,
            Accent = FactionVisualPolicy.CommandAccent(Owner.Player, MatchConfig.PlayerFaction, source.Owner, source.FactionId, ammo.Accent),
        });
    }

    private void AddBeam(CombatSourceModel source, AmmoDefinition ammo, CombatTargetKind targetKind, int targetId, Vector2 targetPosition, Vector2 muzzle)
    {
        Beams.Add(new BeamModel
        {
            Id = _nextBeamId++,
            SourceKind = source.Kind,
            SourceId = source.Id,
            TargetId = targetId,
            TargetKind = targetKind,
            Start = muzzle,
            End = targetPosition,
            Duration = ammo.BeamDuration,
            Age = 0,
            Width = ammo.BeamWidth,
            Accent = FactionVisualPolicy.CommandAccent(Owner.Player, MatchConfig.PlayerFaction, source.Owner, source.FactionId, ammo.Accent),
        });
    }

    private void UpdateProjectiles(float dt)
    {
        for (var index = Projectiles.Count - 1; index >= 0; index--)
        {
            var projectile = Projectiles[index];
            var targetPosition = CombatTargetPosition(projectile.TargetKind, projectile.TargetId);
            if (targetPosition is null && projectile.ImpactPosition is null)
            {
                Projectiles.RemoveAt(index);
                continue;
            }

            var aimPoint = projectile.ImpactPosition ?? targetPosition!.Value;
            var toTarget = aimPoint - projectile.Position;
            var distance = toTarget.Length();
            var step = projectile.Speed * dt;

            if (distance <= step + CombatTargetRadius(projectile.TargetKind, projectile.TargetId) * 0.35f)
            {
                if (ProjectileHitsTarget(projectile, targetPosition, aimPoint))
                {
                    ApplyDamage(projectile.TargetKind, projectile.TargetId, projectile.Damage, projectile.SourceKind, projectile.SourceId);
                }
                Projectiles.RemoveAt(index);
                continue;
            }

            if (projectile.Behavior == ProjectileBehavior.Tracking && targetPosition is not null)
            {
                var desiredVelocity = toTarget.Normalized() * projectile.Speed;
                var turnAmount = Mathf.Clamp(dt * projectile.TrackingStrength, 0, 1);
                projectile.Velocity = projectile.Velocity.Lerp(desiredVelocity, turnAmount);
                if (projectile.Velocity.LengthSquared() > 0.01f)
                {
                    projectile.Velocity = projectile.Velocity.Normalized() * projectile.Speed;
                }
            }
            else
            {
                projectile.Velocity = toTarget.Normalized() * projectile.Speed;
            }

            projectile.Position += projectile.Velocity * dt;
        }
    }

    private bool ProjectileHitsTarget(ProjectileModel projectile, Vector2? targetPosition, Vector2 impactPosition)
    {
        if (projectile.HitRule == HitRule.Guaranteed)
        {
            return true;
        }

        if (targetPosition is null)
        {
            return false;
        }

        var hitRadius = CombatTargetRadius(projectile.TargetKind, projectile.TargetId) * projectile.HitRadiusMultiplier;
        return impactPosition.DistanceTo(targetPosition.Value) <= hitRadius;
    }

    private float DamageForTarget(AmmoDefinition ammo, CombatTargetKind targetKind, int targetId)
    {
        return targetKind switch
        {
            CombatTargetKind.Unit when UnitById(targetId) is { } unit => EffectiveDamageAgainst(ammo.Kind, unit.RuntimeDescriptor),
            CombatTargetKind.Building when BuildingById(targetId) is { } building => EffectiveDamageAgainst(ammo.Kind, BuildSpecCatalog.For(building.Kind)),
            _ => ammo.BaseDamage,
        };
    }

    private Vector2 BallisticDeviation(int sourceId, int targetId, int projectileId, CombatTargetKind targetKind)
    {
        var radius = CombatTargetRadius(targetKind, targetId);
        var weight = TargetWeight(targetKind, targetId);
        var distance = weight switch
        {
            UnitWeightClass.Light => radius * 2.6f + 18,
            UnitWeightClass.Medium => radius * 0.2f,
            UnitWeightClass.Heavy => radius * 0.14f,
            _ => radius * 0.25f,
        };
        var angle = DeterministicAngle(sourceId, targetId, projectileId);
        return Vector2.FromAngle(angle) * distance;
    }

    private UnitWeightClass TargetWeight(CombatTargetKind targetKind, int targetId)
    {
        return targetKind switch
        {
            CombatTargetKind.Unit when UnitById(targetId) is { } unit => unit.RuntimeDescriptor.WeightClass,
            CombatTargetKind.Building => UnitWeightClass.Heavy,
            _ => UnitWeightClass.Medium,
        };
    }

    private static float DeterministicAngle(int sourceId, int targetId, int projectileId)
    {
        var seed = unchecked((sourceId * 73856093) ^ (targetId * 19349663) ^ (projectileId * 83492791));
        seed ^= seed << 13;
        seed ^= seed >> 17;
        seed ^= seed << 5;
        var normalized = (seed & 0x7fffffff) / (float)int.MaxValue;
        return normalized * Mathf.Tau;
    }

    private void UpdateBeams(float dt)
    {
        for (var index = Beams.Count - 1; index >= 0; index--)
        {
            var beam = Beams[index];
            beam.Age += dt;

            if (beam.Age >= beam.Duration)
            {
                Beams.RemoveAt(index);
            }
        }
    }
}
