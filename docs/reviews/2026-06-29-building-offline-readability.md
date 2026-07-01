# Review Record - building offline readability

Step:
M1/M7 focused presentation slice
Milestone:
Building projection and Soft Old City readability
Owner AI:
Claude/worker implementation
Reviewer AI:
Codex integrator source audit
Integrator AI:
Codex main thread

Scope:
- Files/folders:
  - scripts/core/sim/BuildingPresentationProjection.cs
  - scripts/world/BuildingView.cs
  - tools/CombatBehavior/Program.cs
  - tools/ReviewGate/Program.cs
- Non-goals:
  - No new gameplay authority.
  - No broad damaged-state visual overhaul.
  - No TODO parent completion claim for all low-power/offline/damaged building states.

Implementation summary:
- Building presentation projections expose construction paused state and pause reason from EntityWorld construction components.
- BuildingView prefers projected power, build progress, construction pause, and pause reason values.
- BuildingView draws compact low-obstruction offline and paused-construction marks without using owner color as warning color.

Automated gates:
- Command: dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore
  Result: pass
  Evidence: ReviewGate project builds after restoring the missing buildingofflinereadability gate function visibility.
- Command: dotnet run --project tools/ReviewGate/ReviewGate.csproj buildingofflinereadability --no-restore
  Result: pass
  Evidence: Focused gate requires projected paused/offline fields, BuildingView drawing hooks, CombatBehavior assertions, and this durable review record.
- Command: dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore
  Result: pass
  Evidence: CombatBehavior passed with the projected offline readability assertions in place.

Manual/visual gates:
- Check: Source visual audit
  Result: pass-with-warnings
  Evidence: Offline and paused construction indicators are compact badge/progress treatments; screenshot QA remains useful before marking the broader building-state TODO complete.

Reviewer result:
- Status: pass-with-warnings
- Required fixes:
  - None for the narrow projected offline/paused readability slice.
- Residual risks:
  - Damaged-building state readability remains a separate visual task.
  - Final balance of badge contrast should be verified in live Godot screenshots.

TODO update:
- Items marked done:
  - None by this record alone.
- Items left open:
  - Low-power / offline / damaged building states are readable (art + alert), not silent.
- Reason:
  - This proves offline and paused-construction projection/readability only; damaged-state readability still needs a dedicated slice.
