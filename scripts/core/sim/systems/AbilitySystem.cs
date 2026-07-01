namespace ProceduralRts.Core;

/// <summary>
/// Deterministic active ability executor. Ability data lives on EntitySpec;
/// runtime cooldowns live on AbilityRuntimeComponentState; player/AI intent
/// enters through AbilityEntityCommand.
/// </summary>
public sealed partial class AbilitySystem : ISimSystem
{
    private const float RepairFieldCooldownSeconds = 1.0f;
    private const float ShieldFieldCooldownSeconds = 2.0f;
    private const float ShieldFieldDurationSeconds = 4.0f;
    private const float ScanCooldownSeconds = 3.0f;
    private const float DefaultScanDurationSeconds = 3.0f;
    private const string ScanRevealSpecId = "effect.scan_reveal";
    private const float DeployCooldownSeconds = 0.5f;
    private const float DefaultDeploySetupSeconds = 0.6f;
    private const float DefaultDeployRangeMultiplier = 1.55f;

    public void Step(SimContext context)
    {
        TickShields(context.World, context.FixedDelta);
        TickScanReveals(context.World, context.FixedDelta);
        TickDeploySetup(context.World, context.FixedDelta);
        TickCooldowns(context.World, context.FixedDelta);

        foreach (var sequenced in context.Commands)
        {
            if (sequenced.Command is AbilityEntityCommand ability)
            {
                ApplyAbility(context.World, ability);
            }
        }
    }

    private static void ApplyAbility(EntityWorld world, AbilityEntityCommand command)
    {
        foreach (var subjectId in command.Subjects)
        {
            if (!world.TryGet(subjectId, out var caster)
                || caster.OwnerId.Value != command.Issuer.Value
                || IsDead(caster)
                || !world.TryGetSpec(caster.SpecId, out var spec)
                || !TryGetAbility(spec, command.Ability, out var ability)
                || !caster.Components.TryGet<AbilityRuntimeComponentState>(out var runtime))
            {
                continue;
            }

            var isToggleOff = command.Ability == AbilityKind.Deploy && IsDeployed(caster);
            if (!isToggleOff && IsOnCooldown(runtime, command.Ability))
            {
                continue;
            }

            if (!HasValidTarget(world, command, caster, ability)
                || (!isToggleOff && !CanPayCost(world, command.Issuer, ability)))
            {
                continue;
            }

            var applied = false;
            if (command.Ability == AbilityKind.RepairField
                && TryResolveTargetPoint(world, command, caster, out var targetPoint)
                && ApplyRepairField(world, caster, targetPoint, ability) > 0)
            {
                applied = true;
                SetCooldown(caster, runtime, command.Ability, RepairFieldCooldownSeconds);
            }
            else if (command.Ability == AbilityKind.ShieldField
                && TryResolveTargetPoint(world, command, caster, out targetPoint)
                && ApplyShieldField(world, caster, targetPoint, ability) > 0)
            {
                applied = true;
                SetCooldown(caster, runtime, command.Ability, ShieldFieldCooldownSeconds);
            }
            else if (command.Ability == AbilityKind.Scan
                && TryResolveTargetPoint(world, command, caster, out targetPoint)
                && ApplyScan(world, caster, targetPoint, ability))
            {
                applied = true;
                SetCooldown(caster, runtime, command.Ability, ScanCooldownSeconds);
            }
            else if (command.Ability == AbilityKind.Deploy
                && ApplyDeployToggle(caster, ability))
            {
                applied = true;
                SetCooldown(caster, runtime, command.Ability, DeployCooldownSeconds);
            }

            if (applied && !isToggleOff)
            {
                PayCost(world, command.Issuer, ability);
            }
        }
    }

}
