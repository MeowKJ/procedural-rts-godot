# Review Record - Sandbox Spawn Authoring

Step: Add pure authoring data for sandbox spawn-any-spec controls.
Milestone: M8 Sandbox developer controls.
Owner AI: Worker Franklin.
Reviewer AI: ReviewGate sandboxspawn plus SandboxSpawnAuthoringQa.
Integrator AI: Main thread.

Scope:
- Files/folders: `scripts/core/SandboxSpawnAuthoring.cs`, `tools/SandboxSpawnAuthoringQa/SandboxSpawnAuthoringQa.csproj`, `tools/SandboxSpawnAuthoringQa/Program.cs`, `tools/VerifyAll/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-sandbox-spawn-authoring.md`.
- Non-goals: no runtime spawn hotkeys, no BattleRoot UI, no in-world placement cursor, no owner/faction/team switch UI.

Implementation summary:
- Added `SandboxSpawnAuthoring` as a pure core entry point over `UnitDesignCatalog` and `BuildSpecCatalog`.
- Exposed stable entries, kind/category/faction filtering, `EntitySpecFor`, and explicit `SandboxSpawnRequest` creation with owner and transform data.
- Added `SandboxSpawnAuthoringQa` to prove deterministic ordering, entry/spec round-trips, UnitDesign-backed units, BuildSpec-backed structures, Dog/Cat filters, and request preservation.
- Wired `SandboxSpawnAuthoringQa` into `VerifyAll`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
  Evidence: build completed with 0 warnings and 0 errors during worker validation.
- Command: `dotnet run --project tools/SandboxSpawnAuthoringQa/SandboxSpawnAuthoringQa.csproj --no-restore`
  Result: pass.
  Evidence: `SandboxSpawnAuthoringQa PASSED: entries 34, units 26, buildings 5, turrets 3`.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- sandboxspawn`
  Result: pass.
  Evidence: `ReviewGate passed` with 0 errors and 0 warnings.

Manual/visual gates:
- Runtime sandbox UI still needs a later visual check once entries become clickable spawn controls.

Reviewer result:
- Status: pass.
- Required fixes: none known.
- Residual risks: authoring data exists, but no scene-level spawn interaction is wired yet.

TODO update:
- Items marked done: none.
- Items left open: the broad sandbox parent remains open for runtime spawn controls, owner/faction/team switching, debug overlay rendering, and stress tests.
