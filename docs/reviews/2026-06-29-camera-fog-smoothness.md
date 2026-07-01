# Review Record - camera and fog smoothness

Step:
- M6 camera/fog smoothness slice for camera pan stutter and slow fog updates.
Milestone:
- M6 performance and gameplay feel.
Owner AI:
- Codex implementation worker.
Reviewer AI:
- Pending independent reviewer / integrator.
Integrator AI:
- Pending main-thread integrator.

Scope:
- Files/folders:
  - scripts/controllers/CameraController.cs
  - scripts/core/FogOfWarMap.cs
  - scripts/core/FogOfWarVisualPolicy.cs
  - scripts/world/FogOfWarLayer.cs
  - scripts/BattleRoot.cs
  - tools/FogOfWarQa/Program.cs
  - tools/ReviewGate/Program.cs
  - docs/reviews/2026-06-29-camera-fog-smoothness.md
- Non-goals:
  - No command, combat, movement, building-view, UnitSpec, or TODO.md edits.
  - No visual restyle, grid changes, or fog gameplay-rule changes.

Implementation summary:
- Camera view-change notifications are throttled independently from visual camera smoothing, with immediate notification for large jumps.
- BattleRoot resets the culling timer after an immediate view-culling refresh to avoid duplicate event/timer refreshes.
- FogOfWarMap now tracks dirty mask ranges, caches stats, and skips repeated reveal work when the world size and vision-source signature are unchanged.
- FogOfWarLayer queues redraws only when the fog mask revision changes or the camera-scoped upload rect moves far enough to justify another texture request.
- Medium fog cadence was reduced from 0.16s to 0.12s while avoiding unconditional redraw/upload work.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj`
  Result: pass
  Evidence: build succeeded with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/FogOfWarQa/FogOfWarQa.csproj --no-restore`
  Result: pass
  Evidence: baseline before this slice failed at 4159ms; after implementation, `fog 100-source unchanged-source performance smoke: 4ms (<3500ms)` and Fog-of-war QA passed.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj fog --no-restore`
  Result: pass
  Evidence: ReviewGate reported 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj camera --no-restore`
  Result: pass
  Evidence: ReviewGate reported 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj presentation --no-restore`
  Result: pass
  Evidence: ReviewGate reported 0 errors, 0 warnings.

Manual/visual gates:
- Check: In-app camera pan/zoom feel and fog reveal latency.
  Result: not run by this worker.
  Evidence: Code-level gates prove throttling and dirty/update behavior; desktop visual feel still needs an integrator smoke pass because this slice was intentionally small and non-visual.

Reviewer result:
- Status: pass-with-warnings
- Required fixes: none from automated gates.
- Residual risks:
  - Godot `ImageTexture.Update` still uploads the full texture when a dirty scoped request is accepted; this slice reduces when it happens and how much CPU image work precedes it, but does not implement GPU partial texture upload.
  - Vision-source signature skips exact unchanged sources only; moving units still recompute fog normally.
  - Camera feel needs a manual desktop pan/zoom check to tune the 50ms notification cadence if it feels too conservative.

TODO update:
- Items marked done:
  - None by this worker.
- Items left open:
  - Broader M6 render performance and camera/fog feel work.
- Reason:
  - This is a verified narrow performance slice; TODO.md is reserved for main-thread integration after review.
