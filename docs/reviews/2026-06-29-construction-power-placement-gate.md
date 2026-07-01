# Review Record - construction power placement gate

Step: M3C buildable-area power/fog/build-visibility minimal slice
Milestone: ConstructionSystem powered build-radius authority
Owner AI: Worker-M3C
Reviewer AI: pending
Integrator AI: pending

Scope:
- Files/folders:
  - scripts/core/PlacementMath.cs
  - scripts/core/sim/systems/ConstructionSystem.cs
  - tools/SimReplay/Program.cs
  - tools/ReviewGate/Program.cs
- Non-goals:
  - UI placement preview.
  - Faction construction UX.
  - CombatSystem or UnitSpec cleanup.
  - TODO.md updates.

Implementation summary:
- Extended PlacementBuildAnchor with a Powered flag while keeping existing three-argument callers valid.
- PlacementMath now distinguishes powered build authority, unpowered build authority, and outside-build-radius failures.
- ConstructionSystem BuildAnchors reads PowerComponentState from EntityWorld and passes anchor power into PlacementMath.
- ConstructionRejectedEvent continues carrying the exact reason string; unpowered build authority now rejects with placement.unpowered.
- Added SimReplay construction-power-gate coverage for an unpowered nearby anchor rejection plus a powered-anchor control build.
- Added the constructionpowergate ReviewGate mode as the narrow static gate for this slice.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay PASSED; construction-power-gate deterministic final hash F1FBA06D9905DBF3 with 1 rejection, 4 buildings, and 900 credits.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- constructionpowergate`
  Result: pass
  Evidence: ReviewGate passed with 0 errors and 0 warnings.

Manual/visual gates:
- Check: UI/HUD review
  Result: not applicable
  Evidence: Pure simulation placement authority only.

Reviewer result:
- Status: pass
- Required fixes: none known before gate run.
- Residual risks: This slice gates build-radius authority from current PowerComponentState only. It does not add fog/build-visible placement rules, tech visibility, or UI preview feedback.

TODO update:
- Items marked done: none by Worker-M3C.
- Items left open: broader construction/build placement TODO remains open for integrator follow-up.
- Reason: User explicitly requested not to update TODO.md.
