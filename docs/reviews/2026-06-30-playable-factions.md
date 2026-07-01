# Review Record - Playable Factions

Step:
Close the playable-factions vertical-slice TODO: Dog and Cat are fully playable
in 1v1 skirmish, while Corruption remains a locked enum/UI placeholder with no
playable content.

Milestone:
Playable 1v1 skirmish vertical slice.

Owner AI:
Codex main thread.

Reviewer AI:
Integrator gate review via `ReviewGate playablefactions`.

Integrator AI:
Codex main thread.

Scope:
- Files/folders:
  - `TODO.md`
  - `tools/ReviewGate/Program.cs`
  - `docs/reviews/2026-06-30-playable-factions.md`
- Non-goals:
  - No new faction content.
  - No campaign work.
  - No multiplayer/netcode work.
  - No art redesign.

Implementation summary:
- Added `ReviewGate playablefactions` as an aggregate contract over the existing
  Dog/Cat playable proof slices.
- Locked the third faction as UI-visible but disabled and absent from
  `FactionCatalog` playable content.
- Required the durable evidence chain: roster authoring QA, runtime faction
  selection, FactionCatalog start bridge, player loop, AI loop, counter
  readability, active battle performance, and Soft Old City readability.

Automated gates:
- Command:
  `dotnet build ProceduralRts.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  Project builds before the aggregate TODO closure.
- Command:
  `dotnet run --project tools/RosterAuthoringQa/RosterAuthoringQa.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  Dog and Cat roster completeness is validated; Corruption has no playable roster.
- Command:
  `dotnet run --project tools/PlayerLoopQa/PlayerLoopQa.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  Player build, harvest, T1-T3 production, rally, selection, move/attack/stance,
  victory, and defeat are covered.
- Command:
  `dotnet run --project tools/AiOpponentLoopQa/AiOpponentLoopQa.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  Enemy harvest/build/production/defense/attack loop is covered through commands.
- Command:
  `dotnet run --project tools/CounterReadabilityQa/CounterReadabilityQa.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  Counter relationships are validated by data and deterministic combat cases.
- Command:
  `dotnet run --project tools/DesktopHudQa/DesktopHudQa.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  HUD readability stays covered for the playable desktop slice.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- playablefactions`
  Result:
  Pass.
  Evidence:
  Aggregate static gate locks the playable-faction contract.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=playable-factions`
  Result:
  Pass.
  Evidence:
  Review record exists and includes required gate evidence.
- Command:
  `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  Full project verification includes roster, player loop, AI loop, counter,
  HUD, skirmish-flow, and active-battle performance gates.

Reviewer result:
Pass. The slice is an aggregate closure of already implemented gameplay,
readability, performance, and roster gates; no new gameplay authority was added.

Status:
Pass.

Residual risks:
- Corruption intentionally has no playable content yet.
- Broader M1-M8 architecture, sandbox, construction, upgrade, and campaign work
  remains open.

TODO update:
- Marked done: `Factions: Dog and Cat fully playable; a third faction exists only
  as a locked placeholder`.
