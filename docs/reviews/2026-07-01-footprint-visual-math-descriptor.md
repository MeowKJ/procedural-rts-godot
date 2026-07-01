# Review Record - FootprintVisualMath UnitSpec descriptor cleanup

Step:
- FootprintVisualMath UnitSpec descriptor cleanup

Milestone:
- M1 EntityWorld authority

Owner AI:
- Codex

Reviewer AI:
- Codex

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/FootprintVisualMath.cs
  - tools/CombatBehavior/Program.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-07-01-footprint-visual-math-descriptor.md
- Non-goals:
  - Do not change footprint visuals, colors, durations, spacing, or mark kinds.
  - Do not delete `UnitDefinition` globally in this slice.
  - Do not change balance, movement, combat, fog, UI layout, or roster data.

Implementation summary:
- Changed `FootprintVisualMath.StyleFor(...)` and its land-style helper to accept
  `UnitSpecRuntimeDescriptor` directly.
- Removed CombatBehavior's footprint QA compatibility `UnitDefinition`
  intermediates.
- Kept the same weight/domain footprint outputs: light steps, medium twin treads,
  heavy track plates, aircraft contrails, and naval wake marks.
- Added `ReviewGate footprintvisualmathdescriptor` and updated the existing
  UnitDesign definition catalog gate to forbid the old footprint compatibility
  projection from returning.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors after clean solo rerun.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- footprintvisualmathdescriptor`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- unitdesigndefinitioncatalog`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay PASSED.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=footprint-visual-math-descriptor`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings after evidence backfill.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll PASSED, 23/23 steps.

Manual/visual gates:
- Check: Visual/UI review
  Result: not applicable
  Evidence: This slice preserves existing footprint style constants and only
    changes the data type feeding the pure style math.

Reviewer result:
- Status: pass after integration review.
- Required fixes:
  - None.
- Reviewer notes:
  - The slice reduces the `UnitDefinition` compatibility surface without changing
    live rendering behavior.
- Residual risks:
  - Other compatibility paths still use `UnitDefinition` until later deletion
    slices.
  - ReviewGate is string/regex-based rather than semantic type analysis.

TODO update:
- Items marked done:
  - FootprintVisualMath UnitSpec descriptor cleanup
- Items left open:
  - Broader `UnitDefinition` compatibility deletion remains future work.
- Reason:
  - `FootprintVisualMath` now consumes `UnitSpecRuntimeDescriptor` directly and
    CombatBehavior footprint QA no longer constructs compatibility
    `UnitDefinition` intermediates for this path.
