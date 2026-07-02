# Procedural RTS - Master Plan

> File rules: UTF-8, ASCII-only where possible. `[x]` done, `[ ]` open - no Unicode checkboxes.
> Full historical backlog (645 lines, all completed milestones) is archived in
> `docs/TODO-Archive-2026-06-29.md`. This file is the living, de-duplicated plan.
>
> GitHub issue workflow: active open work is now mirrored as private repository
> issues at `https://github.com/MeowKJ/procedural-rts-godot/issues`. Use issues
> for new agent assignments, progress comments, verification evidence, and close
> decisions. Keep this file as the planning source/snapshot until a later cleanup
> removes duplicated completed detail.

## Vision - The Most Elegant RTS Framework

We are building a desktop RTS in Godot 4.7 Mono + C# whose value is not any single
feature but a framework so clean that new content is cheap, behavior is provably
correct, and the game feels precise. Seven pillars, each with a concrete test:

1. Most elegant framework - one entity language (Entity/Component/System/Command/
   Spec/View/Event), no runtime inheritance, no god objects. Test: a building, a
   turret, a unit, a resource node, a projectile all describe through the same
   `EntitySpec` + components.
2. Most elegant code - Simulation is Godot-free and deterministic; Presentation
   only reads projections and submits commands. Test: the whole sim runs headless
   in `tools/*` with zero Godot nodes.
3. Most AI-test-friendly - every system is a pure `ISimSystem` over data, driven by
   a command log on a fixed tick. Test: same seed + same commands => identical
   `EntityStateHash` for thousands of ticks (replay/rollback ready).
4. Strongest algorithms - flow-field/corridor group movement, ORCA-style soft
   avoidance + positional separation, range-ring attack slotting, broadphase
   spatial grids. Test: 30 units attack one target without clumping; ranged hold
   at range; firing units are never shoved.
5. Strongest performance - fixed 30Hz sim, near-linear systems, pooled VFX,
   culled/dirty rendering, throttled fog. Test: 200+ units at 60 FPS / 1080p;
   `tools/PerfSmoke` gates sim-step ms.
6. Best feel - intent vs slot separation, soft arrival, target stickiness,
   graceful degradation, crisp command feedback. Test: command-feel metrics
   (time-to-first-shot, arrival jitter, compactness) stay in band.
7. Most elegant UI - Soft Old City tactical-map aesthetic, edge-docked low-
   obstruction HUD, owner color separate from faction identity, day/fog/night
   tone without layout change. Test: readable at 1280/1600/1920 + high-DPI.

## AI Collaboration & Review Gates

All TODO work now follows `docs/AICollaborationProtocol.md`.

Required rule: no item is marked `[x]` until an Owner AI has implemented a bounded
scope, a separate Reviewer AI has reviewed the touched subsystem, and the Integrator
AI has run the automated gates for that work type.

Per-step contract:
- Step / Owner / Reviewer / Scope / Non-goals.
- Automated gates: build, replay, smoke, ReviewGate, or visual QA as appropriate.
- Done evidence: current command output, screenshot, replay hash, or reviewed file
  references.

Default gate commands:
- Architecture/sim: `dotnet build ProceduralRts.csproj`,
  `dotnet run --project tools/SimReplay/SimReplay.csproj`,
  `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj`.
- Performance: `dotnet run --project tools/PerfSmoke/PerfSmoke.csproj`,
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj presentation
  --max-warnings=<accepted-baseline>`.
- Fog/vision: `dotnet run --project tools/FogOfWarQa/FogOfWarQa.csproj`.
- TODO/review process: `dotnet run --project tools/ReviewGate/ReviewGate.csproj`.
  Use `dotnet run --project tools/ReviewGate/ReviewGate.csproj review` to verify
  persistent review records. Use
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj review
  --require-record=<slice-name>` for each implemented TODO slice.

Current review-gate baseline:
- `ReviewGate` fails unknown modes.
- `ReviewGate review` requires concrete records in `docs/reviews/*.md`.
- `ReviewGate review --require-record=<slice-name>` proves the current slice has a
  specific durable review record.
- `ReviewGate presentation --max-warnings=0` is the current presentation baseline;
  future performance slices must reduce or justify this number.

## Architecture (locked) - Simulation / Presentation / Authoring

```
ProceduralRts.Core (Simulation)   Godot-free authority. Deterministic.
  EntityWorld          entities + spec registry + system pipeline + RNG +
                       relations + visibility + events + metrics + outcome
  EntitySpec/Instance  Spec is immutable per match; runtime = SpecId + OwnerId +
                       Transform + ComponentState (no inheritance)
  Components           Health, Selectable, Commandable, Movement, MovementProfile,
                       Collision, Vision, WeaponUser, Stance, Harvester,
                       ResourceCargo, ProductionQueue, Construction, Power, Dock,
                       RallyPoint, Footprint, BuildRadius, Objective,
                       PresentationPulse
  Systems (ISimSystem) Command -> Combat -> Movement -> Separation -> Vision ->
                       Outcome  (registered in stable order, iterate stable
                       EntityId order, no Godot)
  SimClock             fixed 30Hz; real delta -> integer ticks
  EntityCommand(Buffer)Move/AttackMove/Attack/Stop/Hold/SetStance + Group variants
  DeterministicRng     SplitMix64, folded into state hash
  Sim*Event/Sink       sim -> view, read-only
  EntityProjection     the ONLY way views read entity state

ProceduralRts (Presentation)   Godot nodes. Reads projections, submits commands.
  BattleRoot           drives SimClock + EntityWorld; mirrors relations
  *View / *Layer       draw from projections/events; never mutate authority

Authoring                       Writes data, shallow inheritance only.
  UnitDesign -> UnitSpec -> EntitySpec (via UnitSpecEntityBridge)
```

Hard boundaries that must never regress:
[x] Simulation never references Godot Node/SceneTree/_Process/real time as authority.
[x] Views never mutate authoritative health/movement/target/queue/economy/outcome.
[x] Faction identity never decides runtime hostility - only `OwnerRelationTable`.
[x] Relation colors live in overlays (selection/health/minimap/target), never in entity body art.
[x] Pure-presentation effects (tracers, dust, flashes) are pooled, not full entities.

## Status - What The Sim Core Already Proves

The deterministic core exists and is verified headless (`tools/SimReplay` 12 checks,
`tools/PerfSmoke` baseline, `tools/CombatBehavior`, `tools/SimulationSmoke`):

[x] Fixed-tick clock, command buffer, deterministic RNG, event sink, relations, projection.
[x] Systems: Command, Combat (acquire/rotate/standoff/seeded damage/death), Movement
    (soft avoidance), Separation (positional, firing anchors), Vision (broadphase +
    per-owner), Outcome (generic victory-critical).
[x] Group commands decomposed into formation slots (move) and range-ring slots (attack).
[x] Authored dog/cat units flow through generic systems with zero unit-specific code.
[x] Replay determinism: same seed + commands => identical state hash (movement, combat,
    authored, group-move, group-attack, outcome scenarios).
[x] Marquee feel: 30-unit move no-clump (transit min sep 23.6px), 30-unit attack ring
    (0 center-stacked, all in firing band), ranged standoff.
[x] Perf baseline + regression gate; VisionSystem broadphase (400u 10.3->8.4ms).

Note: `EntityWorld` runs in `BattleRoot` as a non-authoritative shadow today; the live
game still renders from `UnitBattlefield`/`GameState`. Making it authoritative is the
next phase.

## Deliverable - Playable 1v1 Skirmish (Vertical Slice)

The goal of this TODO is one shippable thing: a satisfying 1v1 skirmish against the
computer. Everything in the roadmap and design sections serves this. Scope is
deliberately locked small so it can be finished, elegant, fast, and fun.

Scope locks (do not expand without a decision):
[x] Mode: 1v1 only - one human player vs one computer AI. No multiplayer netcode yet
    (but the deterministic command core keeps it possible later).
[x] Factions: Dog and Cat fully playable; a third faction exists only as a locked
    placeholder (enum slot + greyed UI), no content.
    Progress: UnitDesign runtime faction-select slice fixed `BattleRoot` so the
    live `UnitBattlefield` starting units come from selected skirmish factions
    instead of hard-coded Dog player / Cat AI defaults. `SkirmishFlowQa` now selects
    Cat player vs Dog AI and verifies both legacy `GameState` loadouts and new
    UnitDesign runtime starting design ids. `ReviewGate runtimefactionselect`
    prevents this regression from returning. The broad faction item remains open
    until Dog/Cat playable completeness is proven together with the vertical-slice
    player/AI/counter/readability gates.
    Progress: Faction start bridge slice aligned legacy `FactionCatalog.StartingUnits`
    with the UnitDesign runtime starting rosters: Dog and Cat now both seed seven
    equivalent units through old `UnitKind` compatibility and new design-id runtime
    paths. `CombatBehavior` projects FactionCatalog starts through
    `UnitKindDesignBridge` and proves they match `UnitDesignRuntimeLoadouts`;
    `ReviewGate factionstartbridge` locks the compatibility bridge.
    [x] Faction start UnitDesign loadout cleanup:
        Deleted duplicate legacy starting-unit lists from `FactionCatalog` /
        `FactionDefinition`. `MatchStartLoadouts` should source starting unit
        design ids from `UnitDesignRuntimeLoadouts`, while old `GameState`
        converts those ids through `UnitKindDesignBridge.KindForDesignId(...)`
        only at the compatibility edge. Gates passed: `CombatBehavior`,
        `ReviewGate startloadout`, `ReviewGate factionstartbridge`, `SimReplay`,
        full `ReviewGate`, review-record gate, and grouped `VerifyAll` 23/23.
    Final gate: `ReviewGate playablefactions` aggregates `rosterauthoringqa`,
    `runtimefactionselect`, `factionstartbridge`, `playerloopqa`,
    `aiopponentloop`, `counterreadability`, `activebattleperf`, and
    `softoldcity`; `VerifyAll` runs roster/player/AI/counter/HUD/skirmish/perf
    proof so Dog/Cat are playable and Corruption remains a locked no-content
    placeholder.
[x] Third faction placeholder: `FactionId.Corruption` exists as an enum-only locked
    slot, appears greyed/disabled in skirmish setup, and is not registered in
    `FactionCatalog` content.
[x] Faction select: player picks Dog or Cat at skirmish setup; AI takes the other (or
    same, for mirror). Relation is owner-based, never faction-based.
[x] Tiers: T1, T2, T3 only. No super-units, no experimental tier.
[x] Unit classes this slice: Light (infantry-style), Tank (vehicle), Aircraft, plus
    Harvester (economy). Ships/Naval are designed on paper but NOT built this slice.
[x] Structures: HQ, Power, Refinery, Barracks (light), Factory (tank), Airfield (air).
[x] Turrets: at least one anti-ground and one anti-air defense turret per faction.
[x] Campaign: out of scope (TBD). Skirmish only.

Definition of Done (the slice is "playable" when all pass):
[x] Boot -> main menu -> skirmish setup (faction, map seed, AI difficulty) -> battle.
[x] Player can: build base in build radius, train T1-T3 from producers, harvest and
    bank credits, set rally, group-select, move/attack/stance, win by destroying enemy
    HQ / lose if own HQ falls.
    Progress: player build-radius slice added owner-aware live placement validation
    in `GameState`, routed `BuildPlacementController` preview/click placement through
    `PlaceBuildingWithinBuildRadius`, localized build-radius failure reasons, and
    added `ReviewGate playerbuildradius` plus CombatBehavior proof. The broader
    player loop remains open until training T1-T3, economy, rally, commands, and
    win/loss are proven together as one vertical-slice gate. Follow-up:
    UnitDesign tier-production slice added concrete T1-T3 `ProductionSpec`
    coverage, `ProductionDesignOptionStates`, `EnqueueProductionDesign`, dynamic
    12-slot HUD command buttons, and `ReviewGate playertierproduction` plus
    CombatBehavior proof that Dog/Cat T3 units can be queued by design id.
    Final gate: `tools/PlayerLoopQa` now proves the full player loop in one
    deterministic headless pass: build-radius placement, harvest/bank credits,
    producer rally, T1/T2/T3 UnitDesign production, group selection, move/attack/
    stance commands, victory by destroying enemy HQ, and defeat when the player HQ
    falls. `VerifyAll` runs `PlayerLoopQa`, and `ReviewGate playerloopqa` locks the
    completed contract.
[x] AI opponent harvests, builds, produces a mixed army, defends, and attacks in waves
    - all via the command buffer (no cheating, only sees its `VisibilityIndex`).
    Progress: `UnitBattlefieldEnemyProductionAi` now maintains harvester economy,
    starts required buildings through `ConstructBuilding` / `StartConstructionEntityCommand`,
    and queues concrete UnitDesign outputs for mixed armies. `UnitBattlefieldEnemyAttackWaveAi`
    rebuilds `VisibilityIndex`, filters targets through `IsVisibleTo`, defends visible
    base threats, scouts with command-buffer attack-move when no target is visible, and
    attacks through `CommandAttackUnits`. `tools/AiOpponentLoopQa` proves a 96-second
    runtime loop with harvest assignment, resource depletion, AI base building, mixed
    production, defense hits, repeated waves, player HQ damage, and command bridge
    deltas for harvest/production/waves. `ReviewGate aiopponentloop` locks the static
    no-direct-attack/move-state contract, and `VerifyAll` now runs the AI loop QA.
[x] Counters feel real: light beats nothing-special but is cheap/fast, tanks beat
    structures/vehicles and lose to air-less anti-tank, aircraft beat ground but die to
    anti-air turrets/AA units. Rock-paper-scissors is legible in 1 minute of play.
    Progress: `tools/CounterReadabilityQa` now proves the counter triangle with direct
    data checks (`WeaponTargetProfile`, `MovementDomain`, `ArmorTag`, cost, speed) plus
    seven deterministic 60-second combat cases: light pressure, tank-vs-vehicle,
    tank-vs-structure, rocket-vs-vehicle, aircraft-vs-ground-tank, AA-unit-vs-aircraft,
    and AA-turret-vs-aircraft. `CombatSystem` now rejects manual attack targets that a
    weapon cannot legally hit, so ground cannons cannot be forced to shoot aircraft.
    `BalanceReport`, `ReviewGate counterreadability`, and full `VerifyAll` passed.
[x] Runs at 60 FPS / 1080p with both bases active and a 40+ unit battle; sim under
    budget (`tools/PerfSmoke` green); deterministic (`tools/SimReplay` green).
    Progress: camera/fog perf slice added stable visual delta clamping for pan/zoom and
    dirty-view fog upload checks so camera movement does not cause needless fog redraws.
    `FogOfWarQa`, `PerfSmoke`, and full `VerifyAll` passed, but this remains open until
    an active-base 1080p visual performance gate/manual camera check is recorded.
    Final gate: `ActiveBattlePerfQa` now seeds both active bases plus a 40+ unit fight
    at 1920x1080, writes a screenshot/headless note, and is wired into `VerifyAll`.
    The real-window run passed with 53 live / 53 visible units, frame avg 13.53ms,
    process avg 2.89ms, sim avg 0.02ms, fog 2.23ms / 16 uploads. Headless Godot,
    `PerfSmoke`, `SimReplay`, `ReviewGate activebattleperf`, and
    `ReviewGate review --require-record=active-battle-perf` passed; full `VerifyAll`
    passed 22/22. Rendering changes: movement no longer triggers unit redraws, static
    buildings use dirty signatures, hot vector arcs/strokes were reduced, dense
    grid-floor marks were removed, and project defaults now disable VSync, 2D MSAA,
    and 2D HDR for crisp 2D line art.
[x] Reads as Soft Old City: clear silhouettes, owner color separate from faction, HUD
    edge-docked and low-obstruction, fog/day/night legible.
    Progress: Dog/Cat unit recipes now have heavier Dog silhouettes, slimmer crescent
    Cat silhouettes, small owner-color decal zones, and turret/building line motifs.
    Owner-color decal protection stays centralized in `EntityRenderPalette`. Visual QA
    screenshots for day/fog/dusk were regenerated, but this remains open until full
    faction silhouette, HUD obstruction, and environment readability checks pass together.
    Final gate: `DesktopHudQa` now verifies 1280x720, 1600x900, 1920x1080, and
    high-DPI HUD layout constraints; `VisualQaCapture` regenerated current
    `battle_hud_1280x720.png`, `battle_hud_1600x900.png`,
    `battle_hud_1920x1080.png`, `battle_hud_style1b_fog.png`, and
    `battle_hud_style1c_dusk.png`. `ReviewGate softoldcity` locks canonical
    palette usage, environment-tone rendering, owner-color art layers, Dog/Cat
    shape-language hints, and visual QA coverage. Manual review passed for
    1920x1080, 1280x720, fog, and dusk captures; `ReviewGate review
    --require-record=soft-old-city-readability` passed; full `VerifyAll` passed
    23/23.

## Roadmap & AI Execution Matrix - Open Work (by milestone)

Use this matrix when assigning AI work. One Owner AI implements exactly one slice;
one Reviewer AI audits that slice before TODO status changes.

M1 EntityWorld authority:
- Owner scope: `scripts/core/entities`, `scripts/core/sim`, `scripts/BattleRoot.cs`,
  `scripts/core/units/runtime`.
- Reviewer focus: no duplicate authority, live commands enter `EntityCommandBuffer`,
  projections drive views, legacy paths are behind flags only.
- Gates: build, SimReplay, CombatBehavior, ReviewGate architecture.

M2 Movement and autonomy:
- Owner scope: `scripts/core/sim/systems`, pathing/math helpers, SimReplay scenarios.
- Reviewer focus: group move/attack does not clump, firing anchors are respected,
  explicit player orders are never overwritten by autonomy.
- Gates: SimReplay movement/group/autonomy scenarios, PerfSmoke.

M3-M4 Build, construction, production, economy:
- Owner scope: new pure systems plus authoring specs; avoid HUD rewrites in the same
  slice.
- Reviewer focus: queues and resource changes are authoritative in EntityWorld, not
  duplicated in `GameState`/`UnitBattlefieldBuildingTarget`.
- Gates: build, SimReplay economy/construction cases, CombatBehavior.

M5 Combat elements:
- Owner scope: weapon state machine, projectile/effect rules, turret entity support.
- Reviewer focus: tank/turret mounts are bindings, not entities unless independently
  interactable; pure VFX stays pooled presentation.
- Gates: SimReplay projectile/turret/veterancy cases, PerfSmoke.

M6 Performance:
- Owner scope: one hotspot family at a time: redraw/culling, fog, VFX pooling, or sim
  broadphase.
- Reviewer focus: before/after evidence, no new unconditional world redraw loops, no
  hidden allocation spikes.
- Gates: PerfSmoke, ReviewGate presentation, relevant visual/manual camera check.

M7 UI and art:
- Owner scope: palette, ArtRecipe, world/HUD surfaces, building/unit recipe migration.
- Reviewer focus: Owner color is the only body ownership signal; relation color stays
  in overlays; day/fog/night preserve readability.
- Gates: build, ReviewGate todo/presentation, screenshots at multiple view sizes.

M8 AI/campaign/sandbox:
- Owner scope: planners and sandbox tooling only through command submission.
- Reviewer focus: AI does not cheat state writes and only sees `VisibilityIndex`;
  sandbox debug tools do not become gameplay authority.
- Gates: SimReplay command-driven AI scenario, PerfSmoke, sandbox smoke.

### M1 - EntityWorld Becomes Authoritative (retire the shadow)

The single most important next step: the live game must render and play from the
deterministic core, then legacy runtimes get deleted.

Status: complete as of 2026-07-01. EntityWorld is authoritative for live units,
resources, production, construction, building identity, and gameplay-facing
presentation projections. Legacy `UnitKind`, `BuildingKind`, `UnitCatalog`, and
the remaining UnitKind conversion edges were deleted; UnitSpec/BuildSpec design
ids now own gameplay, tooling, and QA paths. Full slice-by-slice record archived
in docs/TODO-Archive-2026-06-29.md ("M1 Full Completed Detail"), with final
closure recorded in docs/reviews/2026-07-01-m1-legacy-kind-deletion.md.

Completed:
[x] UnitSpec phase-3 duplicate-data cleanup: finish removing legacy read paths
    (GameState UnitDefinition* enumerations, UnitPresentationCatalog/FactionCatalog
    duplicates) now that UnitDesign/UnitSpec catalogs are the source; keep only the
    isolated compatibility shims ReviewGate already locks.
    Progress: GameState legacy production queues now carry concrete `UnitDesignId`
    through `ProductionQueueItem`, `ProductionQueueSnapshot`, and
    `CompletedProductionItem`. `ProductionKindDesignBridge.LegacySpecFor`,
    `LegacyProductionSpecs`, and the generic `BuildLegacyProductionSpecs` table were
    deleted; old production runtime reads are now faction-aware via
    `SpecFor`/`TrySpecFor`/`PlayableProductionSpecs` or direct
    `UnitDesignCatalog.Spec(item.DesignId)` queue reads. ReviewGate now forbids the
    removed ProductionKind-only legacy spec table from returning. Full `VerifyAll`
    passed after this slice.
    Follow-up: legacy `UnitModel`/`UnitDeathInfo` now expose `DesignId`,
    `Spec`, and `RuntimeDescriptor` at the compatibility boundary. `UnitView`,
    `FootprintLayer`, `DynamicUnitIcon`, and BattleRoot selection/culling/death
    VFX now read UnitDesign data through those boundaries instead of each view
    scattering `UnitKindDesignBridge.TryGetSpec/TryGetRuntimeDescriptor` calls.
    ReviewGate now locks these view paths to the UnitModel/UnitDesign-id boundary;
    full `VerifyAll` passed after this slice.
    Follow-up: controllers/debug/VFX/GameState internals now consume the
    `UnitModel` boundary directly. `SelectionController`, `ControlGroupController`,
    `PathDebugLayer`, `CombatEffectsLayer`, and `SimulationSmoke` read
    `unit.Spec`/`unit.RuntimeDescriptor`; GameState internal harvester and runtime
    descriptor paths use `IsHarvesterUnit(UnitModel)` or `unit.RuntimeDescriptor`;
    `UnitDeathInfo` and HUD selection summaries carry `DesignId` instead of
    regrouping by `UnitKind`. ReviewGate locks these paths under the UnitSpec
    bridge gates; full `VerifyAll` passed after this slice.
    Follow-up: `UnitModel` now carries native `DesignId` identity and demotes
    `UnitKind` to optional `LegacyKind` compatibility projection. GameState start
    loadouts and production completion spawn units by UnitDesign id; same-type
    selection groups by `DesignId`; `CompletedProductionItem` and `UnitDeathInfo`
    no longer carry legacy output/death `UnitKind`. Production queue snapshots,
    production presentation descriptors, `DynamicUnitIcon`, HUD command buttons,
    HUD portraits, selection icon summaries, and shared entity presentation
    descriptors no longer carry legacy `UnitKind` output/icon fallback identity.
    `ReviewGate unitmodeldesignidnative`, `dynamicuniticonunitspec`, and related
    production/presentation gates lock the native identity boundary.
    Follow-up: GameState developer sandbox seeding no longer has an `AddUnit(UnitKind)`
    wrapper or generic `UnitKind.LightTank`/`Infantry`/`Harvester` spawns; sandbox
    faction rows now instantiate every roster entry directly from
    `PlayableDesignIds`. `FogOfWarQa` unit fixtures are native design-id fixtures
    with no `UnitKind`, `UnitKindDesignBridge`, or `LegacyKind`. CombatBehavior
    skirmish start/sandbox roster checks assert `DesignId` instead of converting
    starts/playables back to `UnitKind`, and `GameState` no longer exposes
    `UnitRuntimeDescriptorFor(UnitKind)` or `IsHarvesterUnit(UnitKind)`.
    `ReviewGate gamestatesandboxrosterunitspec`, `fogofwarqaunitspecreadpath`,
    `gamestateharvesterunitspec`, and `unitmodeldesignidnative` lock this edge;
    full `VerifyAll` passed 23/23 after the slice.
    DONE (2026-07-01): final M1 sprint removed `UnitKind`, `UnitKindDesignBridge`,
    `BuildingKind`, and the old compatibility identity fields. `UnitModel.Kind`
    now aliases native `DesignId` for old call-site transition, building identity
    and construction commands carry `BuildingDesignIds` string spec ids, and
    `BuildSpecCatalog` is keyed by design id. CombatBehavior, SimReplay,
    AiOpponentLoopQa, sandbox spawning, construction events, building snapshots,
    production lanes/options, and presentation descriptors now read unit/building
    runtime identity from UnitSpec/BuildSpec design ids. ReviewGate forbids
    reintroducing `UnitCatalog.cs`, `UnitKind.cs`, `UnitKindDesignBridge.cs`, and
    `BuildingKind.cs`; full `VerifyAll` passed 23/23 after the deletion.
[x] Delete legacy once the entity path fully owns gameplay: `UnitKind`,
    `BuildingKind`, `UnitCatalog` (and the remaining UnitKind conversion edges).
    DONE (2026-07-01): deleted the legacy enum/bridge/catalog files and converted
    all gameplay/tooling construction and unit runtime paths to design-id identity.
    Remaining mentions of old names are ReviewGate guard strings or explanatory
    assertion messages only, not runtime dependencies.
[x] Remove unit projection opt-out: the old `PROCEDURAL_RTS_USE_ENTITY_WORLD_UNITS`
    compatibility switch and `UnitInstanceView.ProjectionEnabledProvider` are gone.
    Unit views now always consume EntityProjection when available, and `VerifyAll`
    no longer carries a separate legacy-units Godot boot step. DONE (2026-07-01):
    `dotnet build`, `ReviewGate filesize`, full `ReviewGate`, and full `VerifyAll`
    passed 22/22 after the removal.

### M2 - Movement Algorithms & Unit Autonomy (best RTS feel)

Pathing & group movement:
[x] Flow-field / shared corridor for large same-target groups; per-unit local
    steering only (replace per-unit A* for group moves).
    Progress: live multi-select group move now uses
    `PathfindingMath.FindSharedCorridor` to route same-domain units through a
    shared spine before splitting into formation slots. `tools/SimReplay` proves
    the wall-detour `shared-corridor` case with 4/4 members reusing the spine and
    max path inflation 1.53; `ReviewGate movementpathing` locks the slice.
    DONE (2026-07-01): EntityWorld live group movement now runs
    `PathfindingSystem` before `MovementSystem` inside `UnitBattlefield`, groups
    same-owner/same-domain/same-intent formation-slot moves, writes shared
    `PathfindingComponentState` assignments, and falls back to single-entity
    pathing for everything else. `entity-shared-corridor` replay proves
    deterministic command -> shared path -> arrival behavior; `PlayerLoopQa`
    proves real selected-unit movement uses the shared corridor path.
[x] LOS path simplification + funnel smoothing already exist in math helpers -
    route the EntityWorld movement through them via a `PathfindingSystem`.
    Progress: EntityWorld now has `PathfindingComponentState` plus a pure
    `PathfindingSystem` in the live `BattleRoot` system pipeline before
    `MovementSystem`. It plans around static EntityWorld blockers through
    `PathfindingMath.FindPathWithDebug`, feeds simplified waypoints to movement,
    clears paths on stop/hold/completion, and is covered by deterministic
    `entity-pathfinding` replay plus `ReviewGate entitypathfinding`.
[x] Full command-feel metric suite in `SimMetrics`: path inflation, corner count,
    arrival jitter, compactness, stuck seconds, repath count, target switches,
    anchor-push events; assert bands in SimReplay.
    Progress: `SimMetrics` now records path inflation, movement corner count,
    arrival jitter, compactness radius, stuck seconds, repath count, target-switch
    count, and anchor-push events. `MovementSystem`, `CombatSystem`, and
    `SeparationSystem` feed the metrics; `tools/SimReplay` asserts command-feel
    bands in the existing group-move/group-attack/firing-anchor scenarios; and
    `ReviewGate commandfeelmetrics` locks the contract.
    Progress: `MovementSystem` now gives unslotted same-point move orders a
    deterministic crowded-arrival stop instead of stacking on the exact target;
    `SeparationSystem` treats cooldown-gated in-range attackers as hard anchors
    between shots. `tools/SimReplay` now proves 30-unit same-point move compactness/stability
    and cooldown-gated attacking-anchor protection.
[x] Attack slot anchoring cleanup: group attack slot assignment now treats
    attackers already inside weapon range as firing anchors that reserve their
    ring bearing; rear movers choose remaining attack slots instead of pushing
    through anchors.
    `CombatSystem` includes target collision radius in standoff math while keeping
    actual firing range weapon-based for balance, and preserves valid
    group-attack `FormationSlot`s against ground/building targets instead of
    overwriting them every tick. Air targets bypass static slot preservation so
    anti-air units keep dynamic standoff pursuit.
    `SimReplay` proves `attack-slot math` and
    `anchored-group-attack-slotting`; dog infantry HP was tuned from 50 to 52
    after `BalanceReport` exposed clearer-slot light parity drift. `ReviewGate
    attackslotanchoring` locks the behavior.

Unit autonomy redesign (the core of feel) - a clean behavior model on top of the
existing stance component. Design: each armed unit runs a small deterministic
decision each tick that NEVER overrides an explicit player order, and degrades to
a readable "good-enough" action rather than jitter. Three decoupled radii and a
clear priority chain replace ad-hoc nearest-enemy logic.

[x] Autonomy model: separate three radii per unit - `WeaponRange` (can fire),
    `AcquireRange` (will auto-pick a target), `LeashRange` (max wander from anchor
    before returning). Stance tunes which radii are active, not bespoke code paths.
    Progress: `AutonomyComponentState` now stores explicit `AcquireRange`,
    `LeashRange`, and `AnchorPosition`; authored armed units receive default
    autonomy data through `UnitSpecEntityBridge`; CombatSystem reads that data
    while weapon range remains weapon-authored. The state is hashed and validated
    by `SimInvariants`, and `tools/SimReplay` proves `autonomy-radii`.
[x] Autonomy decision chain (evaluated in order, first match wins): (1) explicit
    player command, (2) valid current target within leash (stickiness), (3) shared
    ally threat within stance rules, (4) auto-acquire highest-priority target in
    AcquireRange, (5) return toward anchor if beyond leash, (6) idle hold.
    Progress: explicit manual attacks still win, valid auto targets are sticky
    within leash, auto-acquire is constrained by `AcquireRange`, and ReturnGuard
    clears auto targets and returns to its anchor beyond leash. PassiveRetaliate
    now consumes direct-damage retaliation state without doing normal auto-acquire.
    Target-visibility slice added current `VisibilityIndex` gating for non-manual
    auto-acquire, sticky auto targets, and PassiveRetaliate while preserving manual
    attack focus; `target-visibility` replay and `ReviewGate targetvisibility`
    prove the behavior. Last-known memory first slice now preserves a decaying
    remembered point after fog loss: short-range units chase it, ranged units hold,
    and tracking missile users are not forced into blind movement.
    Threat-priority slice added deterministic weighting for visible non-manual
    candidates that are directly attacking this unit in small local fights;
    `target-threat-priority` proves auto-acquire prefers the automatic threat
    source while manual focus stays fixed. Large army fights fall back to base
    weapon-profile scoring to avoid deterministic focus cascades.
    DONE (2026-07-01): shared ally-threat scoring is now part of the same
    deterministic candidate-priority path: a responder can prefer a visible enemy
    automatically attacking a nearby self/allied entity, respecting Hold range,
    ReturnGuard leash, Ignore/PassiveRetaliate exclusions, and manual-focus
    priority. `shared-ally-threat` replay proves the full chain.
[x] Target stickiness + hysteresis: do not switch targets unless the new one is
    meaningfully better (priority margin) or the current is dead/out-of-leash;
    kills target-flicker. Add a short re-acquire cooldown.
    Progress: bounded slice added non-manual auto-target stickiness in
    `CombatSystem`; current valid targets are retained inside an acceptable range
    unless a candidate beats the priority/distance hysteresis margin. Re-acquire
    cooldown slice added deterministic `AutoReacquireCooldownRemaining` state,
    hashes/invariants it, starts the short cooldown only after an automatic target
    is lost, and clears/bypasses it for manual attack. `SimReplay` proves
    `target-reacquire-cooldown`, `manual-attack-reacquire-bypass`, and the
    negative-cooldown invariant; `ReviewGate targetreacquirecooldown` locks it.
[x] Target priority scoring already in `WeaponTargetProfile` - feed it the gameplay
    `VisibilityIndex` (only fire on visible/last-known) and a threat weighting so
    units prefer what is shooting them / what they counter.
    Progress: current-visible target gating is now wired through `CombatSystem`
    and verified by `target-visibility`; bounded direct automatic-threat weighting
    is wired through `CombatSystem` and verified by `target-threat-priority`.
    Last-known memory first slice is wired through `CombatSystem` and verified by
    `last-known-target-memory`.
    DONE (2026-07-01): ally/shared threat scoring is wired through
    `AutoTargetPriority` without adding new runtime ownership or faction logic;
    weapon-profile counter scoring remains the base score, then direct/self and
    shared ally threats apply deterministic multipliers only for non-manual
    automatic targets. `shared-ally-threat` and `target-threat-priority` prove the
    combined priority behavior.
[x] Stance semantics on the new model: Hold (fire in range, never leave anchor),
    Aggressive (acquire+pursue within sight, leash large), ReturnGuard (pursue then
    snap back to anchor via leash), PassiveRetaliate (only retaliate, no acquire),
    Ignore (no acquire, no ally calls, manual-only).
    Progress: `autonomy-radii` replay proves Hold anchors, Aggressive chases,
    ReturnGuard leashes home, and Ignore stays passive. `passive-retaliate` replay
    proves passive units stay idle until damaged, retaliate against the attacker,
    and preserve manual focus when already explicitly ordered.
[x] Kiting / micro for mobile ranged units: when `CanFireWhileMoving` and a faster
    enemy closes inside min-range, back off to standoff while firing; setup/siege
    weapons instead require a stop+deploy delay before firing.
    DONE (2026-07-01): `WeaponDefinition.MinRange` is now data, `LightRepeater`
    authors an initial 118px minimum range, and `CombatSystem` backs mobile
    fire-while-moving units away from targets inside that band before fire-anchor
    hold logic can freeze them. Firing checks now respect per-weapon minimum range.
    `ranged-min-range-kiting` proves a fast ranged unit restores spacing and keeps
    firing deterministically.
[x] Firing anchors (already in Separation): a unit that has fired recently is a
    non-displaceable anchor for a short window so micro does not shove shooters.
[x] Last-known-position memory: target entering fog leaves a decaying trail; melee
    chases to it, ranged holds, missiles follow weapon rule (M5).
    Progress: first combat-autonomy slice adds deterministic
    `LastKnownTargetPosition` / `LastKnownTargetRemaining` to weapon users, hashes
    and validates it, refreshes it only while an automatic target is visible, and
    clears active fire authority when the target enters fog. Short-range
    non-tracking units chase the remembered point, ranged units stop blind combat
    movement, and tracking missile users are left under weapon projectile rule
    instead of being forced into fog. `SimReplay` proves
    `last-known-target-memory` plus invariant probes; `ReviewGate
    lastknowntargetmemory` locks the slice. Full M5 projectile entity behavior
    remains tracked under the projectile/M5 work.
[x] Deterministic autonomy tests in SimReplay: no target flicker (target-switch
    count under threshold), ranged units keep min-range, leashed units return,
    ignore-stance units never auto-fire; all reproducible by seed.
    Progress: `tools/SimReplay` now includes `target-stickiness` /
    `no-target-flicker`, asserting auto target stays stable with
    `TargetSwitchCount == 0`.
    Progress: `autonomy-radii` now also asserts leashed units return and
    ignore-stance units never auto-fire.
    Progress: `passive-retaliate` now asserts passive units do not auto-acquire
    nearby enemies, react only after direct damage, and keep manual focus.
    Progress: `target-visibility` now asserts hidden enemies are not auto-acquired
    or used for PassiveRetaliate, visible alternatives are preferred, and manual
    attack focus still works through fog.
    Progress: `target-threat-priority` now asserts a visible automatic target
    attacking the unit beats a closer non-threat target for auto-acquire, while
    manual attack orders still ignore the automatic threat weight.
    Progress: `last-known-target-memory` now asserts a visible automatic target
    that enters fog leaves a decaying remembered point, short-range units chase
    that point, ranged/tracking-missile users do not blind-chase, and memory
    expires deterministically.
    Progress: `shared-ally-threat` now asserts a visible enemy automatically
    attacking a nearby allied unit beats a closer non-threat target, while manual
    attack focus and Ignore stance remain untouched. The remaining open assertion
    is ranged min-range/kiting, which depends on the kiting TODO above.
    Progress: `entity-shared-corridor` now asserts live EntityWorld group movement
    plans a shared wall-detour corridor and reaches formation slots; `PlayerLoopQa`
    adds a UnitBattlefield smoke so this is not only shadow-sim coverage. Ranged
    min-range/kiting remains the open autonomy assertion.
    DONE (2026-07-01): `ranged-min-range-kiting` closes the last open autonomy
    assertion by proving a mobile fire-while-moving ranged unit backs out of
    minimum range, rebuilds spacing, and damages the target in deterministic replay.

### M3 - Build & Construction System (faction-distinct methods)

Unify building authoring and support different faction construction UX on ONE data
model. A build is data (`BuildSpec`), construction is components, placement is rules.

[x] `BuildSpec` (one authoring path, replaces `BuildingDefinition`+`BuildDefinition`):
    outputEntitySpecId, category, cost, buildTime, footprint, requiredTech,
    requiredProducer, powerProvided/Used, buildRadius, placementRules, refundRatio.
    Progress: the legacy `BuildingDefinition`, `BuildDefinition`, and
    `BuildCatalog` compatibility shells were deleted; `BuildSpecCatalog` is the
    single building/build authoring catalog.
    DONE (2026-07-01): `BuildSpec` now owns entity spec id, category, cost,
    build time, footprint, required producer/buildings, power provided/used,
    build radius, placement domain, refund ratio, and construction method
    metadata. `BuildingTargetEntityBridge.ToEntitySpec()` plus initial component
    generation connect it to Construction, Power, Footprint, BuildRadius,
    ProductionQueue, Dock, Vision, WeaponUser, and PresentationPulse states.
    Existing construction/production/economy replays prove the backend
    integration. Player-facing construction UX remains tracked below.
[x] Buildable-area system: placement legality = inside an owner `BuildRadius` source
    (already a component) + passable terrain for the footprint + no overlap + tech +
    power + fog/build-visibility. Pure `PlacementMath` returns a reason on failure.
    Progress: first `PlacementMath` slice added `ValidateBuildableArea` and
    `PlacementBuildAnchor`, returning distinct reasons for missing tech, blocked
    footprint, outside build radius, world bounds, and impassable terrain/domain.
    `ConstructionSystem` now calls it and emits `ConstructionRejectedEvent`.
    Power anchor slice added powered build-radius anchors: unpowered anchors inside
    range now reject with `placement.unpowered` while powered anchors still allow
    construction. Build visibility slice added `PlacementBuildVisibility`, samples
    the footprint against current self/allied completed `VisionComponentState`
    sources, rejects unseen placement with `placement.notVisible`, and is proved by
    `construction-visibility` plus `ReviewGate constructionvisibility`. Explored-
    memory-only construction policy remains a future design decision, not part of
    this current-visibility gate.
[x] Faction-distinct construction methods on the same backend (method is faction
    metadata, not a code fork):
    [x] Dog method - "deploy/MCV style": structures build in place from a builder/
        engineer or deploy unit; build radius extends from deployed cores.
    [x] Cat method - "C&C sidebar style": queue in a producer, pay/reserve cost,
        timer runs, then enter placement mode when ready.
    [x] Shared third path - "repair/restart/capture" of pre-existing objective
        structures (campaign), via the same Construction component.
    Progress: construction-method metadata slice added `ConstructionMethodKind`,
    `BuildPlacementMode`, `ConstructionMethod`, `FactionConstructionPolicy`, and
    `BuildConstructionPolicy` to `BuildSpec`. Dog `DeployInPlace`, Cat
    `SidebarPlacement`, and shared `RestartCapture` all point at the same
    `StartConstructionEntityCommand` / `ConstructionComponentState` backend.
    `construction-methods` replay and `ReviewGate constructionmethods` prove the
    methods are data, not Dog/Cat runtime system forks. Player-facing UX remains
    tracked separately in M7.
    Cat queue-ready backend slice added `QueueConstructionEntityCommand` and
    `ConstructionPhase.Queued` / `ReadyToPlace` on the shared
    `ConstructionComponentState`; `construction-queue-ready` proves a Cat-style
    sidebar ticket can pay, advance, and become ready without acting like a
    completed building, build-radius anchor, footprint blocker, or tech
    prerequisite.
    Progress: player placement handoff slice moved `BuildPlacementController`
    confirmation from legacy instant `GameState.PlaceBuildingWithinBuildRadius`
    to `UnitBattlefield.ConstructBuilding`, so live placement now spends credits,
    spawns an under-construction entity, and progresses through the shared
    `ConstructionSystem`. Cat ready-ticket placement, Dog build authority, and
    shared restart/capture backend were closed in follow-up slices.
    DONE (2026-07-01): Cat ready-ticket placement consumption now reuses
    `StartConstructionEntityCommand` via optional `ReadyTicket`. Invalid placement
    keeps the ready ticket and does not charge again; valid placement consumes the
    ticket, spawns a complete BuildSpec-backed building with a footprint, and keeps
    credits at the queue-time spend. Live `UnitBattlefield` exposes
    `QueueConstructionTicket`, `ReadyConstructionTickets`, and
    `PlaceReadyConstructionTicket`; `BuildPlacementController` preview now uses the
    same live placement validation as confirmation. `construction-ready-placement`
    and `PlayerLoopQa` lock the backend and live facade.
    DONE (2026-07-01): Dog build-authority backend now treats `AbilityKind.Build`
    as passive data that emits `BuildRadiusComponentState` through
    `UnitSpecEntityBridge`. `DogEngineer` carries a 220px build radius, Build does
    not enter active ability cooldowns, and `ConstructionSystem` accepts any live
    friendly BuildRadius authority (building, unit, signal/objective) while keeping
    completed-building rules for building anchors. Specs with Deploy must be
    deployed and setup-complete before their build radius is active. SimReplay
    `dog-build-authority` and `deploy-build-authority` are locked by review record
    `m3-dog-build-authority`.
    DONE (2026-07-01): shared restart/capture backend now uses
    `ConstructionPhase.RestartCapture` on the same `ConstructionComponentState`.
    `ConstructionSystem` does not auto-advance restart/capture objectives; a
    repair-capable unit issues `RepairEntityCommand`, neutral objectives are
    captured through `EntityWorld.ChangeOwner`, and `RepairSystem` spends repair-
    equivalent credits to advance construction progress. On completion, the phase
    becomes normal `Building`, so existing systems such as `SignalNetworkSystem`
    emit build radius/vision without a special campaign fork. SimReplay
    `restart-capture-construction` is locked by review record
    `m3-restart-capture-construction`.
[x] `ConstructionSystem` (pure `ISimSystem`): advances `ConstructionState.Progress`,
    handles queued -> placing -> under-construction -> complete -> paused(offline)
    -> destroyed; cancel returns refund.
    Progress: a first pure `ConstructionSystem` slice consumes
    `StartConstructionEntityCommand`, validates required producer/build radius/
    credits, spends credits, spawns BuildSpec-backed unfinished structures, and
    advances `ConstructionComponentState.Progress` deterministically to complete.
    `tools/SimReplay` proves `construction-loop` with accepted/rejected builds,
    progress, active power/production visibility, and hash `5D1A493543651765`.
    Progress: construction cancel/refund slice added `CancelConstructionEntityCommand`
    and `ConstructionCancelledEvent`; under-construction buildings owned by the
    issuer refund `Cost * RefundRatio * (1 - Progress)` and are removed, while
    completed buildings cannot be cancelled. `construction-cancel` replay proves
    deterministic refund and completed-building rejection.
    Progress: construction pause/offline slice added `ConstructionPauseReason` on
    `ConstructionComponentState`, hashes/validates pause state, and pauses only
    already-started power-consuming construction when `PowerSystem` marks it
    unpowered. `construction-paused-offline` replay proves pause, resume, and
    non-consuming construction no-self-lock behavior; `ReviewGate
    constructionpause` locks the contract.
    Progress: construction queue-ready slice now covers the queued -> ready-to-place
    portion of the lifecycle for Cat sidebar construction tickets. Cat
    ready-ticket placement slice now consumes ready tickets through the same
    `StartConstructionEntityCommand` backend without a second credit spend.
    Progress: player placement handoff slice now proves direct live placement uses
    `StartConstructionEntityCommand` through `UnitBattlefield.ConstructBuilding`
    instead of legacy instant AddBuilding. `PlayerLoopQa` asserts credit spend,
    under-construction progress, and eventual completion.
    Progress: Dog deploy/build-authority slice closes the unit/deployed-core build
    radius backend. Shared restart/capture closes the objective repair/restart
    backend through `ConstructionPhase.RestartCapture`.
    DONE (2026-07-01): destroyed-state lifecycle now closes inside
    `ConstructionSystem`: any entity with `ConstructionComponentState` and
    `Health.Hp <= 0` emits `ConstructionDestroyedEvent` plus the generic
    `EntityDestroyedEvent`, queues removal, and releases footprint/authority on
    the same tick. SimReplay `construction-destroyed-lifecycle` proves dead
    under-construction buildings, completed construction buildings, and
    restart/capture objectives are removed deterministically and that replacement
    construction can start on the released footprint. Review record:
    `m3-construction-destroyed-lifecycle`.
[x] BuildRadius / power gating: powered state affects production speed and turret
    activity (M5); placement requires being inside a friendly build radius unless the
    BuildSpec is a radius-seeding core.
    Progress: `PlacementBuildAnchor` now carries powered state from
    `PowerComponentState`, and ConstructionSystem rejects construction inside only
    unpowered build authority with `placement.unpowered`. `construction-power-gate`
    replay proves powered vs unpowered anchor behavior.
    DONE (2026-07-01): backend power consequences are also covered for production
    pause/speed and turret activity through `ProductionSystem`, `TurretCombatSystem`,
    and `power-consequences` replay. Placement still uses friendly powered
    BuildRadius anchors unless the spec itself seeds build authority.
[x] Deterministic construction tests: placement math, build radius, tech/power gates,
    progress, cancel/refund, faction-method differences - all in SimReplay.
    Progress: `construction-loop` now covers required producer, build radius,
    credits, deterministic progress, complete-state integration, overlap rejection,
    outside-radius rejection, impassable terrain/domain rejection, and
    construction cancel/refund. `construction-power-gate` covers powered build-radius
    anchors. `construction-visibility` covers current build visibility rejection.
    `construction-methods` covers Dog/Cat/shared method metadata on the same
    backend. `construction-queue-ready` covers Cat queued sidebar tickets becoming
    ready without granting building authority. `construction-ready-placement`
    covers invalid ready placement preservation plus successful no-second-spend
    ticket consumption. `construction-paused-offline` covers offline pause/resume
    and non-consuming construction no-self-lock behavior.
    `dog-build-authority` covers Dog engineer build-radius authority; `deploy-build-
    authority` covers Deploy+Build cores rejecting before/during setup and accepting
    after setup on the same StartConstruction backend.
    `restart-capture-construction` covers neutral objective capture, non-auto-
    advancing restart construction, repair-driven progress, and post-completion
    signal build-radius emission.
    `construction-destroyed-lifecycle` covers dead construction removal,
    construction-specific destroyed events, generic destroyed events, footprint
    release, and replacement construction after destruction.
    Progress: M3 backend closure audit confirmed `BuildSpec` and BuildRadius/power
    gates are implemented and covered by existing replays. Full faction
    construction UX tests and player HUD handoff remain open.
    Progress: `PlayerLoopQa` now covers player construction handoff from placement
    to `ConstructionSystem` completion, plus Cat ready-ticket queue -> ready ->
    invalid placement preservation -> successful placement consumption. Remaining
    deterministic construction tests are now complete for the backend lifecycle;
    remaining work is player-facing Dog construction UX/HUD handoff.

### M4 - Production & Economy System

Production is a building capability (a component on a producer entity), not a UI
special case. Authority lives on the producer; UI only aggregates.

[x] `ProductionSystem` (pure `ISimSystem`): per-producer `ProductionQueue` advances by
    buildTime, spawns at a spawn point, then moves to the producer `RallyPoint`;
    supports pause(reason), cancel(refund), and prerequisite/power gating.
    Progress: `ProductionSystem` consumes `ProduceEntityCommand` and
    `CancelProductionEntityCommand`, spends/refunds owner Credits, advances each
    producer's `ProductionQueueComponentState`, records `ProductionPauseReason`,
    gates by required producer kind + tech tier + construction/power state, spawns
    completed authored `UnitSpec` units, and sends them toward
    `RallyPointComponentState`. `tools/SimReplay` proves completion, pause, cancel,
    refund, and rally behavior in `production-loop`.
[x] Per-producer authority: each barracks/factory owns its queue/progress/rally;
    multiple producers are independent lanes. UI may aggregate by producer type when
    none selected, but never invents a shared queue.
    Progress: EntityWorld producers own independent `ProductionQueueComponentState`
    and `RallyPointComponentState`; `tools/SimReplay` proves two powered producers
    complete in parallel while a third unpowered producer keeps its own paused queue.
[x] `ResourceSystem` (pure `ISimSystem`): harvester gather -> dock reservation (avoid
    refinery congestion) -> unload credits -> field depletion; `ResourceNode` /
    `ResourceCargo` / `Dock` components already exist.
    Progress: `ResourceSystem` now runs as a pure `ISimSystem` on EntityWorld,
    `HarvestEntityCommand` is translated by `CommandSystem`, `EntityWorld` owns
    deterministic owner credit banks, resource nodes deplete through
    `ResourceNodeComponentState`, harvesters reserve `DockComponentState` before
    unloading, and `tools/SimReplay` proves a deterministic `resource-loop`.
[x] Economy is tunable from day one: gather rate, depletion behavior, refinery
    congestion, credits-per-minute metric in `SimMetrics`.
    Progress: `EconomyTuningConfig` now exposes gather distance, dock distance,
    gather rate, and unload rate as EntityWorld-owned pure data. `ResourceSystem`
    reads those values instead of hard-coded rates, node depletion behavior remains
    data on `ResourceNodeComponentState`, refinery congestion and credits-per-minute
    are covered by `SimMetrics`, and `tools/SimReplay` proves tuned rates change
    throughput and deterministic state hash. `ReviewGate economytuning` locks the
    contract.
[x] Deterministic economy/production tests: dock reservation under congestion,
    queue/rally/cancel/refund, multi-producer independence, in SimReplay.
    Progress: `tools/SimReplay` now has deterministic `resource-loop` and
    `production-loop` scenarios. They assert resource depletion, credit banking,
    dock wait/refinery congestion, producer-owned queues, rally intent,
    cancel/refund, unpowered pause, and two powered producers completing in
    parallel. `ReviewGate economyproductiontests` locks this coverage.

### M5 - Unit Progression & Combat Elements (upgrades, turrets, projectiles)

All as data/components on the shared entity language - never new runtime classes.

[x] Turrets as entities, mounts as bindings: `EntityKind.Turret` is a buildable/
    selectable/repairable/destroyable fixed platform (own Health/Vision/WeaponUser/
    Power). A rotating gun on a tank/turret is `WeaponMountSpec` + `ArtBinding`, NOT
    an entity, unless it can be independently selected/damaged/repaired/destroyed.
[x] Ordinary buildings have no weapons just for being buildings; any fixed defense/
    support field is an `EntityKind.Turret` (may show under a "defense" build tab).
    Progress: `m5-turret-entities` proves Ground/AntiAir turrets project from
    `BuildSpec` into `EntityKind.Turret` with `WeaponMountSpec` and
    `WeaponUserComponentState`; ordinary producer/resource/power/airfield buildings
    stay `EntityKind.Building`, gain no weapon state from `BuildingKind`, and are
    ignored by `TurretCombatSystem` even if a fake weapon component is attached.
[ ] Projectiles/ammo as data: `WeaponDefinition` + `AmmoDefinition` already model
    range/cooldown/arc/projectile-kind/damage-profile/hit-rule; add `WeaponSystem`
    (acquire->rotate->warmup->fire->cooldown->reload state machine) and
    `ProjectileSystem` for tracking missiles, beams, splash. Projectiles are entities
    ONLY when gameplay-affecting (interceptable/tracking/mines/DoT); tracers/flashes
    are pooled presentation.
    Progress: weapon and ammo definitions are now authored as reflection-discovered
    `WeaponDesign` / `AmmoDesign` classes instead of an embedded catalog dictionary,
    and `ContentAuthoringQa` verifies every discovered weapon links to discovered ammo.
    DONE slice (2026-07-01): tracking ammo now spawns `EntityKind.Projectile`
    entities via deferred `EntityWorld.QueueSpawn`, then `ProjectileSystem` moves,
    tracks, impacts, applies shared shield/damage/death logic, and queues cleanup.
    `SimSystemPipeline`, turret combat, unit-vs-building combat, and manual
    UnitBattlefield combat bridges all step projectile entities after combat.
    Direct/beam/ballistic remain immediate until they need gameplay-affecting
    entity behavior. The unified `WeaponSystem`, beam/splash/interceptable
    projectile state-machine work remains open.
    Integration fix: tracking projectiles now continue after the original shooter
    dies, use swept segment impact checks, and keep source-death damage from
    creating invalid retaliation targets. Seeker rockets also carry an explicit
    vehicle damage profile so delayed tracking impacts still read as anti-vehicle
    counters. Gates passed: `BalanceReport`, `CounterReadabilityQa`, `SimReplay`,
    full `ReviewGate`, and `VerifyAll` 22/22.
    回归修复 (2026-07-02, #76): `ProjectileVfxMath` now owns the shared readable
    projectile style for legacy `GameState.Projectiles` and ECS
    `ProjectilePresentationProjection`; `CombatEffectsLayer` uses segment culling,
    fog visibility gating, and draws after `FogOfWarLayer` so visible ordinary
    projectiles and seeker rockets are not swallowed by fog/theme overlays.
    `CombatBehavior`, `ReviewGate simhot`, full `ReviewGate`, and `VerifyAll`
    lock the legacy/ECS paths.
[ ] Upgrade system as match-time derived modifiers, NEVER mutating the immutable
    `Spec`: an `UpgradeState` (per owner) resolves into derived combat/move/vision
    values applied on top of the spec. Tech tiers, weapon/armor upgrades, veterancy
    (per-unit promotion from kills) all flow through this one resolver.
    Progress: added owner-scoped `UpgradeState`, `UpgradeCatalog`, and
    `UpgradeResolver`; `EntityWorld` folds completed upgrades into deterministic
    state hash. Combat damage/range, command attack-slot range, turret/building
    damage/range, VisionSystem sight, and MovementSystem speed now read derived
    values without mutating `UnitSpec`, `WeaponDefinition`, or component base data.
    `upgrade-progression` replay proves damage/range/sight/speed modifiers and
    immutable base specs. Veterancy core now composes through the same resolver;
    tech research commands/UI remain open.
[ ] Veterancy: kills accrue rank on a `VeterancyComponentState`; ranks apply derived
    damage/hp/regen multipliers via the upgrade resolver; rank shown via an Owner-
    neutral art layer / glyph.
    DONE slice (2026-07-01): combat kills now award deterministic XP/rank on
    `VeterancyComponentState`; armed `UnitSpec` entities receive the component;
    `UpgradeResolver` composes owner upgrades with per-entity rank modifiers for
    damage/range/sight/speed/max-hp; rank-up increases current/max HP without
    mutating `UnitSpec` or `WeaponDefinition`; `EntityProjection` exposes rank and
    kills; `UnitInstanceView` draws owner-neutral rank dots. `veterancy-progression`
    proves two valuable combat kills promote to rank 3, increase max HP/damage,
    project rank/kills, and stay deterministic. Normal battle balance stayed inside
    `BalanceReport` after promotion thresholds were raised to long-game pacing.
    Regen remains open under the separate self-repair/regen TODO.
[x] Deterministic progression tests: upgrade resolves without touching base spec,
    turret fires/dies independently, projectile lifetime/tracking, veterancy ranks -
    in SimReplay.
    Progress: `upgrade-progression` now covers owner upgrade resolution, derived
    damage/range/sight/speed, immutable spec/weapon data, system read paths, and
    deterministic state hash. `projectile-tracking` now proves tracking ammo
    creates a projectile entity before damage, moves deterministically, impacts,
    damages, survives source death, and cleans itself up; `EntityWorld` also folds
    `_nextEntityId` into state hash so transient projectile id consumption is
    visible. `m5-turret-entities` covers turret entity firing/death boundaries.
    `veterancy-progression` now covers rank XP/kills, rank-derived damage/max-HP,
    owner-neutral projection, and deterministic state hash.

### M6 - Performance (see "Performance Optimization Plan" below)

[x] Broadphase `CombatSystem.NearestHostile` (next O(n^2) hot spot after Vision).
[x] Dirty-flag / culled rendering; pooled VFX; cached static grid; throttled fog.
    Progress: `ReviewGate renderperf --max-warnings=0` now aggregates the
    presentation dirty-redraw baseline, camera culling, visible-rect grid cache,
    pooled/budgeted VFX, and throttled/scoped fog checks. This marks the current
    render-performance boundary complete while leaving deeper unit batching and
    future art-polish work as separate open TODOs.
[x] In-engine `PerfHud` overlay (FPS, frame ms, sim ms, render ms, counts).

### M7 - UI & Presentation Polish (Soft Old City)

[x] CommandPlate: replace tile-based plate with a continuous rounded fog-like field.
    Progress: `TerrainFloorMath` no longer classifies command zones through cached
    terrain tiles, and `GridLayer` now draws command areas as camera-culled soft
    circular fields with layered lobes and curved edge hints. `ReviewGate grid`
    rejects tile-local rectangular CommandPlate motifs so the plate cannot regress
    into a grid implementation.
[x] One canonical palette source shared by world + HUD (reconcile `SoftOldCityTheme`
    with `WorldThemeMath`); `EnvironmentTone` drives day/fog/night/corruption.
    Progress: `SoftOldCityPalette` now owns the shared paper/ink/command/repair/
    route/fog/dusk/night color vocabulary, and both `SoftOldCityTheme` plus
    `WorldThemeMath` derive their HUD/world colors from it. `ReviewGate palette`
    verifies the canonical source hooks so future UI/world palette edits do not
    drift back into separate hard-coded theme islands.
[x] `EntityRenderPalette.Resolve(ColorRole, OwnerColor, EnvironmentTone)`; collapse
    `ColorUse` -> `ColorRole` (Body/Ink/Owner/Effect/Warning/Shadow).
[ ] Build/production UI: per-faction construction UX (dog deploy vs cat sidebar),
    multi-producer lanes, per-building queue/rally/cancel, aggregate when none
    selected; build tabs (command/power/economy/infantry/vehicle/defense/air/naval);
    placement preview with legality reason, rotation, refund feedback.
    Progress: placement preview/confirmation now uses the live `UnitBattlefield`
    build legality and construction command path, so preview and accepted placement
    share the same backend rules. Full HUD build tabs, per-faction construction UI,
    ready-ticket placement, refund feedback, and aggregate build/production UX
    remain open.
[ ] Upgrade/veterancy UI: tech tree affordances, upgrade cost badges, rank glyphs.
[x] Extract shared `UiTheme`/`UiFactory` from MainMenu/Settings/Pause/Outcome/Hud.
    Progress: noncombat overlay slice added `scripts/ui/UiFactory.cs` for shared
    Pause/Outcome panel, label, button, and button-style construction while
    leaving MainMenu/Settings/Hud extraction open. Menu/settings slice then moved
    MainMenu/SkirmishSetup and Settings panel/label/button styling through
    `UiFactory`, added `UiFactory.StyleButton(BaseButton, Color)`, and locked the
    noncombat extraction with `ReviewGate` plus
    `docs/reviews/2026-06-29-ui-factory-menu-settings.md`. Hud extraction remains open.
    HUD style-factory slice moved HUD panel style, label construction/shadow, and
    button state styling into `UiFactory` while keeping battle HUD layout helpers in
    `HudLayer`; `ReviewGate presentation --max-warnings=0` stays green. Full HUD
    composition/build/production UI extraction remains open.
    Final HUD factory slice moved remaining action/command/move/stance/control-group
    draw-style resolution through `UiFactory`; `DesktopHudQa` now statically proves
    HUD factory usage in addition to layout constraints, and `ReviewGate huduifactory`
    locks the shared HUD theme contract. Build/production UX redesign remains a
    separate open TODO above.

### M8 - AI, Campaign, Sandbox (command-driven, never cheating)

[ ] AI planners (Economy/Production/Defense/AttackWave/Scout/TacticalMicro) submit
    commands through the same buffer; no direct state writes; only sees `VisibilityIndex`.
[ ] Objective-graph campaign (trigger/condition/action/tone-cue) over entities.
[ ] Sandbox stronger than missions: spawn any spec, switch owner/faction/team/
    relation/environment, time scale, debug overlays (paths/slots/avoidance/rings/
    anchors/components/command-log/state-hash), one-click stress tests.
    Progress: Sandbox time scale slice added F2/F3/F4 controls for slow/default/fast
    developer playback. `SandboxTimeScaleMath` keeps bounded presets at 0.25x, 0.5x,
    1x, 2x, and 4x, and only scales gameplay delta when `LaunchMode.Sandbox` is
    active; Skirmish remains unscaled. `CombatBehavior` proves preset stepping,
    clamp behavior, sandbox delta scaling, and Skirmish isolation; `ReviewGate
    sandboxtimescale` locks the wiring. The broad sandbox item stays open for
    spawn-any-spec, owner/faction/team switches, debug overlays, and stress tests.
    Progress: Sandbox spawn authoring slice added `SandboxSpawnAuthoring` as a pure
    core list/filter/request API over `UnitDesign` and `BuildSpec` data. The new
    `SandboxSpawnAuthoringQa` proves deterministic ordering, unit/building/turret
    coverage, Dog/Cat faction filters, EntitySpec round-trips, and request owner/
    transform preservation; `VerifyAll` now runs this QA, and `ReviewGate
    sandboxspawn` locks the slice. Runtime UI spawn controls remain open.
    Progress: Sandbox debug overlay state slice added `SandboxDebugOverlayState`
    with explicit flags for paths, slots, avoidance, rings, anchors, components,
    command-log, and state-hash, plus movement/diagnostics/all presets. `SimulationSmoke`
    proves toggle/set/preset/status behavior; `ReviewGate sandboxdebugoverlays`
    locks the core state model. Drawing and hotkey integration remain open.
    Progress: Sandbox developer context slice added pure core owner/faction/team/
    relation/environment/time/debug-overlay state plus parsed requests and context-
    safe spawn filtering. `SandboxSpawnAuthoringQa` proves Dog/Cat context filters,
    owner/team/relation/environment/time/overlay switching, context-safe requests,
    and Corruption staying locked; `ReviewGate sandboxdevelopercontext` locks the
    slice.
    Progress: Sandbox runtime UI/stress slice wired a sandbox-only HUD developer
    panel for owner/faction/team/relation/time/environment/overlay cycling and a
    one-click stress spawn button. `SandboxStressSpawnPlanner` reuses context-safe
    `SandboxSpawnAuthoring` entries with capped unit/building/turret requests;
    `SandboxSpawnAuthoringQa` proves Dog/Cat stress plans plus locked Corruption
    rejection, and `ReviewGate sandboxruntimeui` locks Sandbox-only UI/stress
    wiring. Full spawn browser, actual debug overlay drawing, command-log, and
    state-hash display remain open.

### M9 - Elegance & Decoupling (technical-debt paydown)

Audit-driven (2026-06-30 full-codebase sweep). The framework's macro architecture is
clean (sim/Godot boundary holds, systems are pure `ISimSystem`, data/behavior split),
but the implementation layer accumulated copy-paste debt during fast expansion. Every
item below is evidence-backed; each must keep `tools/SimReplay` hashes identical (pure
refactor, no behavior change) and `tools/VerifyAll` green.

Duplication - one concept, many copies (highest priority):
[x] Shared `SpatialGrid<T>`: broadphase was reimplemented in 3 systems with a
    copy-pasted, DIVERGENT `Cell()` (CombatSystem guarded `MathF.Max(cellSize,1f)`,
    VisionSystem did not - a latent divide-by-zero). DONE: extracted
    `scripts/core/sim/SpatialGrid.cs` (`Add`/`Neighbors`/`Reset`/`Clear`/`CellRadiusFor`
    + one guarded `Cell`, with an allocation-free struct `NeighborEnumerator`).
    CombatSystem (dynamic cell-size growth), VisionSystem, SeparationSystem all migrated;
    removed the dead `Cell()` and redundant `_targetGridCellSize` field. Pure refactor:
    SimReplay 45/45 hashes byte-identical; ReviewGate updated to forbid re-rolling
    `Cell()` and to require `_grid.Neighbors(`/SpatialGrid use. Note: PathfindingSystem
    uses `GridObstacle` for A* (a different concept), correctly left out of scope. Perf:
    400u sim step ~1.36ms (within budget); alloc/tick is higher than the raw hand-rolled
    loops (an enumerator abstraction costs more than inline dict lookups) but the gate is
    time-based and green - revisit only if alloc becomes a profiled bottleneck.
[x] Retire the second grid style: `MovementSystem` now uses
    `SpatialGrid<LocalAvoidanceBody>` for local avoidance and crowded-arrival queries;
    `LocalAvoidanceMath` keeps only the deterministic avoidance force math. The old
    private `Dictionary<GridObstacle, List<LocalAvoidanceBody>>` / `BuildHashInto`
    path is removed from MovementSystem, `AdvancedPathingPolicy` now names
    `UseSpatialGridLocalAvoidance`, and `ReviewGate architecture` locks the shared-grid
    path. `SelectionStress` now references the main project instead of hand-linking
    copied math files. Pure refactor: build, `SelectionStress`, `SimReplay`, and
    `ReviewGate architecture` pass; full `VerifyAll` passed 23/23 after this slice.
[x] Shared `WeaponMath` helper: `WeaponRange(...)` was reimplemented in 6 systems
    (Combat, BuildingTargetCombat, Command, Movement, Separation, TurretCombat) and
    damage math in 3 (Combat, BuildingTargetCombat, TurretCombat). DONE: extracted
    `scripts/core/sim/weapon/WeaponMath.cs` with composable cores - `MaxMountRange`,
    `MaxRangeAndCooling` (single-pass for hot anchor checks), `EffectiveRange`
    (deploy-aware), `ResolveTargetProfile`, `BaseDamage`. Each caller keeps exact
    semantics (deploy multiplier opt-in; only CombatSystem layers RNG jitter; only
    buildings keep the missing-spec guard). All 6 hand-written range loops removed;
    pure refactor verified - SimReplay 45/45 hashes byte-identical to baseline.
[x] KNOWN BUG (found during M9, pre-existing) FIXED: `combat-behavior` tool crashed -
    `UnitKindDesignBridge.KindForDesignId("dog.sky_patrol_aircraft")` threw
    KeyNotFoundException because new data-only air units have no legacy UnitKind. Fix:
    the bridge-coverage check now filters to designs that actually have a legacy kind
    (`.Where(TryGetKindForDesignId)`), so adding a unit no longer requires touching the
    dying UnitKind enum (M1/brick-style). Also repaired ReviewGate text-drift: deploy
    range-multiplier check repointed to `WeaponMath.EffectiveRange`; two stale
    CombatBehavior assertion substrings re-synced. VerifyAll green again.
[x] Building entity mutable outlet cleanup: the code invariants are done (private
    `BuildingEntityByTargetId`, public id-based `BuildingEntityIdByTargetId` fail-closed,
    CombatBehavior test helper isolation); review record at
    docs/reviews/2026-07-01-building-entity-mutable-outlet-cleanup.md. Gates passed:
    local build, `CombatBehavior`, `ReviewGate buildingentitymutableoutletcleanup`,
    and review-record gate.

Single responsibility - god-class breakup:
[x] File-size discipline guard: M1 now treats the user's thresholds as architecture
    policy: < 200 / 200-400 / 400-600 / > 600 / > 1000. `ReviewGate filesize`
    lives in `tools/ReviewGateFileSize/FileSizeGate.cs`, and full ReviewGate runs it.
    New C# files over 600 lines fail by default, stable subsystem entrypoints are
    required, vague helper-style filenames fail, bridge/legacy growth is capped, and
    same-prefix/directory crowding is surfaced as warnings. The validation system
    itself is stricter: every `tools/ReviewGate/**/*.cs` source file must stay at
    200 lines or below; the old monolithic `Program.cs` and historical
    `ReviewGateChecks` aggregate cannot return. Follow-up (2026-07-02, #69):
    moved the weapon engagement domain cluster to `scripts/core/sim/weapon/`, dropping
    the `scripts/core/sim/` root from 31 to 25 C# files so `ReviewGate filesize
    --max-warnings=0` is clean again.
[x] ReviewGate system budget cleanup: the old ~16k-line validation system is no
    longer preserved as hundreds of historical C# checks. `ReviewGateRunner` and
    `ReviewGateRegistry` now keep the stable command surface, historical narrow
    modes are discovered from TODO/docs and routed to broad domain gates
    (architecture/content/presentation/regression), and `FileSizeGate` enforces a
    1000-line runner budget for `tools/ReviewGate`, a 1000-line budget for every
    validation tool suite, and a 200-line per-file ceiling for the ReviewGate
    family. Build output is redirected to `artifacts/dotnet/ReviewGate`, and
    `ReviewGate filesize` now fails if `tools/ReviewGate/bin` or
    `tools/ReviewGate/obj` returns. The main Godot csproj also excludes `.godot`,
    `artifacts`, and `tools` C# generated/tool sources from gameplay compilation.
    ReviewGate runner current source budget: 9 C# source files / 567 total lines; largest C# file tools/ReviewGate/ReviewGateEvidence.cs has 148 lines. `ReviewGate filesize` now also fails if this exact source-budget
    evidence drifts from TODO or the review record. Validation tool suites current source budget: 135 C# source files / 18415 total lines across 53 suites; largest C# file tools/CombatBehaviorSkirmish/SkirmishAi.cs has 393 lines; largest suite tools/ReviewGateDomains has 881 lines. Full `ReviewGate`, historical narrow mode
    samples, and `presentation --max-warnings=0` pass with 0 errors / 0 warnings;
    full `VerifyAll` passes 23/23.
[x] `GameText` red-line split: the old 695-line localization file is now a tiny API
    shell plus `GameText.English.cs` and `GameText.ChineseSimplified.cs` partial
    dictionaries. Current max file is 363 lines, below the 400-line normal ceiling;
    `GameText.cs` was removed from the file-size debt whitelist.
[x] `UnitBattlefield` god-file split: the old 3991-line live runtime facade is now a
    partial family split by responsibility: core queries, harvest/repair, building
    lifecycle/projection/sync/state, selection/picking, production/rally, commands,
    visibility/combat bridges, EntityWorld system stepping, command bridge/apply,
    runtime sync, and legacy utilities. Current max `UnitBattlefield.*.cs` companion
    is 359 lines, under the 400-line normal ceiling; `UnitBattlefield.cs` was removed
    from the file-size debt whitelist. The companion family now lives under
    `scripts/core/units/runtime/battlefield/`, keeping the stable entry point in place.
[x] `CombatSystem` god-file split: the old 1234-line combat system is now a small
    partial system family. `CombatSystem.cs` keeps the `ISimSystem` entry point and
    orchestration; target search, guard resolution, target state/memory, autonomy,
    engagement, damage, and broadphase grid live in focused
    `scripts/core/sim/systems/combat/Combat*System.cs` companion files.
    Current max combat-system file is 210 lines, all under 400 and
    most under 200; `CombatSystem.cs` was removed from the file-size debt whitelist.
[x] Domain-directory consolidation: `GameState.*.cs`, `HudLayer.*.cs`,
    `UnitBattlefield.*.cs`, and `Combat*System` companions now live under focused
    domain directories while their stable entry files remain in place. `ReviewGate`
    evidence readers and QA source readers are partial-aware across those directories.
    Follow-up consolidation moved the remaining `scripts/core` root files into
    focused `ai`, `build`, `combat`, `commands`, `economy`, `factions`, `fog`,
    `localization`, `match`, `pathing`, `presentation/*`, `production`, `sandbox`,
    `signal`, `terrain`, and `units` directories; the root now keeps only the stable
    `GameState.cs` entry point.
[x] Yellow-file cleanup sprint: `PathfindingMath`, `CommandSystem`,
    `ConstructionSystem`, `ResourceSystem`, `AbilitySystem`, `FogOfWarMap`,
    `CombatEffectsLayer`, `FootprintLayer`, `MovementSystem`,
    `UnitBattlefieldEnemyProductionAi`, style/showcase roots, `tools/SelectionStress`,
    and `tools/CounterReadabilityQa` were split into focused partials or suite files.
    The yellow watchlist is cleared; no C# file is over 400 lines, and no C# file is
    over the 600-line red line. Full `ReviewGate` passes with 0 errors / 0 warnings,
    and `VerifyAll` passes 23/23.
[ ] Converge the three combat systems: `CombatSystem` partial family + `TurretCombatSystem`
    (318) + `BuildingTargetCombatSystem` (265) = 1993 lines running the same loop
    (acquire -> range -> rotate -> fire -> damage -> cooldown). Difference is only
    "can it move" / "has a mount" - a DATA difference, not a code one. Long-term
    target: one `WeaponEngagementSystem` over any `WeaponUser`-bearing entity, movement
    gated by presence of a `Movement` component. Cuts ~500 duplicated lines and honors
    "new content = data, not a new system". (Range/damage math already shared via
    `WeaponMath` from M9 debt #1; this is the remaining structural convergence.)
    Progress: first convergence slice extracted shared weapon-engagement primitives:
    `WeaponEngagementMath` owns cooldown clamp, mount turn rate, rotate-toward, and
    aim tolerance; `WeaponEngagementQueries` owns target-kind and any-mount
    targetability; `WeaponEngagementState` owns mount cooldown/write storage helpers;
    `WeaponMath` now also owns target profile priority and targetability. The three
    systems keep their distinct scheduling/visibility/movement semantics for now, but
    no longer carry separate copies of those low-level rules. Verified with build,
    SimReplay, CombatBehavior, CounterReadabilityQa, ReviewGate architecture, and
    ReviewGate filesize. Follow-up: `WeaponEngagementResolution` now owns shared
    `WeaponFiredEvent` emission, tracking-projectile spawn, immediate damage
    dispatch, and projectile-impact dispatch. `CombatSystem`, `TurretCombatSystem`,
    `BuildingTargetCombatSystem`, and `ProjectileSystem` call that shared path while
    preserving CombatSystem's shield/retaliation/veterancy damage authority.
    Verified with build, SimReplay, CombatBehavior, ReviewGate architecture, and
    ReviewGate filesize. Follow-up: `WeaponEngagementMountLoop` now owns the shared
    mount loop for cooldown tick, mount rotation, fire authorization, damage mode,
    fire-only-one-mount turret policy, and fire-anchor policy. The three combat
    systems now choose target/movement semantics, then pass explicit options into the
    same runner. Verified with build, SimReplay, CombatBehavior, CounterReadabilityQa,
    ReviewGate architecture, and ReviewGate filesize.

Wiring & coverage gaps (found during the sweep):
[x] Shared live `SimSystemPipeline`: `BattleRoot.ConfigureEntityWorld()` now calls
    `SimSystemPipeline.ConfigureLiveGameplay(...)` instead of hand-registering the
    authoritative system order, and `tools/SimReplay` has
    `AssertLiveSimSystemPipeline()` using the same factory as executable coverage.
    ReviewGate now checks the shared pipeline evidence instead of brittle
    `BattleRoot` AddSystem strings. Note: `BuildingTargetCombatSystem` is a
    UnitBattlefield migration bridge, not a full-pipeline system; it must not be
    mixed with `CombatSystem`/`TurretCombatSystem` in the same live pipeline until
    the combat convergence item above removes the double-resolution risk. Full
    `VerifyAll` passed after this slice.
[ ] Per-tick allocation paydown: `SpatialGrid<T>` now exists (M9 debt #2) with an
    allocation-free struct enumerator, but the abstraction still allocates more than the
    old raw inline dict loops (PerfSmoke ~333k bytes/tick @400u vs ~140k pre-grid; gate
    is time-based and green). Reuse buffers and eliminate per-`Step` `new List`/`new
    Dictionary`/LINQ in hot systems (ConstructionSystem ~14, TurretCombat ~9, Production
    ~7, Command ~7 alloc-ish sites). Only pursue if profiling shows GC is a real cost;
    keep identical replay hashes. Progress: M9 turret/building-target allocation slice
    removed the LINQ target-selection chain from `TurretCombatSystem`, removed
    per-engage mount `ToArray()` copies from `TurretCombatSystem` and
    `BuildingTargetCombatSystem` by reusing writable mount storage, and made
    `WeaponEngagementState.CoolMountsCopy` allocate only when a cooldown actually
    changes. Replay hashes stayed identical; CombatBehavior and CounterReadabilityQa
    passed. Follow-up: M9 production/pathfinding/projectile allocation slice removed
    the per-tick `ProjectileSystem` entity array snapshot, changed `ProductionSystem`
    to reuse its producer tick snapshot and spawn-obstacle storage, made
    `ProductionSpawnMath` keep candidate directions/ring scales as static data, and
    made `PathfindingSystem` reuse shared-corridor planned/group/member/assignment
    buffers plus blocker de-duplication storage. `ProductionSystem` was split with
    `ProductionSystem.Spawning.cs` to keep file-size governance green, and
    `ReviewGate simhot` now locks these hooks. Verified with build, SimReplay,
    CombatBehavior, PerfSmoke, `ReviewGate simhot`, `ReviewGate regression`, and
    `ReviewGate filesize`; full `VerifyAll --skip-perf` passed 22/22 including
    Godot headless QA. PerfSmoke still reports 400u alloc/tick at 192620 bytes,
    so the broad item remains open. Remaining allocation debt is mostly
    Construction/Command, immutable queue/path arrays, placement-list construction,
    and broader profiler-guided GC cleanup. Follow-up: #63 added a broad
    `ReviewGate simhot` regression check for the projectile projection presentation
    path, locking #61/#62 so `CombatEffectsLayer.ActiveEffectCount` uses the count
    API and `DrawProjectiles()` reuses a caller-owned projection buffer instead of
    allocating a render-ready list. Follow-up: #64 reused `CommandSystem` group
    move / group attack scratch buffers for owned-subject lists and slot lookup
    dictionaries, removing the command-layer `ToList()` / `ToDictionary()` allocations
    while preserving SimReplay group-move and group-attack hashes. Follow-up: #65
    added `AttackSlotMath.AssignAttackSlotsInto(...)` and routed `CommandSystem`
    group attack through caller-owned assignment, ordered-unit, anchor, mover, and
    free-slot buffers; SimReplay group-attack hashes stayed unchanged. Follow-up:
    #66 added `FormationMath.CreateMoveDestinationsInto(...)` and routed
    `CommandSystem` group move through caller-owned destination, ordered-unit, slot,
    and remaining-slot buffers; SimReplay group-move hashes stayed unchanged.
    Follow-up: #67 split `ConstructionSystem` placement query helpers into
    `ConstructionSystem.PlacementQueries.cs`, keeping placement behavior unchanged
    while reducing `ConstructionSystem.Queries.cs` file pressure before any later
    placement-list allocation paydown. Follow-up: #70 reused ConstructionSystem
    placement validation buffers for build anchors, footprint obstacles, and build
    visibility sources, replacing the placement helper LINQ `ToList()` paths and
    locking the no-list-construction contract in `ReviewGate simhot`. Follow-up:
    #71 reused ConstructionSystem required-building and construction-subject
    ordering buffers, replacing the remaining construction command
    `RequiredBuildings.OrderBy(...)` / `Subjects.OrderBy(...)` paths and locking
    them in `ReviewGate simhot`. Follow-up: #72 removed `PathfindingSystem`
    shared/single path `ToArray()` copies before `PathfindingComponentState`,
    reusing non-empty `PathfindingMath` path results directly while keeping the
    one-point goal fallback and locking the no-copy path in `ReviewGate simhot`.
    Follow-up: #73 reused `CommandSystem` scalar movement/combat/stance subject
    buffers for Move, Patrol, Guard, Attack, Stop, and Stance commands, replacing
    those `OwnedSubjects(...)` yield-iterator paths while leaving harvest/repair
    and selection as separate child slices. `ReviewGate simhot` now routes
    command allocation evidence through `CommandSystemAllocationReviewGate`.
    Follow-up: #74 reused a `CommandSystem` selection subject id set, replacing
    the `SetSelectionEntityCommand` `Select(...).ToHashSet()` path and locking the
    no-`ToHashSet()` selection contract in `ReviewGate simhot`. Follow-up: #75
    routed Harvest, AutoHarvest, and Repair commands through the reusable scalar
    subject buffer, removed the old `OwnedSubjects(...)` yield helper, and replaced
    AutoHarvest's nested `HarvestEntityCommand` / one-entity array allocation with
    direct `ApplyHarvestIntent(...)` reuse. `ReviewGate simhot` locks the no-iterator
    and no-nested-harvest-command contracts. Follow-up: #77 reused
    `EntityCommandBuffer` ordered snapshot, ready command, and removal sequence
    buffers for `DrainUpToTick(...)`, replacing the drain path
    `Where(...).ToList()` / `Select(...).ToHashSet()` allocations while preserving
    tick / issuer / sequence ordering. `ReviewGate simhot` locks the no-LINQ drain
    contract. Follow-up: #79 reused an `AbilitySystem` cooldown scratch buffer for
    cooldown ticking and `SetCooldown(...)`, replaced cooldown `Any(...)` queries with
    explicit loops, and extended `ReviewGate simhot` to forbid the old
    `runtime.Cooldowns.ToArray()` / `Append(...).ToArray()` / cooldown `Any(...)`
    paths. Verified with build, SimReplay, ReviewGate simhot, full ReviewGate, and
    full VerifyAll 23/23. Follow-up: #80 reused UnitBattlefield construction
    ticket buffers for queue/place bridge before-id tracking and ready-ticket
    projection, replacing the construction ticket `ToHashSet()` and
    `Where(...).OrderBy(...).LastOrDefault()` chains while preserving public
    ready-ticket snapshot semantics. `ReviewGate simhot` locks the bridge evidence.
    Verified with build, PlayerLoopQa, ReviewGate simhot, full ReviewGate, and full
    VerifyAll 23/23. Follow-up: #81 reused UnitBattlefield selection buffers for
    single-select, same-unit select, and building-select command bridges, replacing
    selection `ToHashSet()` / `new HashSet<EntityId>()` paths while preserving
    `SubmitSelectionCommand` ordering semantics. `ReviewGate simhot` locks the
    selection bridge evidence. Verified with build, SelectionStress,
    ReviewGate simhot, full ReviewGate, and full VerifyAll 23/23. Follow-up:
    #82 reused UnitBattlefield harvest/repair command buffers and removed refinery
    validation snapshot allocations; #83 reused unit/building death-removal buffers;
    #84 reused UnitBattlefield production sync buffers with an explicit queued-before
    snapshot type. `ReviewGate simhot`/`regression` lock the bridge evidence.
    Verified with build, CombatBehavior, PlayerLoopQa, ReviewGate simhot/regression,
    full ReviewGate, review-record gate, and full VerifyAll 23/23. Follow-up:
    #88 reused the internal UnitBattlefield selection command entity buffer,
    replacing `SubmitSelectionCommand(...)` subject `Where/Distinct/OrderBy/ToList`
    materialization with explicit valid-id collection, duplicate scan, and in-place
    `EntityId` sort. `ReviewGate simhot` locks the no-LINQ selection command
    subject path. Follow-up: #89 reused UnitBattlefield construction subject
    building-id and entity-id buffers, replacing `ConstructionSubjectEntities(...)`
    snapshot/order `ToList()` materialization with explicit matching, in-place
    building-id sort, and subject entity fill. `ReviewGate simhot` locks the
    no-LINQ construction subject bridge path. Follow-up: #90 reused a selected-
    building rally producer-id buffer for both `SetSelectedBuildingRallyPoints(...)`
    overloads, replacing selected/producers `ToList()` materialization with an
    explicit selected-building scan and in-place producer sort. The helper lives in
    `UnitBattlefield.ProductionRallySelection.cs` to keep `ProductionRally.cs`
    below the yellow file-size threshold, and `ReviewGate simhot` locks the
    no-LINQ selected-building rally bridge path. Verified #88-#90 together with
    build, SelectionStress, CombatBehavior, PlayerLoopQa, ReviewGate simhot, full
    ReviewGate, review-record gates, and full VerifyAll 23/23.

Discipline (keep it from regressing):
[x] Analyzer/gate for residual debt: ReviewGate now FORBIDS re-rolling
    `(int X,int Y) Cell(Vector2` in combat/vision/separation (M9 debt #2).
    DONE (2026-07-02, #68): `ReviewGate architecture` now also scans
    `scripts/core/sim/systems/**/*.cs` and fails if a private
    `WeaponRange(...)` helper returns to a system/command partial. The remaining
    non-deploy range math lives in `WeaponMath.BaseRange(...)`, while mobile
    deploy-aware range still uses `WeaponMath.EffectiveRange(...)`. System file
    red lines are covered by `ReviewGate filesize`, which fails unregistered C#
    source over 600 lines.
[ ] Comment discipline: critical public/internal APIs, system entrypoints, cross-layer
    boundaries, deterministic/performance invariants, compatibility bridges, and
    non-obvious algorithms must explain responsibility and safe usage. Do not require
    mechanical comments on every function; simple private functions should stay
    self-explanatory through names. ReviewGate should eventually catch missing comments
    only on these high-risk surfaces, not enforce blanket template comments.
[x] TODO hygiene: file had grown to ~3141 lines (verbose slice-by-slice "Progress:"
    notes from many codex sessions). DONE (2026-07-01): archived M1's ~1490-line
    completed detail to docs/TODO-Archive-2026-06-29.md ("M1 Full Completed Detail"),
    replaced it with a concise status + 2 open items, and merged the empty Roadmap
    header into the AI Execution Matrix. File is now ~1650 lines and scannable; open
    items (90) preserved. Re-run this compression when a milestone's done-detail bloats
    the living plan again (M2's 16 progress-notes are the next candidate once it closes).

### M10 - Brick-Style Content Authoring (adding content "like drinking water")

The promise: adding a new unit / building / weapon / effect / terrain must be pure
DATA - one new spec file, zero system edits. If adding content requires changing a
system, the model is wrong; fix the model. This is the payoff of the ECS core.

[x] Audit current authoring friction: list every step needed today to add (a) a new
    unit, (b) a new building, (c) a new weapon/ammo, (d) a new turret. Any step that
    is NOT "write a data/spec file" is a friction point to remove.
[ ] Single declarative spec path: a new unit is one `UnitDesign`/`EntitySpec` file
    discovered by the catalog automatically (no manual registry edits, no enum
    additions, no UnitKind bridge - see M1 cleanup). Same for `BuildSpec`,
    `WeaponDefinition`/`AmmoDefinition`, turret entity specs.
    Progress: unit, building, weapon, ammo, and turret-backed building data now live
    as focused `UnitDesign` / `BuildingDesign` / `WeaponDesign` / `AmmoDesign`
    classes discovered by catalog reflection. Weapon/ammo runtime identity is now a
    string id; `WeaponKind`/`AmmoKind` remain only as legacy aliases for existing
    content. Tool-local throwaway unit/building/weapon/ammo specs are discoverable
    through explicit assembly scans without polluting runtime catalogs. Remaining
    friction: localization keys, building sort order, and turret art recipes.
[x] Content registry by reflection/convention: catalogs discover specs by scanning the
    authoring assembly or a data folder, so dropping in a file is enough. Deterministic
    discovery order (sorted by id) to keep replay stable.
    Progress: `UnitDesignCatalog`, `BuildSpecCatalog`, and `WeaponCatalog` now scan
    concrete design classes and preserve deterministic ordering. Unit/building/
    weapon/ammo catalogs support explicit assembly scans for QA/tool content, and
    `ContentAuthoringQa` proves a brand-new string weapon/ammo id can be injected
    into one `EntityWorld` and fight without enum, catalog, or system edits.
[x] Authoring validation: a `tools/ContentAuthoringQa` (or extend RosterAuthoringQa)
    that proves a brand-new sample unit + sample building + sample weapon load, spawn,
    and behave through the generic systems with NO system file changed in the diff.
    Final gate: `tools/ContentAuthoringQa` is wired into `VerifyAll`; it counts all
    concrete design classes, verifies catalog coverage/references, projects
    UnitDesign/BuildSpec data to `EntitySpec`, spawns a unit plus turret through
    one live `EntityWorld` tick, and now uses QA-local throwaway UnitDesign /
    BuildingDesign / WeaponDesign / AmmoDesign classes to prove discovery, generic
    combat, generic construction, string-id weapon/ammo injection, and no runtime
    catalog pollution.
[x] Documentation: a short "How to add X" recipe per content type, each ending in
    "rebuild, it appears - no system code touched". See
    `docs/ContentAuthoringRecipes.md`.
[x] Verify: add one throwaway test unit and one test building purely as data; confirm
    they spawn/fight/build via existing systems and VerifyAll stays green; then remove
    them. The diff for adding them must touch only data files. Final proof keeps
    throwaway data inside `tools/ContentAuthoringQa` instead of runtime catalogs:
    the sample unit fights through generic combat with a tool-local string weapon/
    ammo id, and the sample building completes through `ConstructionSystem`;
    `ContentAuthoringQa` and review records
    `2026-07-01-m10-content-authoring-throwaway-proof.md` and
    `2026-07-01-m10-weapon-ammo-string-authoring.md` lock the behavior.

### M11 - Map Authoring Pipeline (two authors, one data format)

Maps must be authorable two ways that produce the SAME pure-data `MapSpec`: (1) AI /
seeded / procedural generation for skirmish, and (2) hand-designed in the Godot editor
for campaign/story maps that need specific terrain, ambush points, trigger zones, and
narrative nodes. The deterministic sim reads only `MapSpec` and never knows which
author produced it. (Supersedes the old "No map editor" non-goal, which is removed.)

Design (Option C - Godot scene as the human canvas, baked to pure data):
[x] `MapSpec` (pure C# record, NO Godot types): terrain grid, spawn points per owner,
    resource nodes, build zones/radius seeds, trigger areas, narrative/objective nodes,
    map metadata (name, size, recommended players). This is the single authority the
    sim and `tools/SimReplay` load.
[x] `MapLoader`: seeds an `EntityWorld` from a `MapSpec` deterministically; headless
    tools load the same `MapSpec` so a hand-designed map is replay-testable.
[x] Author path A - procedural/AI: parameters or a seed -> `MapSpec` generator
    (the existing seeded fair-map generation produces a `MapSpec`).
[x] Author path B - human/Godot editor: design a map as a Godot scene (`.tscn`) by
    placing Marker2D/Area2D nodes for spawns, resources, build zones, triggers, and
    narrative beats; a `MapBakeTool` (editor export step / plugin) bakes the scene into
    a pure-data `MapSpec`. The bake tool is the ONLY thing that touches `.tscn`; the sim
    never reads scenes.
[x] Campaign hooks: trigger areas and objective/narrative nodes in the baked `MapSpec`
    feed the future objective-graph campaign system (M8), so story map design and
    trigger design happen on one canvas.
[x] Verify: bake one hand-designed sample map AND generate one seeded map; confirm both
    load through `MapLoader` into a playable match and run deterministically in
    `tools/SimReplay`; `MapSpec` stays Godot-free (ReviewGate guard).
    Progress: `scripts/core/map/MapSpec.cs` is Godot-free data, `MapLoader` seeds
    `EntityWorld`, `SkirmishMapGenerator.GenerateSpec` converts the existing fair map
    into `MapSpec`, and `tools/MapAuthoringQa` bakes
    `tools/MapAuthoringQa/fixtures/hand-designed-map.tscn` into the same format.
    `tools/SimReplay` runs `map-spec-loader` deterministically, `VerifyAll` includes
    `map-authoring-qa`, and `ReviewGate mapauthoring` locks the `.tscn` boundary.
    Residual integration risk: live `GameState` / `UnitBattlefield` presentation still
    keeps compatibility seeding paths, so a later campaign slice should route real
    story-map boot through `MapLoader` instead of only proving EntityWorld loading.

## Performance Optimization Plan

Target: stable 60 FPS at 1920x1080 with 200+ live units, full fog, and combat VFX;
sim under budget at 30Hz. Measure before/after. Metrics (docs/RTS99Design.md "性能"):
`simulationTickMs`, `renderMs`, `entityCount`, `projectileCount`, `effectPoolUsage`,
`fogUpdateMs`, `pathRequestsPerSecond`.

### Instrumentation first
[x] Headless `tools/PerfSmoke`: 50/100/200/400 units x 1200 ticks, percentile sim-step
    ms, regression gate (worst avg < 50% of 33.3ms budget). Baseline: 50u 0.46, 100u
    0.35, 200u 1.27, 400u 8.41ms (after VisionSystem broadphase).
[x] In-engine `PerfHud` overlay (toggle): FPS, frame ms, sim-step ms, render ms, live
    entity/projectile/effect/visible-unit counts, fog update ms.
    Progress: `PerfHudLayer` is installed by `BattleRoot`, hidden by default,
    toggleable with F3, and can default on with `PROCEDURAL_RTS_PERF_HUD=1`.
    It displays FPS, last/average frame ms, 1%-low frame time/FPS, process ms,
    render/wait estimate, sim-step ms, live entity/unit/visible-unit counts,
    projectile/effect counts, fog update ms, and fog texture upload count.
    `ReviewGate perfhud` verifies hooks and `Godot_v4.7... --headless --scene
    res://scenes/Battle.tscn --quit-after 2` starts the battle scene cleanly.
[x] Per-system step ms inside `EntityWorld.Step` behind a debug flag.
    Progress: `EntityWorld.SystemTimingEnabled` (or
    `PROCEDURAL_RTS_SIM_TIMING=1`) now enables per-system Stopwatch timing into
    `SimMetrics.SystemTimings`; default remains off so normal sim hot paths avoid
    timing overhead. `SimReplay` asserts timing stays off by default and records
    samples when enabled. `ReviewGate timing` verifies the debug flag, metrics, and
    test hooks.
[x] `PresentationMetrics`: rolling averages + 1%-low frame time (spikes, not just mean).
    Progress: `PresentationMetrics` now keeps a fixed rolling frame window with
    average frame/process/sim-step ms plus 1%-low frame time/FPS. `BattleRoot`
    records `_Process` frame delta, process cost, and EntityWorld shadow step cost
    every frame, giving the future `PerfHud` a stable data source. `SimReplay`
    verifies rolling eviction and spike-sensitive 1%-low behavior; `ReviewGate
    presentationmetrics` verifies the instrumentation hooks.

### Camera & frame rate
[x] Frame-rate-independent camera smoothing (`1 - exp(-k*dt)`), identical feel at 30/60/144.
[x] Damped camera/minimap jumps; throttle dependent redraws to actual camera-rect changes.
    Progress: `CameraController` now keeps target position/zoom and smooths actual
    camera motion with `CameraInputMath.ExponentialSmoothingFactor`
    (`1 - exp(-k*dt)`). `FocusOnWorldPoint` moves the target for damped minimap
    jumps, and `ViewChanged` fires only when actual position/zoom changes so culling
    can refresh from real camera-rect motion. `ReviewGate camera` verifies the
    smoothing hooks. Completed: `CameraInputMath.SmoothToward` centralizes the
    exponential integration, `CameraController` uses it for pan/zoom, and
    `SelectionStress` simulates 30/60/144Hz smoothing to verify matching results.
    Follow-up: `ViewChanged` now uses a 50ms notification throttle while preserving
    immediate large-jump updates, reducing repeated culling/fog refresh during
    smooth pans. `ReviewGate camera` and the camera/fog smoothness review record
    cover the change.
[x] Frame cap / vsync setting (Off/VSync/60/144) persisted in `DisplayAudioSettings`.
[x] Set `Engine.MaxFps`/`PhysicsTicksPerSecond` intentionally; sim authority stays on `SimClock`.
    Progress: `FrameRateMode` now supports Off/VSync/60/144 and is loaded/saved
    through `DisplayAudioSettings` under `settings.cfg` display data.
    `ApplyFrameRateMode` controls `DisplayServer.WindowSetVsyncMode`,
    `Engine.MaxFps`, and intentionally sets `Engine.PhysicsTicksPerSecond = 60`
    without touching `SimClock` authority. `SettingsOverlayLayer` exposes the mode
    in the settings UI with localized labels. `ReviewGate display` verifies the
    persistence/apply/UI hooks. `scenes/DisplaySettingsQa.tscn` verifies
    Off/VSync/60/144 apply the expected runtime caps in Godot headless.

### View redraw & culling (biggest current render cost)
[x] Stop unconditional per-frame `QueueRedraw()` in every view; redraw on dirty/throttle.
    Progress: `GridLayer` and `SignalNetworkLayer` no longer redraw every frame;
    `PathDebugLayer` redraws only while enabled. `ReviewGate presentation` dropped
    from 16 to 6 redraw warnings. `ResourceFieldView` now redraws at 20Hz.
    `SelectionController` redraws at 60Hz while dragging and 30Hz while idle;
    `FootprintLayer` redraws at 30Hz, bringing the baseline from 16 to 9.
    Follow-up: `BuildingView` redraws at 20Hz; `UnitInstanceView` and legacy
    `UnitView` redraw at 30Hz while keeping position updates per-frame.
    `ReviewGate presentation` is now down from 9 to 6 warnings, all menu/showcase
    roots.
    Completed: menu/showcase roots now redraw at 20Hz or on interaction, bringing
    `ReviewGate presentation` to 0 warnings.
[x] Off-screen culling via `CameraController.VisibleWorldRect()` (+margin) /
    `VisibleOnScreenNotifier2D`; skip `_Draw` and hide off-screen views.
    Progress: `BattleRoot.RefreshViewCulling()` now uses camera visible rect +
    margin to hide and disable processing for building, legacy unit, UnitInstance,
    and resource field views. `ReviewGate culling` verifies the centralized pass.
    Completed: combat effects, command acknowledgements, and footprint layers now
    receive the same culling rect and skip off-screen drawing while preserving state
    updates. `ReviewGate culling` verifies view + overlay coverage.
[x] `GridLayer`: cache static grid (texture/MultiMesh) or draw only the visible rect.
    Progress: first pass removed per-frame full-map redraw and tile regeneration;
    full texture/cache work remains open.
    Follow-up: `TerrainFloorMath.CreateTileLayout` + `GridLayer` layout cache now
    keep terrain rect/kind/noise stable across theme redraws; theme changes only
    re-apply palette colors. `ReviewGate grid` verifies that GridLayer no longer
    calls themed `CreateTiles(WorldSize, palette)` during drawing. Full rendered
    texture/MultiMesh or visible-rect drawing remains open.
    Completed: `BattleRoot.RefreshViewCulling()` now feeds the camera culling rect
    to `GridLayer.VisibleWorldRect`, and `GridLayer` filters floor panels, strata,
    survey marks, command washes, water highlights, and trace lines against a
    grown visible draw rect. `ReviewGate grid` verifies the visible-rect hooks,
    and Godot headless Battle scene startup passes.
[ ] Batch unit bodies (`MultiMeshInstance2D` or per-design atlas) so 200+ units are not
    200+ `CanvasItem._Draw` passes of many `DrawCircle`/`DrawArc`.
    Progress: `UnitBodyRenderRecipeCache` now precompiles `UnitArtRecipe`
    body/mount/runtime-pulse layer groups and closed polygon points once per
    recipe, so `UnitVisualRenderer` no longer filters/groups art layers or
    allocates mount lists during each unit draw. `ReviewGate unitbodyrendering`
    locks this first cache slice. Follow-up: `UnitInstanceView` now uses a
    redraw signature so static, unselected units do not keep issuing fixed 30Hz
    redraws; selected/alert/command-pulse and changed-state units still redraw
    promptly. True MultiMesh/per-design atlas batching remains open.
[x] Pool combat VFX/footprints; cap concurrent effects; fade oldest under load.
    Progress: `CombatEffectsLayer` now pools reusable unit-death effects and applies
    soft/hard budgets; effects beyond the soft budget fade out quickly and hard
    overflow returns to the pool. `FootprintLayer` now applies soft/hard mark
    budgets and accelerates old marks under load. `ReviewGate vfx` verifies the
    budget/fade/pool hooks. Full VFX pooling remains open for other effect families
    and any future pooled projectile/impact layers.
    Follow-up: `CommandAcknowledgementLayer` now pools reusable ring effects with
    soft/hard ring budgets and under-load fade-out. `ReviewGate vfx` verifies the
    ring pool/budget hooks. Completed: `CombatEffectsLayer` now also pools impact
    flash effects from combat hit callbacks with soft/hard budgets and under-load
    fade-out. Current projectile/beam visuals are drawn from gameplay projectile
    and beam models rather than spawned pure VFX entities. `ReviewGate vfx` now
    locks all current pure presentation VFX families to pooled/budgeted paths.

### Fog of war rendering
[x] Reuse the fog mask `ImageTexture` buffer; upload only when visibility changed.
    Progress: `FogOfWarMap` already reused `_maskImage`/`_maskTexture`; it now also
    tracks previous visible/explored mask strengths and only marks the texture dirty
    when `MaskChangedSincePreviousUpdate()` detects actual mask data changes.
    `MaskRevision` and `MaskTextureUploadCount` provide QA instrumentation, and
    `tools/FogOfWarQa` asserts that unchanged vision sources do not dirty the mask
    while changed vision does. `ReviewGate fog` verifies the cache/dirty hooks.
    Follow-up: `FogOfWarMap` now also short-circuits unchanged vision-source
    signatures, keeps dirty mask ranges, and caches stats. `tools/FogOfWarQa`
    proves the 100-source unchanged-source smoke at 4ms (<3500ms).
[x] Scope fog recompute to camera rect (+margin) for maps larger than the screen;
    keep off-screen explored memory cached.
    Progress: `BattleRoot.RefreshViewCulling()` now feeds the camera culling rect
    into `FogOfWarLayer`, and the world fog layer requests
    `FogOfWarMap.MaskTexture(VisibleWorldRect)`. `FogOfWarMap` updates only the
    corresponding mask-cell range for partial world draws, while full calls keep
    the minimap/all-map path intact and partial updates leave off-screen dirty
    memory pending until a full update. `ReviewGate fog` and `FogOfWarQa` verify
    the scoped mask path.
[x] Fog quality tier (Low/Med/High): mask resolution + `WorldRedrawIntervalSeconds`.
    Progress: `FogQualityTier` now defines Low/Medium/High; `FogOfWarVisualPolicy`
    maps tiers to mask cell sizes and redraw intervals; `GameState` constructs
    `FogOfWarMap` with the selected tier and `FogOfWarLayer` uses the same tier
    for redraw throttling. `FogOfWarQa` verifies Low/High mask sizes and redraw
    interval ordering.
    Follow-up: default cadence is tightened to 0.12s, while `FogOfWarLayer`
    redraws on mask revision or meaningful camera-scoped rect change instead of
    blind timer redraws. `ReviewGate fog` keeps the scoped/dirty contract.
[x] Minimap consumes the cached fog mask (already migrated); no per-refresh re-sampling.
    Progress: `BattleRoot.RefreshMinimap()` feeds `_state.FogOfWar.MaskTexture()`
    to HUD, and `FogOfWarQa` checks runtime scripts do not use `FogOfWar.Snapshot()`
    for normal world/minimap rendering.

### Simulation step cost
[x] Broadphase `CombatSystem.NearestHostile`; reuse scratch buffers in hot systems
    (Combat/Movement/Separation) to cut per-tick allocations / GC.
    Progress: `MovementSystem` now reuses a shared `SpatialGrid<LocalAvoidanceBody>`
    for local avoidance queries, avoiding a second grid dictionary style while preserving
    replay hashes. `VisionSystem` now reuses owner/viewer/grid scratch storage while
    preserving deterministic owner order. `PerfSmoke` now reports `alloc/tick`, and
    `ReviewGate simhot` verifies the scratch-buffer hooks. Remaining allocation
    pressure is still significant (400u PerfSmoke ~284KB/tick), so Combat mounts,
    event drain, and other hot-path allocations remain open.
    Completed: `CombatSystem.NearestHostile` now uses a reusable `_targetGrid`
    broadphase with deterministic EntityId tie-breaking, `SeparationSystem` now
    reuses deterministic collision buckets instead of allocating its bucket map each
    tick, and `ReviewGate simhot` verifies Movement, Vision, Combat, Separation,
    event-drain, and PerfSmoke allocation instrumentation hooks. Current PerfSmoke
    passes at 400 units with avg 1.164ms and 115204 bytes/tick.
    Follow-up: `CombatSystem.NearestHostile` now uses a per-tick reusable target
    broadphase grid instead of scanning every entity for each attacker, while
    preserving EntityId tie-break determinism. `ReviewGate simhot` verifies the
    combat broadphase hooks. Current PerfSmoke after this slice: 400u avg 1.169ms,
    p99 1.821ms, alloc/tick ~188KB. Combat mount list updates and event drain
    allocations remain open.
    Follow-up: `SimEventSink.DrainInto(List<SimEvent>)` now drains into reusable
    caller-owned buffers. `BattleRoot.StepEntityWorld` and `tools/PerfSmoke` use
    this path, and `SimReplay` asserts reusable drain semantics. This removes the
    per-drain snapshot array, but the broad item remains open because 400u
    PerfSmoke is still ~188KB/tick; Combat mount list updates and other hot-path
    allocations still dominate.
    Follow-up: `CombatSystem` now updates `WeaponMountRuntimeState` in place via
    `WritableMounts` instead of allocating a fresh `List<WeaponMountRuntimeState>`
    for cooldown/aim updates. `ReviewGate simhot` warns if the per-update mount
    list allocation returns. Current PerfSmoke after this slice: 400u avg 1.161ms,
    p99 1.628ms, alloc/tick ~130KB. Remaining allocation pressure is no longer
    dominated by event drain or combat mount list updates.
    Follow-up: M9 production/pathfinding/projectile allocation paydown now reuses
    `ProductionSystem` producer/spawn-obstacle buffers, `PathfindingSystem`
    shared-corridor buffers, and direct `ProjectileSystem` ordered-entity
    iteration. `ReviewGate simhot` locks these hooks. Current PerfSmoke after this
    slice: 400u avg 11.173ms, p99 11.670ms, alloc/tick 192620 bytes; time is still
    under budget, while remaining allocation work stays open.
[x] Prefer `EntityWorld.OrderedEntities` (no copy) over `StableEntities`/`StableSpecs`
    on hot paths.
    Progress: simulation systems already iterate `world.OrderedEntities`; runtime
    scripts no longer use the allocating `StableEntities`/`StableSpecs` accessors
    outside `EntityWorld` itself. `ReviewGate simhot` now warns if runtime scripts
    reintroduce those accessors.
[x] Metric for dropped-tick backlog events (SimClock catch-up cap).
    Progress: `SimClock` now tracks dropped backlog events/ticks/seconds plus the
    last dropped backlog amount when the catch-up cap truncates a hitch frame.
    `SimMetrics.RecordClockBacklogDrop` accumulates the read-only metric, and
    `BattleRoot.StepEntityWorld` records it after `SimClock.Advance`. `SimReplay`
    asserts the cap and metrics behavior; `ReviewGate simclock` verifies the hooks.
[x] Ability to disable the non-authoritative shadow path to isolate per-system cost.
    Progress: `BattleRoot` now guards the non-authoritative EntityWorld shadow
    configuration/step path behind `RunEntityWorldShadow`. Default behavior still
    runs the shadow sim, while `PROCEDURAL_RTS_DISABLE_ENTITY_SHADOW=1` or
    `PROCEDURAL_RTS_ENTITY_SHADOW=0` disables it for profiling isolation.
    `ReviewGate shadow` verifies the toggle hooks.

## Design Reference - Art & Style (Soft Old City)

Visual target: a repaired old-city tactical paper-map, not a neon grid, not a white
app, not generic sci-fi. Warm off-white/beige board, muted ink lines, low-saturation
faction accents, half-filled procedural silhouettes. All art is procedural vector
(no image sprites), built from reusable `ArtLayer`s bound to `ColorRole`.

[x] Color roles (collapse `ColorUse` -> `ColorRole`): Body (low-fatigue main), Ink
    (outline), Owner (ownership sticker color - the ONLY ownership signal on the
    body), Effect (ability/state), Warning (danger), Shadow (grounding).
[x] Relation colors (player/enemy/ally) live ONLY in overlays: selection ring, health
    bar, minimap pip, target bracket, command line, alerts. Never in the body.
[x] EnvironmentTone (Day/FogMorning/Dusk/Night/Corruption) tones Body/Ink/Shadow/
    Effect/Warning while Owner color keeps min contrast; layout never changes.
    Progress: `EnvironmentToneRoleProfile` now gives Body/Ink/Shadow/Owner/Effect/
    Warning role-specific tone profiles, including Dusk and corruption-driver
    mapping through `EnvironmentTonePalette`. Unit and building views resolve live
    tone through shared palette paths; `CombatBehavior` proves Body/Fog, Ink/Dusk,
    Shadow/Night, and Effect/Corruption color deltas while owner color remains the
    body ownership signal. `ReviewGate environmenttone` locks the contract. Visual
    screenshot tuning across all tones remains under broader Soft Old City QA.
[ ] Faction shape language (identity = shape+glyph, NOT color):
    [ ] Dog: blocky, sturdy, rounded-corner repaired plating; warm cyan-teal +
        repaired-gold accents; loyal/engineering motif; chunky treads/limbs.
    [ ] Cat: sleek, angular, crescent/blade motifs; muted rose/mauve/moonlit violet;
        stealthy/precise; thin fast lines.
    [ ] Third faction: placeholder shape kit + restrained red-purple corruption
        accents (locked, no roster).
[ ] Per-class silhouette rules (must read at minimap zoom in <1s):
    [ ] Light/infantry: small round body, thin step footprints, minimal glyph.
    [ ] Tank/vehicle: wide hull + distinct rotating turret, paired tread strokes,
        heavier outline.
    [ ] Aircraft: above-ground body with a soft drop-shadow blob, no ground tracks,
        jet/contrail tail; visually "floats".
    [ ] Ship (paper-design only this slice): elongated hull, wake ripple, naval-only.
    [ ] Building: repaired-facility silhouette, warm paper fill, ink outline, corner
        banner/door-lintel/roof-stripe as Owner-color layers.
    [ ] Turret: compact platform base + prominent rotating mount; clearly a fixed
        weapon, not a building.
[ ] Owner-color zones are normal `ArtLayer`s (stripes/badges/banners/turret rings/
    wing marks), not a separate decal system.
[x] Death/impact VFX vary by weight class + domain + ammo (flash ring, fragments,
    smoke, EMP dissolve); pooled, capped, fade-oldest under load.
    Progress: death style already varies through `DeathVfxMath.StyleFor`; impact
    flashes now use `ImpactVfxMath.StyleFor(weight, domain, ammo, damage)` and
    `BattleRoot` passes live target weight/domain/ammo into pooled hit flashes.
    `CombatBehavior` asserts heavy rocket/cannon and ion/air differences, while
    `ReviewGate vfx` verifies the math hooks and pooled presentation boundary.
[x] Footprints/trails as readability cues: light = thin fast steps, tank = tread
    plates, aircraft = contrail, ship = wake; low-contrast, suppressed under UI/fog.
    Progress: `FootprintVisualMath` maps light units to step marks, medium vehicles
    to paired treads, heavy vehicles to track plates, aircraft to contrails, and
    paper-design naval units to wake ripples. `FootprintLayer` keeps them
    low-contrast, visibility/fog filtered, capped, and faded under load.
    `CombatBehavior` and `ReviewGate vfx` verify class/domain readability hooks.

## Design Reference - Unit Classes & Operation Logic

Each movement domain has a DISTINCT control feel - that difference is the gameplay,
not a stat tweak. All classes share the entity/component/system model; the feel comes
from component values + which systems apply, never bespoke runtime classes.

[ ] Light (infantry-style, `MovementDomain.Land`): cheap, fast, tight turn radius,
    pack densely (small collision), weak individually. Squad feel: move compact,
    react quickly, die fast. Counter role: numbers, scouting, anti-light.
[ ] Tank (vehicle, `Land`): hull + INDEPENDENT turret mount - body turns with
    momentum (lower turn rate, accel/decel), turret tracks target separately. Tread
    footprints, heavier mass (pushes light units, anchors well). Counter role: core
    line, anti-vehicle/anti-structure; vulnerable without anti-air.
[ ] Aircraft (`Air`): ignores ground obstacles and terrain passability, flies straight
    over, banking turns, cannot be blocked; either strafing (fast pass, fixed-forward)
    or hover (omni mount). Must return-to-base/rearm or has limited loiter. Counter
    role: hits ground hard, dies to anti-air turrets and AA units; cannot be hit by
    ground-only weapons (`WeaponTargetProfile` domain gate).
    Progress: `CatScoutAircraft` now exists as a data-driven `UnitDesign` with
    `MovementDomain.Air`, `ArmorTag.Aircraft`, non-blocking collision, and procedural
    aircraft art. It is covered by BalanceReport Tank-vs-Air and Air-vs-AA scenarios.
    Dog air closure slice added `DogSkyPatrolAircraft` as a data-driven
    `UnitDesign` with `MovementDomain.Air`, `ArmorTag.Aircraft`, non-blocking
    collision, Airfield production, Dog-specific procedural aircraft art, and an
    air-only anti-air weapon. `CombatBehavior` proves it appears in Dog playable
    production options and completes into a runtime `UnitInstance`.
[ ] Harvester (economy, `Land`): unarmed (or token), gather-return-unload loop, dock
    reservation, flees/retreats under fire (passive stance), high value target.
[x] Ship/Naval (PAPER DESIGN ONLY this slice): `Naval`/`Amphibious` domain, stays in
    navigable water, wake trail; documented in `docs/unit-data` but not built.
    Progress: `docs/unit-data/naval-paper-design.md` defines the paper-only naval
    direction in Chinese, including `MovementDomain.Naval`, `MovementDomain.Amphibious`,
    wake/readability rules, future roles, and explicit current-slice bans. `ReviewGate
    unitclasses` verifies the document exists while blocking playable ship/naval
    `UnitKind` or `UnitDesign` content.
[ ] Operation logic differences are data, surfaced through systems:
    [ ] Movement: turn rate, accel, fire-while-moving, domain pathing - all spec data.
    [ ] Targeting: `WeaponTargetProfile` allowed domains/armor + priority - e.g. AA
        can hit Air, anti-tank cannot hit Air, splash favors clustered Light.
    [ ] Mount facing: BodyFixed (light strafing), Independent (tank/turret tracking),
        Omni (hover/point-defense).
[ ] T1/T2/T3 roster per faction (Dog & Cat), reusing existing `UnitDesign` files where
    present, all flowing through `EntitySpec` + generic systems:
    [ ] T1: scout/basic light, basic harvester, basic tank or AT light, MCV/deploy or
        build core per faction method.
    [ ] T2: main battle tank, rocket/AA light, support (engineer/repair), basic
        aircraft (scout/fighter).
    [ ] T3: heavy/siege tank, advanced aircraft (bomber/gunship), specialist
        (sniper/shield/artillery) - keep to a few strong, legible options.
    Progress: `tools/RosterAuthoringQa` now checks playable Dog/Cat roster
    authoring across tiers, domains, production categories, starting units,
    counter hooks, locked third-faction placeholder, no playable naval, and i18n
    keys. It is included in `tools/VerifyAll`. Dog air closure slice upgraded the
    QA from a Dog-air warning to a hard requirement that every playable faction
    includes playable air; this parent stays open for final roster completeness
    and balance acceptance.
[ ] Counter triangle must be legible: Light <-> Tank <-> Aircraft <-> Anti-Air, with
    structures/turrets as the static anchor. Tunable via damage/armor profiles only.
[ ] Faction asymmetry is flavor + small numeric/role differences, NOT separate code:
    Dog leans durable/defensive/repair; Cat leans fast/precise/stealthy.

## Design Reference - Resource, Mining & Environment Regeneration

Economy is tunable from day one and runs entirely in `ResourceSystem` (pure sim).

[x] Mining loop: harvester picks nearest available `ResourceNode` -> travels ->
    gathers to `ResourceCargo` capacity -> reserves a refinery `Dock` (one harvester
    per dock slot, queue if busy - avoids congestion) -> unloads -> credits bank ->
    returns. All deterministic, dock reservation prevents pile-ups.
    Progress: `AutoHarvestEntityCommand` lets harvesters choose the nearest
    non-depleted `ResourceNode` through shared `ResourceMiningMath` stable-order
    queries. `ResourceSystem` now falls back to the next nearest available node when
    the current field is depleted or missing, while preserving dock reservation,
    unload, credit banking, and deterministic replay. `tools/SimReplay` proves
    `auto-harvest`; `ReviewGate autoharvest` locks the contract.
[x] `ResourceNode` data: amount, maxAmount, gatherRateModifier, depletionBehavior
    (deplete-to-zero vs deplete-then-regrow), visibilityRule, corruptionState.
    Progress: `ResourceNodeComponentState` carries amount/maxAmount,
    gatherRateModifier, depletionBehavior, visibilityRule, and corruptionState;
    `EntityStateHash` and `SimInvariants` include resource-node state.
[x] Environment resource regeneration (the "alive map" hook): nodes can slowly regrow
    up to a cap when conditions allow - a deterministic regen tick in `ResourceSystem`.
    Progress: `ResourceSystem` now advances deterministic resource regeneration for
    `DepleteThenRegrow` nodes, using `EconomyTuningConfig` rate/cap knobs,
    `ResourceAtmosphere`, resource corruption state, and optional
    `ResourceRegenerationAuraComponentState` sources. `tools/SimReplay` proves
    deterministic `resource-regen` behavior, and `ReviewGate resourceregen` locks it.
    [x] Regen rate is environment-modulated: lit/repaired/safe zones (signal towers,
        road lights, dog defense net) BOOST regrowth; corruption/contested zones
        SLOW or POISON it (reduced yield, or flips node to hostile-tainted).
        Progress: Generic powered regeneration auras boost nearby nodes, tainted
        nodes regrow slower, hostile nodes are suppressed, and day/night/corruption
        atmosphere multipliers are pure EntityWorld data hooks.
    [x] Regen respects a per-node cap and a global pacing so the economy can't runaway;
        exposed as tunables (rate, cap, boost/penalty multipliers).
        Progress: `RegenerationCapRatio`, `RegenerationRate`, corruption multipliers,
        aura multipliers, and atmosphere multipliers are all tunable in
        `EconomyTuningConfig`; SimReplay asserts a 75% cap.
    [x] Day/night/atmosphere ties in via `EnvironmentTone`/signal state: e.g. night or
        corruption suppresses regen, repaired daytime zones accelerate it.
        Progress: `ResourceAtmosphere` is hashed EntityWorld state; SimReplay proves
        day regeneration outpaces night regeneration. Signal/light gameplay can feed
        this through powered `ResourceRegenerationAuraComponentState` sources.
[x] Multiple resource types optional later; this slice ships ONE credit resource to
    keep the loop tight. Design leaves room for a second (rare) resource.
    Progress: `ResourceInventory` banks only `Credits`; production/build definitions
    expose one `Cost`; harvester unload paths add cargo into Credits; resource fields
    and cargo are single-channel amount/cargo-capacity models. `ReviewGate
    resourcescope` rejects ResourceKind/ResourceType/secondary-resource source hooks.
[x] Economy metrics in `SimMetrics`: credits-per-minute, harvester idle time, dock wait
    time, resource trip time, refinery congestion; assert healthy bands in SimReplay.
    Progress: `SimMetrics` now records CreditsBanked/CreditsPerMinute,
    HarvesterIdleSeconds, HarvesterActiveTripSeconds/AverageResourceTripSeconds,
    DockWaitSeconds, RefineryCongestionEvents, and ResourceTripCompletions.
    `ResourceSystem` feeds those read-only metrics, and `tools/SimReplay` asserts
    resource-loop throughput plus dock-congestion behavior.
[x] Deterministic economy tests: gather/dock/unload/deplete, regen up-to-cap with
    environment modifiers, congestion fairness, in SimReplay.
    Progress: `resource-loop` asserts gather/dock/unload/deplete and credit banking;
    `AssertDockCongestionMetrics` proves two waiting harvesters eventually unload
    under dock congestion; `resource-regen` asserts cap, day/night modifier,
    tainted/hostile corruption modifiers, aura boost, and non-regrowing depletion
    behavior. `ReviewGate economyproductiontests` now requires the regen scenario.

## Design Reference - Match Lifecycle & Map Generation

A skirmish must set up, run, and tear down deterministically from one config object.

[x] `MatchConfig` (immutable): player faction, AI faction, AI difficulty, map seed,
    starting credits, world size. Feeds world seeding; same config => same start.
    Progress: `MatchConfig` is now an immutable record with starting credits, map
    seed, AI difficulty, world size, player faction, AI faction, and launch mode.
    `SkirmishOptions` bridges to it for the current menu, while `GameState` stores
    and seeds from `MatchConfig` directly. `CombatBehavior` proves same config gives
    stable resource and starting-building setup, and `ReviewGate matchconfig`
    verifies the source hooks.
[x] Deterministic seeded map generation for skirmish: symmetric/fair layout - mirrored
    HQ start positions, balanced resource-node placement and counts, passable terrain
    with some chokes/obstacles, no side advantaged. Pure math from the seed; reuse
    `GridTerrain`/`TerrainFloorMath`.
    Progress: `SkirmishMapGenerator` now produces a pure `SkirmishMapLayout` from
    `MatchConfig`, including mirrored non-default HQ starts, paired equal-value
    resource nodes, and paired choke obstacles. `GameState.Seed()` consumes the
    generated layout for owner loadouts/resources and stores generated map
    obstacles in `MapObstacles`, which feed path obstacles. Default seed preserves
    the existing hand-authored start positions for current UI smoke compatibility.
    `CombatBehavior` proves same-seed stability, different-seed variation,
    mirrored HQs, mirrored/equal resources, mirrored obstacles, and path-obstacle
    integration; `ReviewGate seededmap` locks the contract.
[ ] Match lifecycle: setup -> seed world (bases, harvesters, resources, build radius) ->
    run -> outcome -> clean teardown -> quit-to-menu / rematch with same or new seed.
    No leaked nodes, no carried-over sim state.
[x] Starting loadout per faction (deterministic): HQ + build core, 1-2 harvesters,
    small starting credit float; defined as data, not hard-coded in BattleRoot.
    Progress: `MatchStartLoadouts` now owns faction start building/unit placement
    data and reads `FactionCatalog.StartingBuildings/StartingUnits`; `GameState`
    seeds player/AI starts from `MatchConfig` factions instead of hard-coded unit
    and building calls. Dog/Cat starts include HQ/refinery and one faction harvester,
    and GameState/AI/smoke tests now treat Dog/Cat harvesters as the shared economy
    role. `ReviewGate startloadout`, `CombatBehavior`, and `SimulationSmoke` prove
    the data path.
[x] Pause that truly halts the sim clock (no ticks advance) and resumes cleanly.

## Design Reference - Controls & Command Feel

The hands-on feel layer. All inputs become commands; nothing bypasses the buffer.

[ ] Selection: single-click pick, box-select (combat-priority over harvesters), double-
    click select-same-type on screen, shift add/remove, select-all-army, idle-harvester
    cycle hotkey. (Box/double-click logic exists in legacy `SelectionController` - port
    onto the EntityWorld path, do not duplicate.)
[ ] Control groups: Ctrl+1-9 assign, 1-9 recall, double-tap recall+center. (Exists in
    `ControlGroupController` - route through EntityWorld selection.)
    Progress: `ControlGroupController` now prefers the live `UnitBattlefield`
    selection source for save/recall/HUD snapshots and `BattleRoot` wires the local
    player slot into it. Double-tap recall now focuses the camera on the recalled
    live group. The item stays open until final EntityWorld command-buffer
    selection is implemented.
[ ] Shift-queued orders: shift+command appends waypoints / a chain of orders
    (move->move->attack); a `CommandQueueComponentState` holds the per-entity queue,
    consumed by `CommandSystem`. Visible as a chained command line.
[ ] Command feedback: acknowledgement rings (move/attack/harvest/rally/invalid),
    distinct cursor/preview modes, audio cues - all driven by `SimEvent`s, pooled.
[ ] Move modes (exist): direct / attack-move / ignore-move with hotkeys; attack-move
    uses the M2 autonomy acquire chain.
[ ] Hotkey legend overlay + remappable bindings persisted in settings; full i18n
    (en-US/zh-CN exists) for all new strings.
[x] Camera: WASD/edge-scroll/zoom, minimap click-to-jump and drag, frame-rate-
    independent feel (see Perf plan), optional follow-selection.
    Progress: `CameraController` already supports WASD/edge scroll, mouse wheel
    zoom, damped `FocusOnWorldPoint`, and frame-rate-independent smoothing through
    `CameraInputMath`. `HudLayer.MinimapSurface` now supports both minimap click
    and left-button drag-to-jump, wired through `BattleRoot.OnMinimapJumpRequested`.
    `ReviewGate camera` and `SelectionStress` verify the control hooks and
    smoothing math.

## Design Reference - Balance & Tuning Data (one source of truth)

The counter-triangle and economy must be tunable in ONE place so "legible counters"
is achievable without hunting through code.

[ ] `BalanceConfig` data table: damage profiles (weight/domain/armor multipliers),
    unit costs/build times, weapon ranges/cooldowns, harvest/regen rates, AI difficulty
    knobs. Specs reference balance values; tuning is one edit, not twelve files.
    Progress: CatBasic HP was tuned from 44 to 52 after target re-acquire cooldown
    made light-infantry parity deterministic; `BalanceReport` now passes with
    dog infantry vs cat basic at 17%/83% and all counter checks intact. The full
    `BalanceConfig` table migration remains open.
[ ] Balance is data the sim reads, never scattered magic numbers in systems; systems
    stay generic, values stay external.
[x] A `tools/BalanceReport` (headless): runs canonical duels (Light vs Tank, Tank vs
    Air, Air vs AA, army vs army) N times and reports win rates, so the counter-triangle
    is verified numerically, not by vibes. Fail if a "should win" matchup loses.
    Progress: `tools/BalanceReport` now runs multi-seed EntityWorld canonical duels
    and reports win/draw rates, average ticks, survivors, and remaining HP. It fails
    on unacceptable parity bands or should-win counter checks. The first run exposed
    a 100% dog-tank win rate in vehicle parity; `CatTank` was tuned to restore the
    matchup to a 42%/58% split while preserving rocket-vs-tank and anti-light checks.
    Completed: `CatScoutAircraft` is now a real `UnitDesign` on the EntityWorld path,
    and BalanceReport covers Light-vs-Tank, Tank-vs-Air pressure, Air-vs-AA,
    vehicle parity, light parity, anti-light, and army-vs-army composition checks.

## Design Reference - Abilities, Repair & Support Powers

`AbilityKind` already exists (Harvest, RepairField, ShieldField, Scan, Deploy, Build).
Wire it into an `AbilitySystem` rather than inventing per-unit code - abilities are the
biggest "fun lever" beyond raw combat.

[x] EntityWorld RepairField ability core: engineer/support units repair nearby friendly
    damaged entities through `AbilitySpec` data, `AbilityEntityCommand`, deterministic
    cooldown state, and a pure `AbilitySystem`.
    Progress: `AbilityEntityCommand` now routes active ability intent through the
    command buffer; `AbilityRuntimeComponentState` stores deterministic cooldowns and is
    hashed/validated; `UnitSpecEntityBridge` attaches runtime ability state from authored
    `UnitSpec.Abilities`; `AbilitySystem` executes `RepairField` by healing friendly
    damaged entities inside the ability radius and setting a cooldown after successful
    casts. `tools/SimReplay` proves deterministic `repair-field`: the ally is healed
    twice after cooldown (50 -> 82), a hostile in range stays at 40 HP, and a friendly
    unit outside radius stays at 50 HP. `ReviewGate repairfieldability` locks the
    contract.
[x] EntityWorld ShieldField ability core: support units grant temporary damage
    absorption to nearby friendly entities through `AbilitySpec` data, deterministic
    shield duration/cooldown state, and `CombatSystem` damage absorption.
    Progress: `ShieldComponentState` stores remaining absorb and duration and is
    hashed/validated. `AbilitySystem` applies `ShieldField`, ticks shield durations,
    excludes hostile and out-of-radius targets, and sets cooldown after successful
    casts. `CombatSystem` consumes shield absorb during authoritative damage
    resolution before HP loss. `tools/SimReplay` proves deterministic `shield-field`:
    with the same incoming 4 shots, a shielded ally ends at 97.7 HP while the
    unshielded comparison ends at 79.7 HP; hostiles and far allies receive no shield.
    `ReviewGate shieldfieldability` locks the contract.
[x] EntityWorld Scan ability core: support units create temporary reveal zones through
    `AbilitySpec` data, `AbilityEntityCommand`, deterministic scan marker entities,
    and `VisionSystem` gameplay visibility.
    Progress: `ScanRevealComponentState` stores scan radius and remaining duration and
    is hashed/validated. `AbilitySystem` handles `AbilityKind.Scan` by spawning a
    short-lived gameplay reveal marker entity at the target point and removing it
    on expiry. `VisionSystem` consumes scan reveal components as temporary viewers, so
    fog/AI/minimap can share the same visibility source. `tools/SimReplay` proves
    deterministic `scan`: a hostile inside the scan radius becomes visible, a farther
    hostile remains hidden, the reveal expires and is removed, and visibility drops
    afterward. `ReviewGate scanability` locks the contract.
[x] EntityWorld Deploy ability core: siege/turret-mode units toggle into a stopped
    setup state, gain deterministic weapon range after setup completes, and can
    undeploy responsively without being trapped by cooldown.
    Progress: `DeployComponentState` stores deployed/setup/range-multiplier state
    and is hashed/validated. `AbilitySystem` handles `AbilityKind.Deploy`, ticks
    setup time, clears movement on deploy, and allows toggle-off to bypass cooldown
    for feel. `CombatSystem` blocks firing during setup, applies deploy range only
    after setup, and only writes chase targets to entities with movement profiles.
    `MovementSystem` holds deployed entities still. `tools/SimReplay` proves
    deterministic `deploy`: setup shots stay at 0, deployed range fires at an
    out-of-base-range target, undeploy stops those shots, and movement stays clear.
    `ReviewGate deployability` locks the contract.
[x] Ability cost and target legality core: active abilities can declare a credit
    cost and a target rule (`Self`, `Point`, `FriendlyEntity`,
    `FriendlyPointOrEntity`, etc.) on `AbilitySpec`; `AbilitySystem` validates
    target legality and owner resources before applying effects, then spends only
    after a successful cast.
    Progress: `AbilityTargetRule` and optional `AbilitySpec.Cost` are data, not
    per-unit code. Default rules map Deploy to self, Scan to point, and support
    fields to friendly point/entity targeting. Repair/Shield friendly checks now
    use `OwnerRelationTable` (`Self`/`Allied`) instead of same-owner shortcuts.
    `tools/SimReplay` proves deterministic `ability-legality`: a hostile support
    target is rejected, one legal paid repair spends credits 30 -> 5 and heals
    the ally once, cooldown prevents repeat spending, and insufficient credits do
    not refresh cooldown. `ReviewGate abilitylegality` locks the contract.
[ ] `AbilitySystem` (full active ability framework): drives all ability cooldown/charge,
    targeting modes (self / point / entity / area), costs, and effect application for
    RepairField, ShieldField, Scan, Deploy, Build, and future abilities.
[x] EntityWorld targeted repair-over-time core: a `RepairEntityCommand` lets repair
    support units move into range of a damaged friendly entity, spend credits over
    time, and restore HP through deterministic ECS state.
    Progress: `RepairEntityCommand` is translated by `CommandSystem` into
    `RepairOrderComponentState` only for units with authored `RepairField` data
    and friendly (`Self`/`Allied`) damaged targets. `RepairSystem` moves repairers
    into range, spends owner Credits at 1 credit per HP, repairs in deterministic
    HP chunks, and leaves the order active when the target is still damaged but
    credits are exhausted. Repair order state is hashed/validated. `tools/SimReplay`
    proves deterministic `targeted-repair`: hostile repair targets are rejected,
    an out-of-range repairer moves toward the ally, 8 credits repair 8 HP, and the
    repairer stops in range. `ReviewGate targetedrepair` locks the contract.
[ ] Engineer / repair expansion remaining: capture/restart objective structures
    (shared M3 path), targeted repair UI/smart-right-click wiring, repair feedback,
    and richer per-unit repair economy tuning beyond the core deterministic command.
[ ] Support fields expansion: playable roster/UI wiring for Deploy/ShieldField/
    Scan-capable units, richer support-power presentation, target legality, and
    per-unit authored tuning beyond the core deterministic ability behavior.
[x] Self-repair / regen as derived from upgrades/veterancy (M5), not a special case.
    Progress: `RegenerationComponentState` stores authored self-repair rate and
    fractional progress, `RegenerationSystem` heals only entities with that
    component, and `UpgradeResolver.HealthRegen` composes owner upgrade modifiers
    with per-entity veterancy rank modifiers. `UpgradeIds.FieldRepairs` is data in
    `UpgradeCatalog`, and veterancy ranks now contribute regen multiplier through
    `VeterancyRules`; no unit id or faction id gets special-case self-healing.
    `derived-regeneration` proves deterministic behavior: no component stays at
    50 HP, base regen reaches 62, FieldRepairs reaches 71, FieldRepairs+rank 3
    reaches 76, and capped regen stops at 100. Full `VerifyAll` passed 22/22.
[ ] `SpecialAttackHook` already exists for unique weapons (charge-up, beam, chain,
    area) - route through `WeaponSystem`, keep generic.
[x] Deterministic ability tests in SimReplay: cooldown gating, cost, target legality,
    effect (repair heals, shield absorbs, scan reveals), reproducible by seed.
    Progress: `repair-field`, `shield-field`, `scan`, `deploy`, and
    `ability-legality` replays cover current active ability behavior with stable
    state hashes and focused assertions. Build ability, UI wiring, and future
    abilities remain under the full `AbilitySystem` and support expansion items.

## Design Reference - Power, Signal Network & Base Systems

Power and the signal network (`PowerComponentState`, `SignalNetworkMath`,
`SignalNodeKind` already exist) make base-building a real decision, not just placement.

[x] Power as a constraint with consequences: total provided vs used per owner; when
    under-powered, production slows and defense turrets go offline/low-rate. Drives the
    "build power plants" decision. Rules live in `ProductionSystem`/`CombatSystem`.
    Progress: `PowerSystem` now deterministically totals active `PowerComponentState`
    Provided/Used values per owner, updates consumer Powered state before sim
    consumers run, and ignores destroyed or unfinished power entities. `ProductionSystem`
    already pauses unpowered producers, while `CombatSystem` now prevents unpowered
    weapon users/turrets from acquiring or firing. `tools/SimReplay` proves sufficient
    power vs low-power behavior in `power-consequences` / `power-consequences-low`;
    `ReviewGate powerconsequences` locks the source contract.
[x] EntityWorld signal network capabilities: lit/repaired signal entities extend build
    radius by day/fog, emit night/corruption vision, and provide powered safety resource
    regeneration zones through ordinary ECS components.
    Progress: `SignalNetworkComponentState` now stores node kind plus day-control,
    night-vision, and safety-aura values. `SignalNetworkSystem` is a pure `ISimSystem`
    that requires completed, alive, powered signal nodes, then emits
    `BuildRadiusComponentState`, `VisionComponentState`, and
    `ResourceRegenerationAuraComponentState` according to `ResourceAtmosphere`; inactive
    nodes remove those outputs. `tools/SimReplay` proves deterministic
    `signal-network-day` and `signal-network-night` scenarios: powered day nodes boost
    nearby tainted resource regeneration (50 > 30), and powered night nodes reveal
    hostile targets through `VisibilityIndex`. `ReviewGate signalnetwork` locks the
    contract.
[ ] Signal network live integration: campaign/mission repair or restart commands feed
    signal entities; live `EnvironmentTone`/day-night drivers set `ResourceAtmosphere`;
    presentation shows readable repaired/offline/safe-zone state without becoming
    authority.
[ ] Base teardown/sell for partial refund; rally points per producer; repair at HQ/depot.
[x] Low-power / offline / damaged building states are readable (art + alert), not silent.
    Progress: `BuildingPresentationProjection` exposes powered/offline,
    construction paused, pause reason, HP ratio, missing HP ratio, and damage
    readability level from EntityWorld component state. `BuildingView` renders
    compact low-power/offline badges, paused-construction progress, edge cracks,
    local gaps, and heavy-damage sparks without using owner color as a warning
    color. `CombatBehavior`, `ReviewGate buildingofflinereadability`, and
    `ReviewGate buildingdamagedreadability` lock the projection and visual hooks.

## Design Reference - Alerts, Notifications & Audio

`AlertKind` and `TacticalAudioCue` (Selection/Move/Attack/Alert/Production) exist - make
them `SimEvent`-driven so the sim stays the source and presentation only reacts.

[ ] Alerts driven by `SimEvent`s: under-attack (with minimap ping + space/hotkey jump to
    location), production complete, insufficient credits, idle harvester, low power,
    building lost, unit lost. Throttled (cooldown per kind) so they never spam.
[ ] Minimap is a first-class readout: owner pips from `EntityProjection`, fog from the
    cached mask, camera rect, attack pings, click/drag to jump. Consumes projection +
    `VisibilityIndex`, never raw entity positions.
[ ] Procedural audio cues per event (selection/move/attack ack, alert, production, build
    complete, death, low-power), spatial-ish, de-duplicated under load; volume in settings.
[ ] First-run readability: concise tooltips + what-beats-what affordances on hover; no
    persistent instructional text in the HUD (Soft Old City rule).

## Design Reference - Combat Juice & Feedback ("juiciest battle")

The explicit goal is satisfying combat. Juice is PRESENTATION ONLY (driven by SimEvents,
pooled, capped) - it never touches authority or determinism.

[ ] Hit feedback: target flash + small recoil/knockback-look (visual only), muzzle flash
    at the mount, projectile tracer/beam, impact spark/scorch. All from WeaponFired/
    EntityDamaged events.
[ ] Death satisfaction: weight/domain/ammo-varied death burst (flash ring, fragments,
    smoke, EMP dissolve), short-lived wreck/scorch decal that fades; overkill = bigger.
[ ] Weight & impact: heavy hits feel heavy - bigger flash/shake for siege/explosive,
    crisp/light for needle/MG; optional subtle screen-shake on nearby big impacts (toggle).
[ ] Combat readability under juice: effects stay low-contrast vs selection/command
    markers, suppressed under heavy fog/UI; never obscure who is winning.
[x] Selection/command snappiness: instant ring, dashed command line to intent point,
    crisp ack rings, responsive turret tracking - the feel of precise control.
    Progress: `CommandAcknowledgementLayer.Add` inserts and redraws command rings
    immediately; `SelectionController` draws dashed command lines and pulsing intent
    markers from `CommandVisualTarget`; `UnitBattlefield` stores visual targets and
    pulses on move/attack/harvest/rally commands; runtime weapon mounts aim through
    `AimWeaponMounts` and are rendered by `UnitInstanceView`. `ReviewGate
    commandsnappiness` and `CombatBehavior` verify the current feedback path.
[x] All current juice is pooled + capped + fade-oldest under load (Perf plan); zero
    effect can stall the sim or desync a replay.
    Progress: `CombatEffectsLayer` pools and budgets death effects plus impact
    flashes, `CommandAcknowledgementLayer` pools and budgets command rings, and
    `FootprintLayer` keeps decorative marks in a bounded fading list. `ReviewGate
    vfx` locks the current pure-presentation effect families to pooled/budgeted
    paths; future new effect families must extend that gate before being accepted.

## Design Reference - Command Vocabulary Completeness

Beyond Move/AttackMove/Attack/Stop/Hold/SetStance/Group already implemented, the slice
needs these classic RTS orders (all through the command buffer, all deterministic):

[x] Patrol: loop between two+ points, engaging hostiles encountered, returning to route.
    Progress: `PatrolEntityCommand` and `PatrolOrderComponentState` store a
    two-endpoint deterministic patrol route through the command buffer; state is
    hashed/validated. `CommandSystem` applies patrol as attack-move route intent and
    explicit Move/Attack/Stop/etc. clear patrol. `MovementSystem` flips endpoints on
    arrival and resumes the route after active combat clears. `SimReplay patrol`
    proves A->B->A looping, route-threat engagement, resume, and explicit override;
    `ReviewGate patrol` locks the contract. UI hotkeys and smart-right-click wiring
    remain open.
[x] Guard: follow/protect a friendly unit or hold an area, engaging threats to it.
    Progress: `GuardEntityCommand` and `GuardOrderComponentState` support
    protecting a friendly entity or fixed point/radius. Guard units return to
    guard range, follow moving protected entities, engage bounded guard threats,
    and clear Guard on explicit Move/Attack/Stop/Hold/Harvest/Repair/Patrol.
    Guard state is hashed, invariant-checked, deterministic in `SimReplay guard`,
    and locked by `ReviewGate guard`. UI hotkeys remain future polish.
[x] Smart rally / smart right-click: right-click a resource = harvest, an enemy = attack,
    a damaged ally (engineer) = repair, a transport = load, ground = move - context from
    target kind, one button.
    Progress: right-click hostile units/buildings routes attack, resources route
    selected harvesters to harvest, damaged self/allied units/buildings route
    repair-capable units through `RepairEntityCommand`, selected production
    buildings can resource-rally with a retained `ResourceNode` target entity, and
    ground falls back to move. Repair preview/ack feedback is distinct. Transport
    load remains future work because transports are not in this slice.
    `CombatBehavior` and `ReviewGate smartclick` lock the current branches.
[x] EntityWorld resource-rally auto-harvest core: producers can store a rally point
    plus target entity id, and harvesters produced from a resource rally immediately
    enter the deterministic harvest loop.
    Progress: `SetRallyPointEntityCommand` routes rally intent through the command
    buffer; `RallyPointComponentState` stores optional `TargetEntityId` and is
    hashed/validated. `ProductionSystem` applies rally commands, preserves normal
    point rally for all units, and maps produced harvesters onto
    `HarvesterMode.MovingToField` when the rally target is a live `ResourceNode`.
    `tools/SimReplay` proves deterministic `resource-rally-production`: a factory
    rallies to a resource node, produces a harvester, the harvester targets that
    field, gathers from it, and retains command visuals at the resource point.
    `ReviewGate resourcerally` locks the contract.
[x] EntityWorld repeat production core: producers can store a repeat output spec
    and automatically refill their own queue when idle and affordable.
    Progress: `SetRepeatProductionEntityCommand` toggles repeat through the
    command buffer; `ProductionQueueComponentState.RepeatOutputSpecId` is
    hashed/validated. `ProductionSystem` validates the repeated `UnitSpec`, waits
    for sufficient Credits, spends only when enqueueing the next loop item, and
    preserves producer-owned queues/rally behavior. `tools/SimReplay` proves
    deterministic `repeat-production`: 240 credits loop exactly two dog infantry,
    repeat remains armed, and the producer waits empty when credits are exhausted.
    `ReviewGate repeatproduction` locks the contract.
[ ] Queued command modifiers remain open: shift-queue production commands, UI
    repeat/loop controls, AI planner use of repeat, rally onto moving units, and
    UI/smart-right-click rally wiring.
[ ] Force-fire / attack-ground for splash weapons; force-move (ignore targets).

## Design Reference - AI Difficulty Design

Difficulty is planner parameters, never cheating (AI only sees its `VisibilityIndex`).

[ ] Easy: slower production pace, smaller/later waves, passive defense, limited scouting,
    no micro; forgiving for new players.
[ ] Normal: steady economy, mixed armies, reactive defense, periodic waves, basic micro
    (focus fire, retreat low-health).
[ ] Hard: efficient economy, tech progression, combined-arms pushes, active scouting,
    counters the player's composition, good micro (kiting, target priority).
[ ] Difficulty changes only `BalanceConfig`/planner knobs (pace, wave size, aggression,
    scout frequency, micro level) - the same planners, tuned; no resource/vision cheats.
[x] `tools/BalanceReport` (or an AI smoke) runs AI-vs-AI per difficulty to sanity-check
    pacing and that Hard reliably beats Easy.
    Progress: `tools/AiDifficultySmoke` now probes Easy/Normal/Hard production and
    attack-wave pacing through the real UnitBattlefield production/wave AIs, then
    runs an Easy-vs-Hard wave-size duel. Hard now waits for a meaningful wave before
    attacking and the smoke is part of `tools/VerifyAll`.

## Engineering Conventions (keep it elegant)

These are the rules that keep the framework clean as it grows. They are testable.

[x] Simulation purity: no Godot types in authoritative logic except value math
    (Vector2/Mathf). No Node/SceneTree/_Process/real-time/RNG-from-Godot in `Core/sim`.
[x] Every system is a pure `ISimSystem` over components, iterates stable EntityId order,
    and has at least one deterministic SimReplay assertion.
    Progress: `ReviewGate simconventions` now scans `scripts/core/entities` and
    `scripts/core/sim` for forbidden authoritative runtime hooks (Node/SceneTree/
    `_Process`, real-time clocks, Godot RNG, `new Random`) while allowing value math.
    It also verifies every `*System` implements `ISimSystem`, uses `Step(SimContext)`,
    iterates `EntityWorld.OrderedEntities` or ordered command-buffer output, and that
    SimReplay covers movement, combat, group move, group attack, vision/outcome.
[ ] New content = new data (UnitDesign/BuildSpec/EntitySpec), not new runtime classes or
    edits across many systems. If adding content needs a system change, the model is
    wrong - fix the model.
[ ] Components hold state only; behavior lives in systems; views hold no authority.
[x] One verify gate: `tools/VerifyAll` (or a script) builds + runs SimReplay + PerfSmoke
    + CombatBehavior + SimulationSmoke + BalanceReport and returns a single pass/fail.
    This is the AI-friendly "is it still correct?" button.
    Progress: `tools/VerifyAll` now runs build, SimReplay, CombatBehavior,
    SimulationSmoke, FogOfWarQa, SelectionStress, ReviewGate, PerfSmoke, and Godot
    headless Battle/display QA sequentially to avoid dotnet DLL locks. It now also
    runs `tools/BalanceReport` by default, so the full current verification suite
    returns a single pass/fail result. `VerifyAll` now resolves Godot through
    `GODOT_BIN`/`GODOT4_BIN` or common Windows/Linux PATH names, and
    `tools/verify-all.sh` gives headless Linux workers the same one-button gate.
[x] Godot headless managed-resource cleanup: C#-created Godot `Resource` wrappers are
    detached and disposed on scene exit so headless QA does not fail at Mono shutdown.
    Progress: `ManagedGodotResourceCleanup` releases label settings, theme styleboxes,
    canvas materials, texture references, fog mask images/textures, SVG temp images,
    generated audio streams, and settings config files. `tools/VerifyAll` proves the
    Godot Battle, display settings, skirmish flow, and pause QA steps exit cleanly.
[x] `SimInvariants` debug pass (toggle): asserts no NaN/inf transforms, hp within
    [0,max], no targets pointing at dead/removed entities, no orphaned dock reservations,
    command queue bounded. Runs in tests and optionally in-engine debug builds.
    Progress: `SimInvariants` now validates finite transforms/movement/pulses,
    health bounds, target liveness/existence, dock references plus duplicate
    reservations, production queue length, and the new `CommandQueueComponentState`
    bound. `EntityWorld.SimInvariantsEnabled` can be toggled directly or with
    `PROCEDURAL_RTS_SIM_INVARIANTS=1`, and `tools/SimReplay` proves valid worlds
    pass while malformed transforms, HP, targets, docks, and command queues fail.
[x] Naming/structure: systems `*System`, component states `*ComponentState`, commands
    `*EntityCommand`, math helpers `*Math` (pure static). Keep files single-purpose.
    Progress: `ReviewGate naming` now verifies sim system files are named and defined
    as `*System : ISimSystem`, `*Math.cs` files expose matching static helper classes,
    component-state records inherit `EntityComponentState`, actual command records
    inherit `EntityCommand`, and the command-buffer sequencing wrapper is named
    `SequencedCommandEnvelope` so it does not masquerade as a gameplay command.
[x] Accessibility: owner colors must be colorblind-distinguishable (owner color is the
    ONLY ownership signal); provide a colorblind-safe palette option.
    Progress: `OwnerColorPaletteMode` adds Standard and ColorblindSafe modes,
    `DisplayAudioSettings` persists `ui.owner_colors`, `SettingsOverlayLayer`
    exposes the option, and `SoftOldCityPalette.PlayerColor` resolves owner colors
    through the selected mode. `CombatBehavior`, `DisplaySettingsQa`, and
    `ReviewGate accessibility` verify the mode exists, the safe colors differ from
    the standard palette, and safe owner colors stay separated.

## Explicit Non-Goals (this slice) - keep scope honest

[x] No multiplayer networking (core stays deterministic so it is possible later).
    Progress: `ReviewGate scopeguards` and `ReviewGate mode1v1` reject multiplayer
    launch modes, player-count/network config fields, multiplayer UI entry points,
    and Godot networking APIs while preserving the deterministic command core.
[x] No campaign / missions / scripted triggers yet.
    Progress: `ReviewGate scopeguards` and `ReviewGate skirmishonly` reject
    campaign/mission/chapter launch modes, scenes, and runtime script surfaces; the
    playable surface remains skirmish plus developer sandbox.
[x] No naval units built (paper design only).
    Progress: `ReviewGate unitclasses` and `CombatBehavior` keep ship/naval units out
    of current playable rosters while allowing future enum hooks and the paper design.
[x] No third faction content (placeholder only).
    Progress: `ReviewGate scopeguards` and `ReviewGate thirdplaceholder` allow the
    reserved `Corruption` enum/locked menu placeholder but reject registered
    third-faction catalog content or third-faction `UnitDesign` implementation.
[x] No super-units / tier 4+ / hero units.
    Progress: `CombatBehavior` now scans legacy `UnitKind` plus discovered
    `UnitDesign` type names for hero/super/experimental/commander/ultimate/T4/T5
    terms, while the existing tier checks keep all definitions in T1-T3. `ReviewGate
    tiers` mirrors the source scan so future content cannot silently add hero,
    super-unit, experimental, T4, or T5 unit content to this slice.
[x] No save/load of in-progress matches (command-log replay is a cheap future add).
    Progress: `ReviewGate scopeguards` rejects SaveGame/LoadGame/SavedMatch/
    MatchSave runtime or scene surfaces while leaving ordinary settings persistence
    intact.
[ ] Map editor: hand-designed maps via the Godot editor ARE now in scope for campaign/
    story maps - see M11 (two-author pipeline: AI/seeded + Godot-editor, one pure-data
    `MapSpec`). Skirmish still defaults to seed-driven generation; the editor path is
    for specific story maps. (Reverses the earlier "no map editor" scope lock per the
    campaign requirement.)
