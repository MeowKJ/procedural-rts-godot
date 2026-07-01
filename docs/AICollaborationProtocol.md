# AI Collaboration Protocol

This project is now run as a reviewed multi-agent workflow. No TODO item should be
treated as done only because one agent implemented it. Each step needs an owner,
an independent reviewer, and an automated evidence gate.

## Roles

Owner AI:
- Implements one bounded TODO item.
- Owns a clear file/module scope before editing.
- Does not revert unrelated work from other agents.
- Adds or updates the smallest useful automated proof.

Reviewer AI:
- Reviews the owner's changed files and the touched subsystem.
- Looks first for correctness, regressions, missing tests, and architecture drift.
- Does not rewrite the feature unless the owner asks for a concrete patch.
- Produces a pass/fail review with file references and required fixes.

Integrator AI:
- Merges the owner result with current worktree reality.
- Runs the automated gate commands.
- Updates TODO only after evidence proves the item is done.
- Keeps unfinished items open even if the direction is promising.
- Writes a review record under `docs/reviews/` for every implemented TODO slice.

## Step Contract

Every TODO step must have this contract before implementation starts:

```
Step:
Owner:
Reviewer:
Scope:
Non-goals:
Automated gates:
Manual/visual gates:
Rollback risk:
Done evidence:
```

Rules:
- Scope must name files, folders, or systems.
- Non-goals prevent silent milestone expansion.
- Automated gates must include at least one command, tool, or deterministic replay.
- Manual/visual gates are required for UI, art, camera feel, fog, and readability.
- Done evidence must be current command output, rendered artifact, or reviewed code.

## Required Gates By Work Type

Architecture / simulation:
- `dotnet build ProceduralRts.csproj`
- `dotnet run --project tools/SimReplay/SimReplay.csproj`
- `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj`
- Reviewer checks: no Godot Node/SceneTree authority in sim systems; stable entity
  order; commands write through the command buffer; no faction-based hostility.

Performance:
- `dotnet build ProceduralRts.csproj`
- `dotnet run --project tools/PerfSmoke/PerfSmoke.csproj`
- `dotnet run --project tools/ReviewGate/ReviewGate.csproj presentation`
- Reviewer checks: no new unconditional world redraw loops; culling/dirty flags for
  view work; fog update and texture upload are measured or throttled.

Fog / vision:
- `dotnet run --project tools/FogOfWarQa/FogOfWarQa.csproj`
- `dotnet run --project tools/SimReplay/SimReplay.csproj`
- Reviewer checks: gameplay visibility and visual fog stay separate; hidden enemies
  are not selectable/visible through fog; signal lights use the same vision sources.

UI / art:
- `dotnet build ProceduralRts.csproj`
- A screenshot or in-app visual check at desktop and smaller viewport sizes.
- Reviewer checks: owner color remains readable in day/fog/night; relation color
  only appears in overlays; no grid-like CommandPlate regression.

Gameplay / feel:
- `dotnet run --project tools/SimReplay/SimReplay.csproj`
- `dotnet run --project tools/PerfSmoke/PerfSmoke.csproj`
- Reviewer checks: command intent is visible; group move/attack does not clump;
  firing anchors are not shoved by incoming units.

Data / roster:
- `dotnet build ProceduralRts.csproj`
- Sandbox spawn smoke, or a deterministic authoring test when available.
- Reviewer checks: new units/buildings are data-driven; owner color is not faction
  color; specs do not store runtime state.

## Review Cadence

For every milestone:
1. Explorer AI audits the current state and names risks.
2. Owner AI implements one bounded slice.
3. ReviewGate runs the relevant automated check.
4. Reviewer AI reviews changed files plus the affected TODO item.
5. Integrator AI fixes required issues or sends the slice back.
6. TODO is updated only after gates and review pass.

## TODO Update Rules

- Mark `[x]` only when the done evidence exists now.
- Leave `[ ]` open for bridge work, shadow systems, or partial migrations.
- If a TODO is too broad to verify, split it before work starts.
- Every broad milestone keeps at least one measurable acceptance test.
- Encoding stays UTF-8 and ASCII where possible to avoid TODO corruption.
- Keep review records in `docs/reviews/` for every slice; final responses can
  summarize them, but they do not replace durable evidence.
