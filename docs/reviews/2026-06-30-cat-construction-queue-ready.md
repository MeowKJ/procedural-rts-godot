# Review Record - Cat construction queue ready

Step: Cat sidebar queued placement ready state.
Milestone: M3 faction-distinct construction methods.
Owner AI: Worker B.
Reviewer AI: Integrator sanity review.
Integrator AI: Main Codex thread.

Scope:
- Files/folders: `scripts/core/entities/EntityCommand.cs`, `scripts/core/entities/EntityComponentState.cs`, `scripts/core/entities/EntityStateHash.cs`, `scripts/core/sim/SimInvariants.cs`, `scripts/core/sim/systems/ConstructionSystem.cs`, `tools/SimReplay/Program.cs`, `tools/ReviewGate/Program.cs`, `docs/reviews/2026-06-30-cat-construction-queue-ready.md`.
- Non-goals: No UI placement mode, no deploy semantics, no restart/capture objective UX, no unit data/catalog changes, no TODO.md updates.

Implementation summary:
- Added `QueueConstructionEntityCommand` for Cat-style pre-placement construction preparation.
- Added `ConstructionPhase` on the existing `ConstructionComponentState`, including `Queued` and `ReadyToPlace`, so the same construction component represents queued sidebar state.
- `ConstructionSystem` now validates tech/producer/credits for queued construction, spends the BuildSpec cost, creates a non-footprint queue ticket, advances it deterministically, and promotes it to `ReadyToPlace`.
- Ready queue tickets are not treated as completed buildings, build-radius anchors, footprint blockers, or tech prerequisites.
- `EntityStateHash` and `SimInvariants` include the phase so replay and malformed ready states are guarded.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: Pass.
  Evidence: Build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: Pass.
  Evidence: `SimReplay PASSED`; `OK [construction-queue-ready]: ticket 2 phase ReadyToPlace, credits 700, rejected 1.`
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj constructionqueueready --no-restore`
  Result: Pass.
  Evidence: `ReviewGate passed` with 0 errors and 0 warnings.

Manual/visual gates:
- Check: None.
  Result: Not applicable.
  Evidence: Pure simulation/data slice.

Reviewer result:
- Status: pass with residual risk.
- Required fixes: None for this slice.
- Residual risks: Ready tickets are not yet consumed by a placement command; UI placement handoff remains future work. Cancellation of ready tickets currently uses remaining-progress refund math, so a fully ready ticket refunds zero until Cat-specific refund semantics are designed. Shared restart/capture objective structures remain open.

TODO update:
- Items marked done: None by Worker B.
- Items left open: Broader M3 faction-distinct construction UX, full Cat placement handoff, Dog deploy semantics, and shared restart/capture objective structures.
- Reason: This is a narrow backend proof for queued-to-ready state only.
