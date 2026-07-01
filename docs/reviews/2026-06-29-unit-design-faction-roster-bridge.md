Step: Add a UnitDesign-driven faction roster read entrypoint while preserving legacy FactionCatalog unit rosters.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Worker C
Reviewer AI: pending
Integrator AI: pending

Scope:
- Files/folders: `scripts/core/units/UnitDesignFactionRosterCatalog.cs`, `scripts/core/units/UnitDesignRuntimeLoadouts.cs`, `tools/CombatBehavior/Program.cs`, `TODO.md`.
- Added `UnitDesignFactionRosterCatalog.For(UnitFactionId)` as the UnitSpec/UnitDesign-side read entrypoint for playable design ids and starting design ids.
- Kept `FactionCatalog`, `FactionDefinition`, `UnitKind`, legacy `MatchStartLoadouts`, and legacy UnitKind spawn paths unchanged.
- Non-goals: deleting `FactionCatalog`, migrating old `UnitKind` start loadouts, changing unit stats, changing localization, changing UI, or changing balance reports.

Implementation summary:
- `UnitDesignFactionRosterCatalog` derives playable Dog/Cat design ids from discovered `UnitDesign` classes with `ProductionSpec` data.
- Starting design ids and formation offsets now live in the UnitDesign roster bridge and are validated back through `UnitDesignCatalog.Spec`.
- `UnitDesignRuntimeLoadouts` is now a compatibility facade over the UnitDesign faction roster bridge.
- Production design lookup now resolves from playable UnitSpec authoring data by production kind/category and preferred archetype, preserving the current Dog/Cat defaults.
- `CombatBehavior` asserts deterministic playable roster order, repeated starting design ids, UnitSpec faction/production validity, runtime facade delegation, and default production selection.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed with the UnitDesign faction roster bridge assertions.

Manual/visual gates:
- Check: visual inspection not required for this bounded data read-entrypoint slice.
  Result: not run.
  Evidence: no rendering, UI, movement, combat, or balance-report path was changed.

Reviewer result:
- Status: pass.
- Required fixes: none identified.
- Residual risks: this is an owner-side bridge slice; legacy `FactionCatalog.AvailableUnits` and legacy start loadouts still exist and remain authoritative for old UnitKind flows until later migration slices.

Status:
- Pass.

Residual risks:
- The broad duplicate-data TODO remains open.
- Starting UnitDesign formation slots are still authored as explicit design ids because spawn count, duplicates, offsets, and facing are loadout data rather than intrinsic UnitSpec data.
- `ProductionKind` remains a legacy compatibility concept; this slice maps it onto UnitSpec archetypes but does not remove it.

TODO update:
- Items marked done: none.
- Items left open: parent UnitSpec duplicate-data cleanup and later deletion of `UnitKind` / `UnitCatalog` / `FactionCatalog` authority.
- Reason: this slice adds the roster read bridge and deterministic evidence without migrating or deleting legacy systems.
