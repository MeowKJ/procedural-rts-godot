static class ContentAuthoringReviewGate
{
    public static void Check(string root, GateResult result)
    {
        RequireUnitSpecAuthoring(root, result);
        RequireUnitKindCleanupEdges(root, result);
        RequireBuildSpecAuthoring(root, result);
        RequireMapAuthoring(root, result);
        RequireEconomyAndProductionSystems(root, result);
        RequireScopeLocks(root, result);
    }

    private static void RequireUnitSpecAuthoring(string root, GateResult result)
    {
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "units", "UnitDesign.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "units", "UnitSpec.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "units", "UnitDesignCatalog.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "units", "UnitDesignFactionRosterCatalog.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "units", "UnitDesignRuntimeLoadouts.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "units", "UnitSpecRuntimeDescriptor.cs");
        ReviewGateSource.RequireTextInFile(root, result, "OrderBy(design => design.Id", "scripts", "core", "units", "UnitDesignCatalog.cs");
        ReviewGateSource.RequireTextInFile(root, result, "required string DesignId", "scripts", "core", "units", "UnitModel.cs");
        ReviewGateSource.RequireTextInFile(root, result, "UnitDesignCatalog.Spec", "scripts", "core", "units", "UnitModel.cs");
        var gameState = ReviewGateEvidence.ReadSourceWithPartials(Path.Combine(root, "scripts", "core", "GameState.cs"));
        ForbidText(gameState, "UnitRuntimeDescriptorFor(UnitKind", "GameState must not keep UnitKind runtime descriptor helpers.", result);
        ForbidText(gameState, "IsHarvesterUnit(UnitKind", "GameState must not expose UnitKind harvester helpers.", result);
        RequireText(gameState, "IsHarvesterUnit(UnitModel unit)", "GameState harvester checks must use UnitModel/UnitSpec.", result);
        ReviewGateSource.ForbidFile(root, result, "scripts", "core", "units", "UnitCatalog.cs");
        ReviewGateSource.ForbidFile(root, result, "scripts", "core", "units", "UnitKind.cs");
        ReviewGateSource.ForbidFile(root, result, "scripts", "core", "units", "UnitKindDesignBridge.cs");
        ReviewGateSource.ForbidFile(root, result, "scripts", "core", "build", "BuildingKind.cs");
        ReviewGateSource.ForbidTextInSources(root, result, "UnitDefinition", "scripts", "tools/CombatBehavior", "tools/FogOfWarQa", "tools/SimulationSmoke");
    }

    private static void RequireUnitKindCleanupEdges(string root, GateResult result)
    {
        var seeding = ReviewGateSource.Read(root, "scripts", "core", "game-state", "GameState.SeedingMap.cs");
        ForbidText(seeding, "AddUnit(UnitKind", "GameState seeding must not keep a UnitKind AddUnit wrapper.", result);
        ForbidText(seeding, "UnitKind.", "GameState seeding must not reference legacy UnitKind values.", result);
        RequireText(seeding, "PlayableDesignIds", "Developer sandbox faction lines must enumerate UnitDesign playable ids.", result);
        RequireText(seeding, "AddUnit(designId", "Developer sandbox faction lines must spawn directly by design id.", result);

        var fogQa = ReviewGateSource.Read(root, "tools", "FogOfWarQa", "Program.cs");
        ForbidText(fogQa, "UnitKind", "FogOfWarQa unit fixtures must not depend on legacy UnitKind.", result);
        ForbidText(fogQa, "UnitKindDesignBridge", "FogOfWarQa unit fixtures must not bridge through UnitKindDesignBridge.", result);
        ForbidText(fogQa, "LegacyKind", "FogOfWarQa unit fixtures must not populate LegacyKind.", result);
        RequireText(fogQa, "PlayerScoutDesignId", "FogOfWarQa must name unit fixtures by design id.", result);
        RequireText(fogQa, "UnitDesignCatalog.Spec(designId)", "FogOfWarQa must validate unit fixtures through UnitDesignCatalog.", result);

        var combatProgram = ReviewGateSource.Read(root, "tools", "CombatBehavior", "Program.cs");
        var skirmishAi = ReviewGateSource.Read(root, "tools", "CombatBehaviorSkirmish", "SkirmishAi.cs");
        ForbidText(combatProgram + skirmishAi, "StartingUnitKinds", "CombatBehavior skirmish start checks must not convert starting design ids back to UnitKind.", result);
        ForbidText(combatProgram + skirmishAi, "PlayableUnitKinds", "CombatBehavior sandbox roster checks must not filter playable design ids through UnitKind.", result);
        RequireText(skirmishAi, "unit.DesignId == designId", "CombatBehavior skirmish roster checks must assert native unit design ids.", result);
        RequireText(skirmishAi, "dog.guard_tank", "CombatBehavior sandbox mirror checks must identify dog units by design id.", result);
        RequireText(skirmishAi, "cat.tank", "CombatBehavior sandbox mirror checks must identify cat units by design id.", result);
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
        ReviewGateSource.RequireTextInFile(root, result, "public virtual string Id", "scripts", "core", "combat", "WeaponDesign.cs");
        ReviewGateSource.RequireTextInFile(root, result, "public virtual string Id", "scripts", "core", "combat", "AmmoDesign.cs");
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

    private static void RequireMapAuthoring(string root, GateResult result)
    {
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "map", "MapSpec.cs");
        ReviewGateSource.RequireFile(root, result, "scripts", "core", "map", "MapLoader.cs");
        ReviewGateSource.RequireFile(root, result, "tools", "MapAuthoringQa", "Program.cs");
        ReviewGateSource.RequireTextInFile(root, result, "map-authoring-qa", "tools", "VerifyAll", "Program.cs");
        ReviewGateSource.RequireTextInFile(root, result, "RunMapAuthoringScenario", "tools", "SimReplay", "Program.cs");
        var mapSpec = ReviewGateSource.Read(root, "scripts", "core", "map", "MapSpec.cs");
        ForbidText(mapSpec, "using Godot", "MapSpec must stay pure C# without Godot imports.", result);
        ForbidText(mapSpec, "Vector2", "MapSpec must not expose Godot Vector2.", result);
        ForbidText(mapSpec, "Godot.Color", "MapSpec must not expose Godot Color.", result);
        var loader = ReviewGateSource.Read(root, "scripts", "core", "map", "MapLoader.cs");
        ForbidText(loader, ".tscn", "MapLoader must never read Godot scene files.", result);
        var simReplayMap = ReviewGateSource.Read(root, "tools", "SimReplayContent", "MapAuthoringScenarios.cs");
        RequireText(simReplayMap, "MapLoader.Load", "SimReplay must replay authored maps through MapLoader.", result);
        RequireText(simReplayMap, "AssertDeterministic", "SimReplay map authoring scenario must be deterministic.", result);
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
