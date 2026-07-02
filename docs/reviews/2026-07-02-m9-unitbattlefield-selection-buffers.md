# Review Record - M9 UnitBattlefield Selection Buffers

Step:
M9 UnitBattlefield selection buffer reuse (#81)

Milestone:
M9 - Elegance & Decoupling

Owner AI:
Remote Linux Codex

Reviewer AI:
Remote Linux Codex

Integrator AI:
Remote Linux Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/core/units/runtime/battlefield/UnitBattlefield.SelectionPicking.cs`, `tools/ReviewGateDomains/CommandSystemAllocationReviewGate.cs`, `TODO.md`.
- Non-goals: 不改变 pick priority、distance ordering、hover projection、minimap projection、selection UX、`PickUnit` ranking LINQ 或 `SubmitSelectionCommand` 的排序/去重语义。

Implementation summary:
- 在 `UnitBattlefield` 增加 `_selectionEntityBuffer`，复用 selection command bridge 的 entity-id set。
- 将 `SelectSingleAt(...)`、`SelectSameUnitsAt(...)`、`SelectBuildingTargetAt(...)` 改为通过 buffer helper 构建 selection set。
- 用显式稳定迭代替代 same-unit selection 的 LINQ filter。
- 扩展 `ReviewGate simhot` command allocation evidence，禁止 `UnitBattlefield.SelectionPicking` 回退到 per-command `ToHashSet()` / `new HashSet<EntityId>()`。

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: Debug build succeeded; initial parallel run had a transient DLL-copy retry warning from concurrent dotnet commands, and serial VerifyAll reruns build.
- Command: `dotnet run --project tools/SelectionStress/SelectionStress.csproj --no-restore`
  Result: pass
  Evidence: Selection stress passed 80 cases.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- simhot --max-warnings=0`
  Result: pass
  Evidence: ReviewGate simhot passed with 0 errors and 0 warnings, including UnitBattlefield selection buffer evidence.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: Full ReviewGate passed with 0 errors and 0 warnings after budget evidence updates.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll passed 23/23, including SelectionStress, PlayerLoopQa, PerfSmoke, and Godot headless QA.

Manual/visual gates:
- Check: Visual QA
  Result: not applicable
  Evidence: Selection command allocation refactor only; no rendering or UI layout changed.

Reviewer result:
- Status: pass
- Required fixes: none
- Residual risks: `SubmitSelectionCommand(...)` still builds the sorted distinct command subject list internally; this slice only removes the caller-side selection HashSet allocation.

TODO update:
- Items marked done: none; M9 per-tick allocation paydown remains open for broader profiler-guided cleanup.
- Items left open: picking/hover/minimap LINQ ranking paths and other UnitBattlefield presentation/compat bridge allocations.
- Reason: This closes only the selection command bridge allocation child slice.
