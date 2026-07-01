# Review Record - Combat mount in-place updates

Step:
Reduce simulation hot-path allocation by updating combat weapon mount runtime
state in place.

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
  - `scripts/core/sim/systems/CombatSystem.cs`
  - `tools/ReviewGate/Program.cs`
  - `TODO.md`
  - `docs/reviews/2026-06-29-combat-mount-inplace.md`
- Non-goals:
  - Do not redesign `WeaponUserComponentState`.
  - Do not claim all sim hot-path allocations are gone.
  - Do not change combat target selection, damage, cooldown, or deterministic
    outcome semantics.

Implementation summary:
- Replaced per-update `new List<WeaponMountRuntimeState>` allocation in
  `CombatSystem.EngageTarget` with indexed updates through `WritableMounts`.
- Replaced `CoolMounts()` allocation with in-place cooldown updates.
- Kept a fallback copy path for any future non-writable `IReadOnlyList` mount
  implementation, while current authored units/buildings use writable arrays.
- Extended `ReviewGate simhot` to require `WritableMounts` and warn if
  `new List<WeaponMountRuntimeState>` returns.

Automated gates:
- Command:
  `dotnet build ProceduralRts.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  Sequential build completed with 0 warnings and 0 errors. A prior parallel build
  produced a transient Godot DLL copy warning, then succeeded.
- Command:
  `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  All deterministic scenarios passed with unchanged final hashes and combat
  metrics.
- Command:
  `dotnet run --project tools/PerfSmoke/PerfSmoke.csproj -c Release --no-restore`
  Result:
  Pass.
  Evidence:
  400-unit allocation fell to 130527 bytes/tick from the previous ~188125
  bytes/tick; worst average was 1.161ms under the 16.667ms budget.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj simhot`
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

Manual/visual gates:
- Check:
  Visual runtime.
  Result:
  Not run.
  Evidence:
  This is pure deterministic combat-state plumbing; replay and perf gates cover
  this slice.

Reviewer result:
- Status: pass-with-warnings
- Required fixes:
  - None for this bounded allocation slice.
- Residual risks:
  - Independent reviewer was not available.
  - The broad sim allocation TODO remains open; PerfSmoke still reports ~130KB/tick
    at 400 units.

TODO update:
- Items marked done:
  - None.
- Items left open:
  - Broad simulation hot-path allocation item.
- Reason:
  - Combat mount list allocation is fixed and measured, but other per-tick
    allocations remain.
