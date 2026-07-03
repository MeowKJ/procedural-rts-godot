# Review Record - M9 UnitSpec Tag Set

Step: #194 `[M9] Replace UnitSpecEntityBridge tag LINQ set`
Milestone: M9 - Elegance & Decoupling
Owner AI: Remote Linux Codex
Reviewer AI: ReviewGate regression / UnitSpecAbilityAllocationReviewGate
Integrator AI: Remote Linux Codex

Scope:
- Files/folders: `scripts/core/entities/UnitSpecEntityBridge.cs`, `tools/ReviewGateRuntime/UnitSpecAbilityAllocationReviewGate.cs`.
- Non-goals: 不改变 UnitSpec 数据、role tag 内容、dedupe、EntitySpec contract、weapons、abilities、movement 或 production。

Implementation summary:
- `UnitSpecEntityBridge.ToEntitySpec(...)` now delegates tag construction to `CreateTags(...)`.
- `CreateTags(...)` fills a `HashSet<string>` with explicit role-tag iteration and then adds the archetype tag.
- `UnitSpecAbilityAllocationReviewGate` locks the bridge against tag `Select/Append/Distinct/ToHashSet` materialization.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass，0 warnings / 0 errors。
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass，EntitySpec conversion remains deterministic in replay coverage。
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass，runtime UnitSpec behavior preserved。
- Command: `dotnet run --project tools/RosterAuthoringQa/RosterAuthoringQa.csproj --no-restore`
  Result: pass，Dog/Cat playable design authoring remains valid。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- regression --max-warnings=0`
  Result: pass，0 errors / 0 warnings。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-unitspec-tag-set`
  Result: pass，record is present with concrete automated gate evidence。

Reviewer result:
- Status: pass.
- Required fixes: none currently known.

Residual risks:
- HashSet iteration order remains unspecified as before; no downstream code should depend on tag order.
- Parent #10 remains open for broader allocation paydown.

TODO update:
- Items marked done: none.
- Items left open: broader M9 per-tick allocation paydown remains open under #10.
