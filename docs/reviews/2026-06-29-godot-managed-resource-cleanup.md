Step: Add managed Godot resource cleanup so headless QA exits cleanly.
Milestone: Engineering Conventions / VerifyAll stability
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/ui/ManagedGodotResourceCleanup.cs`, `scripts/BattleRoot.cs`, `scripts/ui/HudLayer.cs`, `scripts/ui/TacticalAudioLayer.cs`, `scripts/world/FogOfWarLayer.cs`, `scripts/core/FogOfWarMap.cs`, `scripts/core/DisplayAudioSettings.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Added a shared cleanup traversal that detaches and disposes C#-created `LabelSettings`, theme styleboxes, canvas materials, shader resources, and texture properties.
- Added Battle scene exit cleanup for HUD icon/fog references, generated audio streams, fog mask images/textures, and event subscriptions.
- Changed SVG image loading and display settings config access to use explicit disposal.
- Non-goals: no rendering style changes, no audio design change, no VerifyAll command weakening.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `Godot_v4.7-stable_mono_win64_console.exe --headless --path . --scene res://scenes/Battle.tscn --quit-after 2`
  Result: pass
  Evidence: previously failing Battle headless step exited with status 0 after cleanup.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj godotresourcecleanup --no-restore`
  Result: pass
  Evidence: Godot resource cleanup gate completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed successfully.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 14 steps successfully, including all four Godot headless QA steps.

Reviewer result:
- Status: pass.
- Design note: the fix preserves the existing QA commands and cleans up managed wrappers before Godot Mono finalization.
- Required fixes: none.

Status:
- Pass.

Residual risks:
- Other scenes with their own C#-created resources should either reuse the shared cleanup or add local release hooks when they become part of VerifyAll.
- This does not replace future profiling for long-running resource churn during gameplay.

TODO update:
- Marked done: engineering slice `Godot headless managed-resource cleanup`.
- Left open: broader TODO roadmap items unrelated to headless shutdown stability.
