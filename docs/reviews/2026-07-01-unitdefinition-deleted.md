# Review Record - UnitDefinition compatibility deletion

Step:
- UnitDefinition compatibility deletion

Milestone:
- M1 EntityWorld authority / UnitSpec duplicate-data cleanup

Owner AI:
- Codex

Reviewer AI:
- Codex plus read-only explorer subagent; ReviewGate unitdefinitiondeleted

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/UnitDefinition.cs
  - scripts/core/units/UnitDesignDefinitionCatalog.cs
  - scripts/core/units/UnitKindDesignBridge.cs
  - tools/CombatBehavior/Program.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-07-01-unitdefinition-deleted.md
- Non-goals:
  - Do not delete `UnitKind` yet.
  - Do not change unit balance, movement, combat rules, production, art, UI, fog,
    or faction roster data.
  - Do not migrate building/runtime architecture in this slice.

Implementation summary:
- Deleted `scripts/core/UnitDefinition.cs`.
- Removed `UnitDesignDefinitionCatalog.CompatibilityDefinition(...)`.
- Removed `UnitKindDesignBridge.CompatibilityDefinition(...)`.
- Updated CombatBehavior runtime-definition QA to validate
  `UnitSpecRuntimeDescriptor` fields directly.
- Updated generic legacy `UnitKind` bridge QA to prove the old enum maps to
  direct UnitSpec runtime and presentation descriptors instead of a
  compatibility runtime object.
- Updated historical ReviewGate checks so they require descriptor/spec evidence
  rather than requiring deleted compatibility projections.
- Added `ReviewGate unitdefinitiondeleted` to keep the deleted file and helpers
  from returning.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors after
    deleting `UnitDefinition.cs`.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors after
    updating ReviewGate.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- unitdefinitiondeleted`
  Result: pass
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- unitdesigndefinitioncatalog`
  Result: pass
  Evidence: ReviewGate passed with 0 errors and 0 warnings after descriptor-only
    catalog expectations replaced compatibility projection expectations.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- unitkinddesignbridge`
  Result: pass
  Evidence: ReviewGate passed with 0 errors and 0 warnings after the bridge gate
    moved to `TryGetRuntimeDescriptor(...)`.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- genericlegacyunitkind`
  Result: pass
  Evidence: ReviewGate passed with 0 errors and 0 warnings after generic legacy
    coverage moved off compatibility runtime objects.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- combatbehaviorunitspecreadpath`
  Result: pass
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- tiers`
  Result: pass
  Evidence: ReviewGate passed with 0 errors and 0 warnings after tier evidence
    moved to UnitSpec and UnitSpecRuntimeDescriptor.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay PASSED.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=unitdefinition-deleted`
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
  Evidence: This slice deletes a runtime data compatibility type and does not
    change presentation or layout.

Reviewer result:
- Status: pass after integration review.
- Required fixes:
  - None.
- Reviewer notes:
  - The read-only explorer identified the exact helper, CombatBehavior, and
    historical ReviewGate callers that needed to move off `UnitDefinition`.
- Residual risks:
  - `UnitKind` still remains as an old runtime enum until the later entity-path
    deletion milestone.
  - ReviewGate still contains historical text about UnitDefinition as forbidden
    strings; the new deletion gate excludes ReviewGate itself from source scans.

TODO update:
- Items marked done:
  - UnitDefinition compatibility deletion
- Items left open:
  - Broader UnitSpec duplicate-data cleanup and final `UnitKind` entity-path
    deletion remain open.
- Reason:
  - This slice deletes the duplicated runtime-definition compatibility type but
    does not delete the old enum or every legacy runtime bridge.
