# Review Record - Dog Air Unit

Step: Add a playable Dog air unit and close the roster QA Dog-air warning.
Milestone: Dog/Cat playable roster completeness.
Owner AI: Main thread.
Reviewer AI: ReviewGate dogairunit plus RosterAuthoringQa.
Integrator AI: Main thread.

Scope:
- Files/folders: `scripts/core/units/dog/DogSkyPatrolAircraft.cs`, `scripts/core/units/dog/DogUnitArt.cs`, `scripts/core/GameText.cs`, `tools/RosterAuthoringQa/Program.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-dog-air-unit.md`.
- Non-goals: no new `UnitKind`, no legacy `UnitCatalog` content expansion, no new weapon system, no final air balance pass, no production paging.

Implementation summary:
- Added `DogSkyPatrolAircraft` as a data-driven Dog `UnitDesign`.
- The unit uses `MovementDomain.Air`, `ArmorTag.Aircraft`, non-blocking collision, Airfield production, and an air-only `SkySpear` weapon.
- Added Dog-specific procedural aircraft art and localized name/role keys.
- Upgraded `RosterAuthoringQa` so playable air is a hard requirement for every playable faction.
- Updated CombatBehavior to prove Dog air appears in UnitDesign production options, is produced from Airfield, can be queued by design id, and completes into a runtime `UnitInstance`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/RosterAuthoringQa/RosterAuthoringQa.csproj --no-restore`
  Result: pass.
  Evidence: Dog now has 11 playable designs, categories include `Air`, domains include `Air`, and `RosterAuthoringQa PASSED`.
- Command: `dotnet run --project tools/RosterAuthoringQa/RosterAuthoringQa.csproj --no-restore -- --strict`
  Result: pass.
  Evidence: strict mode completed with `RosterAuthoringQa PASSED`.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass.
  Evidence: `Combat behavior passed: weapon hit rules, turret states, terrain passability, localization fallback, presentation descriptors, shared threat propagation, rally production, economy, enemy AI, and outcomes`.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- dogairunit`
  Result: pass.
  Evidence: `ReviewGate passed` with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- rosterauthoringqa`
  Result: pass.
  Evidence: `ReviewGate passed` with 0 errors and 0 warnings.

Manual/visual gates:
- In-game visual QA remains useful for final aircraft readability and shadow/contrail tuning.

Reviewer result:
- Status: pass.
- Required fixes: none known.
- Residual risks: Dog air balance and final production UI pagination are still open broader TODO work.

TODO update:
- Items marked done: none.
- Items left open: broad aircraft and Dog/Cat roster parent items remain open until final playable completeness and balance acceptance are proven.
