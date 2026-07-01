# Review Record - M10 weapon/ammo string authoring

Step: M10 weapon/ammo authoring id cleanup
Milestone: M10 Brick-Style Content Authoring
Owner AI: Codex
Reviewer AI: ContentAuthoringQa / ReviewGate
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/combat/`, `scripts/core/entities/EntityWorld.CombatDefinitions.cs`, `scripts/core/sim/WeaponMath.cs`, `scripts/core/sim/systems/`, `tools/ContentAuthoringQa/`, `tools/ReviewGateDomains/ContentAuthoringReviewGate.cs`, `docs/ContentAuthoringFrictionAudit.md`, `docs/ContentAuthoringRecipes.md`, `TODO.md`.
- Non-goals: delete legacy `WeaponKind` / `AmmoKind` aliases in one slice, retune combat balance, or merge the three combat-system loops.

Implementation summary:
- Made `WeaponDefinition` and `AmmoDefinition` id-first with stable string `Id` / `AmmoId`; legacy enum values are optional aliases for existing content.
- Made `WeaponDesign` and `AmmoDesign` expose string ids while keeping legacy enum compatibility for old authored weapons.
- Added string-keyed `WeaponCatalog.WeaponDefinitions` / `AmmoDefinitions` plus `DiscoverWeaponsFrom(...)` and `DiscoverAmmoFrom(...)` for tool-local authoring proofs.
- Added per-world combat definition overlays on `EntityWorld`, so QA/mod/tool content can register weapon/ammo definitions for one simulation without polluting runtime catalogs.
- Migrated EntityWorld combat, turret, building-target combat, weapon math, projectile state, weapon-fired events, and deterministic hash to use string weapon/ammo ids on the runtime path.
- Extended `ContentAuthoringQa` with tool-local `ThrowawayProbeWeaponDesign` and `ThrowawayProbeAmmoDesign`; the throwaway unit mounts `weapon.qa.throwaway.probe`, injects the definitions into one `EntityWorld`, fights through generic combat, and leaves runtime catalogs clean.
- Extended `ReviewGate` content authoring checks to require string-keyed weapon/ammo catalog APIs and the tool-local throwaway weapon/ammo proof.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: local build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ContentAuthoringQa/ContentAuthoringQa.csproj --no-restore`
  Result: pass
  Evidence: QA-local unit/building/weapon/ammo authoring proof passed.
- Command: `dotnet run --project tools/RosterAuthoringQa/RosterAuthoringQa.csproj --no-restore`
  Result: pass
  Evidence: playable Dog/Cat roster remained valid.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: deterministic replay suite passed after string-id hashing.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: behavior checks passed for combat, turrets, presentation descriptors, economy, AI, and outcomes.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: full 23-step verification passed, including build, replay, behavior QA, ReviewGate, perf smoke, balance report, and Godot headless QA.

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: legacy enum aliases still exist for old gameplay and presentation code; future cleanup can delete them after UI/VFX/death records stop requiring enum compatibility.

TODO update:
- Items marked done: M10 content registry by reflection/convention for unit, building, weapon, ammo, and tool-local throwaway content.
- Items left open: the broad single declarative spec path still tracks remaining user-facing friction such as localization keys, building sort order, and turret art recipes.
