# Review Record - M9 BattleRoot Sandbox Command Buffers

Step: #197 `[M9] Reuse BattleRoot sandbox command buffers`
Milestone: M9 - Elegance & Decoupling
Owner AI: Remote Linux Codex
Reviewer AI: ReviewGate presentation / DesktopHudQa
Integrator AI: Remote Linux Codex

Scope:
- Added reusable `BattleRoot` sandbox launch unit and id buffers.
- Replaced sandbox launch selection LINQ with explicit unit collection, in-place distance/id sort, and bounded id fill.
- Replaced sandbox stress structure next-id `Select().DefaultIfEmpty().Max()` with an explicit building snapshot scan.
- Extended `ReviewGate presentation` to lock sandbox launch and structure next-id no-LINQ contracts.
- Non-goals: changing sandbox UI, developer context controls, stress spawn plans, selection semantics, relations, visuals, balance, or story.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
- Command: `dotnet run --project tools/DesktopHudQa/DesktopHudQa.csproj --no-restore`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- presentation --max-warnings=0`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize --max-warnings=0`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass.

Reviewer result:
- Status: pass.
- Required fixes: none known before validation.

Status:
- pass

Residual risks:
- This is a source-structure allocation guard for sandbox/QA setup paths, not a profiler sample.
- Parent #10 remains open for broader allocation paydown.

TODO update:
- Synced validation tool suite budget evidence after moving BattleRoot HUD allocation checks into `ReviewGatePresentation`.
- Evidence will be posted to #10 and #58 after verification.
- Items marked done: none.
- Items left open: parent #10 broader allocation paydown.
