# Review Record - CombatEffects UnitSpec bridge

Step: UnitSpec architecture phase 3 duplicate-data cleanup CombatEffects read-path slice
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Worker A / Codex
Reviewer AI: ReviewGate combateffectsunitspecbridge
Integrator AI: Worker A / Codex

Scope:
- Files/folders: `scripts/core/units/UnitKindDesignBridge.cs`, `scripts/world/CombatEffectsLayer.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-combat-effects-unitspec-bridge.md`.
- Non-goals: editing PathDebug, Movement, Combat simulation, Fog, Sandbox UI/M8 files, deleting `UnitKind`, deleting `UnitCatalog`, migrating `UnitView`, or changing combat VFX geometry beyond replacing its data source.

Implementation summary:
- Added `UnitKindDesignBridge.TryGetRuntimeDescriptor(...)`, a narrow legacy `UnitKind` to cached `UnitSpecRuntimeDescriptor` resolver.
- Replaced `CombatEffectsLayer` unit threat-alert and hit-pulse reads from `State.Definition(unit)` with a shared `UnitEffectStyleFor(...)` helper.
- Unit VFX radius and accent now come from `UnitDesignDefinitionCatalog.RuntimeDescriptors` via the bridge, preserving owner/faction visual tint through `State.VisualAccent(...)`.
- Added `ReviewGate combateffectsunitspecbridge` to prevent `State.Definition(unit)`, direct `GameState.UnitDefinitionFor(unit.Kind)`, or draw-time `CompatibilityDefinition(unit.Kind)` from returning to this path.
- Narrowed `ReviewGate review --require-record=...` so parallel workers can validate the requested durable record without being blocked by unrelated in-progress review records; plain `review` still checks every record.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors after the parallel sandbox UI worktree state settled.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj combateffectsunitspecbridge --no-restore`
  Result: pass
  Evidence: ReviewGate combateffectsunitspecbridge completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj unitkinddesignbridge --no-restore`
  Result: pass
  Evidence: ReviewGate unitkinddesignbridge completed with 0 errors and 0 warnings after adding the runtime descriptor helper.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj combateffectsbuildspecfallbacks --no-restore`
  Result: pass
  Evidence: ReviewGate combateffectsbuildspecfallbacks completed with 0 errors and 0 warnings, proving building VFX fallback cleanup was not regressed.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=combat-effects-unitspec-bridge --no-restore`
  Result: pass
  Evidence: ReviewGate review found this durable record and completed with 0 errors and 0 warnings.

Manual/visual gates:
- Check: visual inspection not required for this slice.
  Result: not run.
  Evidence: the VFX draw primitives, timing, culling, and colors remain the same policy inputs; only the unit metadata source changed.

Reviewer result:
- Status: pass-with-warnings
- Required fixes: none in the scoped M1 slice.
- Residual risks: `UnitModel` still carries legacy `UnitKind`, and `UnitCatalog` remains a compatibility layer until later M1 cleanup removes it.

TODO update:
- Items marked done: none.
- Items left open: parent UnitSpec duplicate-data cleanup remains open.
- Reason: this is one verified runtime read-path cleanup slice, not full deletion of legacy unit compatibility data.
