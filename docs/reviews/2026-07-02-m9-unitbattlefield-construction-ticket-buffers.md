# Review Record - M9 UnitBattlefield Construction Ticket Buffers

Step:
M9 UnitBattlefield construction ticket buffer reuse (#80)

Milestone:
M9 - Elegance & Decoupling

Owner AI:
Remote Linux Codex

Reviewer AI:
Remote Linux Codex

Integrator AI:
Remote Linux Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/core/units/runtime/battlefield/UnitBattlefield.ConstructionTickets.cs`, `tools/ReviewGateDomains/CommandSystemAllocationReviewGate.cs`, `TODO.md`.
- Non-goals: 不改变 construction cost、build radius、placement legality、ticket lifecycle、AI construction behavior、HUD/UX 或 selection picking debt。

Implementation summary:
- 在 `UnitBattlefield` 增加 `_constructionEntityIdsBefore` 与 `_constructionTicketBuffer`，复用 construction queue/place bridge 的 before-id set 和 ticket projection storage。
- 将 queued-ticket 查找、placed-building 查找、ready-ticket collection 改为显式稳定迭代，移除 `ToHashSet()` 与 `Where(...).OrderBy(...).LastOrDefault()` 链。
- 保留 `ReadyConstructionTickets(...)` 的数组快照返回语义，避免调用方持有会被下一次 bridge 操作清空的内部 buffer。
- 扩展 `ReviewGate simhot` command allocation evidence，锁住 construction ticket bridge 不回退到 LINQ/`ToHashSet()` 路径。

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: Debug build succeeded with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/PlayerLoopQa/PlayerLoopQa.csproj --no-restore`
  Result: pass
  Evidence: PlayerLoopQa passed, including cat ready-ticket placement and player construction loop.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- simhot --max-warnings=0`
  Result: pass
  Evidence: ReviewGate simhot passed with 0 errors and 0 warnings, including construction ticket buffer evidence.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: Full ReviewGate passed with 0 errors and 0 warnings after budget evidence updates.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll passed 23/23, including PlayerLoopQa, PerfSmoke, and Godot headless QA.

Manual/visual gates:
- Check: Visual QA
  Result: not applicable
  Evidence: Runtime construction bridge allocation refactor only; no rendering or UI layout changed.

Reviewer result:
- Status: pass
- Required fixes: none
- Residual risks: `ReadyConstructionTickets(...)` still returns a final array snapshot by design; removing that allocation would require an API contract change for callers.

TODO update:
- Items marked done: none; M9 per-tick allocation paydown remains open for broader profiler-guided cleanup.
- Items left open: selection picking and other UnitBattlefield presentation/compat bridge allocations.
- Reason: This closes only the construction ticket bridge allocation child slice.
