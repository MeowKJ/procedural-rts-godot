Step: Add a VerifyAll Godot headless smoke with UseEntityWorldUnits enabled.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `tools/VerifyAll/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Added per-step environment variable support to `VerifyAll`.
- Added `godot-battle-entity-units-headless`, which boots `Battle.tscn` with `PROCEDURAL_RTS_USE_ENTITY_WORLD_UNITS=1`.
- Non-goals: no default flag flip, no scene changes, no unit simulation authority change.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj verifyall --no-restore`
  Result: pass
  Evidence: VerifyAll contract gate requires the entity-unit Godot smoke.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed successfully.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 15 steps successfully, including `godot-battle-entity-units-headless`.

Reviewer result:
- Status: pass.
- Design note: this gives the default-off authority flag a runtime boot proof without changing player-facing defaults.
- Required fixes: none.

Status:
- Pass.

Residual risks:
- The smoke proves boot stability only; it does not yet assert visual parity, drift bounds during a live match, or long-session behavior.
- Full unit authority still remains on the legacy-to-EntityWorld sync path until the later flag flip.

TODO update:
- Marked done: nested M1 slice `UseEntityWorldUnits enabled headless smoke`.
- Left open: parent M1 unit authority flip and legacy unit behavior deletion.
