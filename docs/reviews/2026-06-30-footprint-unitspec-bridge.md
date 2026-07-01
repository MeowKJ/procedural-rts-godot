# Review Record - FootprintLayer UnitSpec bridge

Step: UnitSpec architecture phase 3 duplicate-data cleanup FootprintLayer read-path slice
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: ReviewGate footprintunitspecbridge
Integrator AI: Codex

Scope:
- Files/folders: `scripts/world/FootprintLayer.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-footprint-unitspec-bridge.md`.
- Non-goals: deleting `UnitKind`, deleting `UnitCatalog`, changing `FootprintVisualMath` compatibility QA, changing simulation movement/fog authority, changing `SelectionController`, or migrating the remaining legacy runtime presentation paths.

Implementation summary:
- Replaced `FootprintLayer` live footprint style reads from `State.Definition(unit)` with a local UnitSpec-backed resolver.
- `FootprintLayer` now resolves `UnitModel.Kind` through `UnitKindDesignBridge.TryGetSpec(...)` and `TryGetRuntimeDescriptor(...)`, using runtime descriptor radius, movement domain, and weight class for mark geometry.
- Resource-worker tinting now comes from UnitSpec role tags and art cargo metadata instead of legacy unit-kind branches.
- Footprint mark color uses UnitSpec-derived descriptor accent through `State.VisualAccent(...)` with a light owner-art tint; relation colors remain out of footprint mark styling.
- Mark fog visibility now uses owner relations through `GameState.OwnerRelation(...)` instead of `FactionCatalog.DefaultFactionForOwner` or faction identity.
- Added `ReviewGate footprintunitspecbridge` to lock this live presentation read path.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- footprintunitspecbridge`
  Result: pass
  Evidence: ReviewGate footprintunitspecbridge completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=footprint-unitspec-bridge`
  Result: pass
  Evidence: ReviewGate review found this durable record and completed with 0 errors and 0 warnings.

Manual/visual gates:
- Check: live visual screenshot pass
  Result: not run
  Evidence: this scoped change preserves the existing draw primitives, budgets, and movement thresholds while swapping metadata sources; follow-up visual QA remains useful for broad presentation tuning.

Reviewer result:
- Status: pass-with-warnings
- Required fixes: none in this scoped slice.
- Residual risks: `FootprintLayer` still consumes legacy `UnitModel.Kind` as a compatibility bridge input, and the legacy `FootprintVisualMath.StyleFor(UnitDefinition)` compatibility API remains for existing QA/readability coverage until the broader duplicate-data cleanup deletes it.

TODO update:
- Items marked done: none.
- Items left open: parent UnitSpec duplicate-data cleanup remains open.
- Reason: this removes one live FootprintLayer read path, but M1 duplicate-data cleanup still has other legacy compatibility surfaces.
