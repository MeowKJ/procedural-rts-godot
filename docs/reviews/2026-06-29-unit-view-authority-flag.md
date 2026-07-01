Step: Gate UnitInstanceView EntityProjection reads behind a default-off UseEntityWorldUnits flag.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/world/UnitInstanceView.cs`, `scripts/BattleRoot.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Added a `ProjectionEnabledProvider` to `UnitInstanceView` so projection reads can be switched off without removing the projection provider wiring.
- Added `BattleRoot.UseEntityWorldUnits`, enabled only by `PROCEDURAL_RTS_USE_ENTITY_WORLD_UNITS=1`, and wired unit views to that flag.
- Non-goals: no unit authority flip, no movement/combat rewrite, no legacy unit behavior deletion.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj unitviewauthorityflag --no-restore`
  Result: pass
  Evidence: Unit view authority flag gate completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed successfully.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 14 steps successfully.

Reviewer result:
- Status: pass.
- Design note: this restores the TODO's intended default-off authority switch while preserving existing projection wiring for controlled comparisons.
- Required fixes: none.

Status:
- Pass.

Residual risks:
- The actual `UseEntityWorldUnits` flip remains open and should require drift checks under gameplay load before enabling by default.
- Runtime HUD surfacing for projection drift remains future work.

TODO update:
- Marked done: nested M1 slice `UnitInstanceView UseEntityWorldUnits authority flag`.
- Left open: parent M1 unit authority flip and legacy unit behavior deletion.
