# Review Record - OrderedEntities hot path

Step:
Verify and gate that runtime simulation hot paths prefer `EntityWorld.OrderedEntities`
over allocating `StableEntities` / `StableSpecs` snapshots.

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
  - `tools/ReviewGate/Program.cs`
  - `TODO.md`
  - `docs/reviews/2026-06-29-ordered-entities-hot-path.md`
- Non-goals:
  - Do not remove `StableEntities` / `StableSpecs`; tools and tests may still use
    snapshot accessors.
  - Do not change deterministic state hash behavior.

Implementation summary:
- Added a `ReviewGate simhot` check that scans runtime scripts and warns if any
  script outside `EntityWorld` uses `StableEntities` or `StableSpecs`.
- Confirmed current runtime systems iterate `world.OrderedEntities` instead.
- Marked the hot-path preference TODO complete while leaving broader allocation
  work open.

Automated gates:
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
  Visual/runtime.
  Result:
  Not run.
  Evidence:
  This is a source-level hot-path invariant.

Reviewer result:
- Status: pass-with-warnings
- Required fixes:
  - None.
- Residual risks:
  - Tools still use snapshot accessors where allocations are acceptable.

TODO update:
- Items marked done:
  - Prefer `EntityWorld.OrderedEntities` over `StableEntities`/`StableSpecs` on
    hot paths.
- Items left open:
  - Broad sim allocation item; ~130KB/tick remains at 400 units.
- Reason:
  - The runtime hot-path invariant is now enforced by ReviewGate.
