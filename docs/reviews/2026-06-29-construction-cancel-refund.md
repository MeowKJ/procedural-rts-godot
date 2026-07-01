# Review Record - Construction cancel/refund

Step:
Construction cancel/refund minimal slice.
Milestone:
EntityWorld construction authority and deterministic replay coverage.
Owner AI:
Worker-M3B.
Reviewer AI:
Codex self-review with SimReplay and ReviewGate coverage.
Integrator AI:
Pending human/integrator review.

Scope:
- Files/folders: `scripts/core/entities/EntityCommand.cs`, `scripts/core/sim/systems/ConstructionSystem.cs`, `scripts/core/sim/SimEvent.cs`, `tools/SimReplay/Program.cs`, `tools/ReviewGate/Program.cs`, `docs/reviews/2026-06-29-construction-cancel-refund.md`.
- Non-goals: UI cancel buttons, faction-specific construction UX, CombatSystem, UnitSpec cleanup, Pathfinding, and TODO.md updates.

Implementation summary:
- Added `CancelConstructionEntityCommand` and `EntityCommandKind.CancelConstruction`.
- ConstructionSystem now cancels owner-matched under-construction buildings, refunds `Cost * RefundRatio * remainingProgress`, emits `ConstructionCancelledEvent`, and queues the entity for removal.
- Completed buildings ignore cancellation and do not refund.
- SimReplay adds `construction-cancel` deterministic coverage for refund, removal, and completed cancel no-op behavior.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
  Evidence: Build succeeded with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass.
  Evidence: `SimReplay PASSED`; `OK [construction-cancel]: refund 123, credits 323, remaining buildings 2.`
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj constructioncancel --no-restore`
  Result: pass.
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`

Manual/visual gates:
- Check: Not applicable.
  Result: pass.
  Evidence: Pure sim-only slice with no presentation surface.

Reviewer result:
- Status: pass
- Required fixes: None.
- Residual risks: Entity removal is queued until the end of the tick, matching existing EntityWorld removal semantics; later same-tick systems may still see the entity until flush.

TODO update:
- Items marked done: None; TODO.md intentionally untouched for this worker slice.
- Items left open: Construction UI cancel affordances and broader deterministic construction QA remain separate.
- Reason: This is a minimal pure-sim cancel/refund slice with narrow replay and ReviewGate coverage.
