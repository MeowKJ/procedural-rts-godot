Step: Close the M1 live input EntityCommandBuffer parent item after all command slices passed.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `TODO.md`, `tools/ReviewGate/Program.cs`.
- Marked the parent TODO item `Route live input (select/move/attack/stop/stance) through EntityCommandBuffer` complete.
- Added an `inputcommandcomplete` ReviewGate mode that requires the completed parent plus the selection, selected move/attack, stop/stance, and explicit attack-units slices.
- Non-goals: no runtime behavior changes, no legacy movement/combat deletion, no input UI rewrite.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj inputcommandcomplete --no-restore`
  Result: pass
  Evidence: parent completion gate proved all required input command-buffer slices are tracked and locked.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed successfully.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 15 steps successfully.

Reviewer result:
- Status: pass.
- Design note: the parent item now reflects the implemented command-buffer routing evidence instead of remaining stale-open after its child slices passed.
- Required fixes: none.

Status:
- Pass.

Residual risks:
- Live unit movement/combat behavior still runs in legacy `UnitBattlefield.Update`; that is covered by the next M1 parent item, not by this input-routing closure.
- Building rally/production/economy authority continues to be tracked by their own M1 migration items.

TODO update:
- Marked done: parent M1 item `Route live input (select/move/attack/stop/stance) through EntityCommandBuffer`.
- Left open: unit authority flip, harvester/production/building behavior deletion, and BuildSpec/legacy cleanup parents.
