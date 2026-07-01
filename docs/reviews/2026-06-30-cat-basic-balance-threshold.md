# Review Record - Cat Basic Balance Threshold

Step:
Restore light-infantry parity after the target re-acquire cooldown changed combat
target handoff timing.

Milestone:
Balance & Tuning Data.

Owner AI:
Codex main thread.

Reviewer AI:
BalanceReport plus integrator review.

Integrator AI:
Codex main thread.

Scope:
- Files/folders:
  - `scripts/core/units/cat/CatBasic.cs`
  - `TODO.md`
  - `docs/reviews/2026-06-30-cat-basic-balance-threshold.md`
- Non-goals:
  - No BalanceConfig migration.
  - No weapon catalog retune.
  - No faction roster changes.

Implementation summary:
- Increased `cat.basic` HP from 44 to 52.
- This keeps CatBasic in the light-infantry role while moving it onto the damage
  threshold where the seeded duel is no longer a Dog 100% win.
- Dog patrol vehicles still beat CatBasic, and the mixed-force Cat push still wins
  as expected.

Automated gates:
- Command:
  `dotnet run --project tools/BalanceReport/BalanceReport.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  Light parity reports Dog 17% / Cat 83%, inside the accepted 15%-85% band; all
  other balance scenarios pass.
- Command:
  `dotnet run --project tools/CounterReadabilityQa/CounterReadabilityQa.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  Counter-readability duels still resolve inside the 60-second window.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=cat-basic-balance-threshold`
  Result:
  Pass.
  Evidence:
  Review record is present and satisfies the project review template.

Reviewer result:
Pass. The fix is a narrow data adjustment that restores the existing balance gate.

Status:
Pass.

Residual risks:
- Light parity sits near the accepted edge; future BalanceConfig work should make
  this easier to tune without direct spec edits.

TODO update:
- Added progress under the BalanceConfig data-table item; parent remains open.
