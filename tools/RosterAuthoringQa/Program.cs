using ProceduralRts.Core;

var strict = args.Any(arg => string.Equals(arg, "--strict", StringComparison.OrdinalIgnoreCase));
var failures = new List<string>();
var warnings = new List<string>();
var playableFactions = new[] { UnitFactionId.Dog, UnitFactionId.Cat };

Console.WriteLine("RosterAuthoringQa");
Console.WriteLine(strict ? "Mode: strict" : "Mode: current-slice baseline");
Console.WriteLine();

ValidateNoPlayableThirdFaction(failures);

var allPlayableSpecs = new List<UnitSpec>();
foreach (var faction in playableFactions)
{
    var specs = UnitDesignFactionRosterCatalog.For(faction)
        .PlayableDesignIds
        .Select(UnitDesignCatalog.Spec)
        .OrderBy(spec => spec.Stats.TechTier)
        .ThenBy(spec => spec.Id)
        .ToArray();

    allPlayableSpecs.AddRange(specs);
    PrintFactionSummary(faction, specs);
    ValidateFactionRoster(faction, specs, failures, warnings);
}

ValidateGlobalCounterHooks(allPlayableSpecs, failures);

if (strict)
{
    failures.AddRange(warnings.Select(warning => $"strict: {warning}"));
}

Console.WriteLine();
if (warnings.Count > 0)
{
    Console.WriteLine("Warnings:");
    foreach (var warning in warnings)
    {
        Console.WriteLine($"- {warning}");
    }
}

if (failures.Count > 0)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine("RosterAuthoringQa FAILED:");
    foreach (var failure in failures)
    {
        Console.Error.WriteLine($"- {failure}");
    }

    Environment.Exit(1);
}

Console.WriteLine("RosterAuthoringQa PASSED.");

static void ValidateNoPlayableThirdFaction(List<string> failures)
{
    var corruptionRoster = UnitDesignFactionRosterCatalog.For(UnitFactionId.Corruption);
    if (corruptionRoster.PlayableDesignIds.Count > 0 || corruptionRoster.StartingUnits.Count > 0)
    {
        failures.Add("Corruption must stay a locked placeholder with no playable roster or starting units.");
    }
}

static void PrintFactionSummary(UnitFactionId faction, IReadOnlyList<UnitSpec> specs)
{
    var tiers = string.Join(",", specs.Select(spec => spec.Stats.TechTier).Distinct().Order());
    var categories = string.Join(",", specs
        .Select(spec => spec.Production?.Category)
        .Where(category => category is not null)
        .Distinct()
        .OrderBy(category => category)
        .Select(category => category!.Value));
    var domains = string.Join(",", specs.Select(spec => spec.Movement.Domain).Distinct().OrderBy(domain => domain));

    Console.WriteLine($"{faction}: {specs.Count} playable designs");
    Console.WriteLine($"  tiers: {tiers}");
    Console.WriteLine($"  categories: {categories}");
    Console.WriteLine($"  domains: {domains}");
    foreach (var spec in specs)
    {
        Console.WriteLine($"  - T{spec.Stats.TechTier} {spec.Id} [{spec.Production?.Category}] {spec.Movement.Domain}/{spec.Stats.ArmorTag}");
    }
}

static void ValidateFactionRoster(
    UnitFactionId faction,
    IReadOnlyList<UnitSpec> specs,
    List<string> failures,
    List<string> warnings)
{
    if (specs.Count == 0)
    {
        failures.Add($"{faction} must have a playable UnitDesign roster.");
        return;
    }

    RequireTiers(faction, specs, failures);
    RequireCategories(faction, specs, failures);
    RequireRoles(faction, specs, failures);
    RequireStartingUnits(faction, specs, failures);

    foreach (var spec in specs)
    {
        ValidateSpec(faction, spec, failures);
    }

    if (!specs.Any(spec => spec.Movement.Domain == MovementDomain.Air))
    {
        failures.Add($"{faction} must include a playable air unit for the counter triangle.");
    }
}

static void RequireTiers(UnitFactionId faction, IReadOnlyList<UnitSpec> specs, List<string> failures)
{
    foreach (var tier in new[] { 1, 2, 3 })
    {
        if (!specs.Any(spec => spec.Stats.TechTier == tier))
        {
            failures.Add($"{faction} must have at least one playable T{tier} design.");
        }
    }
}

static void RequireCategories(UnitFactionId faction, IReadOnlyList<UnitSpec> specs, List<string> failures)
{
    foreach (var category in new[] { ProductionCategory.Infantry, ProductionCategory.Vehicle, ProductionCategory.Economy })
    {
        if (!specs.Any(spec => spec.Production?.Category == category))
        {
            failures.Add($"{faction} must cover production category {category}.");
        }
    }
}

static void RequireRoles(UnitFactionId faction, IReadOnlyList<UnitSpec> specs, List<string> failures)
{
    var requiredRoles = new[]
    {
        UnitRoleTag.Infantry,
        UnitRoleTag.Vehicle,
        UnitRoleTag.Economy,
        UnitRoleTag.AntiAir,
        UnitRoleTag.Support,
    };

    foreach (var role in requiredRoles)
    {
        if (!specs.Any(spec => spec.RoleTags.Contains(role)))
        {
            failures.Add($"{faction} must include a playable {role} role.");
        }
    }
}

static void RequireStartingUnits(UnitFactionId faction, IReadOnlyList<UnitSpec> specs, List<string> failures)
{
    var playableIds = specs.Select(spec => spec.Id).ToHashSet(StringComparer.Ordinal);
    var startingUnits = UnitDesignFactionRosterCatalog.StartingUnits(faction);
    if (startingUnits.Count == 0)
    {
        failures.Add($"{faction} must define data-driven starting units.");
        return;
    }

    foreach (var startingUnit in startingUnits)
    {
        if (!playableIds.Contains(startingUnit.DesignId))
        {
            failures.Add($"{faction} starting unit '{startingUnit.DesignId}' must be in the playable roster.");
        }
    }

    if (!startingUnits.Any(unit => UnitDesignCatalog.Spec(unit.DesignId).RoleTags.Contains(UnitRoleTag.Economy)))
    {
        failures.Add($"{faction} starting units must include an economy unit.");
    }
}

static void ValidateSpec(UnitFactionId faction, UnitSpec spec, List<string> failures)
{
    if (spec.Faction != faction)
    {
        failures.Add($"{spec.Id} is listed in {faction} roster but belongs to {spec.Faction}.");
    }

    if (spec.Production is null)
    {
        failures.Add($"{spec.Id} is playable but has no production data.");
    }
    else
    {
        if (spec.Production.Duration <= 0)
        {
            failures.Add($"{spec.Id} production duration must be positive.");
        }

        if (spec.Production.Category == ProductionCategory.Naval)
        {
            failures.Add($"{spec.Id} must not be playable naval content in this slice.");
        }
    }

    if (spec.Stats.TechTier is < 1 or > 3)
    {
        failures.Add($"{spec.Id} tech tier must stay inside T1-T3.");
    }

    if (spec.Stats.Cost <= 0 || spec.Stats.MaxHp <= 0 || spec.Stats.SightRange <= 0)
    {
        failures.Add($"{spec.Id} must have positive cost, HP, and sight range.");
    }

    if (spec.Movement.Domain is MovementDomain.Naval or MovementDomain.Amphibious || spec.Stats.ArmorTag == ArmorTag.Ship)
    {
        failures.Add($"{spec.Id} must not ship playable naval/amphibious content in this slice.");
    }

    if (spec.Movement.Domain == MovementDomain.Air && spec.Collision.BlocksMovement)
    {
        failures.Add($"{spec.Id} is air-domain but still blocks ground movement.");
    }

    if (spec.Movement.Domain == MovementDomain.Land && !spec.Collision.BlocksMovement)
    {
        failures.Add($"{spec.Id} is land-domain but does not block movement.");
    }

    if (!GameText.HasTranslation(spec.NameKey, GameLanguage.English)
        || !GameText.HasTranslation(spec.NameKey, GameLanguage.ChineseSimplified)
        || !GameText.HasTranslation(spec.RoleKey, GameLanguage.English)
        || !GameText.HasTranslation(spec.RoleKey, GameLanguage.ChineseSimplified))
    {
        failures.Add($"{spec.Id} must have en-US and zh-CN name/role text.");
    }

    if (spec.Weapons.Count == 0)
    {
        failures.Add($"{spec.Id} must define at least one weapon mount.");
    }

    foreach (var mount in spec.Weapons)
    {
        if (!WeaponCatalog.Weapons.ContainsKey(mount.WeaponKind))
        {
            failures.Add($"{spec.Id} weapon mount '{mount.MountId}' references missing weapon {mount.WeaponKind}.");
        }
    }

    if (spec.RoleTags.Contains(UnitRoleTag.AntiAir) && !CanAnyWeaponTargetAir(spec))
    {
        failures.Add($"{spec.Id} is tagged AntiAir but no weapon can target aircraft.");
    }
}

static void ValidateGlobalCounterHooks(IReadOnlyList<UnitSpec> specs, List<string> failures)
{
    if (!specs.Any(spec => spec.Movement.Domain == MovementDomain.Air && spec.Stats.ArmorTag == ArmorTag.Aircraft))
    {
        failures.Add("Playable roster must include at least one aircraft target for the counter triangle.");
    }

    if (!specs.Any(spec => spec.RoleTags.Contains(UnitRoleTag.AntiAir) && CanAnyWeaponTargetAir(spec)))
    {
        failures.Add("Playable roster must include at least one anti-air unit whose weapon can target aircraft.");
    }
}

static bool CanAnyWeaponTargetAir(UnitSpec spec)
{
    return spec.Weapons
        .Select(mount => WeaponCatalog.Weapons.TryGetValue(mount.WeaponKind, out var weapon) ? weapon : null)
        .Any(weapon => weapon is not null
            && weapon.TargetProfile.AllowedDomains.Contains(MovementDomain.Air)
            && weapon.TargetProfile.AllowedArmorTags.Contains(ArmorTag.Aircraft));
}
