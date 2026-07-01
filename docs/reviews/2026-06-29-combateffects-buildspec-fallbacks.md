Step: Route CombatEffectsLayer building fallback definition reads through BuildSpecCatalog as a bounded M1 migration cleanup slice.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/world/CombatEffectsLayer.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Updated old-runtime building hit-pulse VFX fallback to read building accent and footprint-derived radius from `BuildSpecCatalog`.
- Removed the fallback dependency on `State.Definition(building)` and `State.CombatTargetRadius(CombatTargetKind.Building, ...)` for building VFX geometry.
- Non-goals: no live `UnitBattlefield` hit-pulse projection changes, no projectile/combat authority rewrite, no legacy model deletion.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj combateffectsbuildspecfallbacks --no-restore`
  Result: pass
  Evidence: CombatEffectsLayer BuildSpec fallback gate completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed successfully.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 14 steps successfully.

Reviewer result:
- Status: pass.
- Design note: building VFX fallback now uses the same BuildSpec source as other presentation fallbacks while leaving the live projection path untouched.
- Required fixes: none.

Status:
- Pass.

Residual risks:
- `GameState` still owns old-runtime building combat, vision, and target geometry until the entity path fully replaces gameplay authority.
- Tools and compatibility QA still intentionally read `GameState.BuildingDefinitions` during the migration.

TODO update:
- Marked done: nested M1 slice `CombatEffectsLayer BuildSpec fallback cleanup`.
- Left open: parent M1 legacy deletion and broader old-runtime authority retirement.
