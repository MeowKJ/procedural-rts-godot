# Review Record - Building entity mutable outlet cleanup

Step: Building entity mutable outlet cleanup
Milestone: M1 EntityWorld authority / building-target runtime hardening
Owner AI: Codex
Reviewer AI: Claude read-only audit plus ReviewGate buildingentitymutableoutletcleanup
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefield.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-07-01-building-entity-mutable-outlet-cleanup.md`.
- Non-goals: building combat/production/dock/power/rally behavior, EntityWorld authority, determinism.

Implementation summary:
- `UnitBattlefield` keeps the mutable lookup `private EntityInstance? BuildingEntityByTargetId(int id)` only as an internal runtime helper and never exposes it publicly.
- The only public entry point is `public EntityId? BuildingEntityIdByTargetId(int id)`, which resolves through the forward id map (`_buildingTargetEntityIds.TryGetValue(id, out var entityId)`) and fails closed when the entity no longer exists (`_entityWorld.TryGet(entityId, out _)`).
- The private mutable helper still validates EntityWorld presence (`_entityWorld.TryGet(entityId, out var entity)`).
- CombatBehavior white-box checks isolate mutable access behind the local test helper `static EntityInstance? BuildingEntityForTargetId(UnitBattlefield battlefield, int buildingId)`, which enters through the public id-based lookup and never calls the private helper.

Automated gates:
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj buildingentitymutableoutletcleanup --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: `Combat behavior passed: ...`
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: all steps passed after this slice.

Manual/visual gates:
- Check: Visual QA
  Result: not applicable
  Evidence: structural runtime-surface hardening only; no presentation change.

Reviewer result:
- Status: pass
- Required fixes: none
- Residual risks: the broader UnitBattlefield public surface still exposes other runtime data that later M1/M9 slices will narrow; this slice only closes the building-entity mutable outlet.

Status: pass

Residual risks:
- None specific to this slice beyond the noted broader-surface narrowing tracked under M1/M9.

TODO update:
- Items marked done: `Building entity mutable outlet cleanup`.
- Items left open: remaining UnitBattlefield public-surface narrowing and the M1 retire-the-shadow work.
- Reason: code invariants are present and the narrow gate passes; adjacent surface cleanup remains separate.
