# Review Record - Sim hot-path separation/combat closeout

Step: Close the sim hot-path TODO by adding the missing SeparationSystem scratch reuse and verifying CombatSystem broadphase.
Milestone: M6 Performance.
Owner AI: Codex.
Reviewer AI: Codex self-review (subagents unavailable in this continuation turn; ReviewGate simhot provides durable source checks).
Integrator AI: Codex.

Scope:
- Files/folders: `scripts/core/sim/systems/SeparationSystem.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-29-simhot-separation-combat-closeout.md`.
- Non-goals: no movement-feel changes, no combat targeting behavior changes, no VFX/presentation pooling work.

Implementation summary:
- `SeparationSystem` now owns a reusable deterministic `_buckets` `SortedDictionary` and clears bucket lists each tick instead of allocating a new bucket map inside `Step`.
- `ReviewGate simhot` now checks SeparationSystem bucket reuse alongside the existing Movement, Vision, Combat, event-drain, and PerfSmoke instrumentation checks.
- Current CombatSystem broadphase was audited as already present: `_targetGrid`, `BuildTargetGrid`, spatial `Cell`, and deterministic target tie-breaking are covered by the gate.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: full deterministic replay suite passed after rerunning sequentially to avoid dotnet temp DLL locks.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj simhot --no-restore`
  Result: pass
  Evidence: ReviewGate reported 0 errors and 0 warnings.
- Command: `dotnet run --project tools/PerfSmoke/PerfSmoke.csproj -c Release --no-restore`
  Result: pass
  Evidence: 400-unit average 1.164ms under the 16.667ms budget; 400-unit allocation 115204 bytes/tick.

Manual/visual gates:
- Check: visual QA
  Result: not applicable
  Evidence: simulation-only container reuse; no presentation behavior changed.

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: allocation pressure is lower but not zero; future profiling can still target component record churn, event generation, and other gameplay systems as separate performance slices.

TODO update:
- Items marked done: `Broadphase CombatSystem.NearestHostile; reuse scratch buffers in hot systems (Combat/Movement/Separation)`.
- Items left open: broader performance/art items such as unit body batching, fog camera-rect recompute, and VFX families not covered by their current budgets.
- Reason: the named hot systems now have broadphase/reuse coverage, source gates verify the hooks, and PerfSmoke provides current timing/allocation evidence.
