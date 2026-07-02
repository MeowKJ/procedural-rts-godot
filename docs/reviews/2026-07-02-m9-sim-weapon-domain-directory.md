# Review Record - M9 sim weapon domain directory

Step: Move weapon engagement helpers out of the crowded sim root
Milestone: M9 Elegance, Decoupling, Performance
Owner AI: Remote Linux Codex
Reviewer AI: ReviewGate filesize / architecture
Integrator AI: Remote Linux Codex

Scope:
- Files/folders: `scripts/core/sim/weapon/`, `tools/ReviewGateDomains/ArchitectureReviewGate.cs`, `TODO.md`, `docs/reviews/2026-07-02-m9-weapon-range-helper-gate.md`.
- Issue: #69, follow-up from the #68 `ReviewGate filesize` directory-shape warning.
- Non-goals: changing namespaces, changing combat behavior, changing weapon range or damage semantics, or moving unrelated sim root files.

Implementation summary:
- Moved `WeaponMath.cs` and the `WeaponEngagement*.cs` helper cluster into `scripts/core/sim/weapon/`.
- Kept namespace `ProceduralRts.Core`, so call sites and compiled symbols remain unchanged.
- Updated `ArchitectureReviewGate` to read `WeaponMath.cs` from the new domain directory.
- Updated current TODO/review evidence that pointed at the active `WeaponMath.cs` location.
- Reduced `scripts/core/sim/` root C# file count from 31 to 25, below the FileStructureGovernance directory warning threshold.

Automated gates:
- Command: `find scripts/core/sim -maxdepth 1 -name '*.cs' -type f | wc -l`
  Result: pass
  Evidence: root sim C# file count is 25.
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: main project build completed with 0 warnings and 0 errors after the file moves.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize --max-warnings=0`
  Result: pass
  Evidence: FileSizeGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- architecture --max-warnings=0`
  Result: pass
  Evidence: ArchitectureReviewGate completed with 0 errors and 0 warnings after the path update.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore --max-warnings=0`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all steps successfully.

Reviewer result:
- Status: pass.
- Required fixes: none.
- Residual risks: this only clears one directory-shape warning; broader combat-system convergence and future sim domain directories remain separate slices.

TODO update:
- Items marked done: none; this is a FileStructureGovernance follow-up under the existing file-size discipline guard.
- Items left open: combat-system convergence and comment discipline.
- Reason: the warning was a local directory-shape issue, not a new gameplay or balance TODO.
