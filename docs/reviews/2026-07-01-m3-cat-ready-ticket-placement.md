# Review Record - M3 Cat ready-ticket placement

Step:
Consume Cat sidebar ready tickets through the shared construction backend.

Milestone:
M3 Build & Construction System.

Owner AI:
Codex main agent.

Reviewer AI:
Harvey and Socrates read-only M3 audits, SimReplay, PlayerLoopQa, and ReviewGate.

Integrator AI:
Codex main agent.

Scope:
- Files/folders: `scripts/core/entities/EntityCommand.cs`, `scripts/core/sim/systems/construction/ConstructionSystem.Commands.cs`, `scripts/core/sim/systems/construction/ConstructionSystem.Queries.cs`, `scripts/core/sim/systems/construction/ConstructionSystem.Spec.cs`, `scripts/core/units/runtime/UnitBattlefieldConstructionTicketSnapshot.cs`, `scripts/core/units/runtime/battlefield/UnitBattlefield.ConstructionTickets.cs`, `scripts/core/units/runtime/battlefield/UnitBattlefield.EntityWorldSystems.cs`, `scripts/controllers/BuildPlacementController.cs`, `tools/SimReplay/Economy/ConstructionReadyPlacementScenarios.cs`, `tools/SimReplay/Core/ReplayPrelude.cs`, `tools/PlayerLoopQa/Program.cs`, `TODO.md`, `docs/reviews/2026-07-01-m3-cat-ready-ticket-placement.md`.
- Non-goals: no HUD sidebar redesign, no Dog deploy-unit UX, no shared restart/capture flow, no balance changes, and no destroyed-terminal construction lifecycle.

Implementation summary:
- Extended `StartConstructionEntityCommand` with optional `ReadyTicket`, preserving one backend command for Dog/Cat/shared construction methods.
- Split construction validation so ready-ticket placement reuses spatial placement checks without recharging credits or requiring producer/tech a second time.
- Ready-ticket placement now rejects invalid positions without consuming the ticket, and consumes the ticket immediately on success before spawning a complete BuildSpec-backed building.
- Added live `UnitBattlefield` facade APIs for queueing, listing, and placing construction tickets; live queued tickets now advance because queued construction phases are stepped.
- Updated build preview drawing to use live `UnitBattlefield.ValidateBuildingPlacement`, matching confirmation logic.
- Added deterministic `construction-ready-placement` coverage and live `PlayerLoopQa` coverage for Cat ready-ticket queue -> ready -> invalid placement -> successful placement.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass.
  Evidence: `OK [construction-ready-placement]: rejected 1, placed 4, credits 580.`
- Command: `dotnet run --project tools/PlayerLoopQa/PlayerLoopQa.csproj --no-restore`
  Result: pass.
  Evidence: PlayerLoopQa passed with `cat ready-ticket placement` in the covered loop.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- m3catreadyticketplacement`
  Result: pass.
  Evidence: ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m3-cat-ready-ticket-placement`
  Result: pass.
  Evidence: ReviewGate found this durable review record.

Manual/visual gates:
- Check: Not applicable.
  Result: not run.
  Evidence: this slice is backend/live facade behavior covered by deterministic and player-loop QA.

Reviewer result:
- Status: pass.
- Required fixes: none.
- Residual risks: Cat still needs real sidebar HUD controls; Dog deploy and shared restart/capture remain separate open M3 work.

TODO update:
- Items marked done: Cat method ready-ticket placement portion of faction-distinct construction.
- Items left open: Dog method, shared restart/capture, HUD sidebar UX, and destroyed-terminal construction lifecycle.
- Reason: Cat queue-ready tickets now have a complete backend and live placement path, but broader M3 faction UX is not fully complete.
