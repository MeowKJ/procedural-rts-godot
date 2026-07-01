# Review Record - No super or hero units

Step:
Lock the vertical slice against hero, super, experimental, T4, or T5 unit content.

Milestone:
Explicit non-goals.

Owner AI:
Codex main agent.

Reviewer AI:
Codex main-agent review with `ReviewGate tiers` and `CombatBehavior`.

Integrator AI:
Codex main agent.

Scope:
- Files/folders:
  - `tools/CombatBehavior/Program.cs`
  - `tools/ReviewGate/Program.cs`
  - `TODO.md`
  - `docs/reviews/2026-06-29-no-super-hero-units.md`
- Non-goals:
  - Do not remove T3 units.
  - Do not alter balance, production, or faction rosters.
  - Do not implement T4/T5 or hero content as disabled placeholders.

Implementation summary:
- `CombatBehavior` now scans legacy `UnitKind` names and discovered `UnitDesign`
  type names for forbidden hero/super/experimental/commander/ultimate/T4/T5 terms.
- `ReviewGate tiers` mirrors the source scan across `UnitKind.cs` and
  `scripts/core/units/**/*.cs`.
- Existing tier checks continue proving all legacy definitions and `UnitSpec` data
  stay within T1-T3.

Automated gates:
- Command:
  `dotnet build ProceduralRts.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  Build completed with 0 warnings and 0 errors.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj tiers --no-restore`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  CombatBehavior passed with the new no-hero/no-super scan.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=no-super-hero-units --no-restore`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  VerifyAll passed all 14 steps: build, SimReplay, CombatBehavior,
  SimulationSmoke, FogOfWarQa, SelectionStress, AiDifficultySmoke, ReviewGate,
  PerfSmoke, BalanceReport, and Godot headless QA scenes.

Manual/visual gates:
- Check:
  None required.
  Result:
  Not applicable.
  Evidence:
  This is a content-scope guardrail, not a visual change.

Reviewer result:
- Status: pass.
- Required fixes:
  - None at record creation.
- Residual risks:
  - A future unit could still be overpowered without using forbidden names; balance
    remains covered by `BalanceReport`, not this naming/tier guard.

TODO update:
- Items marked done:
  - `No super-units / tier 4+ / hero units`.
- Items left open:
  - Broader Dog/Cat full playability and balance tuning.
- Reason:
  - Current content is bounded to T1-T3 and explicit hero/super/T4/T5 naming is now
    rejected by automated gates.
