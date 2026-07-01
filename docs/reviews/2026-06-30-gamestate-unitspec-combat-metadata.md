# Review Record - GameState UnitSpec combat metadata cleanup

Step: UnitSpec architecture phase 3 GameState combat metadata slice
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: ReviewGate gamestateunitspeccombatmetadata / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/GameState.cs`, `scripts/core/WeaponTargetProfile.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-gamestate-unitspec-combat-metadata.md`.
- Non-goals: changing balance values, deleting `UnitDefinition`, migrating building combat metadata, or rewriting combat behavior.

Implementation summary:
- `WeaponTargetProfile` now accepts `UnitSpecRuntimeDescriptor` for unit target legality and priority.
- `GameState` live unit weapon lookup, spawn HP, unit target legality, target priority, damage multiplier, ballistic target weight, passive-retaliate range, and under-attack unit label reads now use `UnitRuntimeDescriptorFor(...)`.
- Legacy `UnitDefinition` overloads remain for compatibility tools while live `GameState` unit combat metadata reads no longer depend on them.
- Added `ReviewGate gamestateunitspeccombatmetadata` to prevent these paths from regressing to direct legacy `UnitDefinition` reads.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors before gate wiring.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- gamestateunitspeccombatmetadata`
  Result: pass
  Evidence: ReviewGate gamestateunitspeccombatmetadata completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=gamestate-unitspec-combat-metadata`
  Result: pass
  Evidence: required review record check completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll passed 23/23 steps, including SimReplay, CombatBehavior, FogOfWarQa, PerfSmoke, BalanceReport, CounterReadabilityQa, and Godot headless QA.

Manual/visual gates:
- Check: visual inspection not required for this narrow runtime data-source migration.
  Result: not run.
  Evidence: combat formulas and target-profile rules remain unchanged; only the source of unit metadata moved to UnitSpec descriptors.

Reviewer result:
- Status: pass
- Required fixes: none known before gate execution.
- Residual risks: `GameState` still keeps the legacy `Definition(UnitModel)` accessor and public `UnitDefinition` compatibility overloads for tools and migration call sites. Building combat metadata remains a later BuildSpec cleanup slice.

TODO update:
- Items marked done: none, because the broad UnitSpec duplicate-data cleanup remains open.
- Items left open: UnitSpec architecture phase 3 duplicate-data cleanup.
- Reason: this completes one bounded combat metadata read-path slice, not deletion of all legacy UnitKind/UnitDefinition compatibility.
