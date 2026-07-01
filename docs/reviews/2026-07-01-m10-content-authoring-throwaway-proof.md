# Review Record - M10 content authoring throwaway proof

Step: M10 content authoring validation
Milestone: M10 Brick-Style Content Authoring
Owner AI: Codex
Reviewer AI: ContentAuthoringQa / ReviewGate
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/UnitDesignCatalog.cs`,
  `scripts/core/build/BuildSpecCatalog.cs`, `tools/ContentAuthoringQa/`,
  `docs/ContentAuthoringRecipes.md`, `TODO.md`.
- Non-goals: remove `WeaponKind` / `AmmoKind` enum ids or alter playable roster
  balance.

Implementation summary:
- Added catalog discovery APIs for explicit assemblies:
  `UnitDesignCatalog.DiscoverDesignsFrom(...)` and
  `BuildSpecCatalog.DiscoverDefinitionsFrom(...)`.
- Kept runtime catalogs unchanged: default `Designs` / `Definitions` still scan
  the gameplay assembly only.
- Added QA-local throwaway `UnitDesign` and `BuildingDesign` classes in
  `tools/ContentAuthoringQa`.
- Extended `ContentAuthoringQa` to prove the throwaway unit is discovered,
  projects to `EntitySpec`, avoids runtime catalog pollution, and fights through
  generic live combat systems.
- Extended `ContentAuthoringQa` to prove the throwaway building is discovered,
  projects to `EntitySpec`, avoids runtime catalog pollution, and reaches
  completed construction through `ConstructionSystem`.
- Added content authoring recipes for unit, building, turret, weapon/ammo, and
  verification flow.

Automated gates:
- Command: `dotnet run --project tools/ContentAuthoringQa/ContentAuthoringQa.csproj --no-restore`
  Result: pass
  Evidence: local run completed with throwaway authoring proof and catalog counts.

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: weapon/ammo ids remain enum-gated; this keeps the broader
  single declarative weapon/ammo path open.

TODO update:
- Items marked done: M10 authoring validation, M10 documentation, M10 throwaway
  unit/building verification.
- Items left open: full single declarative spec path and reflection/convention
  closure until `WeaponKind` / `AmmoKind` enum ids are removed.
