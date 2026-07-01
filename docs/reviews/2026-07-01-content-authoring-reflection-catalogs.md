# Review Record - Content authoring reflection catalogs

Step: Content authoring reflection catalogs
Milestone: M10 Brick-Style Content Authoring
Owner AI: Codex
Reviewer AI: ContentAuthoringQa / ReviewGate
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/combat/`, `scripts/core/build/`, `scripts/core/units/UnitDesignCatalog.cs`, `tools/ContentAuthoringQa/`, `tools/VerifyAll/Program.cs`, `tools/ReviewGate/ContentAuthoringReviewGate.cs`, `TODO.md`, `docs/ContentAuthoringFrictionAudit.md`.
- Non-goals: replacing `WeaponKind`/`AmmoKind` enum ids, changing combat balance, adding the future `WeaponSystem`/`ProjectileSystem`, or redesigning UI.

Implementation summary:
- Converted `WeaponCatalog` from an embedded hand-written weapon/ammo dictionary into reflection-discovered `WeaponDesign` and `AmmoDesign` data classes.
- Converted `BuildSpecCatalog` from an embedded hand-written building dictionary into reflection-discovered `BuildingDesign` data classes while preserving `BuildSpecCatalog.For(...)`.
- Made `UnitDesignCatalog` enumerate discovered unit designs in deterministic sorted id order.
- Added `tools/ContentAuthoringQa` and wired it into `VerifyAll` to prove catalog discovery, reference integrity, `EntitySpec` projection, and one generic live tick for spawned unit/building data.
- Fixed `WeaponUserComponentState` runtime mount lists that were created from read-only collection expressions, replacing them with writable arrays.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: main project build completed with 0 warnings and 0 errors after the reflection catalog migration.
- Command: `dotnet run --project tools/ContentAuthoringQa/ContentAuthoringQa.csproj --no-restore`
  Result: pass
  Evidence: ContentAuthoringQa reported units 26, weapons 7, ammo 5, build specs 8.
- Command: `dotnet run --project tools/RosterAuthoringQa/RosterAuthoringQa.csproj --no-restore`
  Result: pass
  Evidence: Dog and Cat playable rosters still expose T1-T3 land/air/economy/support coverage.

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: weapon and ammo ids are still enums, so brand-new combat content still needs an enum value until M10 replaces enum ids with string/data ids.

TODO update:
- Items marked done: current authoring friction audit.
- Items left open: full single-file authoring for brand-new weapon/ammo ids, throwaway data-only test content proof, and full projectile/weapon system convergence.
- Reason: catalogs now discover unit/building/weapon/ammo designs by convention, and the remaining friction is explicitly documented.
