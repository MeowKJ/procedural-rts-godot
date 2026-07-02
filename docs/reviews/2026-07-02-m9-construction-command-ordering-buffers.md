# Review Record - M9 ConstructionSystem command ordering buffers

Step: Reuse ConstructionSystem construction command ordering buffers
Milestone: M9 Elegance, Decoupling, Performance
Owner AI: Remote Linux Codex
Reviewer AI: SimReplay / ReviewGate simhot
Integrator AI: Remote Linux Codex

Scope:
- Issue: #71.
- Files/folders: `scripts/core/sim/systems/ConstructionSystem.cs`, `scripts/core/sim/systems/construction/ConstructionSystem.Commands.cs`, `scripts/core/sim/systems/construction/ConstructionSystem.Queries.cs`, `scripts/core/sim/systems/construction/ConstructionSystem.Spec.cs`, `scripts/core/sim/systems/construction/ConstructionSystem.Ordering.cs`, `tools/ReviewGateDomains/RegressionReviewGate.cs`, `TODO.md`, `docs/reviews/2026-07-02-m9-construction-command-ordering-buffers.md`.
- Non-goals: changing construction prerequisites, required producer lookup, ready-ticket behavior, cancel refund behavior, queue ticket positioning, placement validation, BuildSpec authoring, UI preview, balance values, or closing the broad #10 allocation paydown item.

Implementation summary:
- Added reusable `ConstructionSystem` buffers for required-building ordering and construction command subject ordering.
- Added focused ordering helpers that copy required buildings or subjects into caller-owned buffers and sort them deterministically.
- Replaced `RequiredBuildings.OrderBy(...)` and `Subjects.OrderBy(...)` call sites in construction queue/start validation, required producer lookup, cancel construction, and queue ticket positioning.
- Extended broad `ReviewGate simhot` evidence so the construction command ordering paths cannot regress to the removed LINQ `OrderBy(...)` paths.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: main project build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay passed, including construction-loop, construction-queue-ready, construction-ready-placement, construction-cancel, construction-power-gate, construction-visibility, and construction lifecycle deterministic scenarios.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed weapon hit rules, turret states, terrain passability, localization fallback, presentation descriptors, shared threat propagation, rally production, economy, enemy AI, and outcomes.
- Command: `dotnet run --project tools/PerfSmoke/PerfSmoke.csproj --no-restore`
  Result: pass
  Evidence: 400-unit run averaged 11.093ms, p99 11.820ms, max 12.696ms, and 192620 bytes/tick, under the 16.667ms active budget.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- simhot --max-warnings=0`
  Result: pass
  Evidence: ReviewGate simhot completed with 0 errors and 0 warnings, including the new Construction command ordering buffer checks.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings after updating the validation-tool source budget lock to 17793 total tool lines.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-construction-command-ordering-buffers`
  Result: pass
  Evidence: review-record gate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 23 steps successfully, including build, SimReplay, CombatBehavior, ReviewGate, PerfSmoke, balance/counter QA, and Godot headless QA. Release PerfSmoke inside VerifyAll reported 400-unit avg 2.962ms and 192620 bytes/tick.

Reviewer result:
- Status: pass.
- Required fixes: none.

Status:
- Complete for #71 after integration gates.

Residual risks:
- This removes one Construction command ordering allocation family only. Remaining #10 debt still includes immutable queue/path arrays and broader profiler-guided GC cleanup.

TODO update:
- Items marked done: none.
- Items left open: `Per-tick allocation paydown`.
- Reason: #71 narrows one allocation family but does not complete the broad M9 allocation-debt item.
