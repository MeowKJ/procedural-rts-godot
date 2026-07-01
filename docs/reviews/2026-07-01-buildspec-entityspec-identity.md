# Review Record - BuildSpec EntitySpec identity cleanup

Step:
- BuildSpec EntitySpec identity cleanup

Milestone:
- M1 EntityWorld authority / Migration cleanup

Owner AI:
- Codex

Reviewer AI:
- Codex; ReviewGate buildspecentityspecidentity

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/BuildSpec.cs
  - scripts/core/BuildSpecCatalog.cs
  - scripts/core/entities/BuildingTargetEntityBridge.cs
  - scripts/core/SandboxSpawnAuthoring.cs
  - tools/CombatBehavior/Program.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-07-01-buildspec-entityspec-identity.md
- Non-goals:
  - Do not delete `BuildingKind` in this slice.
  - Do not change building balance, costs, build times, footprint sizes, weapon
    assignments, construction UX, production, UI, or art.
  - Do not change EntityWorld runtime component semantics.

Implementation summary:
- Added explicit `EntitySpecId` authoring data to `BuildSpec`.
- Added stable `building.*` ids to every `BuildSpecCatalog` definition, preserving
  the previous generated ids.
- Changed `BuildingTargetEntityBridge.ToEntitySpec(...)` to assign
  `EntitySpec.Id` from `BuildSpec.EntitySpecId`.
- Removed the bridge-local `SpecId(BuildingKind)` id generator.
- Changed `SandboxSpawnAuthoring` to expose and resolve BuildSpec entries by
  `BuildSpec.EntitySpecId` instead of keeping a separate id generator.
- Added CombatBehavior assertions that BuildSpec ids are unique and round-trip
  through `ToEntitySpec()`.
- Added `ReviewGate buildspecentityspecidentity`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors after the
    BuildSpec constructor/catalog migration.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors after the
    new ReviewGate mode.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildspecentityspecidentity`
  Result: pass
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildspecbridge`
  Result: pass
  Evidence: ReviewGate passed with 0 errors and 0 warnings after the historical
    BuildSpec bridge gate was updated to require BuildSpec-owned ids.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay PASSED.
- Command: `dotnet run --project tools/SandboxSpawnAuthoringQa/SandboxSpawnAuthoringQa.csproj --no-restore`
  Result: pass
  Evidence: SandboxSpawnAuthoringQa PASSED with 34 entries, 34 specs, 5
    buildings, and 3 turrets.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildspec-entityspec-identity`
  Result: pass
  Evidence: ReviewGate passed with 0 errors and 0 warnings after evidence
    backfill.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll PASSED, 23/23 steps.

Manual/visual gates:
- Check: Visual/UI review
  Result: not applicable
  Evidence: This slice preserves existing building ids and changes only authoring
    ownership of those ids.

Reviewer result:
- Status: pass after integration review.
- Required fixes:
  - None.
- Reviewer notes:
  - Stable id strings were preserved to avoid meaningless replay or artifact churn.
- Residual risks:
  - `BuildingKind` still exists as a legacy/runtime identity enum until the later
    entity-path deletion milestone.
  - This slice does not remove the remaining `_buildingTargetSeedsById` fallback
    state in `UnitBattlefield`.

TODO update:
- Items marked done:
  - BuildSpec EntitySpec identity cleanup
- Items left open:
  - Broader Migration cleanup and final `BuildingKind` / entity-path deletion
    remain open.
- Reason:
  - This slice makes BuildSpec own EntitySpec identity, but it does not delete the
    old building enum or all live migration state.
