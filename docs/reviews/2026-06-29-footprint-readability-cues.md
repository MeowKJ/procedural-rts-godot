# Review Record - Footprint readability cues

Step: Prove footprint/trail styles communicate unit class and movement domain.
Milestone: Art and style - class readability.
Owner AI: Codex.
Reviewer AI: Codex self-review with CombatBehavior and ReviewGate coverage.
Integrator AI: Codex.

Scope:
- Files/folders: `scripts/core/FootprintVisualMath.cs`, `scripts/core/FootprintMarkKind.cs`, `scripts/world/FootprintLayer.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-29-footprint-readability-cues.md`.
- Non-goals: no naval gameplay implementation, no screenshot art-tuning pass, no terrain interaction rewrite.

Implementation summary:
- Verified existing `FootprintVisualMath` maps light infantry-style units to step marks, medium vehicles to twin treads, heavy vehicles to track plates, air units to contrails, and paper-design naval units to wake ripples.
- Verified `FootprintLayer` stores marks in a bounded presentation list, reuses cleanup storage, checks visibility/fog, and fades/removes marks under load.
- Extended `ReviewGate vfx` to lock footprint readability hooks alongside the existing pooling/budget checks.
- Kept wake styling as a paper-design placeholder only; no ship/naval units are added to the playable slice.

Automated gates:
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj vfx --no-restore`
  Result: pass
  Evidence: ReviewGate reported 0 errors and 0 warnings for footprint readability plus VFX pooling checks.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed, including footprint weight/domain readability assertions.

Manual/visual gates:
- Check: rendered footprint style tuning
  Result: not run
  Evidence: this slice verifies style selection and performance boundaries; exact alpha/shape tuning can still happen in later visual QA.

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: visual playtests may tune opacity and spacing for specific maps; ship wake remains paper-design only until naval units are in scope.

TODO update:
- Items marked done: `Footprints/trails as readability cues: light = thin fast steps, tank = tread plates, aircraft = contrail, ship = wake; low-contrast, suppressed under UI/fog`.
- Items left open: unit-class operation logic, naval paper-design docs, and broader visual polish.
- Reason: footprint/trail mark kind, spacing, lifetime, visibility filtering, and budget behavior are implemented and now covered by durable gates.
