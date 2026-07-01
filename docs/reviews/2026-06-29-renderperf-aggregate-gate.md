# Review Record - Render performance aggregate gate

Step: Add one durable gate that proves the current dirty rendering, culling, grid, VFX, and fog performance boundary.
Milestone: Performance optimization - render cost.
Owner AI: Codex.
Reviewer AI: Codex self-review with aggregate ReviewGate coverage.
Integrator AI: Codex.

Scope:
- Files/folders: `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-29-renderperf-aggregate-gate.md`.
- Non-goals: no unit body batching, no new renderer backend, no visual redesign, no claim that future VFX families are automatically complete.

Implementation summary:
- Added `ReviewGate renderperf` as a focused aggregate mode.
- The aggregate gate runs the existing presentation hotspot, camera culling, grid visible-rect/cache, VFX pooling, and fog mask/throttle checks together.
- The intended invocation uses `--max-warnings=0` so the dirty-redraw baseline must remain clean.
- Marked the high-level render-performance TODO done based on the aggregate gate while keeping deeper independent TODOs open.

Automated gates:
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj renderperf --max-warnings=0 --no-restore`
  Result: pass
  Evidence: ReviewGate reported 0 errors and 0 warnings for the aggregate render-performance boundary.

Manual/visual gates:
- Check: visual regression pass
  Result: not run
  Evidence: this slice adds aggregate verification over already-gated presentation systems; Godot headless smoke is covered by VerifyAll.

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: unit body batching remains open; future VFX/art families must add their own pooled/budgeted checks or extend `ReviewGate vfx`.

TODO update:
- Items marked done: `Dirty-flag / culled rendering; pooled VFX; cached static grid; throttled fog`.
- Items left open: `Batch unit bodies`, visual art-polish TODOs, and future gameplay/system migration items.
- Reason: the current render-performance boundary is now proven by a single aggregate gate composed of the existing detailed gates.
