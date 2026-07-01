# Review Record - M1 legacy kind deletion

Step: M1 legacy kind deletion
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex plus worker agents
Reviewer AI: ReviewGate and VerifyAll
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/`, `scripts/core/build/`,
  `scripts/core/entities/`, `scripts/core/sim/`, `scripts/core/production/`,
  `scripts/core/units/runtime/`, `tools/CombatBehavior/`, `tools/SimReplay/`,
  `tools/AiOpponentLoopQa/`, `tools/ReviewGate/ContentAuthoringReviewGate.cs`,
  `TODO.md`, `docs/reviews/2026-07-01-m1-legacy-kind-deletion.md`.
- Non-goals: changing unit balance, changing movement/combat feel, adding new
  content, or redesigning UI.

Implementation summary:
- Deleted `scripts/core/units/UnitKind.cs`,
  `scripts/core/units/UnitKindDesignBridge.cs`, and
  `scripts/core/build/BuildingKind.cs`.
- Kept unit runtime identity native: `UnitModel.Kind` now aliases `DesignId`,
  GameState seeding no longer writes legacy projections, and presentation reads
  unit metadata by design id.
- Added `BuildingDesignIds` and converted `BuildSpec`, `BuildSpecCatalog`,
  building models, construction commands/events, identity components, snapshots,
  production lanes/options, presentation projections, and QA tools to building
  spec ids.
- Updated CombatBehavior, SimReplay, AiOpponentLoopQa, sandbox spawning,
  construction replay, and roster assertions to validate UnitSpec/BuildSpec
  design-id identity directly.
- Added ReviewGate file locks forbidding `UnitCatalog.cs`, `UnitKind.cs`,
  `UnitKindDesignBridge.cs`, and `BuildingKind.cs`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: main Godot C# project compiled with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed unit, production, relations, terrain,
  presentation, enemy AI, and outcome checks.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: ReviewGate completed with 0 errors and 0 warnings after legacy file
  deletion locks were added.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: final VerifyAll rerun completed all 23 steps successfully after the
  migration, TODO update, and review record format fix.

Manual/visual gates:
- Check: source search for legacy runtime identity
  Result: pass
  Evidence: code references to `UnitKind`, `BuildingKind`, `UnitKindDesignBridge`,
  `LegacyKind`, and conversion helpers are gone from runtime/tool code; remaining
  mentions are ReviewGate guard strings or explanatory assertion messages.

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: `UnitModel.Kind` remains as a temporary string alias for
  `DesignId` so older call sites can be migrated gradually without restoring the
  enum. Some assertion messages still mention the deleted names to explain the
  regression being guarded.

TODO update:
- Items marked done: UnitSpec phase-3 duplicate-data cleanup; final legacy
  `UnitKind` / `BuildingKind` / `UnitCatalog` deletion.
- Items left open: none under active M1 summary.
- Reason: EntityWorld/UnitSpec/BuildSpec design-id identity now owns the live
  gameplay, tooling, and QA paths, with ReviewGate preventing legacy file return.
