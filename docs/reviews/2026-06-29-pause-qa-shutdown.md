# Review Record - Pause QA Shutdown Stability

Step: Stabilize headless Pause QA shutdown.
Milestone: Verification Gates
Owner AI: Codex
Reviewer AI: Codex reviewer pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/PauseQaRoot.cs`, `tools/ReviewGate/Program.cs`.
- Non-goals: pause menu UX changes, gameplay pause semantics, visual pause screenshots, battle runtime cleanup policy outside this QA scene.

Implementation summary:
- Reworked `PauseQaRoot` to match the existing skirmish QA pattern: an always-processing runner is attached to the scene tree root, then `Battle.tscn` is loaded through `ChangeSceneToFile`.
- Removed the previous manual `PackedScene.Instantiate` child scene path that caused Godot C# wrapper finalizer crashes after the QA had already passed.
- Updated `ReviewGate pause` to require clean unpause/quit behavior and to forbid `QueueFree()` from this headless QA shutdown path.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `Godot_v4.7-stable_mono_win64_console.exe --headless --path . --scene res://scenes/PauseQa.tscn`
  Result: pass
  Evidence: `Pause QA passed: sim tick held at 4 while paused and resumed at 5.` and exit code 0.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj pause --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: all 14 steps passed, including `godot-pause-qa`.

Manual/visual gates:
- Check: Visual QA
  Result: not applicable
  Evidence: this slice only changes automated QA scene loading/shutdown behavior.

Reviewer result:
- Status: pass
- Required fixes: none
- Residual risks: The fix avoids manual freeing during headless shutdown; it does not change or test all possible user-driven quit paths.

TODO update:
- Items marked done: none.
- Items left open: none changed.
- Reason: this was verification infrastructure stabilization needed to keep `VerifyAll` reliable after the Deploy slice.
