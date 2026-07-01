# Review Record - Fog mask dirty upload

Step:
Avoid re-uploading the fog mask texture when visibility data has not changed.

Milestone:
M6 Performance.

Owner AI:
Codex main agent.

Reviewer AI:
Codex main-agent self-review; independent reviewer was not spawned because the
current thread reached the subagent limit.

Integrator AI:
Codex main agent.

Scope:
- Files/folders:
  - `scripts/core/FogOfWarMap.cs`
  - `tools/FogOfWarQa/Program.cs`
  - `tools/ReviewGate/Program.cs`
  - `TODO.md`
  - `docs/reviews/2026-06-29-fog-mask-dirty-upload.md`
- Non-goals:
  - Do not scope fog recompute to camera rect in this slice.
  - Do not add Low/Med/High fog quality settings in this slice.
  - Do not rewrite the fog shader or minimap.

Implementation summary:
- `FogOfWarMap` keeps reusable previous visible/explored strength buffers.
- `Update()` compares current mask strengths against previous strengths and marks the
  texture dirty only when mask data actually changes.
- Added `MaskRevision` and `MaskTextureUploadCount` instrumentation for QA.
- `ClearMemory()` and map-size changes update the revision/dirty path.
- `FogOfWarQa` now asserts unchanged vision sources do not dirty the mask revision,
  while changed vision sources do.
- Added `ReviewGate fog` to verify fog cache, dirty, layer, and QA hooks.

Automated gates:
- Command:
  `dotnet build ProceduralRts.csproj`
  Result:
  Pass.
  Evidence:
  Build completed with 0 warnings and 0 errors.
- Command:
  `dotnet run --project tools/FogOfWarQa/FogOfWarQa.csproj`
  Result:
  Pass.
  Evidence:
  Fog-of-war QA passed mask channels, feathered edges, explored memory, hidden mobile
  enemies, static memory, 100-source smoke, and no runtime Snapshot rendering.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj fog`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=fog-mask-dirty-upload`
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
  Runtime fog visual QA.
  Result:
  Not run.
  Evidence:
  FogOfWarQa covers logic and source structure, but no in-engine camera/fog visual
  pass was captured.

Reviewer result:
- Status: pass-with-warnings
- Required fixes:
  - None for this bounded source-level slice.
- Residual risks:
  - Independent reviewer was not available due to subagent limit.
  - Dirty detection adds previous-buffer copy/compare work on each fog update; this
    should still be cheaper than redundant texture upload when vision is unchanged,
    but runtime profiling is still useful.
  - Camera-rect scoped recompute remains open.

TODO update:
- Items marked done:
  - None; the broad fog item remains open pending independent review/runtime QA.
- Items left open:
  - Scope fog recompute to camera rect.
  - Fog quality tiers.
  - Runtime visual QA.
- Reason:
  - Evidence proves dirty-gated mask upload behavior, but not all fog rendering TODO
    work.
