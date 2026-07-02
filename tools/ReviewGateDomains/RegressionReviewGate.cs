static class RegressionReviewGate
{
    public static void Check(string root, GateResult result)
    {
        RequireToolProjects(root, result);
        RequireVerifyAllCoverage(root, result);
        RequireDeterministicEvidence(root, result);
        RequireSimHotAllocationEvidence(root, result);
        RequireProjectileProjectionBufferEvidence(root, result);
        RequireCommandSystemGroupOrderBufferEvidence(root, result);
        CommandSystemAllocationReviewGate.Check(root, result);
        RequireConstructionPlacementBufferEvidence(root, result);
        RequireAbilitySystemCooldownBufferEvidence(root, result);
    }

    private static void RequireToolProjects(string root, GateResult result)
    {
        foreach (var project in new[]
        {
            "tools/SimReplay/SimReplay.csproj",
            "tools/CombatBehavior/CombatBehavior.csproj",
            "tools/SimulationSmoke/SimulationSmoke.csproj",
            "tools/FogOfWarQa/FogOfWarQa.csproj",
            "tools/PerfSmoke/PerfSmoke.csproj",
            "tools/PlayerLoopQa/PlayerLoopQa.csproj",
            "tools/CounterReadabilityQa/CounterReadabilityQa.csproj",
            "tools/VerifyAll/VerifyAll.csproj",
        })
        {
            ReviewGateSource.RequireFile(root, result, project.Split('/'));
        }
    }

    private static void RequireVerifyAllCoverage(string root, GateResult result)
    {
        var verifyAll = ReviewGateSource.Read(root, "tools", "VerifyAll", "Program.cs");
        foreach (var token in new[]
        {
            "tools/SimReplay/SimReplay.csproj",
            "tools/CombatBehavior/CombatBehavior.csproj",
            "tools/FogOfWarQa/FogOfWarQa.csproj",
            "tools/ReviewGate/ReviewGate.csproj",
            "tools/PerfSmoke/PerfSmoke.csproj",
            "tools/CounterReadabilityQa/CounterReadabilityQa.csproj",
        })
        {
            RequireText(verifyAll, token, $"VerifyAll must run {token}.", result);
        }
    }

    private static void RequireDeterministicEvidence(string root, GateResult result)
    {
        ReviewGateSource.RequireAnyText(root, result, "AssertDeterministic", "tools/SimReplay");
        ReviewGateSource.RequireAnyText(root, result, "upgrade-progression", "tools/SimReplay");
        ReviewGateSource.RequireAnyText(root, result, "DeterministicStateHash", "scripts/core/entities", "tools/SimReplay", "tools/CombatBehavior");
        ReviewGateSource.RequireAnyText(root, result, "CommandAttackUnits", "tools/AiOpponentLoopQa", "tools/CombatBehavior", "scripts");
    }

    private static void RequireSimHotAllocationEvidence(string root, GateResult result)
    {
        var pathfinding = ReviewGateSource.Read(root, "scripts", "core", "sim", "systems", "PathfindingSystem.cs");
        RequireText(pathfinding, "_sharedPlanned", "PathfindingSystem must reuse the shared-corridor planned set.", result);
        RequireText(pathfinding, "_sharedGroups", "PathfindingSystem must reuse shared-corridor grouping buffers.", result);
        RequireText(pathfinding, "_sharedAssignments", "PathfindingSystem must reuse shared-corridor assignment lookup.", result);
        RequireText(pathfinding, "_seenObstacles", "PathfindingSystem must reuse blocker de-duplication storage.", result);
        RequireText(pathfinding, "PathOrGoal(assignment.Path, goal)", "PathfindingSystem shared paths must avoid extra array copies.", result);
        RequireText(pathfinding, "PathOrGoal(result.Path, pathGoal)", "PathfindingSystem single paths must avoid extra array copies.", result);
        ForbidText(pathfinding, "new HashSet<GridObstacle>()", "PathfindingSystem must not allocate blocker HashSets per path build.", result);
        ForbidText(pathfinding, "new Dictionary<SharedMoveKey", "PathfindingSystem must not allocate shared-corridor dictionaries per tick.", result);
        ForbidText(pathfinding, "assignment.Path.ToArray()", "PathfindingSystem shared path assignment must not copy path lists to arrays.", result);
        ForbidText(pathfinding, "result.Path.ToArray()", "PathfindingSystem single path assignment must not copy path lists to arrays.", result);

        var production = ReviewGateSource.Read(root, "scripts", "core", "sim", "systems", "ProductionSystem.cs");
        RequireText(production, "_producerStepBuffer", "ProductionSystem must reuse its producer tick snapshot.", result);
        RequireText(production, "_spawnObstacles", "ProductionSystem must reuse spawn obstacle storage.", result);
        ForbidText(production, "OrderedEntities.ToList()", "ProductionSystem must not snapshot all entities with ToList each tick.", result);
        ForbidText(production, ".Where(entity => entity.Id.Value != producer.Id.Value)", "ProductionSystem spawn obstacles must not be built with LINQ chains.", result);

        var spawnMath = ReviewGateSource.Read(root, "scripts", "core", "production", "ProductionSpawnMath.cs");
        RequireText(spawnMath, "DirectionOffsets", "ProductionSpawnMath must keep candidate directions as static data.", result);
        RequireText(spawnMath, "RingScales", "ProductionSpawnMath must keep ring scales as static data.", result);
        ForbidText(spawnMath, "CandidateDirections(", "ProductionSpawnMath must not allocate a candidate-direction list per spawn.", result);

        var projectiles = ReviewGateSource.Read(root, "scripts", "core", "sim", "systems", "ProjectileSystem.cs");
        ForbidText(projectiles, "OrderedEntities.ToArray()", "ProjectileSystem must not snapshot all entities every tick.", result);
    }

    private static void RequireProjectileProjectionBufferEvidence(string root, GateResult result)
    {
        var effects = ReviewGateSource.Read(root, "scripts", "world", "CombatEffectsLayer.cs");
        RequireText(effects, "List<ProjectilePresentationProjection> _projectileProjections", "CombatEffectsLayer must keep a reusable projectile projection buffer.", result);
        RequireText(effects, "ProjectileProjectionCount()", "CombatEffectsLayer.ActiveEffectCount must count ECS projectiles without constructing projections.", result);
        ForbidText(effects, "ProjectileProjections().Count", "CombatEffectsLayer.ActiveEffectCount must not allocate projectile projections just to count them.", result);

        var draw = ReviewGateSource.Read(root, "scripts", "world", "CombatEffectsLayer.CombatDraw.cs");
        RequireText(draw, "ProjectileProjections(_projectileProjections)", "CombatEffectsLayer.DrawProjectiles must fill the reusable projectile projection buffer.", result);
        RequireText(draw, "ProjectileVfxMath.StyleFor(projectile.AmmoKind)", "Legacy projectiles must use the shared projectile readability style.", result);
        RequireText(draw, "projectile.Style", "ECS projectiles must carry the shared projectile readability style.", result);
        RequireText(draw, "IsSegmentVisible(tail, position, style.CullingPadding)", "Projectile culling must include the tracer segment, not only the head point.", result);
        RequireText(draw, "IsProjectileVisibleToPlayer(tail, position)", "Projectiles drawn above fog must remain gated by player visibility.", result);
        ForbidRegex(draw, @"UnitBattlefield\s*\.\s*ProjectileProjections\s*\(\s*\)", "CombatEffectsLayer.DrawProjectiles must not call the allocating projectile projection API.", result);

        var projector = ReviewGateSource.Read(root, "scripts", "core", "sim", "ProjectilePresentationProjection.cs");
        RequireText(projector, "ProjectInto(EntityWorld world, PlayerSlotId viewer, List<ProjectilePresentationProjection> result)", "ProjectilePresentationProjector must expose a caller-owned buffer API.", result);
        RequireText(projector, "result.Clear();", "ProjectilePresentationProjector.ProjectInto must clear and reuse the caller-owned buffer.", result);
        RequireText(projector, "Count(EntityWorld world)", "ProjectilePresentationProjector must expose a count-only projectile path.", result);
        RequireText(projector, "ProjectileVfxMath.StyleFor(ammo.LegacyKind)", "ECS projectile projections must use the shared projectile readability style.", result);

        var projectileStyle = ReviewGateSource.Read(root, "scripts", "core", "presentation", "vfx", "ProjectileVfxMath.cs");
        RequireText(projectileStyle, "MinimumTrailWidth = 3.6f", "Ordinary projectile trails must keep a readable minimum width.", result);
        RequireText(projectileStyle, "MinimumCoreWidth = 1.8f", "Ordinary projectile cores must keep a readable minimum width.", result);
        RequireText(projectileStyle, "MinimumHeadRadius = 3.4f", "Ordinary projectile heads must keep a readable minimum radius.", result);
        RequireText(projectileStyle, "MinimumCoreAlpha = 0.82f", "Projectile cores must remain bright enough under theme/fog overlays.", result);
        RequireText(projectileStyle, "AmmoKind.SeekerRocket", "Projectile style policy must explicitly cover seeker rockets.", result);

        var gameStateCombat = ReviewGateEvidence.ReadSourceWithPartials(Path.Combine(root, "scripts", "core", "GameState.cs"));
        RequireText(gameStateCombat, "ProjectileVfxMath.StyleFor(ammo.LegacyKind)", "Legacy GameState projectiles must initialize from the shared projectile readability style.", result);

        var battleRoot = ReviewGateEvidence.ReadSourceWithPartials(Path.Combine(root, "scripts", "BattleRoot.cs"));
        RequireText(battleRoot, "AddChild(_fogOfWar);\n\n        _combatEffects", "CombatEffectsLayer must be added after fog so visible projectiles render above the fog overlay.", result);

        var battlefield = ReviewGateSource.Read(root, "scripts", "core", "units", "runtime", "battlefield", "UnitBattlefield.ProjectileProjection.cs");
        RequireText(battlefield, "ProjectileProjections(List<ProjectilePresentationProjection> result)", "UnitBattlefield must expose a projectile projection buffer-fill API.", result);
        RequireText(battlefield, "ProjectilePresentationProjector.ProjectInto(_entityWorld, viewer, result)", "UnitBattlefield buffer API must delegate to ProjectilePresentationProjector.ProjectInto.", result);
        RequireText(battlefield, "ProjectileProjectionCount()", "UnitBattlefield must expose projectile projection count without constructing a list.", result);
    }

    private static void RequireCommandSystemGroupOrderBufferEvidence(string root, GateResult result)
    {
        var commandSystem = ReviewGateEvidence.ReadSourceWithPartials(Path.Combine(root, "scripts", "core", "sim", "systems", "CommandSystem.cs"));
        RequireText(commandSystem, "List<EntityInstance> _groupOrderMembers", "CommandSystem must reuse a group-order member buffer.", result);
        RequireText(commandSystem, "List<FormationDestination> _groupMoveDestinationResults", "CommandSystem must reuse FormationMath destination result storage.", result);
        RequireText(commandSystem, "List<(float X, float Y)> _groupMoveRemainingSlots", "CommandSystem must reuse FormationMath remaining-slot storage.", result);
        RequireText(commandSystem, "Dictionary<int, FormationDestination> _groupMoveDestinations", "CommandSystem must reuse group-move destination lookup storage.", result);
        RequireText(commandSystem, "List<AttackSlotAssignment> _groupAttackAssignmentResults", "CommandSystem must reuse AttackSlotMath assignment result storage.", result);
        RequireText(commandSystem, "List<Vector2> _groupAttackFreeSlots", "CommandSystem must reuse AttackSlotMath free-slot storage.", result);
        RequireText(commandSystem, "Dictionary<int, AttackSlotAssignment> _groupAttackAssignments", "CommandSystem must reuse group-attack assignment lookup storage.", result);
        RequireText(commandSystem, "CollectOwnedSubjects(world, command.Issuer, command.Subjects, _groupOrderMembers)", "CommandSystem group orders must collect owned subjects into the reusable buffer.", result);
        RequireText(commandSystem, "FormationMath.CreateMoveDestinationsInto", "CommandSystem group move must use the caller-owned FormationMath buffer API.", result);
        RequireText(commandSystem, "AttackSlotMath.AssignAttackSlotsInto", "CommandSystem group attack must use the caller-owned AttackSlotMath buffer API.", result);
        ForbidText(commandSystem, "OwnedSubjects(world, command.Issuer, command.Subjects).ToList()", "CommandSystem group orders must not allocate a subject list per command.", result);
        ForbidText(commandSystem, "FormationMath.CreateMoveDestinations(", "CommandSystem group move must not call the allocating FormationMath API.", result);
        ForbidText(commandSystem, ".ToDictionary(d => d.Id)", "CommandSystem group move must not allocate a destination dictionary per command.", result);
        ForbidText(commandSystem, ".ToDictionary(a => a.Id)", "CommandSystem group attack must not allocate an assignment dictionary per command.", result);
        ForbidText(commandSystem, "AttackSlotMath.AssignAttackSlots(", "CommandSystem group attack must not call the allocating AttackSlotMath API.", result);

        var attackSlots = ReviewGateSource.Read(root, "scripts", "core", "sim", "AttackSlotMath.cs");
        RequireText(attackSlots, "AssignAttackSlotsInto", "AttackSlotMath must expose a caller-owned buffer API.", result);

        var formations = ReviewGateSource.Read(root, "scripts", "core", "commands", "FormationMath.cs");
        RequireText(formations, "CreateMoveDestinationsInto", "FormationMath must expose a caller-owned buffer API.", result);
    }

    private static void RequireConstructionPlacementBufferEvidence(string root, GateResult result)
    {
        var constructionSystem = ReviewGateEvidence.ReadSourceWithPartials(Path.Combine(root, "scripts", "core", "sim", "systems", "ConstructionSystem.cs"));
        RequireText(constructionSystem, "List<PlacementBuildAnchor> _placementBuildAnchors", "ConstructionSystem must reuse placement build-anchor storage.", result);
        RequireText(constructionSystem, "List<PlacementObstacle> _placementObstacles", "ConstructionSystem must reuse placement obstacle storage.", result);
        RequireText(constructionSystem, "List<PlacementBuildVisibility> _placementVisibility", "ConstructionSystem must reuse placement visibility storage.", result);
        RequireText(constructionSystem, "List<string> _requiredBuildingOrder", "ConstructionSystem must reuse required-building ordering storage.", result);
        RequireText(constructionSystem, "List<EntityId> _constructionSubjectOrder", "ConstructionSystem must reuse construction subject ordering storage.", result);
        RequireText(constructionSystem, "BuildAnchors(world, issuer, _placementBuildAnchors)", "Construction placement validation must fill the reusable build-anchor buffer.", result);
        RequireText(constructionSystem, "FootprintObstacles(world, _placementObstacles)", "Construction placement validation must fill the reusable obstacle buffer.", result);
        RequireText(constructionSystem, "BuildVisibilitySources(world, issuer, _placementVisibility)", "Construction placement validation must fill the reusable visibility buffer.", result);
        RequireText(constructionSystem, "CollectRequiredBuildings(spec, _requiredBuildingOrder)", "Construction prerequisites must fill the reusable required-building order buffer.", result);
        RequireText(constructionSystem, "CollectOrderedSubjects(command.Subjects, _constructionSubjectOrder)", "Construction commands must fill the reusable subject order buffer.", result);
        RequireText(constructionSystem, "BuildAnchors(EntityWorld world, OwnerId ownerId, List<PlacementBuildAnchor> result)", "Construction build-anchor collection must use a caller-owned buffer.", result);
        RequireText(constructionSystem, "FootprintObstacles(EntityWorld world, List<PlacementObstacle> result)", "Construction obstacle collection must use a caller-owned buffer.", result);
        RequireText(constructionSystem, "BuildVisibilitySources(EntityWorld world, OwnerId ownerId, List<PlacementBuildVisibility> result)", "Construction visibility collection must use a caller-owned buffer.", result);
        RequireText(constructionSystem, "CollectRequiredBuildings(BuildSpec spec, List<string> result)", "Construction required-building ordering must use a caller-owned buffer.", result);
        RequireText(constructionSystem, "CollectOrderedSubjects(IReadOnlyList<EntityId> subjects, List<EntityId> result)", "Construction subject ordering must use a caller-owned buffer.", result);
        ForbidText(constructionSystem, "private static IReadOnlyList<PlacementBuildAnchor> BuildAnchors", "Construction build-anchor collection must not allocate a list per placement validation.", result);
        ForbidText(constructionSystem, "private static IReadOnlyList<PlacementObstacle> FootprintObstacles", "Construction obstacle collection must not allocate a list per placement validation.", result);
        ForbidText(constructionSystem, "private static IReadOnlyList<PlacementBuildVisibility> BuildVisibilitySources", "Construction visibility collection must not allocate a list per placement validation.", result);
        ForbidText(constructionSystem, "RequiredBuildings.OrderBy(kind => kind)", "Construction prerequisites must not allocate ordered required-building enumerables.", result);
        ForbidText(constructionSystem, "Subjects.OrderBy(id => id.Value)", "Construction commands must not allocate ordered subject enumerables.", result);
        ForbidText(constructionSystem, "subjects.OrderBy(id => id.Value)", "Construction producer lookup must not allocate ordered subject enumerables.", result);
        ForbidText(constructionSystem, ".ToList()", "ConstructionSystem must not allocate LINQ lists in construction placement validation paths.", result);
    }

    private static void RequireAbilitySystemCooldownBufferEvidence(string root, GateResult result)
    {
        var abilitySystem = ReviewGateEvidence.ReadSourceWithPartials(Path.Combine(root, "scripts", "core", "sim", "systems", "AbilitySystem.cs"));
        RequireText(abilitySystem, "List<AbilityCooldownState> _cooldownScratch", "AbilitySystem must reuse cooldown scratch storage.", result);
        RequireText(abilitySystem, "private void TickCooldowns(EntityWorld world, float dt)", "AbilitySystem cooldown ticking must use instance scratch storage.", result);
        RequireText(abilitySystem, "private void SetCooldown(", "AbilitySystem cooldown writes must use instance scratch storage.", result);
        RequireText(abilitySystem, "_cooldownScratch.Add(cooldown with { CooldownRemaining = next })", "AbilitySystem cooldown ticking must fill the scratch buffer.", result);
        RequireText(abilitySystem, "_cooldownScratch.Add(new AbilityCooldownState(kind, seconds))", "AbilitySystem new cooldown writes must fill the scratch buffer.", result);
        ForbidText(abilitySystem, "runtime.Cooldowns.ToArray()", "AbilitySystem cooldown paths must not copy runtime cooldowns before mutation.", result);
        ForbidText(abilitySystem, "Append(new AbilityCooldownState", "AbilitySystem cooldown writes must not use LINQ Append.", result);
        ForbidText(abilitySystem, "runtime.Cooldowns.Any(", "AbilitySystem cooldown checks must not use LINQ Any.", result);
    }
}
