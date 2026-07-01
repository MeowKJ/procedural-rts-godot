# Review Record - View authority boundary

Step: Prove world/UI view layers do not mutate authoritative gameplay state.
Milestone: Architecture hard boundaries.
Owner AI: Codex.
Reviewer AI: Codex self-review with static ReviewGate coverage.
Integrator AI: Codex.

Scope:
- Files/folders: `scripts/world`, `scripts/ui`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-29-view-authority-boundary.md`.
- Non-goals: no claim that controllers or `BattleRoot` are authority-free; those remain part of the EntityWorld authority migration and command-buffer TODOs.

Implementation summary:
- Added `ReviewGate viewauthority`.
- The gate scans `scripts/world/**/*.cs` and `scripts/ui/**/*.cs` for writes to authoritative health, movement, target, selection, production queue, economy, and outcome state.
- The gate allows display-only state such as an outcome screen backdrop while rejecting gameplay model mutation APIs such as `SetCredits`, `EnqueueProduction`, `CommandMoveSelected`, and direct model field writes.
- Verified sample view files still behave as draw/read surfaces: `UnitView`, `BuildingView`, and `HudLayer`.

Automated gates:
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj viewauthority --no-restore`
  Result: pass
  Evidence: ReviewGate reported 0 errors and 0 warnings for view authority.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: all static review gates passed after adding `viewauthority`.

Manual/visual gates:
- Check: in-game visual QA
  Result: not run
  Evidence: this is an architecture/static boundary slice; no rendered output changed.

Reviewer result:
- Status: pass
- Required fixes: initial static pattern treated `OutcomeScreenLayer` display state as authoritative outcome; the gate was narrowed to gameplay authority fields and APIs.
- Residual risks: controllers and `BattleRoot` still submit commands and bridge legacy runtimes; this record only proves `scripts/world` and `scripts/ui` view layers do not mutate the listed authoritative gameplay state.

TODO update:
- Items marked done: `Views never mutate authoritative health/movement/target/queue/economy/outcome`.
- Items left open: EntityWorld authority migration, routing live input through `EntityCommandBuffer`, and pure-presentation VFX pooling.
- Reason: world/UI views are now covered by a durable static gate that rejects authoritative gameplay mutations in view-layer files.
