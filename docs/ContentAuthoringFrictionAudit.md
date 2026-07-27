# Content Authoring Friction Audit

Date: 2026-07-01

This audit records the steps needed today to add content without changing system
behavior. Any step beyond writing a focused design/spec file is friction to pay
down in M10.

## Unit

Add one concrete `UnitDesign` class under `scripts/core/units/<faction>/`.
`UnitDesignCatalog` discovers it by reflection in sorted id order. The design must
provide stats, movement, collision, weapon mounts, optional abilities, production,
and art recipe. If it is playable, the roster is derived from faction and
production data. Remaining friction: name/role localization keys must still be
added to the localization dictionaries.

## Building

Add one concrete `BuildingDesign` class under `scripts/core/build/buildings/`.
`BuildSpecCatalog` discovers it by reflection and exposes the existing
`BuildSpecCatalog.For(...)` API. The design owns cost, footprint, power,
requirements, build radius, and optional weapon. Remaining friction: stable order
currently uses a `SortOrder` property, and user-facing localization keys still
need dictionary entries.

## Weapon And Ammo

Add one concrete `WeaponDesign` class under `scripts/core/combat/weapons/` and,
when needed, one concrete `AmmoDesign` class under `scripts/core/combat/ammo/`.
`WeaponCatalog` discovers both by reflection, validates weapon-to-ammo links, and
keeps deterministic string-id lookup for runtime combat systems. Existing
`WeaponIds` and `AmmoIds` provide stable built-in string ids. A brand-new weapon
or ammo can be added with a new design class and stable string id without editing
an enum or a central lookup switch.

## Turret

Add a `BuildingDesign` whose `BuildSpec.WeaponId` is not null. The generic
`BuildSpec.ToEntitySpec()` projection marks it as `EntityKind.Turret`, gives it a
`WeaponUserComponentState`, and existing construction/repair/selection/power
systems treat it as an entity. Remaining friction: turret art is still derived
from building presentation rather than a standalone turret art recipe.

## Current Generic Verification

`tools/ContentAuthoringQa` counts all concrete runtime `UnitDesign`,
`BuildingDesign`, `WeaponDesign`, and `AmmoDesign` classes, checks catalog
coverage, verifies weapon/ammo/building references, projects unit/building specs
to `EntitySpec`, and spawns a unit plus turret through `EntityWorld` for one
generic live tick.

The QA also owns tool-local throwaway `UnitDesign`, `BuildingDesign`,
`WeaponDesign`, and `AmmoDesign` classes. Explicit assembly scans discover them
without polluting runtime catalogs; the throwaway unit fights through generic
combat systems with a tool-local string weapon/ammo id, and the throwaway
building reaches completed construction through `ConstructionSystem`.
