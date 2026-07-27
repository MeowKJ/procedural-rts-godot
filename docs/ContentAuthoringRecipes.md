# Content Authoring Recipes

These recipes are the contract for adding content without changing runtime
systems. If a recipe asks for system edits, the authoring model regressed.

## Add A Unit

Create one concrete `UnitDesign` under `scripts/core/units/<faction>/`.
Give it a stable `Id`, faction, tags, stats, movement, collision, weapon mounts,
abilities, production metadata if playable, and a `UnitArtRecipe`.

Do not edit `UnitDesignCatalog`; it discovers concrete designs by reflection in
sorted id order. Add localization keys when the unit is user-facing. For QA-only
proofs, place the design in the tool assembly and call
`UnitDesignCatalog.DiscoverDesignsFrom(...)` so the runtime catalog stays clean.

Rebuild, it appears - no system code touched.

## Add A Building

Create one concrete `BuildingDesign` under `scripts/core/build/buildings/`.
Return a `BuildSpec` with kind, entity spec id, health, footprint, sight, armor,
category, cost, build time, requirements, power, build radius, and placement
domain.

Do not edit `BuildSpecCatalog`; it discovers concrete building designs by
reflection. Add localization keys for user-facing buildings. For QA-only proofs,
place the design in the tool assembly and call
`BuildSpecCatalog.DiscoverDefinitionsFrom(...)`.

Rebuild, it appears - no system code touched.

## Add A Turret

Create a `BuildingDesign` whose `BuildSpec.WeaponId` is not null. The generic
`BuildSpec.ToEntitySpec()` projection marks it as `EntityKind.Turret` and adds a
weapon mount; construction, power, targeting, health, selection, and sandbox
authoring read the same spec.

Rebuild, it appears - no system code touched.

## Add Weapon Or Ammo

Create one concrete `WeaponDesign` under `scripts/core/combat/weapons/` and,
when needed, one concrete `AmmoDesign` under `scripts/core/combat/ammo/`.
Give each design a stable string `Id`, and have the weapon reference the ammo by
string `AmmoId`. `WeaponCatalog` discovers both by reflection, validates
weapon-to-ammo links, and keeps deterministic order for replay stability.
Use `WeaponIds` / `AmmoIds` for built-in content and stable string ids for new
content. Combat content identity has no enum-based secondary path.

Rebuild, it appears - no system code touched.

## Verify New Content

Run:

```powershell
dotnet run --project tools/ContentAuthoringQa/ContentAuthoringQa.csproj --no-restore
dotnet run --project tools/RosterAuthoringQa/RosterAuthoringQa.csproj --no-restore
dotnet run --project tools/SandboxSpawnAuthoringQa/SandboxSpawnAuthoringQa.csproj --no-restore
dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore
```

`ContentAuthoringQa` includes a throwaway unit/building/weapon/ammo proof from
the QA tool assembly: the sample unit is discovered without runtime catalog
pollution, fights through generic combat systems with a tool-local string
weapon/ammo id, and the sample building completes through the generic
`ConstructionSystem`.

Rebuild, it appears - no system code touched.
