# Review Record - External production definitions cleanup

Step: UnitSpec architecture phase 3 duplicate-data cleanup external production display slice
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: ReviewGate externalproductiondefinitionscleanup / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `scripts/BattleRoot.cs`, `scripts/ui/HudLayer.cs`, `scripts/world/BuildingView.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-external-production-definitions-cleanup.md`.
- Non-goals: deleting `GameState.ProductionDefinitions`, changing production runtime behavior, changing HUD layout, or changing UnitBattlefield production details.

Implementation summary:
- Moved old-runtime production-complete labels and alerts in `BattleRoot` from `GameState.ProductionDefinitions` to `ProductionKindDesignBridge.LegacySpecFor(...)`.
- Moved old-runtime selected-building production detail text in `BattleRoot` to legacy UnitSpec compatibility specs.
- Kept UnitBattlefield queue detail text design-specific by reading `UnitDesignCatalog.Spec(item.DesignId)`.
- Moved `HudLayer` legacy command-button output icons/tooltips to legacy UnitSpec compatibility specs plus `UnitPresentationCatalog.ForProductionSpec(...)`.
- Moved `BuildingView` production progress bars to legacy `ProductionSpec.Duration`.
- Added `ReviewGate externalproductiondefinitionscleanup` to keep these external display paths off `GameState.ProductionDefinitions`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed weapon hit rules, turret states, terrain passability, localization fallback, presentation descriptors, shared threat propagation, rally production, economy, enemy AI, and outcomes.
- Command: `dotnet run --project tools/PlayerLoopQa/PlayerLoopQa.csproj --no-restore`
  Result: pass
  Evidence: PlayerLoopQa passed build radius, harvest/bank, T1-T3 production, rally, selection, move/attack/stance, victory and defeat.
- Command: `dotnet run --project tools/DesktopHudQa/DesktopHudQa.csproj --no-restore`
  Result: pass
  Evidence: DesktopHudQa passed 1280x720, 1600x900, 1920x1080, high-DPI layout constraints, and HUD UiFactory extraction.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- externalproductiondefinitionscleanup`
  Result: pass
  Evidence: ReviewGate externalproductiondefinitionscleanup completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=external-production-definitions-cleanup`
  Result: pass
  Evidence: required review record check completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed 23/23 checks after the slice.

Manual/visual gates:
- Check: visual inspection not required; HUD and building display behavior is covered by automated headless/HUD checks for this metadata-source migration.
  Result: not run.
  Evidence: no layout or draw geometry changed.

Reviewer result:
- Status: pass
- Required fixes: none after automated gates.
- Residual risks: `GameState.ProductionDefinitions` static compatibility table remains until a final deletion slice proves no callers need it.

TODO update:
- Items marked done: none.
- Items left open: parent UnitSpec duplicate-data cleanup remains open.
- Reason: this removes external callers, not the static table definition.
