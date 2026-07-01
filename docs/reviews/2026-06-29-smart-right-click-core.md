# Review Record - smart right-click core

Step:
Command vocabulary smart-click slice
Milestone:
M2/M7 command feel and input routing
Owner AI:
Worker implementation
Reviewer AI:
Codex integrator source audit
Integrator AI:
Codex main thread

Scope:
- Files/folders:
  - scripts/controllers/SelectionController.cs
  - scripts/core/units/runtime/UnitBattlefield.cs
  - scripts/core/CommandPreviewKind.cs
  - scripts/core/CommandAcknowledgementKind.cs
  - scripts/ui/HudLayer.cs
  - tools/CombatBehavior/Program.cs
  - tools/ReviewGate/Program.cs
- Non-goals:
  - No queued command modifier implementation.
  - No transport/load smart-click branch.
  - No Guard UI or hotkey wiring.

Implementation summary:
- Right-click hostile unit/building routes selected units through the existing attack command path.
- Right-click resource routes selected harvesters through the existing harvest command path.
- Right-click damaged self/allied unit/building routes selected repair-capable units through `RepairEntityCommand`.
- Right-click ground remains the movement fallback.
- Repair preview and acknowledgement kinds were added so the cursor/HUD path can distinguish repair intent.

Automated gates:
- Command: dotnet build ProceduralRts.csproj --no-restore
  Result: pass
  Evidence: Main project builds after the smart-click helpers and preview kinds are present.
- Command: dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore
  Result: pass
  Evidence: CombatBehavior includes assertions for resource, enemy, ground, damaged ally repair, and resource-rally smart right-click branches.
- Command: dotnet run --project tools/ReviewGate/ReviewGate.csproj smartclick --no-restore
  Result: pass
  Evidence: Focused gate checks command routing, repair legality helpers, preview/ack kinds, CombatBehavior proof strings, and this record.

Manual/visual gates:
- Check: Source preview audit
  Result: pass-with-warnings
  Evidence: `CommandPreviewKind.Repair` is drawn in HUD preview; final cursor feel still benefits from a live Godot pass.

Reviewer result:
- Status: pass-with-warnings
- Required fixes:
  - None for the narrow smart-click routing slice.
- Residual risks:
  - Transport/load smart-click and queued modifiers remain open.
  - Repair audio currently reuses move/invalid cues.

TODO update:
- Items marked done:
  - Smart rally / smart right-click can be marked done for current resource/enemy/damaged ally/ground branches after gates pass.
- Items left open:
  - Queued command modifiers remain open.
  - Transport/load smart-click remains future work.
- Reason:
  - The current slice proves the primary 1v1 skirmish smart-click branches through command-buffer-backed UnitBattlefield APIs.
