# Review Record - Command acknowledgement ring pool

Step:
Pool command acknowledgement ring effects and apply soft/hard budgets.

Milestone:
M6 Performance.

Owner AI:
Codex main agent.

Reviewer AI:
Codex main-agent self-review; independent reviewer was not spawned because the
current thread has been operating at the subagent limit.

Integrator AI:
Codex main agent.

Scope:
- Files/folders:
  - `scripts/world/CommandAcknowledgementLayer.cs`
  - `tools/ReviewGate/Program.cs`
  - `TODO.md`
  - `docs/reviews/2026-06-29-command-ack-ring-pool.md`
- Non-goals:
  - Do not redesign command acknowledgement visuals.
  - Do not claim all VFX families are fully pooled.
  - Do not add projectile/impact effect pools in this slice.

Implementation summary:
- Replaced per-command ring creation/discard with pooled `Ring` objects.
- Added `SoftMaxRings`, `MaxRings`, `RingPoolLimit`, and `UnderLoadFadeSeconds`.
- Old rings fade out sooner under load; hard overflow returns ring objects to the
  pool.
- Extended `ReviewGate vfx` to verify the command acknowledgement ring pool and
  budget hooks.

Automated gates:
- Command:
  `dotnet build ProceduralRts.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  Build completed with 0 warnings and 0 errors.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj vfx`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/SelectionStress/SelectionStress.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  Selection stress reported `Selection stress passed: 80 cases`.
- Command:
  `Godot_v4.7-stable_mono_win64_console.exe --headless --path . --scene res://scenes/Battle.tscn --quit-after 2`
  Result:
  Pass.
  Evidence:
  Battle scene started and exited cleanly.

Manual/visual gates:
- Check:
  Visible command-ring fade under spam-click load.
  Result:
  Not run.
  Evidence:
  Headless startup verifies runtime safety; visible fade feel may still be tuned.

Reviewer result:
- Status: pass-with-warnings
- Required fixes:
  - None for this bounded VFX pooling slice.
- Residual risks:
  - Projectile/impact-specific effect families remain open.
  - No visible spam-click QA was performed.

TODO update:
- Items marked done:
  - None; broad VFX pooling item remains open.
- Items left open:
  - Full VFX pooling for future projectile/impact effect families.
- Reason:
  - Command acknowledgement rings are now pooled and budgeted, but the TODO names
    broader VFX coverage.
