# Review Record - Environment tone art profile

Step: M7/UI Art
Milestone: EnvironmentTone data-driven entity art
Owner AI: Worker C
Reviewer AI: ReviewGate environmenttone
Integrator AI: Main thread

Scope:
- Files/folders: scripts/core/entities/EntityRenderPalette.cs; scripts/core/UnitRenderPalette.cs; scripts/world/UnitInstanceView.cs; scripts/world/BuildingView.cs; scripts/BattleRoot.cs; tools/CombatBehavior/Program.cs; tools/ReviewGate/Program.cs
- Non-goals: No command plate, simulation system, construction, command, or combat-system behavior changes.

Implementation summary:
- Added role-aware EnvironmentTone profiles and a centralized EnvironmentTonePalette mapping for Day, FogMorning, Dusk, Night, and Corruption.
- Routed unit instance and building art through the tone palette while keeping owner color as the protected ownership signal and relation color in overlays.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: Build succeeded with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed, including environment tone art profile assertions.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj environmenttone --no-restore`
  Result: pass
  Evidence: ReviewGate environmenttone passed with 0 errors and 0 warnings.

Manual/visual gates:
- Check: Visual redraw
  Result: not run
  Evidence: This slice is data/palette plumbing with focused source and behavior gates; no large visual redraw was requested.

Reviewer result:
- Status: pass
- Required fixes: None.
- Residual risks: Tone values are intentionally conservative and may need later visual tuning against screenshot QA.

TODO update:
- Items marked done: `EnvironmentTone (Day/FogMorning/Dusk/Night/Corruption) tones Body/Ink/Shadow/Effect/Warning while Owner color keeps min contrast; layout never changes.`
- Items left open: M7/UI Art visual screenshot tuning, faction shape language, per-class silhouette rules, owner-color zone polish.
- Reason: Integrator verified the shared tone data and unit/building render paths with `CombatBehavior` and `ReviewGate environmenttone`; visual tuning remains separate.
