# Review Record - UnitBattlefieldBuildingTarget producer projection read cleanup

Step:
- UnitBattlefieldBuildingTarget producer projection read cleanup

Milestone:
- M1 EntityWorld authority

Owner AI:
- Codex

Reviewer AI:
- Codex

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/units/runtime/UnitBattlefield.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-07-01-buildingtarget-producer-projection-reads.md
- Non-goals:
  - Do not change producer eligibility rules, tech-tier checks, queue ordering,
    production costs, or timing.
  - Do not migrate production completion stepping in this slice.
  - Do not remove the private migration wrapper list yet.

Implementation summary:
- Changed both `CandidateProducerIds(...)` overloads to enumerate
  `BuildingTargetIds()` and immutable `BuildingSnapshot` candidates.
- Changed `ProductionDesignIdCore(...)` to read faction through
  `BuildingIdentity(int)`.
- Changed `HasAnyProductionForCore(...)` to read faction and producer kind through
  `BuildingIdentity(int)`.
- Changed `FirstDesignIdFor(...)`, `ProductionDesignSpecs(...)`, and
  `FactionForSlot(...)` to share identity-first faction lookup while preserving
  the existing unit fallback.
- Added `ReviewGate buildingtargetproducerprojectionreads`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetproducerprojectionreads`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetproducereligibilityinternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay passed.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-producer-projection-reads`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll PASSED, 23/23 steps.

Manual/visual gates:
- Check: Visual/UI review
  Result: not applicable
  Evidence: This slice changes producer candidate read plumbing only.

Reviewer result:
- Status: pass after integration review.
- Required fixes:
  - None.
- Reviewer notes:
  - Candidate producer lookup now shares the EntityWorld-first id/snapshot path.
  - Faction and producer-kind checks now resolve through building identity.
  - Existing unit fallback for owner faction inference remains intact.
- Residual risks:
  - Direct private `Buildings` reads remain in production completion,
    construction, placement, combat, fog/visibility source, dock/refinery, and
    cleanup paths.
  - The private migration wrapper list remains until final M1 deletion.
  - ReviewGate is string/regex-based rather than semantic type analysis.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget producer projection read cleanup
- Items left open:
  - Production completion and remaining non-production building list reads.
- Reason:
  - Producer candidate and faction lookup paths no longer directly scan the
    second building runtime list.
