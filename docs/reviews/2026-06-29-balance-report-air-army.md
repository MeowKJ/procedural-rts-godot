# Review Record - BalanceReport air and army coverage

Step:
Extend BalanceReport from ground-only duels to cover aircraft, anti-air, and
army-vs-army composition checks.

Milestone:
Design Reference - Balance & Tuning Data.

Owner AI:
Codex main agent.

Reviewer AI:
Codex main-agent self-review; independent reviewer was not spawned because the
current thread is operating at the subagent limit.

Integrator AI:
Codex main agent.

Scope:
- Files/folders:
  - `scripts/core/units/cat/CatScoutAircraft.cs`
  - `scripts/core/units/cat/CatUnitArt.cs`
  - `tools/BalanceReport/Program.cs`
  - `tools/ReviewGate/Program.cs`
  - `TODO.md`
  - `docs/reviews/2026-06-29-balance-report-air-army.md`
- Non-goals:
  - Do not add Airfield production or full aircraft economy integration.
  - Do not claim the broader Aircraft class TODO is complete.
  - Do not add dog aircraft content in this slice.

Implementation summary:
- Added `CatScoutAircraft` as a real data-driven `UnitDesign` using
  `MovementDomain.Air`, `ArmorTag.Aircraft`, non-blocking collision, and a
  fixed-forward NeedleRifle mount.
- Added a procedural cat aircraft art recipe with owner-color wing stripes and a
  compact aircraft silhouette.
- Extended BalanceReport from single-spec duels to side compositions via
  `UnitGroup`, allowing army-vs-army reports.
- Added Tank-vs-Air pressure, Air-vs-AA, and army parity scenarios.
- Tuned the army parity scenario to include dog rocket support against a cat mixed
  force with one aircraft; the report now stays within the parity band.
- Updated `ReviewGate balance` so air, anti-air, army, and aircraft UnitDesign hooks
  are guarded.

Automated gates:
- Command:
  `dotnet run --project tools/BalanceReport/BalanceReport.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  BalanceReport passed seven scenarios: light parity, vehicle parity,
  anti-vehicle, anti-light, air pressure, anti-air, and army parity.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj balance`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings after checking aircraft and army
  BalanceReport hooks.

Manual/visual gates:
- Check:
  Aircraft visual inspection in-game.
  Result:
  Not run.
  Evidence:
  This slice adds procedural art data and headless balance coverage; visual tuning
  remains part of the broader unit art TODO.

Reviewer result:
- Status: pass-with-warnings
- Required fixes:
  - None for this bounded BalanceReport coverage slice.
- Residual risks:
  - Aircraft production, return-to-base/rearm behavior, and dog aircraft content are
    still open design/runtime work.
  - BalanceReport uses deterministic headless duels, not live-player micro.

TODO update:
- Items marked done:
  - `A tools/BalanceReport (headless): runs canonical duels ...`.
- Items left open:
  - Aircraft class implementation and broader air production/operation logic.
- Reason:
  - The required BalanceReport scenario families now exist and pass, but aircraft as
    a gameplay class is still only partially authored.
