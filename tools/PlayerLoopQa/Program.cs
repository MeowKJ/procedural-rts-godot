using Godot;
using ProceduralRts.Core;

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void Advance(UnitBattlefield battlefield, float seconds, float step = 0.1f)
{
    for (var elapsed = 0f; elapsed < seconds; elapsed += step)
    {
        battlefield.Update(step);
    }
}

static UnitBattlefield NewBattlefield(int credits = 12000)
{
    var battlefield = new UnitBattlefield
    {
        WorldSize = MatchConfig.DefaultWorldSize,
    };
    battlefield.Relations.Set(PlayerSlotId.One, PlayerSlotId.Two, PlayerRelation.Hostile);
    battlefield.SetCredits(PlayerSlotId.One, credits);
    battlefield.SetCredits(PlayerSlotId.Two, credits);
    return battlefield;
}

static UnitBattlefieldBuildingSnapshot AddBuilding(
    UnitBattlefield battlefield,
    int id,
    string kind,
    PlayerSlotId playerSlotId,
    UnitFactionId faction,
    Vector2 position,
    float facing = 0,
    float? hp = null)
{
    var spec = BuildSpecCatalog.For(kind);
    return battlefield.UpsertBuildingTarget(
        id,
        kind,
        playerSlotId,
        faction,
        position,
        facing,
        hp ?? spec.MaxHp);
}

static ResourceFieldModel ResourceField(int id, Vector2 position)
{
    return new ResourceFieldModel
    {
        Id = id,
        Position = position,
        Radius = 64,
        MaxAmount = 8000,
        Amount = 8000,
        Accent = new Color("#f6c55c"),
    };
}

static UnitBattlefieldBuildingSnapshot? TryConstructInBuildRadius(
    UnitBattlefield battlefield,
    string kind,
    PlayerSlotId playerSlotId,
    UnitFactionId faction,
    Vector2 anchor,
    out string status,
    float facing = 0)
{
    var offsets = new[]
    {
        new Vector2(280, 0),
        new Vector2(0, 260),
        new Vector2(-260, 0),
        new Vector2(220, 220),
        new Vector2(-220, 220),
        new Vector2(320, -180),
        new Vector2(-320, -180),
    };

    foreach (var offset in offsets)
    {
        if (battlefield.ConstructBuilding(playerSlotId, faction, kind, anchor + offset, out var building, out status, facing))
        {
            return building;
        }
    }

    status = "no valid construction position";
    return null;
}

static void AssertBuildInRadius()
{
    var battlefield = NewBattlefield(20000);
    var hq = AddBuilding(battlefield, 1, BuildingDesignIds.Headquarters, PlayerSlotId.One, UnitFactionId.Dog, new Vector2(720, 760));
    AddBuilding(battlefield, 2, BuildingDesignIds.PowerPlant, PlayerSlotId.One, UnitFactionId.Dog, new Vector2(900, 760));
    var creditsBefore = battlefield.Credits(PlayerSlotId.One);
    var facing = Mathf.Pi * 0.5f;
    var placed = TryConstructInBuildRadius(
        battlefield,
        BuildingDesignIds.VehicleFactory,
        PlayerSlotId.One,
        UnitFactionId.Dog,
        hq.Position,
        out var status,
        facing);

    Require(placed is not null, $"player loop should start construction inside build radius: {status}");
    Require(placed!.Value.PlayerSlotId == PlayerSlotId.One && placed.Value.Kind == BuildingDesignIds.VehicleFactory, "construction should preserve owner and kind");
    Require(Mathf.IsEqualApprox(placed.Value.Facing, facing), $"direct construction should preserve requested facing {facing}, got {placed.Value.Facing}");
    Require(battlefield.Credits(PlayerSlotId.One) == creditsBefore - BuildSpecCatalog.For(BuildingDesignIds.VehicleFactory).Cost, "construction should spend credits immediately");
    Require(battlefield.BuildingBuildProgress(placed.Value.Id) < 1, "constructed building should start under construction");

    Advance(battlefield, BuildSpecCatalog.For(BuildingDesignIds.VehicleFactory).BuildTime + 0.5f);
    Require(battlefield.BuildingBuildProgress(placed.Value.Id) >= 1, "construction should complete through the shared ConstructionSystem");
}

static void AssertCatReadyTicketPlacement()
{
    var battlefield = NewBattlefield(20000);
    var hq = AddBuilding(battlefield, 1, BuildingDesignIds.Headquarters, PlayerSlotId.One, UnitFactionId.Cat, new Vector2(720, 760));
    var powerSpec = BuildSpecCatalog.For(BuildingDesignIds.PowerPlant);
    var creditsBefore = battlefield.Credits(PlayerSlotId.One);
    var ticket = battlefield.QueueConstructionTicket(PlayerSlotId.One, BuildingDesignIds.PowerPlant, out var queueStatus);
    Require(ticket is not null, $"player loop should queue cat ready-ticket construction: {queueStatus}");
    var ticketValue = ticket ?? throw new InvalidOperationException(queueStatus);
    var creditsAfterQueue = battlefield.Credits(PlayerSlotId.One);
    Require(creditsAfterQueue == creditsBefore - powerSpec.Cost,
        "cat ready-ticket queue should reserve cost once");

    Advance(battlefield, powerSpec.BuildTime + 0.2f);
    var ready = battlefield.ReadyConstructionTickets(PlayerSlotId.One).SingleOrDefault(item => item.EntityId == ticketValue.EntityId);
    Require(ready.ReadyToPlace, "cat ready-ticket should become ready-to-place through the live ConstructionSystem");

    var invalidAccepted = battlefield.PlaceReadyConstructionTicket(
        PlayerSlotId.One,
        UnitFactionId.Cat,
        ready.EntityId,
        hq.Position + new Vector2(1000, 0),
        out _,
        out var invalidStatus);
    Require(!invalidAccepted, $"invalid ready-ticket placement should reject, got {invalidStatus}");
    Require(battlefield.ReadyConstructionTickets(PlayerSlotId.One).Any(item => item.EntityId == ready.EntityId),
        "invalid ready-ticket placement should leave the ticket available");
    Require(battlefield.Credits(PlayerSlotId.One) == creditsAfterQueue, "invalid ready-ticket placement should not spend again");
    var expectedRefund = Mathf.RoundToInt(powerSpec.Cost * powerSpec.RefundRatio);
    Require(battlefield.CancelConstructionTicket(PlayerSlotId.One, ready.EntityId, out var cancelStatus),
        $"player loop should cancel a ready construction ticket: {cancelStatus}");
    Require(cancelStatus.Contains($"+{expectedRefund}", StringComparison.Ordinal),
        $"ready-ticket cancel status should show refund {expectedRefund}, got {cancelStatus}");
    Require(!battlefield.ReadyConstructionTickets(PlayerSlotId.One).Any(item => item.EntityId == ready.EntityId),
        "cancelled ready-ticket should be removed immediately");
    Require(battlefield.Credits(PlayerSlotId.One) == creditsAfterQueue + expectedRefund,
        "cancelled ready-ticket should refund according to the build spec refund ratio");

    var secondTicket = battlefield.QueueConstructionTicket(PlayerSlotId.One, BuildingDesignIds.PowerPlant, out var secondQueueStatus);
    Require(secondTicket is not null, $"player loop should queue a second cat ready-ticket after cancel: {secondQueueStatus}");
    var secondTicketValue = secondTicket ?? throw new InvalidOperationException(secondQueueStatus);
    var creditsAfterSecondQueue = battlefield.Credits(PlayerSlotId.One);
    Advance(battlefield, powerSpec.BuildTime + 0.2f);
    ready = battlefield.ReadyConstructionTickets(PlayerSlotId.One).SingleOrDefault(item => item.EntityId == secondTicketValue.EntityId);
    Require(ready.ReadyToPlace, "second cat ready-ticket should become ready-to-place after the cancel refund path");

    var facing = Mathf.Pi;
    var accepted = battlefield.PlaceReadyConstructionTicket(
        PlayerSlotId.One,
        UnitFactionId.Cat,
        ready.EntityId,
        hq.Position + new Vector2(280, 0),
        out var placed,
        out var status,
        facing);
    Require(accepted && placed is not null, $"player loop should place ready construction ticket: {status}");
    Require(Mathf.IsEqualApprox(placed!.Value.Facing, facing), $"ready-ticket placement should preserve requested facing {facing}, got {placed.Value.Facing}");
    Require(!battlefield.ReadyConstructionTickets(PlayerSlotId.One).Any(item => item.EntityId == ready.EntityId),
        "successful ready-ticket placement should consume the ticket");
    Require(battlefield.Credits(PlayerSlotId.One) == creditsAfterSecondQueue, "ready-ticket placement should not spend a second time");
    Require(battlefield.BuildingBuildProgress(placed!.Value.Id) >= 1, "ready-ticket placement should create a complete building");
}

static void AssertHarvestAndBank()
{
    var battlefield = NewBattlefield();
    var field = ResourceField(1, new Vector2(720, 650));
    battlefield.SetResourceFields([field]);
    AddBuilding(battlefield, 1, BuildingDesignIds.Refinery, PlayerSlotId.One, UnitFactionId.Dog, new Vector2(620, 700));
    var harvester = battlefield.Spawn("dog.harvester", PlayerSlotId.One, new Vector2(690, 665));
    battlefield.SelectUnitsByIds(PlayerSlotId.One, [harvester.Id]);

    var creditsBefore = battlefield.Credits(PlayerSlotId.One);
    Require(battlefield.CommandHarvestSelected(PlayerSlotId.One, field, out var harvestStatus), $"player loop should accept harvest command: {harvestStatus}");
    Advance(battlefield, 28);

    Require(battlefield.Credits(PlayerSlotId.One) > creditsBefore, "player loop should bank credits after harvesting");
    Require(field.Amount < field.MaxAmount, "player loop harvesting should reduce the selected resource node");
}

static void AssertProductionRallyAndTiers()
{
    var battlefield = NewBattlefield(20000);
    var rally = new Vector2(1060, 860);
    var barracks = AddBuilding(battlefield, 1, BuildingDesignIds.Barracks, PlayerSlotId.One, UnitFactionId.Dog, new Vector2(720, 760));
    var factoryA = AddBuilding(battlefield, 2, BuildingDesignIds.VehicleFactory, PlayerSlotId.One, UnitFactionId.Dog, new Vector2(860, 760));
    var factoryB = AddBuilding(battlefield, 3, BuildingDesignIds.VehicleFactory, PlayerSlotId.One, UnitFactionId.Dog, new Vector2(1000, 760));
    AddBuilding(battlefield, 4, BuildingDesignIds.Airfield, PlayerSlotId.One, UnitFactionId.Dog, new Vector2(1140, 760));
    AddBuilding(battlefield, 5, BuildingDesignIds.Headquarters, PlayerSlotId.One, UnitFactionId.Dog, new Vector2(560, 760));

    battlefield.SetBuildingTargetSelected(barracks.Id, true);
    battlefield.SetBuildingTargetSelected(factoryA.Id, true);
    battlefield.SetBuildingTargetSelected(factoryB.Id, true);
    Require(battlefield.SetSelectedBuildingRallyPoints(PlayerSlotId.One, rally, out var rallyStatus), $"player loop should set producer rally points: {rallyStatus}");

    var initialIds = battlefield.Units.Select(unit => unit.Id).ToHashSet();
    var designs = new[] { "dog.infantry", "dog.assault_tank", "dog.siege_artillery" };
    foreach (var design in designs)
    {
        Require(battlefield.EnqueueProductionDesign(design, PlayerSlotId.One, out var status), $"player loop should enqueue {design}: {status}");
    }

    Advance(battlefield, 16);
    var produced = battlefield.Units
        .Where(unit => !initialIds.Contains(unit.Id))
        .OrderBy(unit => unit.Id)
        .ToList();

    foreach (var design in designs)
    {
        Require(produced.Any(unit => unit.Spec.Id == design), $"player loop should complete production for {design}");
    }

    Require(produced.Select(unit => unit.Spec.Stats.TechTier).ToHashSet().IsSupersetOf([1, 2, 3]), "player loop should train T1, T2, and T3 units");
    Require(produced.Any(unit => unit.MoveTarget is not null || unit.CommandVisualTarget is not null || unit.Position.DistanceTo(rally) < 160),
        "produced units should receive or approach the selected rally point");
}

static void AssertSelectionCommandsAndStance()
{
    var battlefield = NewBattlefield();
    var units = new[]
    {
        battlefield.Spawn("dog.guard_tank", PlayerSlotId.One, new Vector2(500, 500)),
        battlefield.Spawn("dog.guard_tank", PlayerSlotId.One, new Vector2(560, 500)),
        battlefield.Spawn("dog.rocket", PlayerSlotId.One, new Vector2(620, 500)),
    };
    var enemy = AddBuilding(battlefield, 10, BuildingDesignIds.Headquarters, PlayerSlotId.Two, UnitFactionId.Cat, new Vector2(900, 500), Mathf.Pi, hp: 260);

    var selected = battlefield.SelectRect(PlayerSlotId.One, new Rect2(460, 455, 220, 100), additive: false);
    Require(selected == units.Length, "player loop should group-select combat units with a selection rectangle");

    var changed = battlefield.CommandSetSelectedStance(PlayerSlotId.One, UnitStance.Hold);
    Require(changed == units.Length && units.All(unit => unit.Stance == UnitStance.Hold), "player loop should change selected unit stance");

    var moveTarget = new Vector2(700, 540);
    battlefield.CommandMoveSelected(PlayerSlotId.One, moveTarget, MatchConfig.DefaultWorldSize);
    Advance(battlefield, 1.5f);
    Require(units.Any(unit => unit.Position.DistanceTo(moveTarget) < 180 || unit.MoveTarget is not null), "player loop should move selected units toward command target");

    battlefield.SelectUnitsByIds(PlayerSlotId.One, units.Select(unit => unit.Id));
    var hpBefore = enemy.Hp;
    battlefield.CommandAttackSelected(PlayerSlotId.One, enemy.Id);
    Advance(battlefield, 8);
    Require((battlefield.BuildingSnapshot(enemy.Id)?.Hp ?? 0) < hpBefore, "player loop should damage an attacked enemy building");
}

static void AssertLiveSharedCorridorPathing()
{
    const float cell = 64f;
    var battlefield = NewBattlefield();
    battlefield.WorldSize = new Vector2(768, 512);
    var units = new[]
    {
        battlefield.Spawn("dog.infantry", PlayerSlotId.One, new Vector2(96, 128)),
        battlefield.Spawn("dog.infantry", PlayerSlotId.One, new Vector2(96, 208)),
        battlefield.Spawn("dog.infantry", PlayerSlotId.One, new Vector2(96, 304)),
        battlefield.Spawn("dog.infantry", PlayerSlotId.One, new Vector2(96, 384)),
    };

    for (var y = 1; y <= 5; y++)
    {
        AddBuilding(
            battlefield,
            100 + y,
            BuildingDesignIds.GroundTurret,
            PlayerSlotId.Two,
            UnitFactionId.Cat,
            new Vector2((4.5f) * cell, (y + 0.5f) * cell));
    }

    battlefield.SelectUnitsByIds(PlayerSlotId.One, units.Select(unit => unit.Id));
    battlefield.CommandMoveSelected(PlayerSlotId.One, new Vector2(608, 280), battlefield.WorldSize);
    battlefield.Update(0.1);

    var paths = units
        .Select(unit => battlefield.EntityWorld.TryGet(unit.EntityId, out var entity)
            && entity.Components.TryGet<PathfindingComponentState>(out var path)
                ? path
                : null)
        .Where(path => path is not null)
        .Select(path => path!)
        .ToList();

    Require(paths.Count == units.Length, $"live group move should create pathfinding state for all selected units, got {paths.Count}");
    Require(CountSharedInteriorWaypoints(paths) >= 3, "live group move should reuse a shared corridor spine");
    Require(paths.All(path => path.Waypoints.All(point => !BlockedWallPoint(point))), "live group move shared corridor should avoid static building blockers");
}

static int CountSharedInteriorWaypoints(IReadOnlyList<PathfindingComponentState> paths)
{
    return paths
        .SelectMany(path => path.Waypoints.Take(Math.Max(0, path.Waypoints.Count - 1)))
        .GroupBy(point => (X: MathF.Round(point.X), Y: MathF.Round(point.Y)))
        .Select(group => group.Count())
        .DefaultIfEmpty(0)
        .Max();
}

static bool BlockedWallPoint(PathPoint point)
{
    return MathF.Floor(point.X / 64f) == 4 && MathF.Floor(point.Y / 64f) is >= 1 and <= 5;
}

static void AssertVictoryAndDefeat()
{
    var victory = NewBattlefield();
    var attackers = new[]
    {
        victory.Spawn("dog.siege_artillery", PlayerSlotId.One, new Vector2(700, 700)),
        victory.Spawn("dog.siege_artillery", PlayerSlotId.One, new Vector2(740, 730)),
    };
    var enemyHq = AddBuilding(victory, 20, BuildingDesignIds.Headquarters, PlayerSlotId.Two, UnitFactionId.Cat, new Vector2(830, 720), Mathf.Pi, hp: 90);
    victory.SelectUnitsByIds(PlayerSlotId.One, attackers.Select(unit => unit.Id));
    victory.CommandAttackSelected(PlayerSlotId.One, enemyHq.Id);
    Advance(victory, 12);
    Require(victory.Outcome == GameOutcome.Victory, "player loop should win after destroying enemy HQ");

    var defeat = NewBattlefield();
    var removedBuildings = new List<string>();
    var outcomeEvents = new List<GameOutcome>();
    defeat.BuildingsRemoved += deaths => removedBuildings.AddRange(deaths.Select(death => $"{death.Id}:{death.Kind}:{death.PlayerSlotId}"));
    defeat.OutcomeChanged += outcomeEvents.Add;
    var enemyAttackers = new[]
    {
        defeat.Spawn("cat.crescent_artillery", PlayerSlotId.Two, new Vector2(820, 730), Mathf.Pi),
        defeat.Spawn("cat.crescent_artillery", PlayerSlotId.Two, new Vector2(860, 760), Mathf.Pi),
    };
    var playerHq = AddBuilding(defeat, 30, BuildingDesignIds.Headquarters, PlayerSlotId.One, UnitFactionId.Dog, new Vector2(720, 740), hp: 90);
    defeat.SelectUnitsByIds(PlayerSlotId.Two, enemyAttackers.Select(unit => unit.Id));
    defeat.CommandAttackSelected(PlayerSlotId.Two, playerHq.Id);
    Advance(defeat, 12);
    var playerHqAfterAttack = defeat.BuildingSnapshot(playerHq.Id);
    Require(defeat.Outcome == GameOutcome.Defeat,
        $"player loop should lose after own HQ falls; outcome {defeat.Outcome}, hq hp {playerHqAfterAttack?.Hp:0.0}, removed [{string.Join(", ", removedBuildings)}], outcome events [{string.Join(", ", outcomeEvents)}]");
}

static void AssertCommandGatewayLivePlayerLoop()
{
    var battlefield = NewBattlefield(20000);
    var localUnit = battlefield.Spawn("dog.guard_tank", PlayerSlotId.One, new Vector2(480, 520));
    battlefield.Spawn("cat.basic", PlayerSlotId.Two, new Vector2(980, 520), Mathf.Pi);
    AddBuilding(battlefield, 20, BuildingDesignIds.Barracks, PlayerSlotId.One, UnitFactionId.Dog, new Vector2(650, 700));
    battlefield.SelectUnitsByIds(PlayerSlotId.One, [localUnit.Id]);

    var observation = battlefield.CreateObservationView(PlayerSlotId.One, tick: 0);
    Require(observation.IsValid, "live observation should be valid for the player slot");
    Require(observation.KnownPlayers.Any(player => player.SlotId == PlayerSlotId.One && player.Credits == 20000),
        "live observation should include known player credits");
    Require(observation.VisibleEntities.Any(entity => entity.Id == localUnit.EntityId && entity.IsOwnedByViewer),
        "live observation should include owned visible entities");
    Require(observation.CommandAffordances.Any(affordance => affordance.Kind == PlayerCommandKind.Move && affordance.IsAvailable),
        "live observation should expose command affordances");

    var controller = new BufferedLocalPlayerController(new PlayerControllerId("qa-local-human"), [PlayerSlotId.One]);
    var localGateway = new CommandGateway();
    var move = new PlayerCommand(
        PlayerSlotId.One,
        1,
        1,
        PlayerCommandKind.Move,
        PlayerCommandPayload.ForPoint([localUnit.EntityId], 720, 560, MoveCommandMode.Ignore));
    controller.Enqueue(move);
    var beforeAccepted = battlefield.AppliedInputCommandCount;
    var localResult = battlefield.SubmitPlayerController(localGateway, controller, PlayerSlotId.One, tick: 1);
    Require(localResult.AcceptedCount == 1 && battlefield.AppliedInputCommandCount > beforeAccepted,
        "local buffered controller should submit live commands through CommandGateway into the battlefield sink");
    Require(localUnit.MoveTarget is not null && localUnit.MoveMode == MoveCommandMode.Ignore,
        "accepted live move should preserve command mode and mutate the unit through the command bridge");

    var beforeRejected = battlefield.AppliedInputCommandCount;
    controller.Enqueue(move);
    var staleResult = battlefield.SubmitPlayerController(localGateway, controller, PlayerSlotId.One, tick: 2);
    RequireRejected(staleResult, CommandGatewayValidationError.NonMonotonicSequence, "duplicate live controller sequence should reject");
    Require(battlefield.AppliedInputCommandCount == beforeRejected, "duplicate live sequence should not mutate simulation state");

    var invalidMove = new PlayerCommand(
        PlayerSlotId.One,
        2,
        2,
        PlayerCommandKind.Move,
        PlayerCommandPayload.ForSubjects([localUnit.EntityId]));
    controller.Enqueue(invalidMove);
    var invalidResult = battlefield.SubmitPlayerController(localGateway, controller, PlayerSlotId.One, tick: 3);
    RequireRejected(invalidResult, CommandGatewayValidationError.InvalidPayloadShape, "malformed live move should reject");
    Require(battlefield.AppliedInputCommandCount == beforeRejected, "malformed live move should not mutate simulation state");

    var unauthorizedGateway = new CommandGateway();
    var unauthorizedSubmission = new CommandGatewaySubmission(
        new PlayerControllerId("qa-authority"),
        PlayerControllerKind.QaAgent,
        [PlayerSlotId.One],
        CurrentTick: 3);
    var unauthorized = new PlayerCommand(
        PlayerSlotId.Two,
        1,
        3,
        PlayerCommandKind.Stop,
        PlayerCommandPayload.ForSubjects([localUnit.EntityId]));
    var unauthorizedResult = unauthorizedGateway.Submit(unauthorizedSubmission, [unauthorized], battlefield);
    RequireRejected(unauthorizedResult, CommandGatewayValidationError.ControllerDoesNotOwnSlot, "unauthorized live slot should reject");
    Require(battlefield.AppliedInputCommandCount == beforeRejected, "unauthorized live slot should not mutate simulation state");

    var agentBattlefield = NewBattlefield();
    var agentUnit = agentBattlefield.Spawn("dog.guard_tank", PlayerSlotId.One, new Vector2(420, 460));
    var agentController = new AgentPlayerController(
        new PlayerControllerId("qa-scripted-agent"),
        new MoveFirstOwnedUnitAgent(),
        [PlayerSlotId.One]);
    var agentResult = agentBattlefield.SubmitPlayerController(new CommandGateway(), agentController, PlayerSlotId.One, tick: 1);
    Require(agentResult.AcceptedCount == 1 && agentUnit.MoveTarget is not null,
        "scripted agent should consume ObservationView and command the battlefield through the same gateway path");

    var productionBattlefield = NewBattlefield(20000);
    AddBuilding(productionBattlefield, 30, BuildingDesignIds.Barracks, PlayerSlotId.One, UnitFactionId.Dog, new Vector2(720, 760));
    Require(productionBattlefield.TryCreateProductionDesignPayload("dog.infantry", PlayerSlotId.One, out var productionPayload, out var productionStatus),
        $"live production payload should resolve producer and spec: {productionStatus}");
    var productionResult = productionBattlefield.SubmitLiveLocalPlayerCommand(PlayerSlotId.One, PlayerCommandKind.Produce, productionPayload);
    Require(productionResult.AcceptedCount == 1 && productionBattlefield.HasQueuedProduction(PlayerSlotId.One),
        "live production command should pass through CommandGateway before queueing production");

    var buildBattlefield = NewBattlefield(20000);
    AddBuilding(buildBattlefield, 40, BuildingDesignIds.Headquarters, PlayerSlotId.One, UnitFactionId.Dog, new Vector2(720, 760));
    var buildPayload = PlayerCommandPayload.ForSpec(BuildingDesignIds.PowerPlant) with
    {
        HasTargetPoint = true,
        TargetPoint = new PlayerCommandPoint(940, 760),
    };
    var buildResult = buildBattlefield.SubmitLiveLocalPlayerCommand(PlayerSlotId.One, PlayerCommandKind.Build, buildPayload);
    Advance(buildBattlefield, 0.2f);
    Require(buildResult.AcceptedCount == 1
        && buildBattlefield.BuildingSnapshots().Any(building => building.Kind == BuildingDesignIds.PowerPlant),
        "live build command should pass through CommandGateway before reaching ConstructionSystem");
}

static void RequireRejected(CommandGatewayResult result, CommandGatewayValidationError expected, string message)
{
    Require(result.RejectedCount == 1, message);
    Require(result.Commands[0].Error == expected, $"{message}: expected {expected}, got {result.Commands[0].Error}");
    Require(!string.IsNullOrWhiteSpace(result.Commands[0].Message), $"{message}: rejection should carry structured feedback");
}

AssertBuildInRadius();
AssertCatReadyTicketPlacement();
AssertHarvestAndBank();
AssertProductionRallyAndTiers();
AssertSelectionCommandsAndStance();
AssertLiveSharedCorridorPathing();
AssertVictoryAndDefeat();
AssertCommandGatewayLivePlayerLoop();

Console.WriteLine("PlayerLoopQa PASSED: build radius, cat ready-ticket placement, harvest/bank, T1-T3 production, rally, selection, shared corridor, move/attack/stance, victory/defeat, and live CommandGateway player loop.");

sealed class MoveFirstOwnedUnitAgent : IPlayerAgent
{
    private int _sequence;

    public PlayerAgentId Id { get; } = new("qa-move-first-owned");
    public PlayerAgentKind Kind => PlayerAgentKind.Qa;

    public PlayerControllerResult Think(in ObservationView observation)
    {
        foreach (var entity in observation.VisibleEntities)
        {
            if (entity.Kind != EntityKind.Unit || !entity.IsOwnedByViewer)
            {
                continue;
            }

            _sequence++;
            var command = new PlayerCommand(
                observation.ViewerSlotId,
                _sequence,
                observation.Tick + 1,
                PlayerCommandKind.Move,
                PlayerCommandPayload.ForPoint([entity.Id], entity.PositionX + 96, entity.PositionY));
            return new PlayerControllerResult([command]);
        }

        return PlayerControllerResult.Empty;
    }
}
