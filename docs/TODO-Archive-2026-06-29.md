# Procedural RTS TODO

Status: Historical archive from 2026-06-29. Do not use this file as the current
TODO source; use root `TODO.md` for active work.

TODO editing rule: keep this file UTF-8 and ASCII-only where possible. Use `[x]` for done and `[ ]` for open items; do not use Unicode checkbox/checkmark symbols.

## Dev Sandbox Plan

[x] Main menu dev entry: add a Sandbox button/entry for current development-stage unit, building, combat, movement, UI, and visual-theme testing.
[x] Sandbox mode: add time-of-day and atmosphere controls so developers can manually test daytime, dusk, night, signal restoration, and corruption-driven visual transitions.

## Chosen Visual Direction - Soft Old City

[x] Main AI UI direction lock: use Soft Old City as the project visual target; stop exploring Porcelain Table, Archive Map, Repair Blueprint, Garden Circuit, and Signal Glass as primary UI directions unless explicitly requested.
[x] Soft Old City core rule: battlefield and HUD should read as a soft repaired old-city tactical board, not a dark neon grid, not a pure white app UI, and not a generic sci-fi dashboard.
[x] Old City Day default state: warm beige/off-white tactical board, muted ink grid, low-saturation dog/cat accents, half-filled procedural unit silhouettes, and low-fatigue edge HUD.
[x] Fog Morning exploration state: gray-beige fog overlay, reduced distant contrast, subtle cat hidden-route marks, softer command lines, and cautious/unknown-area atmosphere.
[x] Dusk Defense crisis state: dark low-glow board, warm repaired light routes, stronger orange command feedback, red-purple AI corruption rings, and defensive pressure without changing HUD layout.
[x] UI implementation target: refactor HUD panels, minimap, production/detail drawers, command buttons, selection panels, alerts, and tooltips to follow Soft Old City palettes and contrast tiers.
[x] UI implementation target: keep the playfield center clear; persistent UI should stay edge-docked, compact, low-opacity, and readable, with no large opaque lower-middle panels.
[x] UI implementation target: use thin ink-like borders, soft translucent fills, restrained shadows, stable panel dimensions, icon-first controls, and hover tooltips instead of visible instructional text.
[x] UI implementation target: selected units, command lines, target markers, minimap threats, disabled buttons, build previews, and alerts may use stronger contrast than passive panels.
[x] Battlefield surface target: do not present the world as a visible square grid; keep any construction/pathing grid internal and render the battlefield as old-city survey marks, soft command zones, routes, and terrain traces.
[ ] CommandPlate visual target: remove grid/tile-based CommandPlate implementation and replace it with a fog-of-war-like continuous rounded field/mask so command zones feel soft, organic, and smoothly blended into the battlefield.
[x] Unit visual target: world units should use Soft Old City half-filled procedural silhouettes, ink-like outlines, muted faction accents, and restrained status rings instead of black-core neon styling.
[x] Unit/player color architecture: add explicit player-owned color zones to unit presentation descriptors so multiplayer player-slot colors render as controlled stickers/stripes/badges instead of recoloring the whole unit body.
[x] Building visual target: migrate buildings away from black-core neon panels toward Soft Old City repaired-facility silhouettes, warm paper fills, ink outlines, and restrained faction/role accents.
[x] Building/player color architecture: add building color zones such as corner banners, door lintel marks, roof stripes, and rally plaques so player ownership remains readable in mirror matches and multiplayer without overriding faction identity.
[x] Multiplayer color rule: keep unit faction metadata for body shape, faction glyphs, and low-saturation identity accents; use `PlayerSlotId` for player-owned color zones; use battle relation color only for selection, health, minimap, alerts, target brackets, and combat warnings.
[ ] Environment tone rule: day/fog/dusk/night/corruption should tone body fill, ink, shadow, glow, and effects through a shared `EnvironmentTone` profile while preserving `OwnerColor` readability as the single ownership signal.
[x] UI implementation target: separate faction identity from ownership - dog/cat/AI shape language and glyphs identify faction, while player/enemy/ally colors drive selection, minimap, health, and command feedback.
[ ] UI implementation reference: compare against `scenes/StyleCandidateDeck.tscn`, `scenes/OverallStyleShowcase.tscn`, and `artifacts/style-candidate-deck-godot.png`; Soft Old City is the chosen family.
[x] UI QA target: capture Soft Old City HUD at 1280x720, 1600x900, 1920x1080, and high-DPI scaling; verify text fit, edge readability, selection clarity, minimap pips, and command visibility in Old City Day, Fog Morning, and Dusk Defense.

## Soft Old City Color Scheme

[ ] Color scheme: define one canonical palette source for battlefield, HUD, units, command feedback, minimap, alerts, fog, and day-night transition states.
[x] Color scheme daytime base: use warm paper/off-white and pale old-city stone colors such as `#eee5d3`, `#d8ccba`, `#b7ad9c`, and muted ink `#4f5961`; avoid pure white and saturated neon.
[x] Color scheme night base: use low-glow blue-black and desaturated slate such as `#121925`, `#1b2633`, `#2c3b45`, with warm repaired-light accents; keep selection and commands readable without returning to neon grid style.
[x] Color scheme dog faction: use loyal cyan-teal and repaired-light accents, e.g. `#64c7c7`, `#8fd8ca`, `#d7b66a`, with shape/glyph identity separate from player/enemy ownership colors.
[x] Color scheme cat faction: use muted rose, mauve, and soft moonlit violet, e.g. `#c98293`, `#b894c7`, `#e2c2c8`, with stealth/readability prioritized over high saturation.
[x] Color scheme AI/corruption faction: use restrained red-purple ink/corruption accents, e.g. `#9d4259`, `#6f3b72`, `#c15b6c`, reserved for threat, infection, and hostile system rewrites.
[x] Color scheme ownership overlay: player, enemy, ally, neutral colors must be a separate overlay layer for selection rings, minimap pips, health borders, and command lines; never encode team only through faction color.
[ ] Color scheme contrast tiers: passive terrain and panels stay low contrast, selectable units and command paths are medium contrast, active target markers, damage, warnings, and minimap threats get the strongest contrast.

## Parallel AI Work Split - A / B

### AI A Goal - Soft Old City UI And Visual Theme Implementation

Owner: UI / visual theme / HUD readability.

[x] A1: Create a shared Soft Old City theme helper/palette for HUD panels, borders, text tiers, command accents, danger accents, disabled states, minimap pips, selection rings, target markers, and build previews.
[x] A2: Migrate the current Battle HUD away from dark neon styling into Old City Day: warm beige/off-white translucent panels, muted ink borders, low-fatigue text, and stronger contrast only for actionable feedback.
[x] A3: Apply Old City Day to right command sidebar, minimap, production drawer, unit detail drawer, command ribbon, alert strip, resource/status strip, pause/outcome overlays where relevant.
[x] A4: Add palette/state hooks for Fog Morning and Dusk Defense without changing HUD layout; missions/sandbox should be able to switch theme state later.
[x] A5: Keep UI low-obstruction: no persistent opaque panel covering center or lower-middle battlefield; right rail, bottom ribbon, top status, and alerts stay compact and edge-docked.
[x] A6: Verify readability of selected units, command lines, attack lines, rally markers, build previews, disabled buttons, warnings, enemy alerts, and minimap threats after palette migration.
[x] A7: Capture UI QA screenshots at 1280x720, 1600x900, and 1920x1080 for Old City Day; if hooks are ready, also capture Fog Morning and Dusk Defense comparison shots.

Completion criteria for AI A:
[x] Game boots into battle with Old City Day HUD as the default visual direction.
[x] Existing HUD interactions still work: selection details, production, minimap, command buttons, alerts, settings/pause/outcome flows.
[x] No obvious text clipping, overlapping UI, unreadable command states, or center-field obstruction at target desktop resolutions.
[x] Provide screenshot artifacts and a short note listing changed UI/theme files.

Suggested files for AI A:
`scripts/ui/*`, `scripts/core/WorldTheme*`, `scripts/world/GridLayer.cs`, `scripts/world/*Layer.cs`, `scripts/MainMenuRoot.cs`, `scripts/BattleRoot.cs`, `scenes/Battle.tscn`, visual QA tools/artifacts.

### AI B Goal - Faction Units, Presentation, And Build/Production Foundation

Owner: gameplay data / faction identity / procedural units / construction foundation.

[x] B1: Finish dog/cat faction identity separated from ownership: `FactionId` and faction palette/glyph/availability should not replace `Owner` alliance logic.
[x] B2: Implement dog T1-T3 unit roster presentation descriptors: infantry/repair dog, assault tank, harvester, shield tank, siege artillery, support/scout aircraft as needed for current campaign and sandbox tests.
[x] B3: Implement cat T1-T3 unit roster presentation descriptors: basic cat, rocket cat, engineer cat, fast tank, harvester, sniper cat, repair vehicle, shield vehicle, special cat, bombard tank, scout/fighter aircraft as needed.
[x] B4: Route unit silhouettes, faction glyphs, role glyphs, production icons, minimap pips, selection/detail icons, and ownership overlays through shared descriptors so AI A can skin UI consistently.
[x] B5: Add sandbox spawning/testing support for dog and cat units, including faction-vs-faction cases such as dog-vs-dog and cat-vs-cat to validate ownership color separation.
[x] B6: Start construction/production foundation: build catalog entries, producer categories, per-building production lanes, queue/progress/cancel/rally data hooks, and disabled/prerequisite states.
[x] B7: Add deterministic tests or smoke tools for faction relation logic, presentation descriptor completeness, production availability, and ownership/faction color rules.

Completion criteria for AI B:
[x] Sandbox can spawn or produce representative dog and cat units with distinct faction silhouettes and reusable presentation descriptors.
[x] Ownership/team color is visibly separate from faction shape/color language in world, minimap, and selected-unit presentation.
[x] Production/build data is structured enough for AI A's right-side production UI to display categories, costs, queues, disabled states, and producer focus.
[x] Provide a short note listing new unit/faction/build definitions and any remaining missing roster items. See `docs/AI_B_COMPLETION_NOTE.md`.

Suggested files for AI B:
`scripts/core/Faction*`, `scripts/core/Unit*`, `scripts/core/Production*`, `scripts/core/Building*`, `scripts/core/Owner.cs`, `scripts/core/FactionRelations.cs`, `scripts/controllers/ProductionController.cs`, `scripts/controllers/BuildPlacementController.cs`, sandbox/skirmish setup, simulation smoke tools.

### Coordination Rules

[ ] AI A should not redesign unit rosters or faction gameplay; use descriptors provided by AI B and add temporary fallbacks only when required for UI testing.
[ ] AI B should not restyle the HUD globally; expose clean presentation/theme data and keep world/unit visuals compatible with Soft Old City.
[ ] Both A and B should keep `Owner`/team logic separate from `FactionId`/identity logic.
[ ] Both A and B should update this TODO section with completed items and add screenshot/test artifacts when finishing a pass.

## Light Low-Contrast UI Theme Plan

[x] Visual theme prerequisite: implement real dog and cat faction units before finalizing the soft tactical map/UI theme, so readability, silhouettes, team colors, and UI icons are tested against actual gameplay entities.
[x] Faction implementation prerequisite: separate faction identity from team ownership before theme polish, supporting dog-vs-dog, cat-vs-cat, dog-vs-cat, ally, enemy, neutral, and future campaign cases.
[x] Dog faction unit pass: implement dog T1-T3 roster with procedural presentation profiles for infantry, engineer, patrol vehicle, harvester, main tank, repair dog, shield tank, siege artillery, and assault tank.
[x] Cat faction unit pass: implement cat T1-T3 roster with procedural presentation profiles for basic cat, rocket cat, engineer cat, scout car, cat tank, harvester, scout aircraft, sniper cat, repair vehicle, shield vehicle, special cat, and crescent artillery.
[x] Presentation prerequisite: route dog/cat unit silhouettes, faction glyphs, role glyphs, accent colors, production icons, minimap pips, and selection/command overlays through shared descriptors before final UI theme QA.
[x] Visual theme direction: replace the current dark neon grid prototype with a purpose-designed soft tactical map style; avoid making the final game look like a debug grid or generic cyber-neon RTS.
[x] Visual theme direction: design the battlefield as a repaired old-city tactical paper/map surface, using warm off-white, beige-gray, muted ink lines, subtle engineering grid hints, and low-saturation faction accents.
[x] Visual theme direction: treat corruption as red-purple ink stains, broken grid lines, distorted annotations, and facility rewrites spreading across the clean map surface.
[x] Visual theme direction: treat lights, safe zones, and dog defense networks as warm-gold repaired lines and restored map clarity rather than generic bright glow.
[x] UI theme design: create a soft light low-contrast command-table palette using warm off-white, pale beige, muted slate text, desaturated faction accents, and no pure white backgrounds.
[x] UI theme design: define contrast tiers so persistent panels stay calm, while selected units, command lines, target markers, alerts, minimap threats, and actionable buttons remain clearly readable.
[x] UI theme design: keep the battlefield visually dominant with edge-docked HUD clusters, compact drawers, generous negative space, and no persistent panels covering the center or lower-middle playfield.
[x] UI theme design: use thin ink-like borders, subtle panel fills, restrained shadows, and minimal glow so the interface feels like a tactical paper map instead of a neon dashboard.
[x] UI theme design: separate faction identity from ownership color - shapes and glyphs show faction, while player/enemy/ally colors drive selection rings, minimap dots, health borders, and command feedback.
[x] UI theme design: specify adaptive day/night variants so the light UI can shift into a darker low-glow mode during night defense, corruption events, or mission-driven atmosphere changes without changing layout.
[x] UI theme QA: test the light low-contrast HUD at 1280x720, 1600x900, 1920x1080, and high-DPI scaling for readability, fatigue, text fit, and command visibility.

## Active Implementation Plan - Faction System And Construction System

### Entity Framework Lock - 99 Point RTS Architecture

[ ] Architecture lock: use `docs/RTS99Design.md` as the whole-game RTS design target and `docs/EntityFrameworkArchitecture.md` as the entity-specific implementation target.
[ ] Architecture lock: stop treating `UnitSpec` as the final root abstraction; promote the long-term root to `EntitySpec` plus `EntityInstance` plus component state, with `UnitSpec` becoming a unit-flavored authoring facade during migration.
[x] Architecture lock: define `EntityKind` as classification only, not inheritance: `Unit`, `Building`, `Turret`, `Resource`, `Objective`, `Projectile`, and `Effect`.
[ ] Architecture lock: runtime identity is `SpecId + OwnerId + Transform + ComponentState`; runtime entities must not care which faction authored the shape except through roster/availability and presentation metadata.
[ ] Architecture lock: keep authoring inheritance optional and shallow, e.g. `DogInfantryDesign : UnitDesign`, but do not create runtime inheritance chains such as `Unit : Entity`, `Building : Entity`, or `Turret : Building`.
[x] Architecture lock: add explicit `EntityId` and `OwnerId` value types or aliases before expanding systems; `OwnerId` should point to player/controller state, while faction, team, alliance, and color remain separate player metadata.
[ ] Architecture lock: make `EntityInstance` thin; move current heavy `UnitInstance` fields into component states such as selection, health, movement, command, weapon, harvester, production, build, dock, vision, pulse, and death state.
[ ] Architecture lock: split current `UnitBattlefieldBuildingTarget` into shared entity/component data instead of growing it as a second building runtime model.
[x] Architecture lock progress: add a `UnitBattlefieldBuildingTarget` to `EntitySpec` / `EntityInstance` bridge with component coverage for health, footprint, construction, power, rally, dock, production, weapon user, and presentation pulse state.
[ ] Architecture lock: convert `BuildingDefinition` and `BuildDefinition` into one entity/build authoring path so combat stats, footprint, construction cost/time, power, producer data, and art do not drift across separate catalogs.
[ ] Architecture lock: ordinary buildings are economy/production/power/tech/docking/objective entities; any fixed combat/support platform is `EntityKind.Turret` even if the UI presents it under a defense/build tab.
[ ] Architecture lock: rotating weapons on tanks, aircraft, turrets, and support platforms remain `WeaponMountSpec` plus art binding, not entities, unless they can be independently selected, damaged, repaired, or destroyed.
[ ] Architecture lock: projectiles and effects are entities only when they affect gameplay; pure tracers, dust, flashes, and decorative explosions should use pooled presentation objects instead of full simulation entities.

### Simulation / View Boundary Lock

[ ] Simulation boundary: gameplay simulation must not depend on Godot `Node`, `SceneTree`, `_Process`, animation callbacks, real time, node traversal order, or physics callbacks as authority.
[ ] Simulation boundary: Godot views read projected entity state and submit commands; views do not directly mutate authoritative health, movement, target, production, economy, or victory state.
[x] Simulation boundary: introduce a fixed simulation tick and route player input through command objects before mutating entity/component state. (Phase 1: `SimClock` fixed 30Hz tick + `EntityWorld.Step` + `CommandSystem`/`MovementSystem`; driven from `BattleRoot.StepEntityWorld`. See docs/Refactor99Plan.md.)
[ ] Simulation boundary: define a command log path for move, attack, attack-move, stop, hold, build, produce, repair, rally, harvest, stance, and debug sandbox commands. (Phase 2: move/attack/attack-move/stop/hold/stance commands implemented in `EntityCommand` + `CommandSystem` and replayed deterministically; build/produce/repair/rally/harvest pending their systems.)
[x] Simulation boundary: add a deterministic replay/hash harness: same seed plus same command log runs twice for thousands of ticks and produces matching state hashes. (Phase 1-2: `tools/SimReplay` runs a 30-unit movement scenario (6000 ticks) and a 24-unit two-team combat scenario (4000 ticks, seeded damage + deaths) twice; hashes match at every checkpoint.)
[x] Simulation boundary: avoid unordered iteration in authority systems; when dictionaries or sets are used, process entities by stable `EntityId` order. (`EntityWorld.OrderedEntities` backed by SortedDictionary; deferred removals via `SortedSet`; `EntityCommandBuffer.Snapshot` orders by tick/issuer/sequence.)
[x] Simulation boundary: use self-owned deterministic random sources for gameplay; presentation may use visual-only time/random pulses. (`DeterministicRng` (SplitMix64) owned by `EntityWorld`; combat damage variance draws from it; state folded into the world hash.)

### Component Model Lock

[ ] Component model: define data components before new gameplay expansion: `Health`, `Transform`, `Selectable`, `Commandable`, `Vision`, `Collision`, `Movement`, `WeaponUser`, `WeaponMountState`, `ProductionQueue`, `Construction`, `Power`, `ResourceNode`, `ResourceCargo`, `Dock`, `RallyPoint`, `Objective`, `BuildRadius`, `FogRevealer`, and `PresentationPulse`. (Most exist in `EntityComponentState`; Phase 2 added `MovementProfile` and `Stance`. Objective/FogRevealer/ResourceNode pending.)
[x] Component model progress: add building-oriented component states for footprint, construction, power, rally point, dock, build radius, and deterministic production queue hashing.
[ ] Component model: define systems around behavior instead of methods scattered across instances: `CommandSystem`, `MovementSystem`, `CombatSystem`, `WeaponSystem`, `ProjectileSystem`, `ProductionSystem`, `ConstructionSystem`, `ResourceSystem`, `VisionSystem`, `SelectionSystem`, `OutcomeSystem`, and `PresentationEventSystem`. (Phase 1-2.5: `ISimSystem` pipeline on `EntityWorld` with `CommandSystem`, `CombatSystem`, `MovementSystem`, `SeparationSystem`, `VisionSystem`, `OutcomeSystem` live + `SimEvent`/`SimEventSink` for `PresentationEventSystem` consumers; Weapon/Projectile/Production/Construction/Resource/Selection systems pending.)
[ ] Component model: no component should own Godot drawing or scene nodes; drawing belongs to `EntityView` / specialized world layers. (Phase 2: `EntityProjection`/`EntityProjector` give views a read-only snapshot boundary; components hold no Godot nodes. Live views still read UnitInstance until that path retires.)
[ ] Component model: `Spec` data is immutable during a match; current hp, cooldowns, buffs, cargo, target, queue progress, build progress, and pulses live only in runtime component state.
[ ] Component model: tech/faction/campaign modifiers resolve into match-time derived data, not into mutable edits to the base spec.

### Command And RTS Feel Lock

[x] Command system: add a real `GroupCommand` layer so selected groups receive one player intent that is then decomposed into per-entity orders. (`GroupMoveEntityCommand`/`GroupAttackEntityCommand` decomposed by `CommandSystem` into per-entity formation/ring slots; proven in `tools/SimReplay` group-move/group-attack scenarios.)
[x] Command system: separate player intent point, formation slot, path corridor, local steering target, and command visualization target; never expose internal slot jitter as the visible command line. (Group commands set `CommandableComponentState.CommandVisualTarget`/`PlayerIntentTarget` to the shared intent while `MovementComponentState.FormationSlot`/`MoveTarget` hold the internal slot.)
[x] Command system: implement range-aware attack positioning: group attacks should assign slots around the target's weapon range ring instead of moving every unit to the target center. (`AttackSlotMath` rings movers at standoff range; SimReplay asserts 0 center-stacked, all attackers in firing band.)
[x] Command system: units already firing from valid positions become temporary combat anchors; rear and late-arriving units path around them instead of pushing them forward. (`AttackSlotMath` marks in-range units as anchors that hold position; movers claim ring slots.)
[ ] Command system: attack-move target acquisition should include target stickiness, threat priority, target filters, current-range preference, and short last-known-position memory.
[x] Command system: movement should degrade gracefully; if perfect formation or perfect attack slot is blocked, units should stop at a readable good-enough position rather than circle or jitter. (`MovementSystem` soft avoidance + `SeparationSystem` positional collision resolution: SimReplay group-move transit min separation 23.6px (was 5.1) with no overlap and no orbiting; deterministic.)
[ ] Command system: add debug metrics for group movement and attack feel: path inflation, corner count, arrival jitter, compactness, stuck seconds, repath count, time-to-first-shot, target switches, and anchor push events. (Phase 2.5: `SimMetrics` (event-derived, deterministic) tracks shots fired, kills, total damage, time-to-first-shot; SimReplay measures compactness (min pairwise separation) + firing-band coverage; remaining metrics - path inflation, corner count, repath, target switches, anchor pushes - pending.)

### Art And Ownership Lock

[ ] Art architecture: rename/replace `ColorUse` with `ColorRole` and collapse entity art roles to `Body`, `Ink`, `Owner`, `Effect`, `Warning`, and `Shadow`; relation colors belong to overlays, not base entity art.
[x] Art architecture: stickers, stripes, badges, turret rings, aircraft wing marks, and building banners are normal `ArtLayer` entries using `ColorRole.Owner`; do not add a separate decal-slot system.
[ ] Art architecture: rename/generalize `UnitArtRecipe` toward `ArtRecipe` so units, buildings, turrets, resources, objectives, projectiles, and gameplay effects can share layer/binding/color behavior.
[x] Art architecture: add `EnvironmentTone` and per-layer `EnvironmentResponse` presets: `Normal`, `OwnerProtected`, `EffectReactive`, and `WarningFixed`.
[x] Art architecture: `Owner` layers must preserve minimum contrast under day, fog, night, corruption, cloak, and damaged/offline states.
[ ] Art architecture: `UnitRenderPalette.SoftOldCity(UnitFactionId, PlayerSlotId)` should move toward `EntityRenderPalette.Resolve(ColorRole, OwnerColor, EnvironmentTone)` so faction identity no longer controls ownership color.

### Migration Order - Do This Before More Feature Growth

[x] Migration 1: add skeleton files for `EntityKind`, `EntityId`, `OwnerId`, `EntitySpec`, `EntityInstance`, `EntityComponentState`, `EntityWorld`, and `EntityCommand` without deleting current unit/building code.
[x] Migration 2: make existing `UnitSpec` convertible to or backed by `EntitySpec`, preserving current dog/cat unit behavior while proving the shared model.
[ ] Migration 3: move `UnitInstance` health, movement, selection, command, weapon, harvester, and presentation pulse data into explicit component states behind compatibility accessors if needed. (Phase 2: `UnitSpecEntityBridge.SpawnUnit` now emits full sim-ready components - Health/Movement/MovementProfile/Collision/Vision/WeaponUser/Stance/Pulse - and generic systems drive them; UnitInstance still the live runtime, EntityWorld is a parallel non-authoritative path.)
[ ] Migration 4: migrate `UnitBattlefieldBuildingTarget` and current building combat/production runtime into entity/component data, then remove building-specific mirror target logic.
[x] Migration 4 progress: bridge existing `UnitBattlefieldBuildingTarget` instances into entity specs/components and cover turret/producers/refinery dock/build progress/power state with deterministic tests.
[ ] Migration 5: merge `BuildingDefinition`, `BuildDefinition`, `BuildingPresentationDescriptor`, and producer/queue metadata into entity/build specs with dedicated construction and production components.
[ ] Migration 6: replace old `UnitKind`, `BuildingKind`, `GameState.Definitions`, `UnitCatalog`, `BuildingPresentationCatalog`, and `FactionCatalog` dependencies only after the new entity path owns live gameplay.
[ ] Migration 7: add deterministic entity-world tests for spawning, selection, move, attack, production, construction, harvest, building destruction, turret firing, projectile lifetime, fog visibility, and replay hash stability. (Phase 2: `tools/SimReplay` covers spawning, move, auto-acquire attack, seeded damage, deaths, projection, and replay-hash stability across movement/combat/authored dog-vs-cat scenarios; production/construction/harvest/fog pending their systems.)
[ ] Migration 8: add sandbox stress tools for 30-unit group move, 30-unit group attack, firing-anchor avoidance, harvester dock congestion, turret defense, owner-color mirror matches, and day/fog/night/corruption readability. (Partial: `tools/SimReplay` covers deterministic 30-unit group move (no clumping) and 30-unit group attack (ring, 0 center-stack, firing anchors); harvester/turret/mirror/readability stress pending.)

### UnitSpec Architecture - Elegant Block Unit Design

[x] UnitSpec architecture goal: make new unit design lightweight: add one unit design class that inherits `UnitDesign`, sets design metadata/data/art, and can be spawned by the battlefield with an external `PlayerSlotId`.
[x] UnitSpec architecture naming rule: `UnitFactionId` is unit design metadata like base HP and may guide art; `PlayerSlotId` is the only unit instance ownership/color context; teams, alliances, and hostility are battle-system rules outside the unit.
[x] UnitSpec architecture relation rule: support same-unit-faction mirrors, cross-unit-faction battles, and mixed alliances by mapping players to battle relations outside the unit instance.
[x] UnitSpec architecture phase 1: add `UnitDesign` as the lightweight inherited class library for new units; do not build a legacy compatibility layer or generate old unit/presentation data from it.
[x] UnitSpec architecture phase 1: define `StatsSpec`, `MovementSpec`, `CollisionSpec`, `WeaponMountSpec`, `AbilitySpec`, `ProductionSpec`, `UnitArtRecipe`, `ArtLayer`, and `ColorUse` as focused reusable pieces.
[x] UnitSpec architecture phase 1: keep `Light`, `Medium`, and `Heavy` as data/collision/combat meaning only; they must not decide body shape, turret art, player color, or unit drawing style.
[x] UnitSpec architecture phase 1: treat turret/gun mounts as reusable logic only through `WeaponMountSpec`; turret visuals remain part of each unit's own `UnitArtRecipe` and bind to a mount id when they need to rotate.
[x] UnitSpec architecture phase 1: replace hard-coded unit colors with `ColorUse` values such as `Body`, `Outline`, `Faction`, `Player`, `Warning`, `Selection`, and `Effect`; final colors come from unit faction, player ownership, battle relation, world theme, and runtime state.
[ ] Entity visual architecture correction: collapse runtime visual ownership color into one `OwnerColor`; remove separate faction/player/relation color meaning from entity art, with relation states expressed through overlays that still derive from owner/hostility policy.
[x] Entity visual architecture correction: treat stickers, stripes, badges, building banners, turret rings, and aircraft wing marks as normal `ArtLayer` shapes using `ColorRole.Owner`; do not add a separate decal-slot system.
[x] Entity visual architecture correction: add `EnvironmentTone` to render context so environment can affect body, ink, shadow, glow, warning, and effect layers without changing ownership semantics.
[x] Entity visual architecture correction: add simple art-layer environment response presets such as `Normal`, `OwnerProtected`, `EffectReactive`, and `WarningFixed`; `Owner` layers should retain minimum contrast/readability under night, fog, and corruption.
[ ] Entity architecture direction: migrate from separate unit/building special cases toward a shared battlefield entity language: `EntitySpec`/core metadata, `OwnerId`, `ArtRecipe`, stats, weapons, abilities, and runtime state, while keeping movement-only and building-only components separate.
[ ] Entity architecture direction: define `EntityKind` as `Unit`, `Building`, `Turret`, `Resource`, `Objective`, `Projectile`, and `Effect`; ordinary buildings should not carry defense behavior just because they are buildings.
[ ] Turret entity rule: treat defensive/support turrets as independent fixed-platform entities with `OwnerId`, footprint/collision, health, vision, weapons or support abilities, and art; they are not ordinary buildings.
[ ] Turret mount rule: treat the rotating gun/platform on top of a turret, tank, or aircraft as `WeaponMountSpec` plus `ArtBinding`, not as a separate entity unless it can be individually selected, damaged, repaired, or destroyed.
[ ] Building architecture rule: keep buildings focused on production, power, economy, tech, construction, docking, and objectives; if a structure attacks or projects fixed defense/support fields, model that as `EntityKind.Turret` or an explicit child entity.
[x] UnitSpec architecture phase 2: migrate unit archetypes into inherited UnitDesign files first, including patrol/rocket/engineer, light fast vehicle, guard tank, and harvester.
[x] UnitSpec architecture phase 2: add cat T1 inherited UnitDesign baseline for basic infantry, scout car, tank, and harvester so normal dog-vs-cat skirmish can use pure UnitSpec units.
[x] UnitSpec architecture phase 2: keep roster/production availability outside the unit instance, but allow it to filter by UnitDesign metadata such as `UnitFactionId`, tech tier, and role tags.
[x] UnitSpec architecture phase 2: update sandbox unit spawning to use pure typed entries or archetype entries instead of scattered dictionary edits and old `UnitKind`-first creation paths.
[x] UnitSpec architecture phase 3: add a new UI icon path so `DynamicUnitIcon` can render inherited `UnitDesign` / `UnitArtRecipe` directly without old `UnitKind` data.
[x] UnitSpec architecture phase 3: add pure UnitInstance world rendering, minimap pips, and selection summary data from `UnitArtRecipe` / `UnitSpec` without old `UnitKind` presentation catalogs.
[x] UnitSpec architecture phase 3: connect pure `UnitBattlefield` / `UnitInstanceView` to the live BattleRoot sandbox path and show new `UnitMinimapPip` data in HudLayer minimap.
[x] UnitSpec architecture phase 3: route sandbox selection input, box select, same-design double click, right-click formation move, command target lines, and selected-unit HUD details through pure `UnitBattlefield` / `UnitInstance` data.
[x] UnitSpec architecture phase 3: add deterministic smoke coverage for new UnitInstance pick selection, rectangle selection, same-design selection, formation move assignment, and runtime movement update.
[x] UnitSpec architecture phase 3: route normal skirmish visible starting units through pure UnitDesign loadouts and UnitInstanceView, including dog player and cat enemy starting forces.
[x] UnitSpec architecture phase 3: mirror legacy production completions into visible UnitInstance spawns using UnitDesign production mappings and completed spawn-point data while production queues are being migrated.
[x] UnitSpec architecture phase 3: add UnitBattlefield unit-vs-unit combat, manual attack commands, auto-acquire, weapon damage, death removal events, and UnitInstance death VFX without old UnitModel combat.
[x] UnitSpec architecture phase 3: register building targets in UnitBattlefield so UnitInstance units can right-click attack buildings, apply weapon damage, clear destroyed building targets, and feed HP back to the existing building removal/outcome path.
[x] UnitSpec architecture phase 3: move building target death events and HQ victory/defeat outcome detection into UnitBattlefield, with BattleRoot consuming the new events for immediate view/HUD updates.
[x] UnitSpec architecture phase 3: switch normal BattleRoot skirmish units, selected-unit panels, and live command/selection input from old `UnitModel` / `UnitKind` paths onto pure `UnitInstance` runtime.
[ ] UnitSpec architecture phase 3: replace duplicate unit definitions in `GameState`, `UnitPresentationCatalog`, and `FactionCatalog` with direct UnitSpec-driven runtime data; do not keep adapter APIs solely for old callers.
[x] UnitSpec architecture phase 3: move visible player production queues, produced-unit records, rally assignment, unit combat, unit deaths, building target deaths, and HQ outcome checks onto UnitBattlefield.
[x] UnitSpec architecture phase 3: migrate enemy production and enemy attack waves from hidden legacy `GameState` units onto UnitBattlefield UnitInstance runtime.
[x] UnitSpec architecture phase 3: in UnitDesign runtime mode, stop advancing hidden legacy `UnitModel` movement, combat, harvesting, and production; keep only world/theme/fog refresh with UnitBattlefield vision sources.
[x] UnitSpec architecture phase 3: migrate player harvesting and resource-field economy loops onto UnitBattlefield UnitInstance runtime, including refinery dock claims, cargo, unload credits, and resource depletion.
[x] UnitSpec architecture phase 3: migrate building self-weapons onto UnitBattlefield building targets, including auto-acquire, cooldown damage, unit death cleanup, and player attack alerts.
[x] UnitSpec architecture phase 3: sync player/enemy resource ownership between UnitBattlefield runtime economy and legacy GameState build-placement economy while both systems coexist.
[x] UnitSpec architecture phase 3: add direct UnitBattlefield vision/resource snapshot APIs and route BattleRoot minimap/resource fog inputs through them, excluding hidden legacy UnitModel vision in UnitDesign runtime mode.
[ ] UnitSpec architecture deletion target: remove old `UnitCatalog`, old unit `FactionId` runtime ownership, and old `UnitKind`-first spawning after `UnitBattlefield` owns movement, combat, production, and rendering paths.
[x] UnitSpec architecture QA: add deterministic tests that every UnitDesign produces a complete UnitSpec, has valid art layers, valid weapon mounts, a UnitFactionId, and resolves color uses under mirror and mixed-alliance cases.

### Weapon System V1

[x] Weapon V1: add explicit weapon target profiles for allowed movement domains, allowed armor tags, and target-priority scoring so units do not always auto-acquire the nearest hostile.
[x] Weapon V1: keep damage as ammo base damage multiplied by weight/domain/armor profiles, but add a separate "can this weapon engage this target" gate before manual attack, auto-acquire, and firing.
[x] Weapon V1: implement dog T1 roster baseline: patrol dog, rocket dog, engineer dog, light fast fire vehicle, guard tank, and retriever harvester.
[x] Weapon V1: make the dog light fast fire vehicle effective against light ground units, able to engage aircraft at reduced value, weak against vehicles, and very poor against structures.
[x] Weapon V1: wire `CanFireWhileMoving` into fire authorization so fixed/setup weapons can later require stopping, while light mobile weapons keep responsive chase-and-fire behavior.
[x] Weapon V1: add deterministic combat tests for target legality, target-priority selection, dog rocket availability, and the light fast fire vehicle's non-specialist anti-air role.
[x] Weapon V1 coupling cleanup: extract weapon and ammo static data from `GameState` into `WeaponCatalog` while preserving compatibility accessors for existing systems and tests.

### Faction System

[x] Faction system phase 1: introduce `FactionId` / `FactionDefinition` separate from `Owner`, so player/enemy/team ownership is not the same thing as faction identity.
[x] Faction system phase 1: define faction data for display name, accent palette, HUD color, unit/building availability, AI profile, starting loadout, and localized text keys.
[x] Faction system phase 1: add `FactionId` to units, buildings, production queues, minimap pips, alerts, and selection/detail presentation while keeping `Owner` for team/alliance logic.
[x] Faction system phase 2: replace hard-coded player cyan/enemy red rendering with faction palette plus hostile/ally overlay rules.
[x] Faction system phase 2: add alliance relation helpers: self, allied, neutral, hostile, visible hostile, and targetable hostile.
[x] Faction system phase 2: route combat targeting, auto-acquire, threat sharing, fog/minimap filtering, and victory checks through relation helpers instead of direct `owner != owner`.
[ ] Faction system phase 3: make skirmish setup choose player faction and enemy faction independently, with deterministic starting base/unit templates per faction.
[ ] Faction system phase 3: add deterministic tests for relation logic, faction color presentation, faction-specific production availability, and enemy AI faction setup.

### Construction System

[ ] Construction system phase 1: replace `B`-cycle prototype placement with build catalog entries grouped by icon tabs: command, power, economy, infantry, vehicle, turret, air, naval.
[ ] Construction system phase 1: add `BuildDefinition` data for cost, build time, footprint, required tech, required producer, power provided/used, build radius, placement terrain, and refund.
[ ] Construction system phase 1: distinguish queued construction, placement preview, placed-under-construction building, completed building, paused/offline building, and destroyed building states.
[ ] Construction system phase 2: implement RTS build queue flow: click build icon, pay/reserve cost, construction timer runs, then placement mode starts when ready.
[ ] Construction system phase 2: support direct placement for cheap/basic structures and delayed placement for advanced structures if the faction design requires it.
[ ] Construction system phase 2: enforce construction constraints: inside build radius, no overlap, passable terrain, tech prerequisites, power state, faction availability, and fog/build visibility rules.
[ ] Construction system phase 3: upgrade right production/build UI so multiple barracks/factories appear as independent producer lanes with per-building queue, progress, rally, cancel, and selected-producer focus.
[ ] Construction system phase 3: when no producer is selected, aggregate production by producer type while still showing which factory owns each queued item.
[ ] Construction system phase 3: implement building hotkeys, disabled states, cost badges, prerequisite warnings, placement grid, rotation, cancel, and refund feedback.
[ ] Construction system phase 4: add deterministic QA for placement math, build radius, power/tech prerequisites, construction progress, multiple factory queues, cancel/refund, and UI state snapshots.

### Suggested Order

[x] Iteration A: add faction data model and relation helpers without changing gameplay behavior.
[x] Iteration B: route rendering, targeting, minimap, alerts, and AI through faction/relation helpers.
[ ] Iteration C: add build catalog data and right-side build tabs, still using existing placement backend.
[ ] Iteration D: add construction queue/build timer/placed-under-construction states.
[ ] Iteration E: implement multiple producer lanes and selected-building production focus.
[ ] Iteration F: polish UX, hotkeys, i18n, tests, and QA.

## Performance Optimization Plan

Performance budget and instrumentation for the desktop RTS. Target: stable 60 FPS at 1920x1080 with 200+ live units, full fog, and combat VFX; simulation tick under budget at 30Hz. All items should be measured before and after, not assumed. Ties into the `docs/RTS99Design.md` "性能" metrics: `simulationTickMs`, `renderMs`, `entityCount`, `projectileCount`, `effectPoolUsage`, `fogUpdateMs`, `pathRequestsPerSecond`.

### Instrumentation First (measure before optimizing)

[ ] Perf metrics: add a `PerfHud` debug overlay (toggle hotkey) showing FPS, frame time ms, `_Process` sim-step ms, render/draw ms, live entity count, projectile/effect count, visible-unit count, and fog update ms.
[ ] Perf metrics: extend `SimMetrics` (or add a `PresentationMetrics`) with rolling averages and 1%-low frame time so spikes are visible, not just mean FPS.
[ ] Perf metrics: add a headless perf smoke tool (`tools/PerfSmoke`) that spawns N units (50/100/200/400), runs M ticks, and reports sim-step ms percentiles with a regression threshold; fail CI if sim tick exceeds budget. (DONE: `tools/PerfSmoke` measures 50/100/200/400 units x 1200 ticks, asserts worst avg < 50% of the 33.3ms tick budget. Baseline: 50u 0.46ms, 100u 0.43ms, 200u 1.72ms, 400u 10.28ms avg - superlinear above 200u confirms the VisionSystem broadphase item below is the next target.)
[ ] Perf metrics: log per-system step ms inside `EntityWorld.Step` behind a debug flag so a slow system (Combat/Movement/Separation/Vision) is identifiable.

### Camera And Frame Rate

[ ] Camera perf: decouple camera movement from frame rate - `CameraController._Process` already scales by delta, but verify edge-scroll and zoom feel identical at 30/60/144 FPS and add a frame-rate-independent smoothing (lerp by `1 - exp(-k*dt)`), not a fixed per-frame factor.
[ ] Camera perf: add optional camera position/zoom smoothing (damped) so fast edge-scroll and minimap jumps do not cause per-frame full-world redraw storms; throttle dependent redraws to actual camera-rect changes.
[ ] Camera perf: expose a frame-rate cap / vsync setting (Off, VSync, 60, 144) in display settings and confirm `DisplayAudioSettings` persists it; verify uncapped mode for perf profiling.
[ ] Camera perf: confirm `Engine.MaxFps` and `Engine.PhysicsTicksPerSecond` are set intentionally; the deterministic sim uses `SimClock` 30Hz, so physics tick should not silently drive gameplay.

### View Redraw And Culling (biggest current cost)

[ ] Render perf: views currently call `QueueRedraw()` unconditionally every frame (`UnitInstanceView`, `UnitView`, `BuildingView`, `GridLayer`, `ResourceFieldView`, `SignalNetworkLayer`, `FootprintLayer`). Only redraw when the projected/observed state actually changed (dirty flag) or on a throttled interval, not every frame.
[ ] Render perf: add off-screen culling - hide or skip `_Draw` for views whose world position is outside `CameraController.VisibleWorldRect()` (plus a margin); `VisibleWorldRect` exists but is unused for culling today.
[ ] Render perf: use Godot `VisibleOnScreenNotifier2D` or a manual visible-rect test so off-screen `UnitInstanceView` nodes stop redrawing and (optionally) hide.
[ ] Render perf: `GridLayer` redraws the whole grid every frame - render the static grid once to a cached texture/`MultiMesh` or only the visible camera rect; avoid per-frame full-world line drawing.
[ ] Render perf: batch per-unit procedural draw calls - evaluate `MultiMeshInstance2D` or pre-rendered per-design sprite atlases for unit bodies so 200+ units are not 200+ `CanvasItem._Draw` passes with many `DrawCircle`/`DrawArc` calls each.
[ ] Render perf: pool combat VFX and footprints (`CombatEffectsLayer`, `FootprintLayer`) - reuse fixed buffers instead of growing per-frame lists; cap concurrent effects and fade oldest under load.

### Fog Of War Rendering

[ ] Fog perf: `FogOfWarLayer` already throttles world redraw via `WorldRedrawIntervalSeconds` and uses a single shader mask - verify the mask `ImageTexture.Update` does not reallocate (reuse the buffer) and only uploads when the visibility buffer actually changed.
[ ] Fog perf: scope fog recomputation to the camera-visible rect plus a margin when the full map is larger than the screen, instead of recomputing all cells; keep off-screen explored memory cached.
[ ] Fog perf: profile fog mask resolution (`FogOfWarVisualPolicy.MaskSize` / `CellSize`) vs readability - pick the lowest resolution that still looks smooth after the shader bilinear/feather pass; expose as a quality setting.
[ ] Fog perf: make fog update frequency and mask resolution a quality tier (Low/Medium/High) in display settings; Low raises `WorldRedrawIntervalSeconds` and coarsens the mask for low-end machines.
[ ] Fog perf: ensure the minimap consumes the same cached fog mask texture (already migrated) and is not re-sampling fog cells per minimap refresh.

### Simulation Step Cost

[ ] Sim perf: `SeparationSystem` and `VisionSystem` are currently O(n*neighbors) / O(viewers*entities) - confirm the spatial hash keeps them near-linear at 200+ units; add a broadphase grid for `VisionSystem` so it is not viewers x all-entities. (DONE for VisionSystem: spatial grid sized to max sight range + per-owner allied resolution; 400u sim step 10.28ms->8.41ms, determinism unchanged. SeparationSystem already hashed; CombatSystem `NearestHostile` is the next O(n^2) hot spot to broadphase.)
[ ] Sim perf: avoid per-tick allocations in hot systems - `CombatSystem`/`MovementSystem`/`SeparationSystem` rebuild lists and dictionaries every tick; reuse buffers (object pools / cleared scratch collections) to cut GC pressure.
[ ] Sim perf: `EntityWorld.StableEntities`/`StableSpecs` allocate a new list per call - audit call sites and prefer `OrderedEntities` (no copy) on hot paths.
[ ] Sim perf: cap fixed-tick catch-up (already capped in `SimClock.MaxTicksPerAdvance`) and confirm a frame hitch never triggers a sim spiral; add a metric for dropped-tick backlog events.
[ ] Sim perf: when both `UnitBattlefield` and `EntityWorld` run during migration, ensure the non-authoritative shadow path can be disabled to isolate and measure each system's cost.

## Goal

Build a desktop RTS prototype in Godot 4.7 Mono + C# with crisp procedural 2D visuals, no image-based unit art, RTS-style selection, movement, combat, economy, base building, AI skirmish flow, and a workflow that supports multiple AI collaboration / multiple AI collaborators working on different components in parallel.

## Branch AI UI / i18n Plan

[x] i18n branch: add settings language selector for English and Simplified Chinese.
[x] i18n branch: persist selected interface language in user settings and apply it on launch.
[x] Battle right UI sketch: implement fixed right command column with minimap top, production/build grid middle, and unit/building detail bottom.
[x] Battle bottom UI sketch: implement always-visible narrow command ribbon with stance group, move-mode group, contextual action icons, and settings entry.
[x] Battle left UI sketch: add a compact global-skill icon stack independent from unit commands.
[x] Battle top UI sketch: move resource/status strip to a compact centered top cluster.
[x] Battle HUD QA: update layout rules for the sketched right column, bottom ribbon, left global skills, and clear center playfield.
[x] Battle right UI behavior: auto-collapse production/detail drawer after inactivity while keeping minimap and right rail visible.
[x] Dynamic unit icon system: reuse unit presentation descriptors to render animated large and small UI icons that match world units.
[x] Battle right UI polish: split production and unit detail into independent lower right panels and convert production tabs to icon-only controls.
[x] Battle right UI polish: remove redundant legacy right-rail icon buttons while keeping the hover expansion rail.
[x] Battle detail UI behavior: selected units and buildings immediately open the detail panel without forcing the production panel open.
[ ] Battle right UI polish: wire production category tabs for building, turret, light, tank, air, and naval pages.
[ ] Battle production UI architecture: support multiple barracks/factories as independent producer lanes with per-building queue, progress, rally, cancel, and selected-producer focus.
[ ] Battle production UI architecture: when no producer is selected, aggregate queues by producer type without hiding which factory owns each item.
[ ] Battle detail UI polish: render multi-unit summaries as mini unit icons plus counts, and single-unit/building details as richer icon-first cards.

## Main AI UI Start Plan - Soft Old City

[x] Soft Old City UI pass: create a shared Soft Old City palette/helper for HUD fills, borders, text, command accents, danger accents, minimap pips, and selection/target markers.
[x] Soft Old City UI pass: migrate Battle HUD panels from dark neon styling to warm beige/off-white translucent panels with muted ink borders and low-fatigue text.
[x] Soft Old City UI pass: update minimap, production drawer, unit detail drawer, command ribbon, alert strip, and resource/status strip to use Old City Day default colors first.
[x] Soft Old City UI pass: add hooks for Fog Morning and Dusk Defense palettes without changing the HUD layout.
[x] Soft Old City UI pass: verify command readability after the palette change - move lines, attack lines, rally markers, build previews, disabled buttons, and enemy alerts must stay stronger than passive panels.

## Redundancy Cleanup Plan

[ ] Redundancy scan: consolidate duplicated RTS UI plan entries across Current Design Direction, Milestones, Auto Iteration Detail Plan, and Current Next Target.
[ ] Redundancy scan: remove the duplicated mid-file `## Goal` header and rehome its branch-plan content under one canonical active backlog section.
[ ] Redundancy scan: consolidate overlapping Soft Old City, Light Low-Contrast UI Theme, Main AI UI Start, and AI A visual-theme plans into one visual-theme source of truth.
[ ] Redundancy scan: consolidate overlapping dog/cat faction roster, faction identity, and production/build foundation plans across Parallel AI B, Light Low-Contrast prerequisites, and Active Implementation Plan.
[ ] Redundancy scan: consolidate duplicated shared-threat entries into one completed implementation note plus one future-polish note.
[ ] Redundancy scan: merge duplicated settings/i18n history so language selection, persistence, and localization coverage live in one canonical section.
[ ] Redundancy scan: extract repeated UI factories and styles from MainMenuRoot, SettingsOverlayLayer, PauseMenuLayer, OutcomeScreenLayer, and HudLayer into a shared UiTheme/UiFactory helper.
[ ] Redundancy scan: reconcile SoftOldCityTheme HUD palette with WorldThemeMath world palette so day/fog/dusk colors are not maintained in two drifting theme systems.
[x] Redundancy scan: remove or repurpose legacy SeparationMath if runtime movement has fully migrated to SpatialHashAvoidanceMath; update SelectionStress so tests cover active runtime movement only.
[ ] Redundancy scan: consolidate fog-of-war completed phase notes into one final implementation note plus one future visual/performance note.
[ ] Redundancy scan: clean generated bin/obj artifacts from tools projects; latest scan found 139 generated bin/obj files visible to source search despite gitignore.
[ ] Redundancy scan: decide whether Godot .uid files are intentional workspace metadata; latest scan found 65 .uid files and they should either be explicitly kept or ignored consistently.
[ ] Redundancy scan: define an artifact retention policy for visual QA screenshots and generated previews; latest scan found 23 files under artifacts.
[ ] Redundancy scan: split TODO.md into Active Backlog and Completed Archive once duplicated completed items are consolidated, so multiple AI branches choose from the same canonical queue.

## Current Design Direction

[x] Core taxonomy: add unit weight classes Light, Medium, Heavy for armor, speed, collision, selection priority, and weapon effectiveness.
[x] Core taxonomy: add movement domains Land, Naval, Air, and Amphibious so terrain, pathing, targeting, and production can branch cleanly.
[x] Terrain adaptation: define passability layers for ground, water, coast/bridge, air, buildings, and future blockers before adding ships and aircraft.
[x] Terrain adaptation: update path requests so ground units avoid water, ships stay in navigable water, aircraft ignore ground blockers but respect map bounds.
[x] Terrain floor design: replace the dark prototype grid with readable procedural battlefield floors, including terrain panels, navigation hints, water/coast variants, and fog-friendly contrast.
[x] Visual theme design: add a dual day-night tactical map aesthetic, with soft off-white/beige low-saturation daytime command-table visuals and dark blue-black nighttime radar visuals.
[x] Visual theme design: keep one shared procedural line-art language across day and night, with muted colors, low eye strain, crisp unit/command readability, and a mission-driven atmosphere transition interface instead of fixed timing.
[x] Visual theme system: expose scriptable day/night and atmosphere transition hooks that missions can drive from story events, defense timers, objective phases, signal restoration, facility corruption, and scripted set pieces.
[x] Visual theme design: make daytime emphasize planning, rebuilding, repair, resource gathering, and readable terrain; make nighttime emphasize pressure, fog, light-network safety, signal noise, and defensive caution.
[x] Visual theme design: make road lights, signal towers, and safe-zone nodes matter in both modes - engineering/control bonuses by day, vision/anti-corruption/safety by night.
[ ] UI theme design: support adaptive day/night HUD palettes without changing layout, using soft light panels by day and low-glow dark panels by night while preserving command, selection, minimap, and alert readability.
[x] Combat range rebalance: increase default weapon ranges and make range/sight/aggro values explicit per unit definition.
[x] Combat fix: light fixed-forward units rotate their body into arc and can attack instead of getting stuck unable to fire.
[x] Combat range tuning: separate shorter engagement stop range from slightly longer projectile fire authorization range to reduce same-speed chase stalls.
[x] Turret system: separate body facing, move facing, turret facing, target acquisition, target leading, and fire authorization.
[x] Turret system: support moving search-and-fire behavior according to move command mode: attack advance, direct advance, ignore advance.
[x] Turret system: make static weapons, mobile turrets, fixed-forward weapons, and future special weapons share one combat interface.
[x] Ammo model: create weapon/ammo definitions with projectile behavior, hit rule, damage profile, splash hooks, visual style, sound cue, and special attack hooks.
[x] Ammo default: Needle dart, a very thin and fast guaranteed-hit light projectile for light units.
[x] Ammo default: Ballistic cannon, a tank shell with random deviation, poor accuracy against Light targets, normal accuracy against Medium and Heavy targets.
[x] Ammo default: Electromagnetic lance, guaranteed hit, high damage against Medium/Heavy armored units, low damage against Light targets.
[x] Ammo default: Ion beam, guaranteed hit, high damage against Light targets, average damage against Medium/Heavy targets.
[x] Ammo default: Seeker rocket, tracking projectile with medium damage and visible turn-in-flight behavior.
[x] Damage model: add damage multipliers by weight class and domain, with room for armor tags such as Infantry, Vehicle, Structure, Ship, Aircraft.
[x] Special attack extensibility: allow future units to override targeting, firing, projectile update, impact, chain, charge-up, beam, and area effects without rewriting combat.
[x] Combat VFX placeholder: add procedural unit death burst with flash ring, fragments, and smoke so destroyed units no longer vanish silently.
[x] Combat VFX polish: vary unit death effects by weight class, movement domain, ammo type, and overkill damage, with debris/embers/EMP dissolve hooks.
[x] Unit presentation model: create one shared display descriptor for world renderer, right-side unit detail, production cards, tooltips, minimap pips, and selection overlays.
[x] Unit presentation model: keep unit art procedural/vector-like, but use reusable shape layers, color roles, status glyphs, and readable silhouette rules.
[x] Visual footprint design: add procedural footprint/track emitters for moving land and naval units, with short-lived vector-like marks that fade by terrain and speed.
[x] Visual footprint design: use footprints to help distinguish unit classes - light units leave thin fast step marks, medium units leave paired tread strokes, heavy units leave broader compressed track plates, ships leave wake ripples.
[x] Visual footprint design: aircraft should not leave ground footprints; use jet exhaust, heat shimmer, or soft contrail/cloud-tail effects based on altitude, speed, and aircraft class.
[x] Visual footprint design: keep footprint effects low-contrast and disabled under heavy UI/fog clutter so they improve readability without competing with selection and command markers.
[x] i18n: add localization keys and preload zh-CN and en-US text resources.
[x] i18n: route all user-facing UI strings through the localization layer before expanding final UI.
[x] UI icon library: use Tabler Icons as the primary vector UI icon set because it is MIT-licensed, broad, and consistent on a 24x24 grid.
[x] UI icon library: optionally use Game-icons.net for weapon/unit semantic icons only with documented CC BY 3.0 attribution, or replace with procedural glyphs where attribution is undesirable.
[x] UI final polish: convert visible instructional labels into icon buttons, compact values, hover tooltips, and state overlays; avoid non-hover tip text inside the game HUD.
[x] UI final polish: right sidebar remains the primary command surface: minimap top, production second, tactical commands/movement modes middle, unit details bottom.
[x] UI final polish: bottom strip stays narrow for selected-unit state, control groups, alerts, and forward objective/command status.
[x] Advanced pathing: make target movement prefer straight readable routes, using global pathing only when direct travel is blocked.
[x] Advanced pathing: separate player intent target, formation slot target, global corridor, and local steering so avoidance does not turn movement into zig-zagging.
[x] Advanced pathing: add line-of-sight path simplification and waypoint pruning after A* so units skip unnecessary grid corners.
[x] Advanced pathing: add corridor/funnel smoothing around building obstacles, keeping units close to a clean path spine instead of following raw grid cells.
[x] Advanced pathing: treat combat anchors as temporary global blockers so rear units path around firing front units instead of pushing through them.
[x] Advanced pathing: add dynamic obstacle handling for buildings, anchors, and dense unit blobs with limited local detours and throttled repath requests.
[x] Advanced pathing: keep local avoidance lateral and soft, with target attraction stronger than separation unless collision would be severe.
[x] QA: add deterministic tests for weapon hit rules, turret state transitions, terrain passability, localization fallback, and presentation descriptor completeness.

## Milestones

[x] Install .NET SDK 8
[x] Install Godot 4.7 Mono
[x] Create Godot C# project
[x] Create main battle scene
[x] Add C# simulation layer
[x] Add procedural grid renderer
[x] Add procedural unit renderer
[x] Add player and enemy starter units
[x] Add camera movement and zoom
[x] Add drag selection
[x] Add right-click move commands
[x] Add HUD shell

[x] Improve unit picking precision
[x] Add selection box polish and zoom stress tests
[x] Fix right-drag box selection
[x] Selection polish: box selection prioritizes combat units over harvesters, but selects harvesters when the box contains only harvesters; single-click and explicit selection still allow economic unit control.
[x] Selection polish: smart harvester box intent lets focused or mostly-economic drag boxes include harvesters in mixed selections, uses footprint overlap for large units, and still excludes harvesters from broad combat drag boxes.
[x] Selection polish: double-click a player unit to select same-kind player units in the current camera view, including explicit harvester double-click selection.
[x] Add control groups: Ctrl+1-9 and 1-9
[x] Add multi-unit formation movement
[x] Add unit separation to reduce overlap
[x] Add edge scrolling

[x] Add unit attack stats
[x] Add right-click enemy attack command
[x] Add turret tracking toward targets
[x] Add projectile rendering
[x] Add beam rendering
[x] Add hit flashes and impact effects
[x] Add unit death and removal
[x] Add basic auto-targeting
[x] Add unit engagement stance model
[x] Add stance: Hold - attack enemies in weapon range without leaving position
[x] Add stance: Aggressive - attack enemies in sight range and pursue
[x] Add stance: Return Guard - attack enemies in sight range, pursue, then return to anchor position
[x] Add stance: Passive Retaliate - ignore enemies until attacked, then retaliate without abandoning position
[x] Add stance UI controls and hotkeys
[x] Add shared threat propagation and ally engagement calls
[x] Threat propagation plan: create a lightweight combat event when a unit is attacked or acquires a target
[x] Threat propagation plan: nearby allied guards copy the threat only if idle, not manually commanded
[x] Threat propagation plan: Hold units assist inside weapon range or a small guard-link radius, without chasing far away
[x] Threat propagation plan: Aggressive units accept shared threats inside sight range and pursue
[x] Threat propagation plan: Return Guard units pursue shared threats, then return to anchor
[x] Threat propagation plan: Passive units only accept direct damage or very close ally calls, without roaming
[x] Threat propagation plan: add short memory and cooldown so the same threat does not constantly overwrite decisions
[x] Threat propagation plan: add visual/debug feedback for alert pulses and ally call radius

[x] Add building models
[x] Add procedural building rendering
[x] Add build placement preview
[x] Add grid snapping and placement validation
[x] Add building health and destruction

[x] Add production queues
[x] Add barracks and vehicle factory production
[x] Add production command UI before rally points
[x] Add shared threat propagation and ally engagement calls.
[x] Add rally points
[x] Add resource inventory
[x] Add resource fields
[x] Add harvester gather-return-unload loop
[x] Add refinery delivery logic

[x] Add RTS-grade interface shell
[x] RTS UI plan: fixed top resource/status bar, bottom selection panel, bottom-right command card, bottom-left minimap
[x] RTS UI plan: selected unit/building info panel with portrait glyph, health, armor/range/speed, stance, carried resources, and production progress
[x] RTS UI plan: command card with icon buttons, hotkeys, disabled states, costs, cooldown/progress overlays, and concise tooltips
[x] RTS UI plan: production/build tabs for structures, infantry, vehicles, economy, queue inspection, cancel, and refund hooks
[x] RTS UI plan: evaluate and migrate toward a right-side command sidebar for production, build, queues, and tactical commands
[x] RTS UI plan: minimap with camera rectangle, unit/building/resource pips, attack alerts, and click-to-jump camera control
[x] RTS UI plan: control group bar with group contents, selected group highlight, and quick recall feedback
[x] RTS UI plan: alert strip for under attack, production complete, insufficient credits, idle harvester, and base power/building events
[x] RTS UI plan: cursor and command preview modes for move, attack, rally, harvest, build placement, invalid placement, and target hover
[x] RTS UI plan: scalable crisp vector theme with stable dimensions, hover/pressed/focus states, readable font hierarchy, and no overlapping text
[x] RTS UI plan: desktop resolution QA at 1280x720, 1600x900, 1920x1080, and high-DPI scaling

[x] Add simple enemy AI production
[x] Add enemy attack waves
[x] Add win condition: destroy enemy HQ
[x] Add loss condition: player HQ destroyed

[x] Add A* pathfinding
[x] Add obstacle and building occupancy grid
[x] Add path debug overlay

[x] Add main menu
[x] Add pause menu
[x] Add victory and defeat screens
[x] Add settings: fullscreen, resolution, audio
[x] Add Windows export preset

## Auto Iteration Detail Plan

[x] Polish main menu settings persistence and restore saved options on launch
[x] Add hotkey legend overlay for stance, groups, camera, build, and debug toggles
[x] Add tactical audio cues with procedural tones for selection, move, attack, alerts, production, and outcome
[x] Add command acknowledgement rings for move, attack, harvest, rally, and invalid targets
[x] Add fog-of-war prototype with minimap concealment and revealed terrain memory
[x] Fog-of-war polish: keep unexplored terrain pure black, explored terrain as memory shadow, current vision fully readable, enemy mobile units hidden outside live vision, and static/environment objects visible from explored memory.
[x] Fog-of-war polish: replace blocky cell presentation with smoother boundaries, finer sampling, cached masks, and future line-of-sight occlusion from terrain/buildings.
[x] Fog-of-war performance hotfix: use lightweight tactical rectangles instead of true mist, coarse sampling, throttled world redraw, throttled minimap snapshots, and low-frequency vision recomputation.
[x] Fog-of-war performance direction: prioritize RTS readability and stable frame rate over true fog visuals; future polish should use cached masks or shaders only after profiling.
[x] Fog-of-war standalone design: implement `docs/FogOfWarDesign.md` as a low-resolution visibility mask texture with GPU smoothing, not per-cell drawing.
[x] Fog-of-war mask first pass: world fog now renders one bilinear-scaled low-resolution alpha mask texture instead of drawing one CanvasItem rectangle per fog cell.
[x] Fog-of-war mask phase 1: replace per-frame `Snapshot()` rendering with reusable visible/explored buffers, stable `ImageTexture.Update`, and logical visibility queries.
[x] Fog-of-war mask phase 2: render world fog as one shader surface using red visible and green explored channels, with pure black unexplored, dark memory explored, and transparent visible.
[x] Fog-of-war mask phase 3: make minimap consume the same cached mask texture instead of looping over fog cells.
[x] Fog-of-war mask QA: add deterministic visibility rules and a performance smoke test for many vision sources with no fog snapshot allocation in normal gameplay.
[x] Fog-of-war mask QA first pass: add deterministic stats checks for 150x100 default mask size, many-source visibility updates, explored memory, and concealed-cell preservation.
[x] Fog-of-war visual smoothing: use feathered visible/explored mask strengths so fog edges are softer while gameplay visibility stays deterministic.
[x] Add enemy difficulty profiles for production pace, attack wave size, and aggression radius
[x] Add skirmish setup screen for starting resources, map seed, and enemy difficulty
[x] Add deterministic simulation smoke test for 5-minute AI/economy stability
[x] Add visual QA screenshot capture for main menu, pause, settings, outcome, and battle HUD
[x] Add export smoke script that builds C#, validates presets, and exports Windows when templates exist
[x] RTS HUD redesign: keep primary UI in right sidebar with minimap top, production second, tactical controls middle, unit details bottom
[x] RTS HUD redesign: compress bottom bar to status, control groups, and alerts only
[x] RTS HUD redesign: replace always-expanded right sidebar with low-obstruction dynamic HUD clusters, top-right radar, contextual bottom command ribbon, and collapsed 48px right rail
[x] RTS HUD redesign: add contextual right drawer behavior for building selection, build mode, mouse-near-edge, hotkey toggle, transient production feedback, and auto-collapse after inactivity
[x] RTS HUD controls: add icon-only move mode buttons for direct advance, attack advance, and ignore advance, with mouse switching and current-mode highlight
[x] RTS HUD controls: add persistent hotkeys for move command mode switching: F1 direct advance, F2 attack advance, F3 ignore advance.
[x] RTS HUD controls: add icon-only unit stance buttons for Hold, Aggressive, Return Guard, Passive Retaliate, and Ignore; sync mouse clicks and Z/X/C/V/B hotkeys.
[x] RTS HUD controls: hide the bottom 1-9 control group strip for now while keeping control-group hotkeys functional.
[x] Movement orders: direct advance, attack advance, and ignore advance
[x] Unit stance: Ignore, immune to auto-targeting and shared threat calls
[x] Hide selected unit path/command lines by default while keeping crisp selection and hover feedback
[x] Pathing polish: reduce moving-unit separation spin and clumped units rotating around each other
[x] RTS movement polish: assigned formation slots, spatial-hash local avoidance, slot deceleration, and holding state to prevent final jitter
[x] RTS movement polish: compact target-centered slot assignment, selected-unit command lines, slot markers, and transient group target pulse
[x] RTS movement polish: selected units draw dashed command lines to assigned target slots with zoom-stable dash spacing
[x] RTS movement polish: command visualization uses the player's virtual target point while hidden formation slots remain internal movement data
[x] RTS movement polish: replace visible final-slot snapping with soft arrival and invisible-only micro correction
[x] Input polish: fast right-click jitter keeps the current selection and issues a command instead of misfiring as right-drag box selection.
[x] Visual footprint plan: design land tracks, naval wake, and aircraft jet/contrail effects as procedural readability cues for unit class and movement domain.
[x] Advanced pathing plan: direct-line first movement, A* only as fallback, path smoothing, corridor following, local detour windows, and repath throttling
[x] Advanced pathing plan: implement path quality metrics for straightness, corner count, travel inflation, final compactness, and jitter after arrival
[x] Advanced pathing plan: debug overlay should distinguish raw A* cells, smoothed corridor, local avoidance vector, assigned slot, and player command point
[x] Combat positioning polish: manual group attacks assign range-aware attack slots and turn firing units into combat anchors that rear units avoid
[x] Combat positioning polish: unit attack targets continuously track moving units and retain last-known-position trail state for future fog-of-war loss handling
[x] Combat source polish: shared weapon mount interface for static building weapons, mobile turrets, fixed-forward weapons, and future special hooks
[x] i18n polish: route in-game HUD, battle alerts, selection details, queue summaries, and command preview labels through GameText
[x] Threat chain polish: shared threat should respect stance, manual orders, memory, and ignore mode

## Current Next Target

[x] Current target: implement the Entity Framework Lock skeleton first: `EntityKind`, `EntityId`, `OwnerId`, `EntitySpec`, `EntityInstance`, component-state containers, and command interfaces, without deleting the current working `UnitBattlefield` path.
[ ] Current target: use `docs/RTS99Design.md` as the whole-game 99 point RTS target before expanding movement, combat, AI, economy, construction, sandbox, or campaign systems.
[x] Current target: use `docs/EntityFrameworkArchitecture.md` as the current architecture lock document before changing unit/building/entity framework code.
[x] Current target: create a compatibility bridge from existing `UnitSpec` / `UnitDesign` into `EntitySpec`, proving dog/cat units can still spawn, render, move, attack, harvest, and appear in sandbox.
[x] Current target: define `ColorRole`, `EnvironmentTone`, and `EnvironmentResponse`, then update unit art TODO/code direction so ownership stickers are `ColorRole.Owner` layers rather than faction/player/relation color branches.
[x] Current target: write an architecture note or code comments documenting the hard boundary: Simulation owns truth; Godot View owns drawing/input projection only.
[x] Current target: add deterministic tests for the new entity skeleton and conversion path before migrating building production/combat.
[ ] Current target: add sandbox/debug acceptance hooks for 30-unit group move, 30-unit group attack, firing-anchor avoidance, owner-color mirror match, and environment readability tests.
[x] Add deterministic QA assertions for weapon hit rules, turret state transitions, terrain passability, localization fallback, and presentation descriptor completeness.
[x] Add export smoke script that builds C#, validates the Windows preset, and exports when local templates exist.
[x] Add visual QA screenshot capture for main menu, settings, battle HUD, pause menu, and outcome screens.
[x] Add deterministic 5-minute AI/economy simulation smoke test with production, wave, harvest, and state-invariant checks.
[x] Implement skirmish setup screen for starting resources, map seed, and enemy difficulty.
[x] Implement enemy difficulty profiles for production pace, attack wave size, and aggression radius.
[x] Design and implement advanced RTS pathing so units prefer straight routes, smooth away grid corners, avoid only locally, and repath sparingly around real blockers.
[x] Remove visible grid-like stopping snap by using soft slot arrival, no hard position assignment, and deterministic no-jitter holding tests.
[x] Harden RTS movement with extracted spatial-hash local avoidance math and deterministic nearby-only/holding-state tests.
[x] Optimize RTS movement and combat positioning with compact assigned slots, selected command visualization, and combat anchors.
[x] Check and fix selected-unit dashed target lines plus moving-unit attack target tracking with last-known trail memory.
[x] Shift selected-unit command visualization from internal formation slots to the player's virtual command point and add icon mode switching.
[x] Redesign RTS UI as a low-obstruction dynamic HUD with compact persistent clusters and contextual right drawer.
[x] Install local Tabler SVG icon subset with MIT attribution and route HUD IconGlyph rendering through it with procedural fallback.
[x] Implement in-game HUD/Battle/command-preview localization pass and repair zh-CN preload strings.
[x] Implement full UI localization pass for main menu, pause, settings, outcome, hotkeys, command statuses, production, rally, harvest, and alert prefixes.
[x] Implement command acknowledgement rings with distinct procedural world-space feedback for move, attack, harvest, rally, and invalid command targets.
[x] Implement fog-of-war prototype with grid visibility, revealed terrain memory, world overlay, and minimap concealment.
[x] Implement fog-of-war final pass with non-blocky visual masks, pure-black unexplored areas, explored static-object memory, and hidden mobile enemies outside active vision.
[x] Implement procedural unit visual descriptors with reusable shape layers, color roles, status glyphs, and descriptor-driven world rendering.
[x] Implement damage profile model with weight, movement-domain, and armor-tag multipliers.
[x] Implement shared combat source interface for static weapons, mobile turrets, fixed-forward weapons, and future special weapons.
[x] Implement RTS movement as assigned formation slots with spatial-hash local avoidance, slot priority, deceleration, and hold-state convergence.
[x] Harden RTS movement slot-priority steering so local avoidance fades near assigned slots and holding convergence resists jitter.
[x] Implement core taxonomy foundation: unit weight classes, movement domains, terrain passability layers, and explicit weapon range data.
[x] Implement combat weapon foundation: ammo definitions, hit rules, turret state model, and reusable weapon special hooks.
[x] Implement presentation foundation: shared unit display descriptor, localization resource layer, and icon-first right-sidebar UI pipeline.
[x] Implement combat prototype: right-click enemy to attack, projectile rendering, damage, and death.
[x] Add basic auto-targeting for idle combat units.
[x] Improve unit picking precision and target affordance.
[x] Add selection box polish and zoom stress tests.
[x] Fix right-drag box selection.
[x] Add unit engagement stance model.
[x] Add control groups: Ctrl+1-9 and 1-9.
[x] Add multi-unit formation movement.
[x] Add unit separation to reduce overlap.
[x] Add edge scrolling.
[x] Add beam rendering.
[x] Add building models.
[x] Add procedural building rendering.
[x] Add build placement preview.
[x] Add grid snapping and placement validation.
[x] Add building health and destruction.
[x] Add production queues.
[x] Add barracks and vehicle factory production.
[x] Add production command UI before rally points.
[x] Add shared threat propagation and ally engagement calls.
[x] Add rally points.
[x] Add resource inventory.
[x] Add resource fields.
[x] Add harvester gather-return-unload loop.
[x] Add refinery delivery logic.
[x] Add RTS-grade interface shell.
[x] RTS UI plan: fixed top resource/status bar, bottom selection panel, bottom-right command card, bottom-left minimap.
[x] RTS UI plan: selected unit/building info panel with portrait glyph, health, armor/range/speed, stance, carried resources, and production progress.
[x] RTS UI plan: command card with icon buttons, hotkeys, disabled states, costs, cooldown/progress overlays, and concise tooltips.
[x] RTS UI plan: production/build tabs for structures, infantry, vehicles, economy, queue inspection, cancel, and refund hooks.
[x] RTS UI plan: evaluate and migrate toward a right-side command sidebar for production, build, queues, and tactical commands.
[x] RTS UI plan: minimap with camera rectangle, unit/building/resource pips, attack alerts, and click-to-jump camera control.
[x] RTS UI plan: control group bar with group contents, selected group highlight, and quick recall feedback.
[x] RTS UI plan: alert strip for under attack, production complete, insufficient credits, idle harvester, and base power/building events.
[x] RTS UI plan: cursor and command preview modes for move, attack, rally, harvest, build placement, invalid placement, and target hover.
[x] RTS UI plan: scalable crisp vector theme with stable dimensions, hover/pressed/focus states, readable font hierarchy, and no overlapping text.
[x] RTS UI plan: desktop resolution QA at 1280x720, 1600x900, 1920x1080, and high-DPI scaling.
[x] Add path debug overlay.
[x] Add main menu.
[x] Add pause menu.
[x] Add victory and defeat screens.
[x] Add settings: fullscreen, resolution, audio.
[x] Add Windows export preset.
[x] Polish main menu settings persistence and restore saved options on launch.
[x] RTS HUD redesign: keep primary UI in right sidebar with minimap top, production second, tactical controls middle, unit details bottom.
[x] RTS HUD redesign: compress bottom bar to status, control groups, and alerts only.
[x] Movement orders: direct advance, attack advance, and ignore advance.
[x] Unit stance: Ignore, immune to auto-targeting and shared threat calls.
[x] Hide selected unit path/command lines by default while keeping crisp selection and hover feedback.
[x] Pathing polish: reduce moving-unit separation spin and clumped units rotating around each other.
[x] Threat chain polish: shared threat should respect stance, manual orders, memory, and ignore mode.

## M1 - EntityWorld Authority - Full Completed Detail (archived 2026-07-01)

> Archived from the living TODO to keep the plan scannable. This is the verbose
> slice-by-slice record of the (now ~96% done) M1 milestone. Two items remained
> open at archive time; they live in the active TODO M1 summary.

The single most important next step: the live game must render and play from the
deterministic core, then legacy runtimes get deleted.

[x] Sync `UnitInstance` spawns into `EntityWorld`; drive `UnitInstanceView` from
    `EntityProjection` behind a `UseEntityWorldUnits` flag (default on, opt out
    with `PROCEDURAL_RTS_USE_ENTITY_WORLD_UNITS=0`), comparing positions against
    the legacy path before flipping the flag.
    [x] UnitBattlefield UnitInstance EntityWorld projection mirror: spawned units
        retain an `EntityId`, movement/selection/health/facing sync into
        `EntityWorld`, and `UnitInstanceView` is wired to `EntityProjection`.
    [x] UnitBattlefield UnitInstance projection drift QA:
        `UnitBattlefield.UnitProjectionDrift()` compares legacy `UnitInstance`
        position/facing against EntityWorld projections so the future
        `UseEntityWorldUnits` flip has a deterministic drift check.
    [x] UnitInstanceView UseEntityWorldUnits authority flag:
        `BattleRoot` now gates `UnitInstanceView` projection reads behind
        `PROCEDURAL_RTS_USE_ENTITY_WORLD_UNITS=1`, defaulting off while keeping
        projection wiring and drift QA ready for the later flip.
    [x] UseEntityWorldUnits enabled headless smoke:
        `tools/VerifyAll` now runs a second Battle headless smoke with
        `PROCEDURAL_RTS_USE_ENTITY_WORLD_UNITS=1`, proving the projection-driven
        unit view path boots before the flag is flipped by default.
    [x] UnitInstanceView UseEntityWorldUnits default-on flip:
        live unit views now prefer EntityWorld `EntityProjection` by default,
        while `tools/VerifyAll` keeps a legacy opt-out Godot smoke with
        `PROCEDURAL_RTS_USE_ENTITY_WORLD_UNITS=0`.
[x] Route live input (select/move/attack/stop/stance) through `EntityCommandBuffer`
    instead of mutating `UnitBattlefield` directly.
    [x] UnitBattlefield selection EntityCommandBuffer bridge: single-click,
        double-click/same-design, box-select, explicit id recall, and clear
        selection enqueue `SetSelectionEntityCommand`, apply through
        `CommandSystem` over the EntityWorld mirror, then copy selection state
        back to legacy units.
    [x] UnitBattlefield selected move/attack EntityCommandBuffer bridge: selected
        move and attack commands enqueue `GroupMoveEntityCommand` /
        `GroupAttackEntityCommand`, apply through `CommandSystem` over the
        EntityWorld mirror, then copy command state back to legacy units.
    [x] UnitBattlefield selected stop/stance EntityCommandBuffer bridge: selected
        stop and stance commands enqueue `StopEntityCommand` /
        `SetStanceEntityCommand`, apply through `CommandSystem` over the
        EntityWorld mirror, then copy command state back to legacy units/HUD.
    [x] UnitBattlefield explicit attack-units EntityCommandBuffer bridge:
        public `CommandAttackUnits(...)` APIs used by AI wave controllers now
        submit `GroupAttackEntityCommand` against EntityWorld unit/building
        mirrors instead of directly mutating legacy `UnitInstance` attack state.
[x] Move harvester, production-completion spawns, and building targets onto the
    EntityWorld path; then delete `UnitBattlefield` behavior methods.
    [x] UnitBattlefield harvest ResourceNode EntityCommandBuffer bridge:
        `ResourceFieldModel` is mirrored as EntityWorld `ResourceNode` entities;
        selected harvesters enqueue `HarvestEntityCommand`, apply through
        `CommandSystem`, then copy harvester command state back to legacy units.
    [x] UnitBattlefield live harvester ResourceSystem bridge:
        live harvester gather/return/unload ticks advance through EntityWorld
        `ResourceSystem`; resource amounts, cargo, dock claims, and banked credits
        sync back to legacy UI/runtime state.
    [x] UnitBattlefield legacy harvester behavior cleanup:
        the old per-unit `UpdateHarvester*` gather/return/unload methods and
        hard-coded live harvest/unload rates were removed from `UnitBattlefield`;
        live harvesting now stays on the `ResourceSystem` bridge.
    [x] UnitBattlefield legacy harvest dock reservation cleanup:
        harvest commands no longer reserve legacy refinery docks immediately in
        `UnitBattlefield`; dock claims are left to EntityWorld `ResourceSystem`
        and synced back through `DockComponentState`.
    [x] UnitBattlefield production completion EntityWorld spawn bridge:
        producer queues advance through `ProductionSystem`; completed units are
        spawned in EntityWorld, adopted into legacy `UnitInstance` presentation
        records, and producer queues sync back from EntityWorld.
    [x] UnitBattlefield production enqueue/cancel EntityWorld command bridge:
        production orders submit `ProduceEntityCommand` /
        `CancelProductionEntityCommand` through `ProductionSystem`; credits and
        producer queues sync back from EntityWorld to legacy UI/runtime state.
    [x] UnitBattlefield rally point EntityWorld command bridge:
        producer rally updates submit `SetRallyPointEntityCommand` through
        `ProductionSystem`; rally points sync back from EntityWorld to legacy
        UI/runtime state before produced units consume the rally.
    [x] UnitBattlefield building target health/death EntityWorld bridge:
        building target damage mutates EntityWorld `HealthComponentState`, syncs
        health back to the legacy building target, and removes destroyed building
        entity mirrors with the legacy death/outcome event path preserved.
    [x] UnitBattlefield building turret CombatSystem bridge:
        armed building targets now step through EntityWorld-only
        `TurretCombatSystem`, syncing weapon target/cooldown/damage events back
        to legacy presentation fields while mobile unit combat remains on its
        existing migration path.
    [x] UnitBattlefield unit-vs-building CombatSystem bridge:
        mobile units attacking building/turret targets now step through
        EntityWorld-only `BuildingTargetCombatSystem`, syncing movement,
        weapon cooldown, building damage, and death events back to legacy
        presentation fields without double-stepping unit-vs-unit combat.
[x] Add `ResourceSystem` (harvest/dock-reservation/unload) and `ProductionSystem`
    (per-producer queue/progress/rally) as pure `ISimSystem`s; prove in SimReplay.
    Progress: EntityWorld now has pure `ResourceSystem` and `ProductionSystem`
    gates. `tools/SimReplay` proves deterministic `resource-loop` and
    `production-loop` scenarios, while `ReviewGate resourcesystem` and
    `ReviewGate productionsystem` lock the source contracts.
[x] UnitBattlefield unit runtime movement ECS bridge:
    live `UnitBattlefield.Update` now advances mobile unit movement and final
    unit separation through its EntityWorld `MovementSystem` and
    `SeparationSystem`, then syncs EntityWorld transform/movement state back to
    `UnitInstance` for compatibility views and events. Unit-vs-unit combat stays
    on the legacy path for this slice, and `FireAnchorRemaining` is preserved
    across bridge syncs so active firing units remain stable while movers route
    around them. The bridge also locks non-blocking resource nodes out of local
    avoidance and sends harvesters to collidable refinery dock approach points
    instead of building centers. `ReviewGate unitruntimeecsbridge` locks the
    boundary.
[ ] UnitSpec architecture phase 3 duplicate-data cleanup remains open:
    UnitSpec presentation entrypoint slice added
    `UnitPresentationCatalog.ForDesign(string designId)` and `ForSpec(UnitSpec)`
    returning `UnitSpecPresentationDescriptor` directly from UnitSpec metadata/art.
    The later Units dictionary deletion slice removed the old
    `UnitPresentationCatalog.Units` facade, so this bridge now stays UnitSpec-only
    for unit presentation metadata.
    UnitDesign faction roster bridge slice added
    `UnitDesignFactionRosterCatalog.For(UnitFactionId)` returning playable
    design ids from discovered `UnitDesign`/`UnitSpec` production metadata plus
    starting design ids validated through `UnitDesignCatalog.Spec`. Later
    cleanup slices moved starting units and starting buildings out of
    `FactionCatalog`; old `UnitKind` conversion remains only at compatibility
    edges.
    UnitDesign definition cleanup slice added `UnitSpecRuntimeDescriptor` and
    `UnitDesignDefinitionCatalog.RuntimeDescriptors`, projecting runtime stats,
    weapon, ammo, and role data directly from `UnitDesignCatalog`/`UnitSpec`.
    `CombatBehavior` now uses this UnitKind-free read path for runtime-definition
    QA, while legacy `UnitDefinition` compatibility is isolated behind an explicit
    shim. `ReviewGate unitdesigndefinitioncatalog` locks the boundary.
    Read-path cleanup slice 2 moved CombatBehavior tier/armor/domain QA from
    `GameState.UnitDefinitionValues` to `UnitDesignDefinitionCatalog`, and
    ReviewGate now prevents that legacy value enumeration from returning to
    CombatBehavior. Legacy runtime access remains for live compatibility.
    Read-path cleanup slice 3 added `UnitDesignDefinitionCatalog.WithRole(...)`;
    CombatBehavior harvester/economy QA now queries UnitSpec role tags instead of
    `GameState.UnitDefinitionEntries`, and ReviewGate prevents that legacy entry
    enumeration from returning to CombatBehavior.
    Read-path cleanup slice 4 added
    `UnitDesignDefinitionCatalog.CompatibilityDefinition(...)`; CombatBehavior
    footprint/readability QA now seeds its legacy-compatible `UnitDefinition`
    checks from `UnitSpecRuntimeDescriptor` by design id instead of directly
    reading `GameState.UnitDefinitionFor(...)`. Legacy runtime compatibility
    remains in place for live behavior.
    Read-path cleanup slice 5 moved CombatBehavior aircraft target-profile QA
    from direct `GameState.UnitDefinitionFor(UnitKind.CatScoutAircraft)` reads to
    `UnitDesignDefinitionCatalog.ForDesign("cat.scout_aircraft")` plus a
    descriptor-backed compatibility projection. ReviewGate prevents that direct
    aircraft definition read from returning.
    Production UI cleanup slice moved live `ProductionOptionState` / HUD command
    buttons onto `UnitSpec` display data (`UnitDesignId`, `ShortCode`, `Accent`)
    so the new runtime production surface no longer needs to round-trip through
    old `UnitKind` presentation entries. `ReviewGate unitspeccleanup` locks this
    read-path boundary; legacy `UnitCatalog` remains as a compatibility layer.
    UnitKind design bridge slice added `UnitKindDesignBridge` as an explicit
    old `UnitKind` -> `UnitDesignId` compatibility map for the Dog/Cat designs
    that already exist. Mapped `UnitCatalog` runtime definitions now project
    through `UnitDesignDefinitionCatalog.CompatibilityDefinition(...)`, while
    unmapped legacy units keep their old hand-authored compatibility definitions.
    `ReviewGate unitkinddesignbridge` locks the boundary.
    UnitKind presentation bridge slice moved mapped legacy `UnitCatalog`
    presentation metadata onto `UnitPresentationCatalog.ForDesign(...)` through
    `UnitKindDesignBridge.CompatibilityPresentation(...)`. Legacy
    `UnitVisualDescriptor` shapes remain explicit compatibility inputs until
    the old visual path is deleted. `ReviewGate unitkindpresentationbridge`
    locks the boundary.
    Dog/Cat UnitDesign coverage slice added authored UnitDesign files for every
    remaining Dog/Cat legacy `UnitKind`, expanded `UnitKindDesignBridge` to cover
    all Dog/Cat legacy units, and moved all Dog/Cat `UnitCatalog` runtime and
    presentation metadata through UnitDesign-backed compatibility projections.
    `ReviewGate unitdesigncoverage` locks the boundary. Generic legacy UnitKind
    slice added `GenericInfantry`, `GenericLightTank`, and `GenericHarvester`,
    mapped the old `Infantry` / `LightTank` / `Harvester` compatibility kinds
    through `UnitKindDesignBridge`, and moved their `UnitCatalog` runtime and
    presentation metadata through UnitDesign-backed compatibility projections.
    `ReviewGate genericlegacyunitkind` locks this final legacy-unit coverage
    bridge. Legacy `UnitKind`, `UnitCatalog`, and `UnitVisualDescriptor` still
    remain as compatibility layers until deletion.
    PathDebug UnitSpec bridge slice moved the live debug path-color read path off
    direct `GameState.UnitDefinitionFor(unit.Kind)` / legacy harvester
    special-casing. `PathDebugLayer` now resolves `UnitModel.Kind` through
    `UnitKindDesignBridge.TryGetSpec(...)`, reads accents from
    `UnitPresentationCatalog.ForSpec(...)`, and identifies economy/worker paths
    via UnitSpec role tags. `ReviewGate pathdebugunitspecbridge` locks this
    runtime presentation/debug cleanup while the broad legacy compatibility
    layers stay open.
    CombatEffects UnitSpec bridge slice moved threat-alert and hit-pulse unit
    VFX reads off `State.Definition(unit)`. `CombatEffectsLayer` now resolves
    draw-time unit radius/accent through
    `UnitKindDesignBridge.TryGetRuntimeDescriptor(...)`, which reads cached
    `UnitDesignDefinitionCatalog.RuntimeDescriptors` data instead of allocating
    legacy compatibility definitions in `_Draw()`. `ReviewGate
    combateffectsunitspecbridge` locks this read-path cleanup.
    UnitView UnitSpec bridge slice moved the legacy live unit view off
    `State.Definition(Unit)`, `PresentationCatalog.Unit(Unit.Kind, ...)`, and
    legacy harvester special-casing. `UnitView` now resolves `UnitModel.Kind`
    through `UnitKindDesignBridge.TryGetSpec(...)` /
    `TryGetRuntimeDescriptor(...)`, renders `UnitPresentationCatalog.ForSpec`
    art through `UnitVisualRenderer.DrawUnitArtRecipe(...)`, applies
    `EnvironmentTonePalette.For(State.VisualTheme)`, and reads cargo/status
    affordances from UnitSpec role tags. `ReviewGate unitviewunitspecbridge`
    locks this legacy-view rendering cleanup.
    SelectionController UnitSpec bridge slice moved old-runtime unit command-line
    and hover feedback off `State.Definition(unit)` / `State.Definition(hoveredUnit)`.
    Legacy selected-harvester semantics now resolve `UnitModel.Kind` through
    `UnitKindDesignBridge.TryGetSpec(...)` and UnitSpec role/ability metadata;
    `ReviewGate selectionunitspecbridge` locks this scoped controller read-path cleanup.
    FootprintLayer UnitSpec bridge slice moved moving footprint style/visibility
    off `State.Definition(unit)` and `FactionCatalog.DefaultFactionForOwner`.
    Footprint marks now resolve `UnitModel.Kind` through
    `UnitKindDesignBridge.TryGetSpec(...)` / `TryGetRuntimeDescriptor(...)`,
    use UnitSpec role/art metadata for resource-worker tinting, and use owner
    relations for fog visibility. `ReviewGate footprintunitspecbridge` locks
    this scoped presentation read-path cleanup.
    [x] FootprintVisualMath UnitSpec descriptor cleanup:
        Move `FootprintVisualMath.StyleFor(...)` off legacy-compatible
        `UnitDefinition` inputs and onto `UnitSpecRuntimeDescriptor` directly.
        CombatBehavior footprint QA should pass UnitSpec runtime descriptors
        without constructing intermediate compatibility definitions. `ReviewGate
        footprintvisualmathdescriptor` locks this direct descriptor path; full
        `VerifyAll` passed 23/23 after the slice.
    BattleRoot UnitSpec read-path slice moved legacy unit death VFX and selected
    unit summaries off direct `GameState.UnitDefinitionFor(death.Kind)`,
    `PresentationCatalog.Unit(...)`, and selected-summary harvester kind checks.
    `ReviewGate battlerootunitspecreadpath` locks this scoped BattleRoot
    presentation/read-path cleanup while broader legacy compatibility remains open.
    ControlGroupController UnitSpec bridge slice moved old-runtime control-group
    infantry, vehicle, and harvest/economy counters off legacy `UnitKind` buckets.
    Legacy `UnitModel.Kind` now resolves through `UnitKindDesignBridge.TryGetSpec(...)`,
    while live `UnitInstance` counters use UnitSpec role tags and authored harvest
    ability metadata. `ReviewGate controlgroupunitspecbridge` locks this HUD
    snapshot read-path cleanup.
    BattleRoot idle-harvest UnitSpec slice moved the old-runtime idle harvester
    alert off `UnitKind.Harvester`. It now resolves `UnitModel.Kind` through
    `UnitKindDesignBridge.TryGetSpec(...)` and checks economy/worker role tags plus
    authored harvest ability metadata. `ReviewGate battlerootidleharvestunitspec`
    locks this alert read-path cleanup.
    UnitBattlefield production status UnitSpec cleanup moved the live
    missing-producer status path off legacy production definitions. The runtime
    now resolves the requested design id first, formats unit labels from
    `UnitSpecRuntimeDescriptor` / `UnitSpec`, and formats producer labels from the
    `UnitSpec.Production.ProducerKind` `BuildSpec`. `ReviewGate
    unitbattlefieldproductionspecstatus` locks this scoped status read path.
    BattleRoot unit culling UnitSpec cleanup moved the old-runtime unit view
    culling radius in `RefreshViewCulling()` off `_state.Definition(unit)`.
    Legacy `UnitModel.Kind` now resolves through the existing BattleRoot
    UnitSpec read path and uses `UnitSpecRuntimeDescriptor.Radius`. `ReviewGate
    battlerootunitcullingunitspec` locks this scoped view-culling read path.
    SimulationSmoke UnitSpec read-path cleanup moved its world-bound unit radius
    assertion off `state.Definition(unit)`. The smoke now resolves legacy
    `UnitModel.Kind` through `UnitKindDesignBridge.TryGetRuntimeDescriptor(...)`
    and uses `UnitSpecRuntimeDescriptor.Radius` for tool-side validation.
    `ReviewGate simulationsmokeunitspecreadpath` locks this scoped tool read path;
    full `VerifyAll` passed 23/23 after the slice.
    FogOfWarQa UnitSpec read-path cleanup moved unit fixture HP off
    `GameState.UnitDefinitionFor(kind)`. The fog QA now resolves fixture
    `UnitKind` values through `UnitKindDesignBridge.TryGetRuntimeDescriptor(...)`
    and uses `UnitSpecRuntimeDescriptor.MaxHp`. `ReviewGate
    fogofwarqaunitspecreadpath` locks this scoped tool read path; full
    `VerifyAll` passed 23/23 after the slice.
    CombatBehavior Unit fixture UnitSpec read-path cleanup moved the tool
    `Unit(...)` fixture HP and combat-anchor radius assertions off direct
    `GameState.UnitDefinitionFor(...)` reads. They now resolve legacy
    `UnitKind` values through `UnitKindDesignBridge.TryGetRuntimeDescriptor(...)`
    and use `UnitSpecRuntimeDescriptor.MaxHp` / `Radius`. `ReviewGate
    combatbehaviorunitfixturereadpath` locks this scoped tool read path; full
    `VerifyAll` passed 23/23 after the slice.
    CombatBehavior remaining UnitSpec read-path cleanup removed the last direct
    `GameState.UnitDefinitionFor(...)` reads from `tools/CombatBehavior`. Default
    tank/infantry/harvester metadata, faction tier QA, generic compatibility QA,
    and entity-attacked label checks now use `RuntimeDescriptorFor(...)` or
    explicit descriptor-backed compatibility projections. `ReviewGate
    combatbehaviorunitspecreadpath` locks the tool against direct legacy unit
    definition reads returning; full `VerifyAll` passed 23/23 after the slice.
    CombatBehavior HasUnitDefinition cleanup moved faction roster and
    presentation coverage checks off `GameState.HasUnitDefinition(...)`. Those
    checks now prove coverage through
    `UnitKindDesignBridge.TryGetRuntimeDescriptor(...)`, keeping tool-side
    content validation on the UnitSpec bridge. `ReviewGate
    combatbehaviorhasunitdefinitioncleanup` locks this read-path cleanup; full
    `VerifyAll` passed 23/23 after the slice.
    CombatBehavior roster UnitSpec QA cleanup moved the early Dog/Cat playable
    roster assertions off handwritten `requiredDogUnits` / `requiredCatUnits`
    legacy `UnitKind` arrays. The tool now compares
    `expectedDogPlayableDesignIds` / `expectedCatPlayableDesignIds` against
    `UnitDesignFactionRosterCatalog` playable design ids, validates runtime and
    presentation coverage by design id, and converts through
    `UnitKindDesignBridge` only inside legacy bridge/sandbox compatibility
    coverage. `ReviewGate combatbehaviorrosterunitspecqa` locks this scoped QA
    cleanup.
    GameState Definition accessor cleanup removed the old
    `GameState.Definition(UnitModel)` and `GameState.Definition(BuildingModel)`
    instance accessors after external callers moved to UnitSpec/BuildSpec read
    paths. The last CombatBehavior occupancy QA caller now reads HQ footprint
    data from `BuildSpecCatalog.For(...)`. `ReviewGate
    gamestatedefinitionaccessorcleanup` locks those public accessors from
    returning; full `VerifyAll` passed 23/23 after the slice.
    CombatBehavior UnitSpec presentation QA cleanup moved shared unit and
    production presentation completeness checks off legacy
    `UnitPresentationCatalog.Units` / `Production` dictionary enumeration.
    The tool now validates bridged and playable unit presentation data through
    `UnitPresentationCatalog.ForDesign(...)` / `ForSpec(...)`, checks owner-color
    art zones and UnitArtRecipe layers, and resolves production presentation via
    `UnitDesignRuntimeLoadouts.ProductionDesignId(...)` plus
    `ForProductionSpec(...)`. `ReviewGate combatbehaviorunitspecpresentationqa`
    locks this scoped QA read path; full `VerifyAll` passed 23/23 after the slice.
    CombatBehavior ProductionSpec read-path cleanup moved remaining production
    QA off `GameState.ProductionDefinitions`. The tool now resolves legacy
    production buttons through `ProductionDesignSpecFor(...)`, reads costs,
    lane metadata, producer kinds, and presentation metadata from UnitSpec /
    ProductionSpec data, and keeps `GameState.ProductionDefinitions` isolated to
    runtime compatibility paths. `ReviewGate combatbehaviorproductionspecreadpath`
    locks this scoped tool cleanup; full `VerifyAll` passed 23/23 after the slice.
    SimulationSmoke ProductionSpec read-path cleanup moved production queue
    validation off `GameState.ProductionDefinitions`. The smoke now resolves
    queued `ProductionKind` + faction pairs through `UnitDesignRuntimeLoadouts`,
    validates `ProductionSpec` lane metadata, and bounds legacy queue progress
    against the UnitSpec production lane duration. `ReviewGate
    simulationsmokeproductionspecreadpath` locks this scoped tool cleanup; full
    `VerifyAll` passed 23/23 after the slice.
    ProductionKindDesignBridge cleanup centralized duplicated legacy
    `ProductionKind` compatibility mapping into
    `scripts/core/units/ProductionKindDesignBridge.cs`. `ProductionSystem`,
    `UnitBattlefield`, `CombatBehavior`, and `SimulationSmoke` now share the
    same UnitSpec-backed resolver for legacy production kinds, playable
    production specs, old-runtime legacy INF/TNK/HAR compatibility specs, and
    queue duration bounds. `ReviewGate productionkinddesignbridge` locks this
    shared bridge and prevents the old local helper mappings from returning to
    tools/systems.
    EnemyProductionAi ProductionSpec read-path cleanup moved legacy enemy
    production choice checks off `GameState.ProductionDefinitions`. The old
    runtime AI now resolves legacy `ProductionKind` choices through
    `ProductionKindDesignBridge.LegacySpecFor(...)`, reads old-compatible cost
    and producer metadata from `UnitSpec`/`ProductionSpec`, and identifies
    rally-capable production buildings from legacy UnitSpec specs. `ReviewGate
    enemyproductionaiunitspecproduction` locks this scoped legacy-AI read path.
    GameState ProductionOptionStates UnitSpec cleanup moved old-runtime
    command-card option metadata off `GameState.ProductionDefinitions`.
    `ProductionOptionStates(...)` now enumerates legacy `ProductionKind` values
    through `ProductionKindDesignBridge.LegacyProductionSpecs()`, reads
    old-compatible cost from `UnitSpec.Stats`, producer/duration/lane metadata
    from `ProductionSpec`, and presentation from
    `UnitPresentationCatalog.ForProductionSpec(...)`. The
    old command click protocol still keeps `UnitDesignId` empty until the legacy
    `ProductionKind` enqueue path is migrated. `ReviewGate
    gamestateproductionoptionsunitspec` locks this scoped GameState read path.
    GameState ProductionLaneSnapshots UnitSpec cleanup moved old-runtime
    per-building production queue snapshots off `GameState.ProductionDefinitions`.
    Queue items now resolve legacy `ProductionKind` through
    `ProductionKindDesignBridge.LegacySpecFor(...)`, read old-compatible cost and
    duration from `UnitSpec`/`ProductionSpec`, derive legacy output-unit compatibility from
    `UnitPresentationCatalog.ForProductionSpec(...)`, and keep the old 50%
    refund ratio explicit as `ProductionRefundRatio`. `ReviewGate
    gamestateproductionlanesunitspec` locks this scoped queue snapshot path.
    GameState production runtime UnitSpec cleanup moved old-runtime
    `EnqueueProduction(...)`, `CancelFirstProduction(...)`,
    `UpdateProductionQueues(...)`, and `SpawnProducedUnit(...)` off
    `GameState.ProductionDefinitions`. The legacy `ProductionKind` queue shape
    remains, but requested labels, producer eligibility, costs, refunds, queue
    duration, and completed output-unit compatibility now resolve through
    `ProductionKindDesignBridge.LegacySpecFor(...)`,
    `UnitSpec`/`ProductionSpec`, and `UnitPresentationCatalog.ForProductionSpec(...)`.
    `ReviewGate
    gamestateproductionruntimeunitspec` locks these runtime methods while the
    static legacy table remains only for external compatibility callers.
    External production definitions cleanup moved `BattleRoot`, `HudLayer`, and
    `BuildingView` display/readability paths off `GameState.ProductionDefinitions`.
    Old-runtime labels, tooltips, icons, production bars, and queue detail text
    now resolve through `ProductionKindDesignBridge.LegacySpecFor(...)` and
    `UnitPresentationCatalog.ForProductionSpec(...)`; UnitBattlefield queue
    details continue to use the queued `UnitDesignId`. `ReviewGate
    externalproductiondefinitionscleanup` locks these external display paths.
    ProductionDefinitions deletion cleanup removed the unused
    `GameState.ProductionDefinitions` table and deleted
    `scripts/core/ProductionDefinition.cs`. Old-runtime INF/TNK/HAR
    compatibility production metadata now lives only in
    `ProductionKindDesignBridge.LegacySpecFor(...)` /
    `LegacyProductionSpecs()`, built from generic UnitSpecs plus explicit legacy
    ProductionSpecs. `ReviewGate productiondefinitionsdeleted` locks the final
    deletion and scans `scripts/**/*.cs` for the removed symbols.
    GameState UnitSpec spatial cleanup moved legacy live unit geometry/vision
    runtime reads off `Definition(unit).Radius`, `.SightRange`,
    `.MovementDomain`, `.Speed`, `.TurnRate`, and `.AttackRange` for selection
    overlap, group move formation data, attack-slot geometry, fog sources,
    picking, dynamic obstacles, movement stepping, path assignment, local
    avoidance, produced-unit spawn geometry, harvester gather distance, death
    VFX payloads, and unit combat-source geometry. `ReviewGate
    gamestateunitspecspatial` locks this scoped `GameState` read-path cleanup;
    full `VerifyAll` passed 23/23 after the slice.
    GameState UnitSpec combat metadata cleanup moved live unit weapon lookup,
    spawn HP, target legality, target priority, damage multipliers, ballistic
    weight, passive-retaliate range, and under-attack labels off legacy
    `UnitDefinition` reads and onto `UnitSpecRuntimeDescriptor`. `WeaponTargetProfile`
    now accepts descriptor-backed unit target metadata directly. The later
    descriptor-only and UnitDefinition deletion slices remove the remaining
    compatibility overloads/projections. `ReviewGate
    gamestateunitspeccombatmetadata` locks this scoped combat metadata read path;
    full `VerifyAll` passed 23/23 after the slice.
    GameState UnitDefinition public read-surface deletion removed
    `GameState.UnitDefinitionFor(...)`, `HasUnitDefinition(...)`,
    `UnitDefinitionValues`, `UnitDefinitionEntries`, and the private
    `LegacyUnitDefinitions` shim. Legacy `GameState` runtime reads now stay
    behind `UnitRuntimeDescriptorFor(...)` / `UnitKindDesignBridge`, while
    tool fixtures continue proving coverage through UnitSpec descriptors.
    `ReviewGate gamestateunitdefinitionpublicdeleted` locks this public surface
    from returning.
    [x] Descriptor-only combat metadata cleanup removed the remaining
        `UnitDefinition` unit-target overloads from `WeaponTargetProfile` and
        the public `GameState` combat metadata helpers. CombatBehavior damage,
        target legality, and target priority QA now pass
        `UnitSpecRuntimeDescriptor` directly instead of descriptor-backed
        compatibility definitions. `ReviewGate descriptorcombatmetadata` locks
        this narrower combat metadata API surface; full `VerifyAll` passed 23/23
        after the slice.
    [x] UnitDefinition compatibility deletion:
        Delete `scripts/core/UnitDefinition.cs`,
        `UnitDesignDefinitionCatalog.CompatibilityDefinition(...)`, and
        `UnitKindDesignBridge.CompatibilityDefinition(...)`; CombatBehavior
        should validate legacy `UnitKind` mapping through direct
        `UnitSpecRuntimeDescriptor` / `UnitSpecPresentationDescriptor` reads
        instead of compatibility projections. `ReviewGate unitdefinitiondeleted`
        locks the deleted type and helper surface; full `VerifyAll` passed 23/23
        after the slice.
    FactionCatalog UnitSpec availability cleanup moved legacy faction
    `AvailableUnits` off `UnitCatalog.UnitsForFaction(...)`. The catalog now
    projects available `UnitKind` values from `UnitKindDesignBridge.DesignIds`
    and filters through `UnitDesignCatalog.Spec(...).Faction`, keeping old
    `FactionDefinition.AvailableUnits` compatible while removing a UnitCatalog
    consumer. `ReviewGate factioncatalogunitspecavailability` locks this bridge.
    [x] FactionCatalog AvailableUnits deletion:
        Deleted the duplicate public `FactionDefinition.AvailableUnits` /
        `FactionCatalog.UnitKindsForFaction(...)` surface. Unit availability
        checks now read `UnitDesignFactionRosterCatalog` playable design ids
        and only convert to legacy `UnitKind` inside compatibility tests or old
        runtime edges. Gates passed: `CombatBehavior`, `ReviewGate
        factioncatalogunitspecavailability`, full `ReviewGate`, and
        review-record gate. Grouped `VerifyAll` passed 23/23 after the
        availability cleanup batch.
    [x] FactionCatalog building/production availability deletion:
        Deleted duplicate public `FactionDefinition.AvailableBuildings` and
        `AvailableProduction` surfaces. Building availability checks should read
        `BuildSpecCatalog`, while production availability should stay in
        UnitDesign production metadata / `ProductionKindDesignBridge` instead of
        mirrored faction tables. Gates passed: `CombatBehavior`, `ReviewGate
        factioncatalogbuildproductionavailabilitydeleted`, full `ReviewGate`,
        review-record gate, and grouped `VerifyAll` 23/23.
    [x] FactionCatalog default AI difficulty deletion:
        Deleted unused `FactionDefinition.DefaultAiDifficulty` and the mirrored
        `EnemyDifficulty.Normal` values in `FactionCatalog`; skirmish AI
        difficulty should stay in `SkirmishOptions` / `MatchConfig`, not faction
        display metadata. Gates passed: local build, `ReviewGate
        factioncatalogdefaultaidifficultydeleted`, full `ReviewGate`, and
        review-record gate.
    [x] Faction start building loadout cleanup:
        Deleted duplicate `FactionDefinition.StartingBuildings` /
        `FactionCatalog` starting-building constructor data. `MatchStartLoadouts`
        now owns start-building lists alongside start positions, while
        `FactionCatalog` remains display metadata only. Gates passed: local
        build, `CombatBehavior`, `ReviewGate startloadout`,
        `ReviewGate factionstartbuildingsdeleted`, full `ReviewGate`, and
        review-record gate all passed. Later grouped `VerifyAll` passed 23/23
        after the default-owner and sandbox-roster cleanup batch.
    [x] FactionCatalog default owner mapping deletion:
        Deleted the remaining `FactionCatalog.DefaultFactionForOwner(...)`
        gameplay helper. Default owner-to-faction fixture assumptions now live
        at test/compatibility edges, not in faction display metadata. Gates
        passed: local build, `CombatBehavior`, `ReviewGate
        factioncatalogdefaultownerdeleted`, full `ReviewGate`, and review-record
        gate; grouped `VerifyAll` passed 23/23 after the sandbox-roster cleanup
        batch.
    DynamicUnitIcon UnitSpec icon cleanup moved the legacy `DrawUnitIcon(...)`
    compatibility entrypoint off `UnitPresentationCatalog.For(kind)` and old
    `UnitVisualRenderer.DrawUnitSilhouette(...)`. Mapped `UnitKind` icon callers
    now resolve through `UnitKindDesignBridge.TryGetSpec(...)` and delegate to
    `DrawUnitDesignIcon(...)`, while unmapped/null calls use the explicit fallback
    glyph path. `ReviewGate dynamicuniticonunitspec` locks this UI read-path slice.
    PresentationCatalog UnitSpec unit cleanup moved the shared
    `PresentationCatalog.Unit(...)` compatibility descriptor off
    `UnitPresentationCatalog.For(kind)`. It now resolves mapped legacy
    `UnitKind` values through `UnitKindDesignBridge.TryGetSpec(...)`, reads
    metadata from `UnitPresentationCatalog.ForSpec(...)`, preserves relation/owner
    color policy, and no longer exposes legacy `UnitVisualDescriptor` data from
    that path. `ReviewGate presentationcatalogunitunitspec` locks the boundary.
    CombatBehavior presentation compatibility UnitSpec cleanup moved the remaining
    tool-side `UnitPresentationCatalog.For(kind)` QA checks onto
    `UnitKindDesignBridge.DesignId(...)` plus `UnitPresentationCatalog.ForDesign(...)`.
    The tool still validates mapped and generic legacy UnitKind coverage, but no
    longer proves that path by actively reading legacy UnitKind presentation
    descriptors. `ReviewGate combatbehaviorpresentationcompatunitspec` locks the
    test read path.
    UnitPresentationCatalog UnitKind entrypoint deletion removed the now-unused
    public `UnitPresentationCatalog.For(UnitKind kind)` compatibility method after
    live UI, shared presentation descriptors, and CombatBehavior QA moved to
    `ForDesign(...)` / `ForSpec(...)`. `ReviewGate unitpresentationforunitkinddeleted`
    locks the deletion while broader `Units` / production compatibility remains.
    UnitPresentationCatalog Units dictionary deletion removed the
    `UnitPresentationCatalog.Units` facade over `UnitCatalog.Presentations`.
    Legacy production presentation defaults now derive short code, icon, accent,
    and role glyph through `UnitKindDesignBridge.DesignId(...)` plus
    `ForDesign(...)`, leaving production compatibility intact while removing the
    unit-presentation dictionary surface. `ReviewGate unitpresentationunitsdeleted`
    locks this deletion.
    UnitCatalog public surface deletion removed the unused public
    `UnitCatalog.Definitions`, `UnitCatalog.Presentations`, and
    `UnitCatalog.UnitsForFaction(...)` members. The file still keeps temporary
    private compatibility entries for later whole-catalog deletion, but active
    code can no longer read those duplicated public dictionaries. `ReviewGate
    unitcatalogpublicsurfacedeleted` locks this surface cleanup.
    UnitCatalog legacy deletion removed the remaining unused compatibility files
    `UnitCatalog.cs`, `UnitCatalogEntry.cs`, `UnitPresentationDescriptor.cs`,
    `UnitVisualDescriptor.cs`, and `UnitTurretVisualKind.cs`. `EntityPresentationDescriptor`
    no longer carries a legacy `UnitVisual` payload, `UnitKindDesignBridge` keeps
    only UnitKind -> UnitDesign/UnitSpec descriptor lookups after the later
    UnitDefinition deletion slice, and
    `UnitVisualRenderer` now exposes only UnitSpec `UnitArtRecipe` rendering.
    Historical coverage gates now validate UnitDesign/UnitSpec evidence instead
    of duplicated UnitCatalog entries. `ReviewGate unitcataloglegacydeleted` locks
    this deletion; full `VerifyAll` passed 23/23 after the slice.
    [x] GameState sandbox UnitDesign roster cleanup:
        Moved `GameState` developer sandbox faction test rows off handwritten
        Dog/Cat `UnitKind` lists and onto `UnitDesignFactionRosterCatalog`
        playable design ids, converting through `UnitKindDesignBridge` only at
        the legacy spawn edge and skipping not-yet-bridged designs. Gates
        passed: local build, `CombatBehavior`, `ReviewGate
        gamestatesandboxrosterunitspec`, full `ReviewGate`, and review-record
        gate; grouped `VerifyAll` passed 23/23 after this cleanup batch.
    [x] GameState harvester UnitSpec role cleanup:
        Moved `GameState.IsHarvesterUnit(UnitKind)` off the handwritten
        `UnitKind.Harvester` / Dog / Cat list and onto UnitSpec economy role plus
        authored harvest ability metadata, leaving `UnitKind` only as the legacy
        bridge key. Gates passed: local build, `CombatBehavior`, `ReviewGate
        gamestateharvesterunitspec`, full `ReviewGate`, and review-record gate.
[x] Migration cleanup: merge `BuildingDefinition`+`BuildDefinition` into one build/
    entity spec with `Construction`/`Power`/`ProductionQueue` components; remove the
    `UnitBattlefieldBuildingTarget` second building runtime.
    [x] UnitBattlefield BuildSpec entity bridge:
        `BuildSpec` and `BuildSpecCatalog` merge legacy `BuildingDefinition` plus
        `BuildDefinition`; `BuildingTargetEntityBridge` and live
        `UnitBattlefield` building entity sync now generate specs/components from
        the unified bridge while old call sites remain compatible during migration.
    [x] BuildSpec EntitySpec identity cleanup:
        `BuildSpec` owns the output `EntitySpecId` explicitly, and
        `BuildingTargetEntityBridge.ToEntitySpec(...)` plus sandbox spawn
        authoring read that id instead of generating parallel `building.*`
        strings from `BuildingKind`. `ReviewGate buildspecentityspecidentity`
        locks the BuildSpec-owned identity path; full `VerifyAll` passed 23/23
        after the slice.
    [x] BattleRoot BuildSpec building upsert bridge:
        live `BattleRoot` building mirroring now calls a `UnitBattlefield`
        BuildSpec-derived upsert overload, so the true game path no longer
        manually forwards `BuildingDefinition` shape/combat fields into the
        secondary building runtime.
    [x] BuildingView EntityProjection bridge:
        `UnitBattlefield` exposes building `EntityProjection` snapshots and
        `BuildingView` now prefers projection position/facing/health data from
        the EntityWorld mirror while keeping `BuildingModel` as a fallback.
    [x] BuildingView building presentation projection bridge:
        `BuildingPresentationProjection` exposes footprint, production queue,
        rally point, powered state, and construction progress from EntityWorld
        components, and `BuildingView` prefers that snapshot while keeping
        legacy `BuildingModel` fields as migration fallbacks.
    [x] BuildingView building selection projection bridge:
        `BattleRoot` syncs legacy building selection into EntityWorld
        `SelectableComponentState`, and `BuildingView` now prefers projected
        selected state for its selection overlay while keeping `BuildingModel`
        as a fallback until selection commands move fully into EntityWorld.
    [x] UnitBattlefield selected building rally input bridge:
        live building rally input now routes through `UnitBattlefield`
        selected building targets and EntityWorld `SetRallyPointEntityCommand`
        when the UnitDesign runtime is enabled, then syncs rally point/pulse
        back to legacy building UI state during migration.
    [x] UnitBattlefield selected building rally projection bridge:
        selected building rally lines now read `BuildingRallyProjection`
        snapshots assembled from EntityWorld building presentation projections
        when the UnitDesign runtime is enabled, leaving legacy
        `State.SelectedBuildings()` rally drawing only for the old runtime path.
    [x] UnitBattlefield selected building command-preview bridge:
        command-preview and HUD context selected-building checks now query
        `UnitBattlefield`/EntityWorld building projections when the UnitDesign
        runtime is enabled, after mirroring legacy selection into the building
        target bridge for migration.
    [x] UnitBattlefield selected building HUD selection bridge:
        `RefreshSelectionInfo` now renders selected building HUD details and
        multi-building summaries from `BuildingSelectionProjection` snapshots
        assembled by `UnitBattlefield`/EntityWorld when the UnitDesign runtime
        is enabled.
    [x] UnitBattlefield building click selection input bridge:
        live building click selection now routes through `UnitBattlefield`
        picking and EntityWorld `SetSelectionEntityCommand`, then mirrors
        selected state back to legacy `BuildingModel` only as a UI fallback.
    [x] UnitBattlefield building hover projection bridge:
        live building hover picking, hover affordance, and building-hover command
        preview now read `UnitBattlefield`/EntityWorld `BuildingHoverProjection`
        snapshots when the UnitDesign runtime is enabled.
    [x] UnitBattlefield building hit-pulse projection bridge:
        live building hit-pulse VFX now reads `UnitBattlefield`/EntityWorld
        `BuildingHitPulseProjection` snapshots in `CombatEffectsLayer`, leaving
        legacy `GameState.Buildings` pulses only as the old-runtime fallback.
    [x] UnitBattlefield building minimap projection bridge:
        live building minimap pips now read `UnitBattlefield`/EntityWorld
        `BuildingMinimapProjection` snapshots, with enemy buildings filtered by
        explored fog memory and legacy `State.Buildings` pips kept only for the
        old-runtime fallback.
    [x] UnitBattlefield building power-alert projection bridge:
        live power alerts now read `UnitBattlefield`/EntityWorld
        `UnitBattlefieldPowerStatusProjection` owner power snapshots, summing
        active `PowerComponentState` providers/consumers and leaving the
        `State.Buildings` PowerPlant check only as the old-runtime fallback.
    [x] BuildingView culling projection bridge:
        live building view culling now reads `UnitBattlefield`/EntityWorld
        `BuildingPresentationProjection` position, facing, health, and footprint
        data in `BattleRoot.RefreshViewCulling`, leaving `BuildingModel` culling
        only as the old-runtime fallback.
    [x] UnitBattlefield building combat-alert projection bridge:
        live building under-attack and destroyed alerts now read
        `UnitBattlefieldBuildingTarget` / `UnitBattlefieldBuildingDeathInfo`
        position, owner slot, and `BuildSpecCatalog` labels, leaving
        `BuildingModel` only for temporary fallback health mirroring.
    [x] BuildingView factory projection bridge:
        live and added building views now share one `CreateBuildingView` path
        that wires `UnitBattlefield`/EntityWorld projection providers, and
        building-online alerts use `BuildSpecCatalog` labels instead of
        `GameState.Definition(building)`.
    [x] BuildingView dock projection bridge:
        refinery dock occupancy, delivery pulse, and selected-building rally
        pulse visuals now prefer `BuildingPresentationProjection` data sourced
        from EntityWorld `DockComponentState` / `PresentationPulseComponentState`,
        leaving `BuildingModel` fields only as old-runtime fallbacks.
    [x] BuildingView exploration projection bridge:
        live building draw visibility now computes explored-memory checks from
        `BuildingPresentationProjection` owner, position, and footprint data,
        and the later explored-provider cleanup removed the old
        `State.IsExploredByPlayer(Building)` fallback from `BuildingView`.
    [x] BuildingView identity projection bridge:
        live building art kind, owner color, and faction identity now prefer
        `BuildingViewProjection` data from `UnitBattlefield`, leaving
        `BuildingModel.Kind` / owner / faction only as old-runtime fallbacks.
    [x] BuildingView BuildSpec fallback bridge:
        `BuildingView` no longer reads `GameState.Definition(Building)` for
        footprint or max-HP fallbacks; projected identity chooses
        `BuildSpecCatalog.For(kind)` instead.
    [x] BuildingView viewer-faction fallback cleanup:
        `BuildingView` no longer infers the viewer faction through
        `FactionCatalog.DefaultFactionForOwner`; `BattleRoot` injects the selected
        player faction, and the no-viewer fallback uses non-relation BuildSpec/
        faction accent data.
    [x] BuildingView BuildSpec presentation cleanup:
        `BuildingView` no longer reads `PresentationCatalog.Building(...)` for
        body or relation colors; it derives body accent from `BuildSpec` plus
        faction visual policy and keeps relation colors in overlay-only policy.
    [x] BuildingView explored-provider bridge:
        `BuildingView` draw visibility now reads explored memory through an
        injected provider from `BattleRoot`, keeping direct `GameState.FogOfWar`
        only as an old-runtime fallback and no longer calling
        `State.IsExploredByPlayer(Building)`.
    [x] BuildPlacementController BuildSpec preview cleanup:
        build placement preview/status text now derives label, accent, and
        footprint from `BuildSpecCatalog` instead of
        `GameState.BuildingDefinitions`.
    [x] UnitBattlefield production-complete BuildSpec label bridge:
        live production-complete status text now derives the producer building
        label from `BuildSpecCatalog.For(building.Kind).Label` and removes the
        hard-coded `BattleRoot.BuildingLabel` helper.
    [x] UnitBattlefield BuildSpec status label bridge:
        live rally, production queued, and missing-producer status text now
        derive producer building labels from `BuildSpecCatalog`, removing the
        hard-coded `UnitBattlefield.BuildingLabel` helper.
    [x] BattleRoot BuildSpec fallback cleanup:
        remaining `BattleRoot` building fallback reads for labels, minimap
        footprint, culling footprint, selected-building max HP, and building HUD
        sight range now derive from `BuildSpecCatalog` instead of
        `_state.Definition(building)`.
    [x] BattleRoot BuildSpec HUD cleanup:
        single-building HUD titles/glyphs/accents and multi-selection building
        icon summaries now derive from `BuildSpecCatalog` plus owner visual policy
        instead of `PresentationCatalog.Building(...)`.
    [x] SelectionController BuildSpec fallback cleanup:
        remaining selected-building rally fallback accent reads in
        `SelectionController` now derive from `BuildSpecCatalog`, and the
        unused legacy building definition read in hover affordance was removed.
    [x] SelectionController building hover BuildSpec geometry cleanup:
        old-runtime building hover affordance radius now derives from
        `BuildSpecCatalog` footprint instead of routing through
        `State.CombatTargetRadius(CombatTargetKind.Building, ...)`.
    [x] CombatEffectsLayer BuildSpec fallback cleanup:
        old-runtime building hit-pulse VFX now derives accent and footprint
        radius from `BuildSpecCatalog` instead of `State.Definition(building)`
        or `State.CombatTargetRadius(CombatTargetKind.Building, ...)`.
    [x] GameState BuildSpec build runtime cleanup:
        build option snapshots, placement validation, placed building HP,
        producer status labels, and produced-unit spawn footprints now derive
        from `BuildSpecCatalog` instead of split `BuildCatalog` /
        `BuildingDefinitions` reads.
    [x] GameState BuildSpec spatial cleanup:
        building exploration rectangles, building fog-of-war vision sources,
        building placement obstacles, and building combat radius now derive from
        `BuildSpecCatalog` footprint/sight data instead of legacy
        `Definition(building)` reads. `ReviewGate gamestatebuildspecspatial`
        locks this scoped spatial cleanup while remaining combat/label legacy
        reads stay open for later slices.
    [x] GameState BuildSpec combat metadata cleanup:
        building weapon lookup, production-lane labels, target legality, target
        priority, damage multipliers, combat-source accents, and under-attack
        labels now derive from `BuildSpecCatalog` / `BuildSpec` instead of
        legacy `Definition(building)` reads. `WeaponTargetProfile` now accepts
        BuildSpec-backed target metadata directly while the old
        `BuildingDefinition` overloads remain for compatibility tools.
        `ReviewGate gamestatebuildspeccombatmetadata` locks this scoped combat
        metadata read path; full `VerifyAll` passed 23/23 after the slice.
    [x] CombatBehavior BuildSpec read-path cleanup:
        building helper HP, structure coverage, airfield semantics, turret
        semantics, HQ weapon checks, structure armor checks, and structure
        target-profile QA now read `BuildSpecCatalog` directly instead of
        compatibility `GameState.BuildingDefinitions` or `BuildCatalog` data.
        `ReviewGate combatbehaviorbuildspecreadpath` locks this tool read-path
        cleanup; full `VerifyAll` passed 23/23 after the slice.
    [x] FogOfWarQa BuildSpec read-path cleanup:
        building fixture HP and explored-memory rectangle geometry now read
        `BuildSpecCatalog` directly instead of compatibility
        `GameState.BuildingDefinitions` data. `ReviewGate
        fogofwarqabuildspecreadpath` locks this tool read-path cleanup; full
        `VerifyAll` passed 23/23 after the slice.
    [x] BuildSpecCatalog authority inversion:
        `BuildSpecCatalog` now directly owns the unified building/build specs,
        replacing the old split `BuildCatalog` / `GameState.BuildingDefinitions`
        source-of-truth direction during migration.
    [x] BuildingDefinitions deletion cleanup:
        deleted `BuildingDefinition`, `BuildDefinition`, and `BuildCatalog`
        compatibility shells. `BuildSpecCatalog` is now the only building/build
        authoring catalog, `BuildSpec` no longer projects back to deleted legacy
        records, and building target legality/priority/damage paths use
        `BuildSpec` directly. `ReviewGate buildingdefinitionsdeleted` locks the
        deleted-symbol contract; `UnitBattlefieldBuildingTarget` remains the
        next broader building-runtime migration surface.
    [x] UnitBattlefieldBuildingTarget spec-backed state cleanup:
        `UnitBattlefieldBuildingTarget` no longer stores duplicated static
        building data (`MaxHp`, `Footprint`, `ArmorTag`, `WeaponKind`) per
        target; those values are now computed from `BuildSpecCatalog`, leaving
        the target object focused on mutable migration state.
    [x] UnitBattlefield BuildSpec upsert signature cleanup:
        the legacy `UpsertBuildingTarget` overload that accepted duplicated
        `MaxHp` / `Footprint` / `ArmorTag` / `WeaponKind` arguments was removed;
        BattleRoot and test tools now call the single BuildSpec-backed building
        target upsert entrypoint.
    [x] BuildingTargetEntityBridge BuildSpec-only cleanup:
        `BuildingTargetEntityBridge` no longer exposes `BuildingDefinition` /
        `BuildDefinition` adapter overloads for entity specs, spawning, or
        component generation; bridge callers now pass `BuildSpec` directly.
    [x] UnitBattlefieldBuildingTarget selection EntityWorld cleanup:
        building selection is no longer mirrored on
        `UnitBattlefieldBuildingTarget.Selected`; selection now lives in
        EntityWorld `SelectableComponentState`, and legacy UI fallback reads it
        through `BuildingProjection`.
    [x] BuildingTargetEntityBridge direct BuildSpec component cleanup:
        building entity component generation now reads static max HP, footprint,
        collision radius, and weapon kind directly from `BuildSpec` instead of
        routing through `UnitBattlefieldBuildingTarget` convenience properties.
    [x] BuildingTargetEntityBridge seed bridge cleanup:
        added `BuildingEntitySeed` as the immutable bridge input for building
        entity creation. `BuildingTargetEntityBridge` no longer depends on
        `UnitBattlefieldBuildingTarget`; the migration wrapper exports a seed,
        and EntityWorld spawn/component creation now combines seed runtime values
        (position, facing, HP, owner, kind) with static `BuildSpec` data.
        `ReviewGate buildingtargetseedbridge` locks the decoupled bridge.
    [x] UnitBattlefield building identity component cleanup:
        added `BuildingIdentityComponentState` with legacy building id, kind,
        player slot, and faction. `BuildingTargetEntityBridge` writes identity
        from `BuildingEntitySeed`, adopted constructed buildings receive the
        component, deterministic hashes include it, and live building view/hover/
        minimap/selection projections now prefer EntityWorld identity over the
        migration wrapper. `ReviewGate buildingtargetidentitycomponent` locks the
        next prerequisite for removing the public target surface.
    [x] UnitBattlefield building public-surface projection cleanup:
        added building-id and hover-projection APIs for hostile/any building
        picking, selected attack, explicit attack, and selected repair. Live
        `SelectionController` hover, attack, and repair input now stores
        `BuildingHoverProjection` snapshots and submits building ids instead of
        holding mutable `UnitBattlefieldBuildingTarget` handles. The old target
        overloads remain as compatibility wrappers while runtime users migrate.
        `ReviewGate buildingtargetpublicsurface` locks this public-surface
        quarantine step.
    [x] UnitBattlefieldBuildingTarget production queue EntityWorld cleanup:
        production queue state is no longer stored on
        `UnitBattlefieldBuildingTarget`; runtime code, enemy production AI, and
        tests now read queues from EntityWorld `ProductionQueueComponentState`
        through `UnitBattlefield.BuildingProductionQueue(...)`.
    [x] UnitBattlefieldBuildingTarget rally EntityWorld cleanup:
        rally point and rally pulse state are no longer stored on
        `UnitBattlefieldBuildingTarget`; runtime code, BattleRoot fallback sync,
        enemy production AI, and tests now read/write rally through EntityWorld
        `RallyPointComponentState` / `PresentationPulseComponentState`.
    [x] UnitBattlefieldBuildingTarget power/construction EntityWorld cleanup:
        powered state and build progress are no longer stored on
        `UnitBattlefieldBuildingTarget`; producer/refinery eligibility, bridge
        initialization, and tests now read/write them through EntityWorld
        `PowerComponentState` / `ConstructionComponentState`.
    [x] UnitBattlefieldBuildingTarget dock EntityWorld cleanup:
        refinery dock reservation and docked-harvester state are no longer
        stored on `UnitBattlefieldBuildingTarget`; runtime harvest cleanup,
        BattleRoot fallback sync, bridge initialization, and tests now read/write
        dock state through EntityWorld `DockComponentState`.
    [x] UnitBattlefieldBuildingTarget presentation pulse EntityWorld cleanup:
        building hit pulse and delivery pulse are no longer stored on
        `UnitBattlefieldBuildingTarget`; combat feedback, refinery delivery
        feedback, BattleRoot fallback sync, bridge initialization, and tests now
        read/write them through EntityWorld `PresentationPulseComponentState`.
    [x] UnitBattlefieldBuildingTarget weapon user EntityWorld cleanup:
        building attack target id/kind and weapon cooldown state are no longer
        stored on `UnitBattlefieldBuildingTarget`; turret combat, death cleanup,
        bridge initialization, and tests now read/write them through EntityWorld
        `WeaponUserComponentState`.
    [x] UnitBattlefield building presentation BuildSpec authority cleanup:
        building display name keys, role keys, short codes, role glyphs, icons,
        and accents are available from `BuildSpec`; `BuildingTargetEntityBridge`
        and UnitBattlefield building selection / hit-pulse projections now read
        those values from `BuildSpec` instead of `BuildingPresentationCatalog`.
    [x] BuildingPresentationCatalog deletion:
        `BuildingPresentationCatalog` and `BuildingPresentationDescriptor` were
        deleted; `PresentationCatalog.Building`, `BuildingTargetEntityBridge`,
        and live UnitBattlefield building projections now use `BuildSpec`
        presentation metadata.
    [x] UnitBattlefieldBuildingTarget radius projection cleanup:
        `UnitBattlefieldBuildingTarget` no longer exposes a duplicated `Radius`
        convenience projection; UnitBattlefield picking, hover, and spawn
        obstacle paths now prefer EntityWorld `BuildingPresentationProjection`
        radius with a `BuildSpec` footprint fallback during migration.
    [x] UnitBattlefieldBuildingTarget static projection deletion:
        `UnitBattlefieldBuildingTarget` no longer exposes BuildSpec convenience
        projections (`MaxHp`, `Footprint`, `ArmorTag`, `WeaponKind`). BattleRoot,
        UnitBattlefield runtime paths, and CombatBehavior QA now resolve static
        building data through `BuildSpecCatalog.For(kind)` / local BuildSpec
        lookup helpers, leaving the target wrapper as mutable migration seed data.
        `ReviewGate buildingtargetstaticprojectiondeleted` locks this cleanup;
        full `VerifyAll` passed 23/23 after the slice.
    [x] UnitBattlefieldBuildingTarget command object overload deletion:
        selected-building repair, selected-building attack, and explicit
        building attack commands no longer expose `UnitBattlefieldBuildingTarget`
        object overloads. SelectionController, enemy attack-wave AI, PlayerLoopQa,
        AiOpponentLoopQa, and CombatBehavior now submit building commands by id,
        with `UnitBattlefield` resolving the migration target internally.
        `ReviewGate buildingtargetcommandobjectdeleted` locks this command API
        boundary; full `VerifyAll` passed 23/23 after the slice.
    [x] UnitBattlefieldBuildingTarget hit-pulse object API deletion:
        public building hit-pulse writes now use `SetBuildingHitPulse(int buildingId, ...)`.
        `UnitBattlefield` resolves the migration target internally and keeps only
        a private resolved-target helper; CombatBehavior QA writes pulses by id.
        `ReviewGate buildingtargetpulseobjectdeleted` locks this public pulse API
        boundary; full `VerifyAll` passed 23/23 after the slice.
    [x] UnitBattlefieldBuildingTarget pulse read object API deletion:
        public building pulse reads no longer expose `UnitBattlefieldBuildingTarget`
        arguments. `UnitBattlefield` keeps only a private hit-pulse helper for
        internal selection fallback, while `BattleRoot` reads delivery pulse from
        the id-based `BuildingPresentationProjection(target.Id)`. `ReviewGate
        buildingtargetpulsereadobjectdeleted` locks this projection read boundary;
        full `VerifyAll` passed 23/23 after the slice.
    [x] UnitBattlefieldBuildingTarget presentation pulse internal id cleanup:
        internal building hit, delivery, rally, shared presentation-pulse, and
        pulse-decay helpers now pass building ids into `BuildingHitPulseCore(int buildingId)`,
        `BuildingPresentationPulseCore(int buildingId)`,
        `SetBuildingPresentationPulseCore(int buildingId, ...)`, and related
        id-based write helpers instead of passing migration wrapper objects.
        `ReviewGate buildingtargetpulseinternalid` locks this internal pulse
        read/write boundary; full `VerifyAll` passed 23/23 after the slice.
    [x] UnitBattlefieldBuildingTarget pulse projection tick cleanup:
        `UnitBattlefield.Update(...)` now decays building presentation pulses by
        enumerating `BuildingTargetIds()` and passing ids into
        `DecayBuildingPresentationPulses(int buildingId, ...)` instead of
        scanning the private `Buildings` wrapper list every tick. `ReviewGate
        buildingtargetpulseprojectiontick` locks this tick read path; full
        `VerifyAll` passed 23/23 after the slice.
    [x] UnitBattlefieldBuildingTarget health sync internal id cleanup:
        internal building health sync now passes building ids into
        `SyncBuildingHealthFromEntityCore(int buildingId)` instead of passing
        migration wrapper objects. Building damage feedback still syncs
        EntityWorld `HealthComponentState` back to the legacy target during the
        migration window. `ReviewGate buildingtargethealthinternalid` locks this
        internal health sync boundary; full `VerifyAll` passed 23/23 after the slice.
    [x] UnitBattlefieldBuildingTarget production queue object API deletion:
        public building production queue reads now use
        `BuildingProductionQueue(int buildingId)`. `UnitBattlefield` resolves the
        migration target internally and keeps only a private queue helper for
        existing runtime migration code; enemy production AI and CombatBehavior QA
        read queues by id. `ReviewGate buildingtargetproductionqueueobjectdeleted`
        locks this public queue API boundary; full `VerifyAll` passed 23/23
        after the slice.
    [x] UnitBattlefieldBuildingTarget production queue internal id cleanup:
        internal `UnitBattlefield` production queue reads now pass building ids
        into `BuildingProductionQueueCore(int buildingId)` instead of passing
        migration wrapper objects through a private queue helper. Production
        selection, queue ordering, status summaries, and completion checks still
        read `ProductionQueueComponentState` items from EntityWorld. `ReviewGate
        buildingtargetproductionqueueinternalid` locks this internal queue boundary;
        full `VerifyAll` passed 23/23 after the slice.
    [x] UnitBattlefieldBuildingTarget queue projection read cleanup:
        `CancelFirstProduction(...)`, `HasQueuedProduction(...)`, and
        `ProductionQueueSummary(...)` now enumerate producers through
        `BuildingTargetIds()`, filter owner through `BuildingIdentity(int)`, and
        read queue items through `BuildingProductionQueue(int)` instead of directly
        scanning the private `Buildings` wrapper list. Cancel commands now carry
        the chosen producer id through sync and entity-command submission.
        `ReviewGate buildingtargetqueueprojectionreads` locks this queue read
        path; full `VerifyAll` passed 23/23 after the slice.
    [x] UnitBattlefieldBuildingTarget ensure production queue component internal id cleanup:
        internal queue-component creation now passes building ids into
        `EnsureProductionQueueComponent(int buildingId, EntityInstance entity)`
        instead of passing migration wrapper objects, while preserving the
        UnitDesign producer-capability check and existing-queue guard. `ReviewGate
        buildingtargetensurequeueinternalid` locks this internal ensure boundary;
        full `VerifyAll` passed 23/23 after the slice.
    [x] UnitBattlefieldBuildingTarget producer eligibility internal id cleanup:
        internal production design and producer-capability reads now pass building
        ids into `ProductionDesignIdCore(int buildingId, ProductionKind ...)` and
        `HasAnyProductionForCore(int buildingId)` instead of passing migration
        wrapper objects through private production helpers. The helpers still
        preserve faction-aware UnitDesign runtime loadout and roster lookup during
        migration. `ReviewGate buildingtargetproducereligibilityinternalid` locks
        this internal producer eligibility boundary; full `VerifyAll` passed 23/23
        after the slice.
    [x] UnitBattlefieldBuildingTarget producer projection read cleanup:
        `CandidateProducerIds(...)` now enumerates immutable `BuildingSnapshot`
        candidates through `BuildingTargetIds()` instead of directly scanning the
        private `Buildings` wrapper list. `ProductionDesignIdCore(...)` and
        `HasAnyProductionForCore(...)` now resolve `BuildingIdentity(int)` for
        faction/kind checks, while `FirstDesignIdFor(...)`,
        `ProductionDesignSpecs(...)`, and `FactionForSlot(...)` share the same
        identity-first faction lookup with the existing unit fallback. `ReviewGate
        buildingtargetproducerprojectionreads` locks this producer read path; full
        `VerifyAll` passed 23/23 after the slice.
    [x] UnitBattlefieldBuildingTarget production completion projection read cleanup:
        `UpdateProductionQueues(...)` now collects active producer ids through
        `BuildingTargetIds()` and id-based production queue reads instead of
        scanning the private `Buildings` wrapper list. Completion matching still
        snapshots producers before `ProductionSystem` steps, matches completed
        units by owner/design/nearest producer position, and publishes the same
        id-derived `ProductionCompleted` payload. `ReviewGate
        buildingtargetproductioncompleteprojectionreads` locks this projection
        read path; full `VerifyAll` passed 23/23 after the slice.
    [x] UnitBattlefieldBuildingTarget combat projection read cleanup:
        `UpdateBuildingCombatFromEntityWorld(...)` now decides whether armed
        building combat needs to step by enumerating `BuildingTargetIds()` and
        resolving `BuildingIdentity(int)` before reading `BuildSpecCatalog`
        weapon metadata, instead of scanning the private `Buildings` wrapper list
        for `building.Kind`. `ReviewGate buildingtargetcombatprojectionreads`
        locks this active-combat read path; full `VerifyAll` passed 23/23 after
        the slice.
    [x] UnitBattlefieldBuildingTarget rally object API deletion:
        public building rally reads now use `BuildingRallyPoint(int buildingId)`
        and `BuildingRallyPulse(int buildingId)`. `UnitBattlefield` resolves the
        migration target internally and keeps only private rally helpers for
        existing runtime migration code; enemy production AI and CombatBehavior QA
        read rally state by id. `ReviewGate buildingtargetrallyobjectdeleted`
        locks this public rally API boundary; full `VerifyAll` passed 23/23
        after the slice.
    [x] UnitBattlefieldBuildingTarget rally internal id cleanup:
        internal rally point and rally pulse reads now pass building ids into
        `BuildingRallyPointCore(int buildingId)` and
        `BuildingRallyPulseCore(int buildingId)` instead of passing migration
        wrapper objects through private read helpers. Rally writes remain a
        separate migration surface. `ReviewGate buildingtargetrallyinternalid`
        locks this internal rally read boundary; full `VerifyAll` passed 23/23
        after the slice.
    [x] UnitBattlefieldBuildingTarget selected rally projection read cleanup:
        selected-building rally commands now enumerate selected producers through
        `BuildingTargetIds()`, filter owner through `BuildingIdentity(int)`, read
        selection through `BuildingProjection(int)`, and carry producer ids into
        `SetRallyPoint(...)` instead of carrying mutable wrapper objects. Single
        producer status labels now derive the producer kind from building identity.
        `ReviewGate buildingtargetselectedrallyprojectionreads` locks this command
        read path; full `VerifyAll` passed 23/23 after the slice.
    [x] UnitBattlefieldBuildingTarget power/construction object API deletion:
        public building power and construction reads now use
        `BuildingPowered(int buildingId)` and `BuildingBuildProgress(int buildingId)`.
        `UnitBattlefield` preserves the migration defaults for missing entities
        and keeps only private resolved-target helpers for internal eligibility
        code; CombatBehavior QA reads power/construction state by id. `ReviewGate
        buildingtargetpowerconstructionobjectdeleted` locks this public API boundary;
        full `VerifyAll` passed 23/23 after the slice.
    [x] UnitBattlefieldBuildingTarget power/construction internal id cleanup:
        internal power and construction reads now pass building ids into
        `BuildingPoweredCore(int buildingId)` and
        `BuildingBuildProgressCore(int buildingId)` instead of passing migration
        wrapper objects through private read helpers. Producer/refinery eligibility
        still preserves missing-component defaults through the id APIs. `ReviewGate
        buildingtargetpowerconstructioninternalid` locks this internal read boundary;
        full `VerifyAll` passed 23/23 after the slice.
    [x] UnitBattlefieldBuildingTarget dock object API deletion:
        public building dock reads now use
        `BuildingDockReservedByHarvesterId(int buildingId)` and
        `BuildingDockedHarvesterId(int buildingId)`. `UnitBattlefield` keeps only
        private resolved-target helpers and still converts EntityWorld dock ids
        back to legacy unit ids for migration callers; BattleRoot and
        CombatBehavior QA read dock state by id. `ReviewGate
        buildingtargetdockobjectdeleted` locks this public API boundary; full
        `VerifyAll` passed 23/23 after the slice.
    [x] UnitBattlefieldBuildingTarget dock internal id cleanup:
        internal dock reservation, dock occupancy, and dock state reads now pass
        building ids into `BuildingDockReservedByHarvesterIdCore(int buildingId)`,
        `BuildingDockedHarvesterIdCore(int buildingId)`, and
        `BuildingDockStateCore(int buildingId)` instead of passing migration
        wrapper objects through private read helpers. `ReviewGate
        buildingtargetdockinternalid` locks this internal dock read boundary; full
        `VerifyAll` passed 23/23 after the slice.
    [x] UnitBattlefieldBuildingTarget weapon read object API deletion:
        public building weapon reads now use `BuildingAttackTargetId(int buildingId)`,
        `BuildingAttackTargetKind(int buildingId)`, and
        `BuildingAttackCooldownRemaining(int buildingId)`. `UnitBattlefield` keeps
        only private resolved-target helpers for internal cleanup; CombatBehavior
        QA reads building target state by id. `ReviewGate
        buildingtargetweaponreadobjectdeleted` locks this public API boundary;
        full `VerifyAll` passed 23/23 after the slice.
    [x] UnitBattlefieldBuildingTarget weapon read internal id cleanup:
        internal building attack target, attack kind, cooldown, and weapon-state
        reads now pass building ids into `BuildingAttackTargetIdCore(int buildingId)`,
        `BuildingAttackTargetKindCore(int buildingId)`,
        `BuildingAttackCooldownRemainingCore(int buildingId)`, and
        `BuildingWeaponStateCore(int buildingId)` instead of passing migration
        wrapper objects through private read helpers. `ReviewGate
        buildingtargetweaponreadinternalid` locks this internal weapon read
        boundary; full `VerifyAll` passed 23/23 after the slice.
    [x] UnitBattlefieldBuildingTarget clear attack target internal id cleanup:
        internal building attack-target clearing now passes building ids into
        `ClearBuildingAttackTargetCore(int buildingId)` instead of passing
        migration wrapper objects, while preserving `WeaponUserComponentState`
        target reset behavior. `ReviewGate buildingtargetclearattackinternalid`
        locks this internal clear-target boundary; full `VerifyAll` passed 23/23
        after the slice.
    [x] UnitBattlefieldBuildingTarget pick object API deletion:
        public building picking no longer returns `UnitBattlefieldBuildingTarget`
        wrappers. `UnitBattlefield` keeps only private pick helpers that preserve
        distance priority and deterministic id tie-breaks, while public callers use
        `PickHostileBuildingId(...)`, `PickBuildingTargetId(...)`,
        `PickAnyBuildingTargetId(...)`, or hover projections. `ReviewGate
        buildingtargetpickobjectdeleted` locks this public pick API boundary; full
        `VerifyAll` passed 23/23 after the slice.
    [x] UnitBattlefieldBuildingTarget visibility object API deletion:
        public building visibility reads now use
        `IsVisibleTo(PlayerSlotId viewer, int buildingId)`. `UnitBattlefield`
        resolves the migration target internally and keeps only a private helper
        that continues to read EntityWorld `VisibilityIndex`; enemy attack-wave AI
        filters visible buildings by id. `ReviewGate
        buildingtargetvisibilityobjectdeleted` locks this public visibility API
        boundary; full `VerifyAll` passed 23/23 after the slice.
    [x] UnitBattlefieldBuildingTarget visibility internal id cleanup:
        internal building visibility reads now pass building ids into
        `IsVisibleToCore(PlayerSlotId viewer, int buildingId)` instead of passing
        migration wrapper objects. The helper still preserves the migration entity
        sync fallback and reads EntityWorld `VisibilityIndex`. `ReviewGate
        buildingtargetvisibilityinternalid` locks this internal visibility boundary;
        full `VerifyAll` passed 23/23 after the slice.
    [x] UnitBattlefieldBuildingTarget visibility projection read cleanup:
        `MarkVisibleBuildingFootprints(...)` now enumerates both building vision
        sources and building footprint targets through `BuildingTargetIds()` plus
        immutable `BuildingSnapshot(int)` reads instead of scanning the private
        `Buildings` wrapper list. Alive/completed source filtering, owner-relation
        target filtering, id-based radius reads, snapshot-position distance math,
        and EntityWorld `VisibilityIndex` writes are preserved. `ReviewGate
        buildingtargetvisibilityprojectionreads` locks this fog/visibility read
        path; full `VerifyAll` passed 23/23 after the slice.
    [x] UnitBattlefieldBuildingTarget repairability internal id cleanup:
        internal building repairability checks now pass building ids into
        `IsRepairableBuildingTargetCore(PlayerSlotId playerSlotId, int buildingId)`
        instead of passing migration wrapper objects, while preserving damaged,
        alive, and friendly/allied relation checks. `ReviewGate
        buildingtargetrepairabilityinternalid` locks this repairability boundary;
        full `VerifyAll` passed 23/23 after the slice.
    [x] UnitBattlefieldBuildingTarget radius internal id cleanup:
        internal building radius reads now pass building ids into
        `BuildingTargetRadiusCore(int buildingId)` instead of passing migration
        wrapper objects. Existing building loops use the id plus fallback-kind
        path, preserving EntityWorld presentation radius first, BuildSpec
        footprint fallback, and avoiding repeated wrapper-list scans for picking,
        visible footprint marking, and spawn obstacles. `ReviewGate
        buildingtargetradiusinternalid` locks this internal radius boundary; full
        `VerifyAll` passed 23/23 after the slice.
    [x] UnitBattlefieldBuildingTarget placement projection read cleanup:
        `SpawnObstacles()`, `BuildingBuildAnchors(...)`, and
        `BuildingPlacementObstacles()` now enumerate building ids through
        `BuildingTargetIds()` and immutable `BuildingSnapshot(int)` reads instead
        of scanning the private `Buildings` wrapper list. Spawn radius, build
        radius, powered state, construction progress, owner filtering, and
        footprint obstacle math still use the existing id/BuildSpec/PlacementMath
        paths. `ReviewGate buildingtargetplacementprojectionreads` locks this
        placement read path; full `VerifyAll` passed 23/23 after the slice.
    [x] UnitBattlefieldBuildingTarget authority projection read cleanup:
        `SyncOwnerRelations()` and `ConstructionSubjectEntities(...)` now read
        building owners and construction producers through `BuildingTargetIds()`,
        `BuildingIdentity(int)`, and immutable `BuildingSnapshot(int)` state
        instead of scanning the private `Buildings` wrapper list. Owner-slot
        discovery keeps units, resource inventories, and baseline slots; producer
        lookup keeps owner, required-kind, alive/completed, deterministic ordering,
        and id-based entity sync behavior. `ReviewGate
        buildingtargetauthorityprojectionreads` locks this authority read path;
        full `VerifyAll` passed 23/23 after the slice.
    [x] UnitBattlefieldBuildingTarget refinery/dock projection read cleanup:
        `FindBestRefineryIdForHarvester(...)`, `ClearRefineryDockClaim(...)`, and
        `SyncDockStateFromEntities()` now enumerate refinery candidates through
        `BuildingTargetIds()`, `BuildingSnapshot(int)`, `BuildingIdentity(int)`,
        and `BuildingEntityByTargetId(int)` instead of scanning the private
        `Buildings` wrapper list. Refinery owner/kind/alive/completed/nearest
        filtering, dock reservation cleanup, dock occupancy cleanup, legacy docked
        harvester projection, and delivery-pulse writes are preserved. `ReviewGate
        buildingtargetrefinerydockprojectionreads` locks this refinery/dock read
        path; full `VerifyAll` passed 23/23 after the slice.
    [x] UnitBattlefieldBuildingTarget sync/cleanup projection read cleanup:
        `SyncBuildingTargetEntities()` and `RemoveDeadUnits()` now enumerate
        buildings through `BuildingTargetIds()` instead of scanning the private
        `Buildings` wrapper list. Entity sync still calls
        `SyncBuildingTargetEntity(int)` and dead-unit cleanup still clears building
        weapon targets through `BuildingAttackTargetKindCore(int)`,
        `BuildingAttackTargetIdCore(int)`, and `ClearBuildingAttackTargetCore(int)`.
        `ReviewGate buildingtargetsynccleanupprojectionreads` locks this sync and
        cleanup read path; full `VerifyAll` passed 23/23 after the slice.
    [x] UnitBattlefieldBuildingTarget ordered wrapper list deletion:
        removed the private ordered `Buildings` list from `UnitBattlefield`.
        Temporary building target seed state now lives only in `_buildingTargetSeedsById`,
        while `BuildingTargetIds()` keeps EntityWorld ordered identity enumeration
        first and uses deterministic `_buildingTargetSeedsById.Keys.OrderBy(...)` only
        as the remaining migration fallback. Add/remove paths no longer maintain a
        second ordered wrapper collection. `ReviewGate buildingtargetorderedlistdeleted`
        locks this list deletion boundary; full `VerifyAll` passed 23/23 after
        the slice.
    [x] UnitBattlefieldBuildingTarget seed wrapper deletion:
        Replace the final `UnitBattlefieldBuildingTarget` migration wrapper with
        direct `BuildingEntitySeed` storage in `UnitBattlefield`, delete
        `UnitBattlefieldBuildingTarget.cs`, keep `BuildingTargetIds()` deterministic
        over EntityWorld identity order plus seed-id fallback, and ensure
        `SyncBuildingTargetEntity(int)` passes seed data directly into
        `BuildingTargetEntityBridge`. `ReviewGate buildingtargetseedwrapperdeleted`
        locks this final wrapper deletion boundary; full `VerifyAll` passed 23/23
        after the slice.
    [x] UnitBattlefieldBuildingTarget id seed fallback deletion:
        removed the remaining seed-only fallback enumeration from
        `BuildingTargetIds()`. Batch building projections now enumerate only
        EntityWorld `BuildingIdentityComponentState` entries that are still backed
        by temporary seed storage, so a stale seed cannot reappear in
        `BuildingSnapshots()`. `CombatBehavior` removes a building identity and
        proves snapshots do not resurrect that seed-only id; `ReviewGate
        buildingtargetidsseedfallbackdeleted` locks the boundary. Local build,
        affected building target gates, `CombatBehavior`, and `SimReplay` passed;
        full `VerifyAll` passed 23/23 after the multi-slice batch.
    [x] UnitBattlefieldBuildingTarget direct seed fallback deletion:
        removed direct seed fallback from `BuildingIdentity(int)` and
        `BuildingSnapshot(int)`. Point snapshot reads now fail closed unless the
        building has an EntityWorld `BuildingIdentityComponentState` and
        `EntityProjection`; seed storage remains only as temporary lifecycle input.
        `CombatBehavior` proves direct `BuildingSnapshot(...)` and
        `BuildingViewProjection(...)` do not synthesize seed fallback snapshots
        after identity removal, then proves same-id upsert restores EntityWorld
        identity. `ReviewGate buildingtargetdirectseedfallbackdeleted` locks this
        direct lookup boundary; local build, affected snapshot gates,
        `CombatBehavior`, and `SimReplay` passed. Full `VerifyAll` passed 23/23
        after the multi-slice batch.
    [x] UnitBattlefieldBuildingTarget id seed guard deletion:
        removed the remaining temporary seed-storage guard from
        `BuildingTargetIds()`. Batch building id enumeration now depends only on
        EntityWorld `BuildingIdentityComponentState` order plus duplicate
        suppression; `_buildingTargetSeedsById` remains a lifecycle write/sync
        input, not a read-model gate. `CombatBehavior` removes the private seed
        entry through reflection while leaving EntityWorld identity intact and
        proves snapshots/views still resolve. `ReviewGate
        buildingtargetidsseedguarddeleted` locks this boundary; local build,
        affected id/seed gates, `CombatBehavior`, and `SimReplay` passed. Full
        `VerifyAll` passed 23/23 after the multi-slice batch.
    [x] UnitBattlefieldBuildingTarget death projection read cleanup:
        building damage/death handling no longer writes EntityWorld health back
        into `_buildingTargetSeedsById`. Existing-entity sync now preserves
        EntityWorld `HealthComponentState` unless an explicit upsert HP override
        is supplied, dead-building detection reads projected
        `BuildingSnapshot(...)` HP, and `BuildingDeathInfo(int)` builds its
        payload from the projected snapshot. Building repair legality also reads
        projected snapshot HP. `ReviewGate buildingtargetdeathprojectionreads`
        locks this boundary; local build, affected death/health gates,
        `CombatBehavior`, and `SimReplay` passed. Full `VerifyAll` passed 23/23
        after the multi-slice batch.
    [x] UnitBattlefieldBuildingTarget attack projection read cleanup:
        selected-building attack and explicit group building attack commands now
        resolve `BuildingSnapshot(...)` plus `BuildingEntityByTargetId(...)`
        instead of reading `_buildingTargetSeedsById`. Relation checks use
        projected owner data, BuildSpec targeting uses projected kind, and the
        submitted `GroupAttackEntityCommand` targets the EntityWorld entity id
        directly. `ReviewGate buildingtargetattackprojectionreads` locks this
        command boundary; local build, affected attack/sync/combat-helper gates,
        `CombatBehavior`, and `SimReplay` passed. Full `VerifyAll` passed 23/23
        after the multi-slice batch.
    [x] UnitBattlefieldBuildingTarget public read seed guard deletion:
        remove temporary seed-storage existence guards from public building read
        APIs for production queue, rally, weapon state, power/construction, dock,
        visibility, hit-pulse writes, and single-id radius fallback. These reads
        should fail closed or use their existing EntityWorld component defaults
        through id-based core helpers instead of consulting `_buildingTargetSeedsById`.
        `CombatBehavior` proves the read APIs still work after the seed entry is
        removed while EntityWorld identity/components remain, and `ReviewGate
        buildingtargetpublicreadseedguardsdeleted` locks the boundary. Local
        build, `CombatBehavior`, and the narrow ReviewGate passed; full
        `VerifyAll` passed 23/23 after the multi-slice batch.
    [x] UnitBattlefieldBuildingTarget rally command projection read cleanup:
        direct `SetRallyPoint(int, ...)` command submission should resolve the
        producer entity and owner from EntityWorld `BuildingIdentityComponentState`
        and submit `SetRallyPointEntityCommand` with the producer `EntityId`,
        without reading `_buildingTargetSeedsById` or `_buildingTargetEntityIds`
        through seed-shaped state. Resource rally keeps its target entity id, and
        `CombatBehavior` proves direct rally commands still work after seed entry
        removal. `ReviewGate buildingtargetrallycommandprojectionreads` locks the
        boundary; local build, `CombatBehavior`, and the narrow ReviewGate passed.
        Full `VerifyAll` passed 23/23 after the multi-slice batch.
    [x] UnitBattlefieldBuildingTarget reverse EntityId index cleanup:
        add and maintain a private reverse `EntityId -> buildingId` index so
        combat event resolution, legacy target conversion, harvester refinery
        sync, and constructed-building adoption do not linearly scan
        `_buildingTargetEntityIds`. All building entity id mapping writes and
        removals now go through helper methods that keep forward and reverse
        maps in sync. `CombatBehavior` proves reverse lookup and cleanup after
        removal; `ReviewGate buildingtargetreverseentityindex`, local build, and
        `SimReplay` passed. Full `VerifyAll` passed 23/23 after the multi-slice
        batch.
    [x] UnitBattlefieldBuildingTarget selection seed guard deletion:
        `SetBuildingTargetSelected(int, bool)` should update EntityWorld
        `SelectableComponentState` only when the building entity already exists,
        without reading `_buildingTargetSeedsById` or invoking the seed sync path.
        Selection projection reads remain unchanged. `CombatBehavior` proves
        selection writes still update projections after seed removal; `ReviewGate
        buildingtargetselectionseedguarddeleted`, local build, and `SimReplay`
        passed. Full `VerifyAll` passed 23/23 after the multi-slice batch.
    [x] UnitBattlefieldBuildingTarget adoption seed guard deletion:
        `AdoptConstructedBuildingId(...)` should reuse the reverse EntityWorld
        entity index directly and restore missing `BuildingIdentityComponentState`
        if needed, instead of validating the existing id through temporary seed
        storage. Existing producer queue component setup is preserved.
        `CombatBehavior` proves seedless adoption identity restoration;
        `ReviewGate buildingtargetadoptseedguarddeleted`, local build, and
        `SimReplay` passed. Full `VerifyAll` passed 23/23 after the multi-slice
        batch.
    [x] UnitBattlefieldBuildingTarget adoption seedless cleanup:
        `AdoptConstructedBuildingId(...)` now maps constructed EntityWorld
        buildings directly through `SetBuildingTargetEntityId(...)` and
        `BuildingIdentityComponentState` without calling the temporary
        `AddBuildingTarget(...)` seed writer. The unused add helper was deleted.
        `CombatBehavior` proves unmapped constructed-building adoption does not
        repopulate `_buildingTargetSeedsById`; `ReviewGate
        buildingtargetadoptionseedless`, local build, and `SimReplay` passed.
        Full `VerifyAll` passed 23/23 after the current multi-slice batch.
    [x] UnitBattlefieldBuildingTarget upsert projection read cleanup:
        `UpsertBuildingTarget(...)` should preserve existing building identity
        from EntityWorld `BuildingIdentityComponentState` and sync by id without
        deciding existing identity through `BuildingTargetById(id)` or deriving
        updated identity from temporary seed state. The later seedless-upsert
        slice removed the temporary seed refresh from this path. `CombatBehavior`
        proves seedless same-id upsert preserves EntityWorld identity while
        refreshing runtime state; `ReviewGate buildingtargetupsertprojectionreads`,
        local build, and `SimReplay` passed. Full `VerifyAll` passed 23/23 after
        the later multi-slice batch.
    [x] UnitBattlefieldBuildingTarget upsert seedless cleanup:
        `UpsertBuildingTarget(...)` now passes explicit `BuildingEntitySeed` data
        into `SyncBuildingTargetEntity(...)` and no longer repopulates
        `_buildingTargetSeedsById`. The sync bridge prefers explicit seed data,
        then falls back to optional seed-cache state or existing EntityWorld
        identity, keeping runtime updates projection-owned. `CombatBehavior`
        proves seedless upsert preserves identity, position, HP, rally, and does
        not recreate temporary seed storage; `ReviewGate
        buildingtargetupsertseedless`, local build, and `SimReplay` passed.
        Full `VerifyAll` passed 23/23 after the current multi-slice batch.
    [x] UnitBattlefieldBuildingTarget next id projection guard cleanup:
        `NextBuildingTargetId()` should skip ids that are still present as
        EntityWorld `BuildingIdentityComponentState` entries even when temporary
        seed storage is missing, while retaining seed storage as a compatibility
        duplicate guard during migration. `CombatBehavior` proves seedless id
        allocation skips EntityWorld identities; `ReviewGate
        buildingtargetnextidprojectionguard`, local build, and `SimReplay`
        passed. Full `VerifyAll` passed 23/23 after the current multi-slice batch.
    [x] UnitBattlefieldBuildingTarget add projection guard cleanup:
        `AddBuildingTarget(...)` now rejects duplicate ids through
        `BuildingTargetIdInUse(target.Id)`, so EntityWorld building identity
        prevents duplicate seed insertion even when temporary seed storage is
        missing. The later adoption seedless slice removed this temporary add
        helper after deleting its last writer. `CombatBehavior` proves the
        seedless adoption replacement; `ReviewGate
        buildingtargetaddprojectionguard`, local build, and `SimReplay` passed.
        Full `VerifyAll` passed 23/23 after the current multi-slice batch.
    [x] UnitBattlefieldBuildingTarget remove state void cleanup:
        `RemoveBuildingTargetState(...)` is now an idempotent seed-cache deletion
        helper instead of returning seed-hit/seed-miss as authority. Public and
        dead-building removal continue to remove EntityWorld mappings/entities
        independently of temporary seed storage. `CombatBehavior` proves seedless
        public removal clears projected reads; `ReviewGate
        buildingtargetremovestatevoid`, local build, and `SimReplay` passed.
        Full `VerifyAll` passed 23/23 after the current multi-slice batch.
    [x] UnitBattlefieldBuildingTarget seedless sync cleanup:
        `SyncBuildingTargetEntity(...)` now treats temporary seed state as an
        optional migration source for existing EntityWorld buildings. When seed
        storage is missing but the building identity/entity exists, sync derives
        seed data from EntityWorld transform, identity, and health, then refreshes
        components without repopulating seed storage. `CombatBehavior` proves
        seedless existing-entity sync; `ReviewGate buildingtargetsyncseedless`,
        local build, and `SimReplay` passed. Full `VerifyAll` passed 23/23 after
        the current multi-slice batch.
    [x] UnitBattlefieldBuildingTarget seed storage deletion:
        Deleted the final temporary building target seed lifecycle storage:
        `_buildingTargetSeedsById`, `BuildingTargetById(...)`, and
        `RemoveBuildingTargetState(...)`. Building target identity, lookup,
        allocation, removal, and sync are now owned by EntityWorld
        identity/projection plus `_buildingTargetEntityIds` /
        `_buildingTargetIdsByEntityId` mappings. `SyncBuildingTargetEntity(...)`
        uses explicit upsert/adoption seed data or derives seed data from an
        existing EntityWorld building via `SeedForExistingBuildingEntity(...)`,
        without keeping a separate seed cache. Gates passed:
        `CombatBehavior`, `ReviewGate buildingtargetseedstoragedeleted`,
        `SimReplay`, full `ReviewGate`, review-record gate, and full
        `VerifyAll` 23/23.
    [x] UnitBattlefieldBuildingTarget produced spawn-point helper deletion:
        removed the unused `ProducedUnitSpawnPoint(UnitBattlefieldBuildingTarget, UnitSpec)`
        helper from `UnitBattlefield`; produced-unit spawn authority remains in
        EntityWorld `ProductionSystem` with shared `ProductionSpawnMath`, and
        `UnitBattlefield` keeps only spawn-obstacle projection with id-based
        building radius. `ReviewGate buildingtargetspawnpointhelperdeleted` locks
        this deletion boundary; full `VerifyAll` passed 23/23 after the slice.
    [x] UnitBattlefieldBuildingTarget event object API deletion:
        public building combat and production events now publish immutable
        `UnitBattlefieldBuildingSnapshot` snapshots instead of mutable
        `UnitBattlefieldBuildingTarget` wrappers. `BattleRoot`, CombatBehavior,
        and AI loop subscriptions consume id/owner/position/kind data from the
        snapshot while `UnitBattlefield` keeps the wrapper conversion private.
        `ReviewGate buildingtargeteventobjectdeleted` locks this event boundary;
        full `VerifyAll` passed 23/23 after the slice.
    [x] UnitBattlefieldBuildingTarget public building list object API deletion:
        external readers use `BuildingSnapshots()`, `BuildingSnapshot(int id)`, or
        `LiveBuildingCount(...)`; BattleRoot, combat effects, enemy production AI,
        enemy wave AI, AiOpponentLoopQa, PlayerLoopQa, and CombatBehavior no
        longer read mutable wrapper lists. The later ordered-list deletion removed
        the private `UnitBattlefield.Buildings` migration list entirely. `ReviewGate
        buildingtargetlistobjectdeleted` locks this public list boundary; full
        `VerifyAll` passed 23/23 after the slice.
    [x] UnitBattlefieldBuildingTarget upsert object API deletion:
        `UpsertBuildingTarget(...)` now returns immutable
        `UnitBattlefieldBuildingSnapshot` data instead of the mutable migration
        wrapper. Test fixtures that need current building HP re-read it through
        `BuildingSnapshot(int id)`, and damaged-building setup updates through
        id-based upsert rather than mutating a returned object. `ReviewGate
        buildingtargetupsertobjectdeleted` locks this public upsert boundary;
        full `VerifyAll` passed 23/23 after the slice.
    [x] UnitBattlefieldBuildingTarget internal wrapper visibility cleanup:
        `UnitBattlefieldBuildingTarget` is now `internal` private migration
        state rather than a public runtime type. Tool fixtures that exercised the
        building entity bridge now use `BuildingEntitySeed` directly, and
        `ConstructBuilding(...)` returns `UnitBattlefieldBuildingSnapshot?`
        instead of an out wrapper. `ReviewGate buildingtargetinternalwrapper`
        locks this visibility boundary; full `VerifyAll` passed 23/23 after the
        slice.
    [x] UnitBattlefieldBuildingTarget internal pick id cleanup:
        `UnitBattlefield` private building pick helpers now return nullable
        building ids through `PickHostileBuildingIdCore`,
        `PickBuildingTargetIdCore`, and `PickAnyBuildingTargetIdCore` instead of
        returning migration wrapper objects. Public callers still use id and
        hover-projection APIs, hostile picking preserves its historical
        distance-only order, and owned/any picks keep deterministic id
        tie-breaks. `ReviewGate buildingtargetpickinternalid` locks this
        internal boundary; full `VerifyAll` passed 23/23 after the slice.
    [x] UnitBattlefieldBuildingTarget pick projection read cleanup:
        internal hostile/owned/any building pick helpers now enumerate through
        `BuildingTargetIds()` and filter immutable `BuildingSnapshot` candidates
        instead of directly reading the private `Buildings` wrapper list. Pick
        distance, radius padding, relation/owner filtering, hostile distance-only
        ordering, and owned/any id tie-breaks are preserved. `ReviewGate
        buildingtargetpickprojectionreads` locks this read-path cleanup; full
        `VerifyAll` passed 23/23 after the slice.
    [x] UnitBattlefieldBuildingTarget internal projection id cleanup:
        `UnitBattlefield` building identity, selection, hit-pulse, hover, view,
        and minimap projections now resolve building identity by id through
        `BuildingIdentity(int buildingId)` instead of passing migration wrapper
        objects through private projection helpers. `ReviewGate
        buildingtargetprojectioninternalid` locks this projection boundary; full
        `VerifyAll` passed 23/23 after the slice.
    [x] UnitBattlefieldBuildingTarget snapshot internal id cleanup:
        `UnitBattlefield` building snapshot reads now use
        `BuildingSnapshot(int id)` and `RequiredBuildingSnapshot(int id)` instead
        of passing migration wrapper objects through snapshot conversion helpers.
        Upsert, construction adoption, combat events, and production events publish
        id-derived immutable snapshots while the wrapper remains private migration
        storage. `ReviewGate buildingtargetsnapshotinternalid` locks this
        snapshot boundary; full `VerifyAll` passed 23/23 after the slice.
    [x] UnitBattlefieldBuildingTarget sync internal id cleanup:
        `UnitBattlefield` building entity synchronization now uses
        `SyncBuildingTargetEntity(int buildingId, ...)` instead of passing
        mutable `UnitBattlefieldBuildingTarget` wrappers through the private sync
        helper. Upsert seed overrides, selected repair, selected/explicit building
        attack, visibility fallback, rally/production, and construction-subject
        paths now sync by id while the wrapper remains private migration storage.
        `ReviewGate buildingtargetsyncinternalid` locks this sync boundary; full
        `VerifyAll` passed 23/23 after the slice.
    [x] UnitBattlefieldBuildingTarget adopt internal id cleanup:
        constructed-building adoption now returns building ids through
        `AdoptConstructedBuildingId(...)` instead of returning mutable
        `UnitBattlefieldBuildingTarget` wrappers. Construction publishes snapshots
        from the adopted id, and unmapped constructed buildings still receive
        identity/queue components without exposing wrapper handles. `ReviewGate
        buildingtargetadoptinternalid` locks this adoption boundary; full
        `VerifyAll` passed 23/23 after the slice.
    [x] UnitBattlefieldBuildingTarget lookup index cleanup:
        temporary building target state moved behind the private
        `_buildingTargetSeedsById` index. `BuildingTargetById(int)` reads from
        that index, removal goes through `RemoveBuildingTargetState(...)`, and id
        allocation checks the shared id-in-use helper instead of linearly scanning
        the old ordered `Buildings` list. The later ordered-list deletion removed
        that list, and the adoption seedless slice deleted the temporary
        `AddBuildingTarget(...)` writer. `ReviewGate buildingtargetlookupindexed`
        locks this lookup boundary; full `VerifyAll` passed 23/23 after the slice.
    [x] UnitBattlefieldBuildingTarget snapshot projection cleanup:
        `BuildingSnapshot(int id)` now resolves building identity by id, prefers
        EntityWorld `EntityProjection` position/facing/health for immutable
        snapshot assembly, and only falls back to private
        `UnitBattlefieldBuildingTarget` seed state while the migration wrapper
        still exists. `LiveBuildingCount(...)` counts immutable snapshots instead
        of mutable wrapper health. `ReviewGate buildingtargetsnapshotprojection`
        locks this read-path direction; full `VerifyAll` passed 23/23 after the
        slice.
    [x] UnitBattlefieldBuildingTarget id projection read cleanup:
        public building snapshot, selected-building, rally, hit-pulse, and minimap
        projection reads now enumerate through `BuildingTargetIds()`, which prefers
        EntityWorld `BuildingIdentityComponentState` order and uses the private
        wrapper list only as a migration fallback. Owner filters now use
        `BuildingIdentity(int)`, and projection liveness uses `EntityProjection`.
        `ReviewGate buildingtargetidprojectionreads` locks this read-path cleanup;
        full `VerifyAll` passed 23/23 after the slice.
    [x] UnitBattlefieldBuildingTarget entity lookup internal id cleanup:
        EntityWorld-to-building lookup now returns building ids through
        `BuildingTargetIdByEntityId(EntityId entityId)` instead of returning
        migration wrapper objects, with event handlers using id-based health sync,
        snapshots, and BuildSpec lookups. `ReviewGate
        buildingtargetentitylookupinternalid` locks this lookup boundary; full
        `VerifyAll` passed 23/23 after the slice.
    [x] UnitBattlefieldBuildingTarget refinery lookup internal id cleanup:
        internal harvester refinery validation now asks for the nearest ready
        refinery id through `FindBestRefineryIdForHarvester(...)` instead of
        returning migration wrapper objects. Harvest command eligibility keeps
        owner, refinery-kind, alive, completed, and nearest-refinery filtering.
        `ReviewGate buildingtargetrefinerylookupinternalid` locks this lookup
        boundary; full `VerifyAll` passed 23/23 after the slice.
    [x] UnitBattlefieldBuildingTarget death internal id cleanup:
        building damage events now keep destroyed-building candidates as ids and
        pass `IReadOnlyList<int>` into `RemoveDeadBuildingTargets(...)`.
        Immutable `UnitBattlefieldBuildingDeathInfo` records are generated through
        `BuildingDeathInfo(int buildingId)`, preserving entity removal, attack
        target cleanup, death events, and outcome updates. `ReviewGate
        buildingtargetdeathinternalid` locks this cleanup boundary; full
        `VerifyAll` passed 23/23 after the slice.
    [x] UnitBattlefieldBuildingTarget candidate producers internal id cleanup:
        internal production candidate helpers now return producer ids through
        `CandidateProducerIds(...)` instead of returning migration wrapper objects.
        Production enqueue, production option summaries, producer entity sync,
        queue reads, and queued-event snapshots use producer ids, while preserving
        owner/faction, powered, completed, producer-kind, design-availability, and
        tech-tier filtering. `ReviewGate buildingtargetcandidateproducersinternalid`
        locks this candidate-producer boundary; full `VerifyAll` passed 23/23
        after the slice.
    [x] UnitBattlefieldBuildingTarget production completion internal id cleanup:
        live production completion matching now stores active producer ids and
        immutable producer snapshots before stepping `ProductionSystem`, instead of
        carrying `UnitBattlefieldBuildingTarget` wrappers through `queuedBefore`.
        Completed-unit matching still uses owner, design id, and nearest producer
        snapshot position, and `ProductionCompleted` publishes the stored snapshot.
        `ReviewGate buildingtargetproductioncompleteinternalid` locks this
        completion boundary; full `VerifyAll` passed 23/23 after the slice.
    [x] UnitBattlefieldBuildingTarget BuildSpec helper deletion:
        deleted the private `BuildingSpec(UnitBattlefieldBuildingTarget)` helper.
        The remaining building-combat weapon check and building-death footprint
        payload now read static data directly through
        `BuildSpecCatalog.For(building.Kind)`, keeping BuildSpec authority explicit
        instead of routing through a wrapper-shaped helper. `ReviewGate
        buildingtargetbuildspechelperdeleted` locks this deletion boundary; full
        `VerifyAll` passed 23/23 after the slice.
    [x] UnitBattlefieldBuildingTarget combat helper internal id cleanup:
        internal building attack filtering now resolves `BuildSpec` from the
        already-id-based building command paths and passes that spec into
        `CanUnitTarget(UnitInstance, BuildSpec)` /
        `CanWeaponTarget(WeaponDefinition, BuildSpec)` instead of passing
        `UnitBattlefieldBuildingTarget` wrappers through combat legality helpers.
        The remaining building damage helper is also BuildSpec-backed. `ReviewGate
        buildingtargetcombathelperinternalid` locks this internal combat-helper
        boundary; full `VerifyAll` passed 23/23 after the slice.
    [x] M1 migration parent completion:
        `BuildingDefinition.cs`, `BuildDefinition.cs`, `BuildCatalog.cs`, and
        `UnitBattlefieldBuildingTarget.cs` are deleted, and `scripts/**/*.cs`
        no longer references those deleted compatibility/runtime symbols.
        `ReviewGate m1migrationparentcomplete` locks the parent boundary so the
        second building runtime cannot return while M1 continues toward final
        `UnitKind` / `BuildingKind` deletion.
    [x] GameState.Definitions public surface cleanup:
        the legacy unit definition table is no longer exposed as
        `GameState.Definitions`. Follow-up UnitSpec cleanup removed the temporary
        `GameState.UnitDefinitionFor(...)`, `HasUnitDefinition(...)`,
        `UnitDefinitionValues`, `UnitDefinitionEntries`, and
        `LegacyUnitDefinitions` surfaces; `GameState` now resolves legacy
        `UnitKind` runtime data through `UnitKindDesignBridge` descriptors.
[ ] Delete legacy once the entity path owns gameplay: `UnitKind`, `BuildingKind`,
    `UnitCatalog`.

