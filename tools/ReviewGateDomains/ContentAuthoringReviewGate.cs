static class ContentAuthoringReviewGate
{
    public static void Check(string root, GateResult result)
    {
        RequireUnitSpecAuthoring(root, result);
        RequireBuildSpecAuthoring(root, result);
        MapAuthoringReviewGate.Check(root, result);
        RequireEconomyAndProductionSystems(root, result);
        RequireScopeLocks(root, result);
    }

    private static void RequireUnitSpecAuthoring(string root, GateResult result)
    {
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "units", "UnitDesign.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "units", "UnitSpec.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "units", "UnitDesignCatalog.cs");
        var rosterCatalog = ReviewGateSource.Read(root, "scripts", "core", "units", "UnitDesignFactionRosterCatalog.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "units", "UnitDesignRuntimeLoadouts.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "units", "UnitSpecRuntimeDescriptor.cs");
        ReviewGateSource.RequireTextInFile(root, result, "OrderBy(design => design.Id", "scripts", "core", "units", "UnitDesignCatalog.cs");
        RequireText(rosterCatalog, "foreach (var designId in For(faction).PlayableDesignIds)", "Production design lookup must scan playable ids without LINQ materialization.", result);
        ForbidText(rosterCatalog, "PlayableSpecs(faction)", "Production design lookup must not allocate playable spec iterators.", result);
    }

    private static void RequireBuildSpecAuthoring(string root, GateResult result)
    {
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "build", "BuildSpec.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "build", "BuildSpecCatalog.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "build", "BuildingDesign.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "combat", "WeaponDesign.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "combat", "AmmoDesign.cs");
        ReviewGateSource.RequireTextInFile(root, result, "public sealed record BuildSpec", "scripts", "core", "build", "BuildSpec.cs");
        ReviewGateSource.RequireTextInFile(root, result, "public static class BuildSpecCatalog", "scripts", "core", "build", "BuildSpecCatalog.cs");
        ReviewGateSource.RequireTextInFile(root, result, "typeof(BuildingDesign)", "scripts", "core", "build", "BuildSpecCatalog.cs");
        ReviewGateSource.RequireTextInFile(root, result, "IReadOnlyDictionary<string, WeaponDefinition>", "scripts", "core", "combat", "WeaponCatalog.cs");
        ReviewGateSource.RequireTextInFile(root, result, "IReadOnlyDictionary<string, AmmoDefinition>", "scripts", "core", "combat", "WeaponCatalog.cs");
        ReviewGateSource.RequireTextInFile(root, result, "DiscoverWeaponsFrom(params Assembly[] assemblies)", "scripts", "core", "combat", "WeaponCatalog.cs");
        ReviewGateSource.RequireTextInFile(root, result, "public abstract string Id", "scripts", "core", "combat", "WeaponDesign.cs");
        ReviewGateSource.RequireTextInFile(root, result, "public abstract string Id", "scripts", "core", "combat", "AmmoDesign.cs");
        ReviewGateSource.RequireTextInFile(root, result, "DiscoverDesigns<WeaponDesign>", "scripts", "core", "combat", "WeaponCatalog.cs");
        ReviewGateSource.RequireTextInFile(root, result, "DiscoverDesigns<AmmoDesign>", "scripts", "core", "combat", "WeaponCatalog.cs");
        ReviewGateSource.RequireFile(root, result, "tools", "ContentAuthoringQa", "Program.cs");
        ReviewGateSource.RequireFile(root, result, "tools", "ContentAuthoringQa", "ThrowawayAuthoringDesigns.cs");
        var contentQa = ReviewGateSource.Read(root, "tools", "ContentAuthoringQa", "Program.cs") + ReviewGateSource.Read(root, "tools", "ContentAuthoringQa", "ThrowawayAuthoringDesigns.cs");
        RequireText(contentQa, "weapon.qa.throwaway.probe", "ContentAuthoringQa must prove a tool-local string weapon id.", result);
        RequireText(contentQa, "ammo.qa.throwaway.spark", "ContentAuthoringQa must prove a tool-local string ammo id.", result);
        RequireText(contentQa, "RequireHasTranslation(spec.NameKey", "ContentAuthoringQa must close declarative spec localization/sort/turret contracts.", result);
        RequireText(contentQa, "RegisterCombatDefinitions(toolWeapons.Values, toolAmmo.Values)", "Tool-local weapon/ammo must be injected into EntityWorld, not the runtime catalog.", result);
        RequireText(contentQa, "!WeaponCatalog.WeaponDefinitions.ContainsKey(ThrowawayProbeWeaponDesign.WeaponId)", "Throwaway weapon must not pollute the runtime WeaponCatalog.", result);
        ReviewGateSource.RequireTextInFile(root, result, "ContentAuthoringQa", "tools", "VerifyAll", "Program.cs");
        ReviewGateSource.RequireAnyText(root, result, "BuildSpecCatalog.For", "scripts", "tools/CombatBehavior", "tools/PlayerLoopQa", "tools/FogOfWarQa");
    }

    private static void RequireEconomyAndProductionSystems(string root, GateResult result)
    {
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "sim", "systems", "ProductionSystem.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "sim", "systems", "construction", "ConstructionSystem.Commands.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "sim", "systems", "resource", "ResourceSystem.Harvester.cs");
        ReviewGateSource.RequireAnyText(root, result, "ProductionQueueComponentState", "scripts/core/sim", "scripts/core/units/runtime", "tools/SimReplay");
        ReviewGateSource.RequireAnyText(root, result, "StartConstructionEntityCommand", "scripts/core/sim", "tools/SimReplay", "tools/AiOpponentLoopQa");
        ReviewGateSource.RequireAnyText(root, result, "HarvestEntityCommand", "scripts/core/sim", "tools/CombatBehavior", "tools/SimReplay");
    }

    private static void RequireScopeLocks(string root, GateResult result)
    {
        var matchConfig = ReviewGateSource.Read(root, "scripts", "core", "match", "MatchConfig.cs");
        RequireText(matchConfig, "FactionId PlayerFaction", "MatchConfig must keep one human player faction.", result);
        RequireText(matchConfig, "FactionId AiFaction", "MatchConfig must keep one computer AI faction.", result);

        var rosterProfile = ReviewGateSource.Read(root, "scripts", "core", "units", "UnitRosterProfile.cs");
        RequireText(rosterProfile, "MaximumTechTier", "Roster profile must keep the current tech-tier ceiling.", result);
        RequireText(rosterProfile, "design.Stats.TechTier > maximumTechTier", "Roster filtering must enforce the tech-tier ceiling.", result);

        var sandboxContext = ReviewGateSource.Read(root, "scripts", "core", "sandbox", "SandboxDeveloperContext.cs");
        RequireText(sandboxContext, "SandboxFactionAvailability.LockedPlaceholder", "Corruption faction must remain a locked placeholder.", result);

        var menuFlow = ReviewGateSource.Read(root, "scripts", "main-menu", "MainMenuRoot.Flow.cs");
        RequireText(menuFlow, "FactionId.Corruption => GameText.T(\"faction.corruption.locked\")", "Main menu must present Corruption as locked.", result);
    }
}
