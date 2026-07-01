Step: Flip UnitInstanceView to EntityWorld projection by default.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/BattleRoot.cs`, `tools/VerifyAll/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Changed `UseEntityWorldUnits` so unit views read `EntityProjection` by default.
- Kept an explicit legacy opt-out with `PROCEDURAL_RTS_USE_ENTITY_WORLD_UNITS=0`.
- Added a VerifyAll Godot headless smoke for the legacy opt-out path.
- Non-goals: no mobile unit simulation authority flip, no deletion of legacy `UnitInstance` fields, no visual redesign.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj unitviewdefaulton --no-restore`
  Result: pass
  Evidence: dedicated unit view default-on gate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj verifyall --no-restore`
  Result: pass
  Evidence: VerifyAll contract gate requires the legacy unit-view opt-out Godot smoke.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed with turret state, economy, enemy AI, and outcome scenarios intact.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings after the record update.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 16 steps successfully, including default Battle headless, EntityProjection-enabled Battle headless, and legacy unit-view opt-out Battle headless.

Reviewer result:
- Status: pass pending gates.
- Design note: the default view authority moves forward, but the opt-out smoke keeps a quick rollback path while runtime authority migration continues.
- Required fixes: none.

Status:
- Pass.

Residual risks:
- Headless smoke proves boot stability, not pixel parity. Visual comparison remains a future QA slice.
- Unit behavior is still synced from legacy runtime into EntityWorld mirrors until the later simulation authority slices retire legacy behavior.

TODO update:
- Marked done: nested M1 slice `UnitInstanceView UseEntityWorldUnits default-on flip`.
- Marked done: parent M1 unit EntityProjection view bridge.
