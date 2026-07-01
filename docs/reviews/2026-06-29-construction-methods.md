# Review Record - Construction methods metadata

Step:
M3 faction-distinct construction methods minimal backend data slice.
Milestone:
Faction-distinct construction method metadata without runtime construction forks.
Owner AI:
Parallel coding Worker E.
Reviewer AI:
Codex self-review with SimReplay and ReviewGate coverage.
Integrator AI:
Pending human/integrator review.

Scope:
- Files/folders: `scripts/core/BuildSpec.cs`, `scripts/core/BuildSpecCatalog.cs`, `tools/SimReplay/Program.cs`, `tools/ReviewGate/Program.cs`, `docs/reviews/2026-06-29-construction-methods.md`.
- Non-goals: UI placement controls, production queue/deploy behavior, placement math changes, CombatSystem, UnitDesignDefinitionCatalog, HudLayer, and TODO.md updates.

Implementation summary:
- Added `ConstructionMethodKind`, `BuildPlacementMode`, `ConstructionMethod`, `FactionConstructionPolicy`, and `BuildConstructionPolicy` metadata next to `BuildSpec`.
- Added Dog deploy-in-place, Cat sidebar-placement, and Shared restart-capture method records, all pointing at `EntityCommandKind.Build` and `StartConstructionEntityCommand`.
- Added `BuildSpec` and `BuildSpecCatalog` method lookup helpers so faction policy can be queried without changing construction runtime state.
- Added SimReplay `construction-methods` coverage proving Dog/Cat metadata differ while both starts still use the same `StartConstructionEntityCommand` and `ConstructionComponentState` backend path.
- Added ReviewGate `constructionmethods` static coverage to guard method metadata and reject Dog/Cat-specific construction system forks.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
  Evidence: Build succeeded with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass.
  Evidence: `SimReplay PASSED`; `OK [construction-methods]: dog DeployInPlace, cat SidebarPlacement, shared RestartCapture, backend StartConstructionEntityCommand, starts 2.`
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj constructionmethods --no-restore`
  Result: pass.
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`

Manual/visual gates:
- Check: Not applicable.
  Result: pass.
  Evidence: Pure backend metadata and deterministic replay slice; no presentation surface changed.

Reviewer result:
- Status: pass
- Required fixes: None from self-review.
- Residual risks: UI placement, deploy-in-place semantics, restart-capture UX, and per-faction build queues remain future work; this slice only records backend method metadata and proves the existing construction backend accepts multiple method-tagged starts.

TODO update:
- Items marked done: None; TODO.md intentionally untouched for this worker slice.
- Items left open: Broader M3 faction-distinct construction method UX and queue/deploy behavior remain open.
- Reason: This is a minimal data/replay/gate slice for backend method metadata.
