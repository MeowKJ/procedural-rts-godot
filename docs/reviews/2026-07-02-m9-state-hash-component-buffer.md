# Review Record - M9 state hash component buffer

Step: Reuse deterministic state-hash component ordering storage
Milestone: M9 Elegance, Decoupling, Performance
Owner AI: Codex
Reviewer AI: ReviewGate regression / SimReplay
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/entities/EntityComponentSet.cs`, `scripts/core/entities/EntityWorld.cs`, `tools/ReviewGateDomains/RegressionReviewGate.cs`, `TODO.md`.
- Non-goals: changing hash inputs, component state fields, gameplay behavior, or the queue/mount ordering logic inside `EntityStateHash`.

Implementation summary:
- Added `EntityComponentSet.StableValuesInto(...)`, which fills a caller-owned list and sorts by component runtime type full name, matching the old `StableValues` order.
- Added an `EntityWorld` reusable `_stateHashComponentValues` buffer and routed `DeterministicStateHash()` through it instead of allocating `entity.Components.StableValues` per entity.
- Added `ReviewGate regression` evidence that requires the reusable hash buffer path and forbids `entity.Components.StableValues` from returning to `DeterministicStateHash()`.
- Updated TODO evidence under the M9 allocation paydown parent.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: sequential rerun passed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: deterministic replay suite passed; existing hashes remained stable, including `production-loop` `427000301860631748`, `combat` `3773882959108536546`, and `outcome` `18128435059327466148`.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- regression --max-warnings=0`
  Result: pass
  Evidence: sequential rerun completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize --max-warnings=0`
  Result: pass
  Evidence: file-size gate completed with 0 errors and 0 warnings; `RegressionReviewGate.cs` remains at 200 lines.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-state-hash-component-buffer`
  Result: pass
  Evidence: required review record gate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- all --max-warnings=0`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings.
- Command: `GODOT_BIN=$(command -v godot-dotnet) DOTNET_ROLL_FORWARD=Major sh tools/verify-all.sh`
  Result: pass
  Evidence: full grouped verification completed 23/23.

Manual/visual gates:
- Check: GUI visual QA
  Result: not run
  Evidence: this was a deterministic validation allocation slice; no rendering behavior changed.

Reviewer result:
- Status: pass.
- Required fixes: none.
- Residual risks: `StableValues` remains as a general read API and still allocates when callers explicitly request it; this slice only removes it from the deterministic hash path. Broader EntityStateHash ordering allocations remain future #10 child work.

TODO update:
- Items marked done: none.
- Items left open: `Per-tick allocation paydown`.
- Reason: #124 closes one state-hash allocation child slice, but #10 remains a broad allocation paydown tracker.
