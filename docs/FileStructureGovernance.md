# File Structure Governance

This document is the standing rule set for splitting large files without creating
a file-navigation mess. It is intended for Codex and other implementation agents.

## Core Policy

Old debt should shrink gently. New code must be strict.

Do not split files just to hit a line count. Split when a file crosses a red
line, mixes responsibilities, or becomes a recurring merge hotspot.

## Size Bands

- `< 200` lines: healthy target.
- `200-400` lines: normal.
- `400-600` lines: yellow; review responsibility boundaries.
- `> 600` lines: red; new files should fail unless explicitly exempted.
- `> 1000` lines: debt; register a ceiling and plan staged extraction.

Existing debt is allowed only as known debt. It must not keep growing.

## Stable Entrypoints

Every large subsystem must keep a stable entrypoint file:

- `CombatSystem.cs`
- `UnitBattlefield.cs`
- `GameState.cs`
- `BattleRoot.cs`
- `HudLayer.cs`
- `tools/ReviewGate/Program.cs`

Entrypoints should expose public API and orchestration only. Detailed logic
belongs in focused files.

## First Split With Partial Classes

For behavior-preserving refactors, prefer `partial class` extraction first.

Good first-stage examples:

- `UnitBattlefield.Commands.cs`
- `UnitBattlefield.Units.cs`
- `UnitBattlefield.Buildings.cs`
- `UnitBattlefield.Production.cs`
- `UnitBattlefield.CombatBridge.cs`
- `UnitBattlefield.LegacySync.cs`

Move code first. Do not change semantics in the same slice. After build and
replay gates pass, a later slice may convert partial files into composed helper
classes where that reduces coupling.

## Domain Directories

If a domain gains too many same-prefix files, create a domain directory.

Example:

```text
scripts/core/sim/systems/combat/
  CombatSystem.cs
  CombatTargeting.cs
  CombatMovement.cs
  CombatFiring.cs
  CombatDamage.cs
  CombatQueries.cs
```

Prefer domain directories over type buckets such as `helpers/` or `utils/`.

## Naming Rules

Allowed suffixes:

- `*System.cs`
- `*Commands.cs`
- `*Queries.cs`
- `*Math.cs`
- `*Projection.cs`
- `*Bridge.cs`
- `*LegacySync.cs`
- `*State.cs`
- `*ComponentState.cs`
- `*Spec.cs`
- `*Design.cs`

Forbidden vague names:

- `Helper.cs`
- `Helpers.cs`
- `Utils.cs`
- `Utility.cs`
- `Misc.cs`
- `Common.cs`
- `Manager.cs` unless it truly owns lifecycle management.

## Bridge And Legacy Rules

Any file containing `Bridge`, `Legacy`, or `Compatibility` in its name must have
a deletion condition in a GitHub issue or governance document.

Bridge files should trend downward. Adding a new bridge requires explicit
justification. ReviewGate should fail when bridge/legacy/compatibility file
count rises above the registered baseline.

## Directory Governance

ReviewGate should enforce or warn on these rules:

- New or unregistered source file `> 600` lines: fail.
- Known debt file exceeds registered ceiling: fail.
- Vague helper-style file name: fail.
- Single source directory `> 30` `.cs` files: warning.
- Same prefix `Xxx.*.cs` count `> 8`: warning; consider a domain directory.
- Stable entrypoint missing for a split subsystem: fail.

## Verification Order

Use this sequence for size-driven refactors:

1. Move code only.
2. Build.
3. Run the relevant ReviewGate mode.
4. Run SimReplay or the current golden-hash check.
5. Commit the behavior-preserving split as evidence.
6. Only then perform semantic cleanup in a second slice.

Never mix a mechanical move and behavior changes in the same slice.

## ReviewGate Scope

Do not add one-off gates for every small issue slice. Prefer a small set of general
discipline gates:

- `ArchitectureGate`: layer boundaries and authority rules.
- `FileStructureGate`: size, names, directories, bridge counts.
- `RegressionGate`: replay, combat behavior, perf smoke, and VerifyAll grouping.

ReviewGate itself must follow this governance. `Program.cs` should become an
entrypoint and registry, not a growing archive of every historical rule.
`tools/ReviewGate` has an additional total C# source budget of 2000 lines. Keep
historical narrow mode names as compatibility aliases, but route them to broad
domain gates instead of adding one C# check per issue slice.
`ReviewGate filesize` must enforce validation-system source budgets directly so
size evidence cannot drift from the actual source tree.
`tools/ReviewGate` build output must live outside that source directory, currently
under `artifacts/dotnet/ReviewGate`; local `tools/ReviewGate/bin` or
`tools/ReviewGate/obj` directories are treated as validation-system pollution.
