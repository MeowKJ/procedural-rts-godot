# Review Record - ReviewGate system budget cleanup

Step: ReviewGate system budget cleanup
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: ReviewGate filesize / broad domain gates
Integrator AI: Codex

Scope:
- Files/folders: `tools/ReviewGate/`, `TODO.md`, `docs/FileStructureGovernance.md`, `docs/reviews/2026-07-01-reviewgate-system-budget.md`.
- Non-goals: changing gameplay behavior, changing unit balance, deleting `UnitKind`, or deleting `BuildingKind`.

Implementation summary:
- Replaced the generated-style registry and hundreds of historical C# check files with a small runner, dynamic historical-mode catalog, domain router, and broad architecture/content/presentation/regression gates.
- Kept the external ReviewGate command surface: `all`, `todo`, `filesize`, `review`, `presentation`, `m1migrationparentcomplete`, and historical narrow modes mentioned in TODO/docs remain valid.
- Added a 2000-line total ReviewGate C# source budget to `FileSizeGate`, alongside the existing 200-line per-file validation-system ceiling.
- Redirected ReviewGate build output to `artifacts/dotnet/ReviewGate` and added a `filesize` gate error if `tools/ReviewGate/bin` or `tools/ReviewGate/obj` returns.
- Added a `filesize` gate check that `ProceduralRts.csproj` excludes `.godot`, `artifacts`, and `tools` C# sources from the gameplay build.
- Removed obsolete `tools/ReviewGate/registry/` and historical per-slice check directories while keeping core TODO/review/file-size checks.

Automated gates:
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: local run completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- unitmodeldesignidnative`
  Result: pass
  Evidence: historical UnitSpec narrow mode routed through the broad content gate and passed.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetsnapshotinternalid`
  Result: pass
  Evidence: historical building-target narrow mode routed through the broad content gate and passed.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- presentation --max-warnings=0`
  Result: pass
  Evidence: presentation gate completed with 0 errors and 0 warnings.
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: main project build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 23 steps successfully, including build, replay, ReviewGate, perf smoke, balance/counter QA, and Godot headless QA.

Manual/visual gates:
- Check: ReviewGate source size audit
  Result: pass
  Evidence: `tools/ReviewGate` now has 28 non-generated files / 1525 total lines, no local `bin` or `obj`. ReviewGate current source budget: 26 C# source files / 1509 total lines; largest C# file tools/ReviewGate/FileSizeStructureChecks.cs has 174 lines.

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: broad domain gates intentionally replace many brittle historical text checks; full behavioral confidence now comes from `VerifyAll` tool projects plus the broad ReviewGate discipline gates.

TODO update:
- Items marked done: ReviewGate system budget cleanup.
- Items left open: none for the ReviewGate size-budget slice. The later
  M1 legacy `UnitKind` / `BuildingKind` deletion is complete in
  `docs/reviews/2026-07-01-m1-legacy-kind-deletion.md`.
- Reason: the validation system now follows the user's file-size thresholds at both file and subsystem level.
