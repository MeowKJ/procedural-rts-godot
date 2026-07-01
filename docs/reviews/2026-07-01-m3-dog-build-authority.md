# Review Record - M3 Dog build authority

Step: M3 Dog deploy/build-authority backend
Milestone: M3 Build & Construction System
Owner AI: Codex
Reviewer AI: ReviewGate m3dogbuildauthority / SimReplay
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/entities/UnitSpecEntityBridge.cs`, `scripts/core/units/dog/DogEngineer.cs`, `scripts/core/sim/systems/construction/ConstructionSystem.Queries.cs`, `tools/SimReplay/Economy/ConstructionDogBuildAuthorityScenarios.cs`, `tools/SimReplay/Core/ReplayPrelude.cs`, `TODO.md`.
- Non-goals: Dog-specific UI/HUD, balance tuning, shared restart/capture UX, or deleting remaining non-M3 TODOs.

Implementation summary:
- Treats `AbilityKind.Build` as passive build-authority data: UnitSpec spawning emits `BuildRadiusComponentState` from the ability radius and does not create an active cooldown entry for Build.
- Gives `DogEngineer` a 220px Build ability while keeping repair as an active ability.
- Generalizes construction build anchors from completed buildings only to any live friendly `BuildRadiusComponentState` authority, while preserving completed-building rules for building anchors.
- Gates deploy-capable build authorities: specs with `AbilityKind.Deploy` must be deployed and setup-complete before their build radius can authorize construction.
- Adds deterministic replay coverage for Dog engineer forward build authority and Deploy+Build core setup gating.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: main project build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: `dog-build-authority` accepted one forward build from Dog engineer radius and rejected outside-radius placement; `deploy-build-authority` rejected before/during setup and accepted after setup.
- Command: `dotnet run --project tools/PlayerLoopQa/PlayerLoopQa.csproj --no-restore`
  Result: pass
  Evidence: player-loop QA passed after the nullable ticket assertion warning was removed.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: combat behavior completed successfully after Dog engineer gained passive Build data.
- Command: `dotnet run --project tools/RosterAuthoringQa/RosterAuthoringQa.csproj --no-restore`
  Result: pass
  Evidence: Dog/Cat authored rosters still load and report the expected T1-T3 designs.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- m3dogbuildauthority`
  Result: pass
  Evidence: historical narrow mode routed through the content gate and passed.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m3-dog-build-authority`
  Result: pass
  Evidence: review record gate found this record and passed.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings.

Manual/visual gates:
- Check: file-size governance
  Result: pass
  Evidence: `ReviewGate filesize` passed; new scenario file is focused and below the 200-line healthy target; no C# file crosses the 400-line normal ceiling.

Reviewer result:
- Status: pass
- Required fixes: none known.
- Residual risks: player-facing Dog construction HUD handoff remains open under M7/M3 UX; shared restart/capture remains open.

TODO update:
- Items marked done: Dog method backend under faction-distinct construction methods.
- Items left open: shared restart/capture and player-facing Dog construction UX/HUD handoff.
- Reason: Dog construction now uses the same `StartConstructionEntityCommand` backend with data-driven unit/deploy-core build authority.
