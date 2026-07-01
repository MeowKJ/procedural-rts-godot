# Review Record - Unit body rendering cache slice

Step: Batch unit bodies first step
Milestone: M6 Performance
Owner AI: Worker C
Reviewer AI: ReviewGate unitbodyrendering
Integrator AI: Main thread

Scope:
- Files/folders: scripts/core/UnitBodyRenderRecipe.cs; scripts/core/UnitVisualRenderer.cs; tools/ReviewGate/Program.cs; docs/reviews/2026-06-30-unit-body-rendering-slice.md
- Non-goals: No MultiMeshInstance2D batching yet; no UnitCatalog, UnitKindDesignBridge, UnitSpec/BuildSpec migration, unit art redesign, body silhouette changes, simulation behavior, or gameplay authority changes.

Implementation summary:
- Added a cached compiled render recipe for UnitArtRecipe layers so body, mount, and runtime-pulse groups are split once per recipe instead of during every CanvasItem draw.
- Precomputed closed polygon point arrays for compiled unit art layers to avoid rebuilding polylines while preserving the existing Soft Old City unit shapes and owner-color zones.
- Routed UnitVisualRenderer's UnitArtRecipe overloads through the compiled recipe cache, leaving UnitInstanceView and visual style unchanged.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/PerfSmoke/PerfSmoke.csproj`
  Result: pass
  Evidence: PerfSmoke passed; worst average was 6.737ms at 400 units, under the 16.667ms budget.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj presentation --max-warnings=0`
  Result: pass
  Evidence: ReviewGate presentation completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj unitbodyrendering`
  Result: pass
  Evidence: ReviewGate unitbodyrendering completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=unit-body-rendering-slice`
  Result: pass
  Evidence: ReviewGate review found the unit-body-rendering-slice record and completed with 0 errors and 0 warnings.

Manual/visual gates:
- Check: Visual style preservation
  Result: not run
  Evidence: This slice changes only recipe grouping/caching and reuses the same UnitShapeLayer draw calls; screenshot QA remains useful for a later atlas/MultiMesh rendering change.

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: This is an allocation/traversal reduction, not true draw-call batching; 200+ units still use individual UnitInstanceView CanvasItem draw passes until a later MultiMesh or atlas slice.

TODO update:
- Items marked done: None.
- Items left open: `Batch unit bodies (MultiMeshInstance2D or per-design atlas) so 200+ units are not 200+ CanvasItem._Draw passes of many DrawCircle/DrawArc.`
- Reason: This is a verifiable first step toward the open batching TODO, but it does not complete full unit body batching.
