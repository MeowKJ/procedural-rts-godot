Step: Move UnitBattlefield building presentation metadata to BuildSpec authority.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/BuildSpec.cs`, `scripts/core/entities/BuildingTargetEntityBridge.cs`, `scripts/core/units/runtime/UnitBattlefield.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Added BuildSpec-backed building presentation identity: `NameKey`, `RoleKey`, `ShortCode`, and `RoleGlyph`.
- `BuildingTargetEntityBridge` now creates `EntityDisplaySpec` from `BuildSpec` display metadata instead of a private short-code table.
- UnitBattlefield building selection HUD projections now read icon, short code, and accent from `BuildSpec`.
- UnitBattlefield building hit-pulse projections now read accent from `BuildSpec`.
- Non-goals: deleting `BuildingPresentationCatalog`, replacing `PresentationCatalog.Building`, deleting `BuildingKind`, or migrating legacy `GameState` building presentation paths.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj buildingpresentationbuildspecauthority --no-restore`
  Result: pass
  Evidence: dedicated BuildSpec presentation authority gate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj buildingselectionhud --no-restore`
  Result: pass
  Evidence: building selection HUD gate completed with 0 errors and 0 warnings after requiring BuildSpec icon/short-code reads.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj buildinghitpulseprojection --no-restore`
  Result: pass
  Evidence: building hit-pulse projection gate completed with 0 errors and 0 warnings after requiring BuildSpec accent reads.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed with BuildSpec display metadata assertions intact.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 16 steps successfully, including SimReplay, CombatBehavior, PerfSmoke, BalanceReport, and Godot headless QA.

Reviewer result:
- Status: pass.
- Design note: this is a bounded prerequisite for deleting `BuildingPresentationCatalog`; it moves UnitBattlefield's live building projections to BuildSpec authority without disturbing old GameState presentation fallbacks.
- Required fixes: none identified after gates.

Status:
- Pass.

Residual risks:
- `BuildingPresentationCatalog` still exists and remains used by `PresentationCatalog.Building`, old HUD paths, and legacy presentation tests.
- `BuildingKind` remains the stable building key for BuildSpec and build placement.
- The next deletion slice should remove or replace `PresentationCatalog.Building` callers before deleting `BuildingPresentationCatalog`.

TODO update:
- Marked done: nested M1 slice `UnitBattlefield building presentation BuildSpec authority cleanup`.
