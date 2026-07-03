# Review Record - M9 EntityWorld Stable Readout Collections

Step: #204 `[M9] Reuse EntityWorld stable readout collections`
Milestone: M9 - Elegance & Decoupling
Owner AI: Remote Linux Codex
Reviewer AI: ReviewGate regression / EntityWorldStableReadoutReviewGate
Integrator AI: Remote Linux Codex

Scope:
- Files/folders: `scripts/core/entities/EntityWorld.cs`, `tools/ReviewGateRuntime/EntityWorldStableReadoutReviewGate.cs`, `tools/ReviewGateDomains/RegressionReviewGate.cs`.
- Non-goals: 不改变 `OrderedEntities` system iteration、entity/spec mutation lifecycle、spawn/remove behavior 或 deterministic hash 语义。

Implementation summary:
- `EntityWorld.StableEntities` and `StableSpecs` now expose sorted dictionary value views as `IReadOnlyCollection<T>` instead of allocating list snapshots.
- Existing callers keep `Count` and LINQ query support through the read-only collection contract.
- `ReviewGate regression` locks the readouts against returning to `_entities.Values.ToList()` / `_specs.Values.ToList()`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass，0 warnings / 0 errors。
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass，full replay suite deterministic。
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- regression --max-warnings=0`
  Result: pass，0 errors / 0 warnings。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-entityworld-stable-readout-collections`
  Result: pass，0 errors / 0 warnings。

Reviewer result:
- Status: pass.
- Required fixes: none currently known.

Residual risks:
- `StableEntities` / `StableSpecs` are live read-only views, not owned snapshots; callers that need mutation-isolated snapshots must allocate explicitly at their boundary.
- Parent #10 remains open for broader allocation paydown.

TODO update:
- Items marked done: none.
- Items left open: broader M9 per-tick allocation paydown remains open under #10 / #58.
