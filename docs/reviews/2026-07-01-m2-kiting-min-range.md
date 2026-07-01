# Review Record - M2 kiting min range

Step:
Add data-driven minimum weapon range and mobile ranged kiting.

Milestone:
M2 Movement Algorithms & Unit Autonomy.

Owner AI:
Codex main agent.

Reviewer AI:
SimReplay, ReviewGate, and Codex main self-review against the M2 autonomy TODO.

Integrator AI:
Codex main agent.

Scope:
- Files/folders: `scripts/core/combat/WeaponDefinition.cs`, `scripts/core/combat/WeaponCatalog.cs`, `scripts/core/sim/WeaponMath.cs`, `scripts/core/sim/systems/combat/CombatEngagementSystem.cs`, `scripts/core/sim/systems/combat/CombatKitingSystem.cs`, `tools/SimReplay/Combat/KitingScenarios.cs`, `tools/SimReplay/Program.cs`, `TODO.md`, `docs/reviews/2026-07-01-m2-kiting-min-range.md`.
- Non-goals: no balance pass, no setup/siege retune, no UI kiting controls, no building/turret min-range behavior, and no pathing rewrite.

Implementation summary:
- Added `WeaponDefinition.MinRange` as weapon data with a conservative first authored value on `LightRepeater`.
- Added shared `WeaponMath` helpers for mount minimum range and effective edge distance against a target collision radius.
- Added a `CombatSystem` kiting partial that plans a bounded backoff target for mobile fire-while-moving weapons when the target is inside minimum range.
- Firing now checks per-mount minimum range, while existing fire-anchor and cooldown-anchor behavior still protects shooters once they are in a legal firing band.
- Added `ranged-min-range-kiting` deterministic replay coverage proving the unit backs away, restores spacing, and resumes damage.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass.
  Evidence: `OK [ranged-min-range-kiting]: spacing 118.0, target hp 110.7.`
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- kitingminrange`
  Result: pass.
  Evidence: ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m2-kiting-min-range`
  Result: pass.
  Evidence: ReviewGate found this durable review record.

Manual/visual gates:
- Check: Not applicable.
  Result: not run.
  Evidence: behavior is deterministic sim logic and is covered by SimReplay.

Reviewer result:
- Status: pass.
- Required fixes: none.
- Residual risks: only `LightRepeater` currently authors a nonzero minimum range; broader balance and unit-by-unit tuning are intentionally left to later roster/balance work.

TODO update:
- Items marked done: M2 kiting / micro for mobile ranged units; deterministic autonomy tests in SimReplay.
- Items left open: broader balance, setup/siege tuning, and future UI/feedback work.
- Reason: the last open deterministic autonomy assertion, ranged min-range/kiting, is now covered by replay and runtime combat logic.
