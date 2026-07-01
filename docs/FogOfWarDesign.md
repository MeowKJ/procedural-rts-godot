# Fog Of War Design

## Goal

Build a performant RTS fog-of-war system that is readable in real time without drawing blocky cells or expensive true mist.

The fog should have three gameplay states:

[√] Unexplored: pure black, no terrain or enemy information.
[√] Explored memory: terrain, resources, static buildings, and environment objects remain readable in a dark memory layer; enemy mobile units are hidden.
[√] Currently visible: no fog overlay; enemy mobile units can be seen and targeted.

## Rendering Strategy

Use a low-resolution visibility mask texture instead of drawing one CanvasItem shape per fog cell.

Recommended first version:

[√] Mask resolution: derive from world size at 16-24 world units per mask pixel. Current 3600x2400 world becomes about 225x150 at 16 units or 150x100 at 24 units.
[√] Channels: red = currently visible, green = explored memory.
[√] CPU update: paint vision circles into byte arrays at 8-12 Hz, not every frame.
[√] GPU rendering: draw one world-sized quad/ColorRect with a CanvasItem shader that samples the mask texture using bilinear filtering and a small blur/falloff.
[√] Visual output: pure black where explored is 0, dark blue-gray memory where explored is 1 and visible is 0, transparent where visible is 1.

This gives soft realtime fog edges with one draw call and one small texture upload per update.

Current pass: gameplay visibility stays deterministic in bool visible/explored grids, while the shared mask texture stores feathered visual strengths: red = currently visible strength, green = explored memory strength, and alpha = tactical overlay opacity. The world layer renders it through a CanvasItem shader; the minimap reuses the same texture with black modulation so the data channels do not show as red/green. This keeps far unexplored space pure black while softening the reveal boundary instead of drawing blocky cells.

QA pass: `tools/FogOfWarQa` verifies mask channel semantics, feathered edge pixels, deterministic visible-to-explored transitions, mobile enemy hiding outside live vision, static building explored-memory visibility, 100-source update performance, and absence of runtime `Snapshot()` calls in world/minimap rendering.

## Gameplay Data

Keep gameplay visibility separate from visual softness.

[√] FogVisibilityMap stores compact arrays:
   visible[x, y] byte or bool
   explored[x, y] byte or bool
   visibleStrength[x, y] float for visual softness only
   exploredStrength[x, y] float for visual memory softness only
[√] Visibility queries use logical thresholds on the mask, not shader output.
[√] Deterministic QA can use aggregate stats such as columns, rows, visible cells, explored cells, and concealed cells without allocating a full cell snapshot.
[√] Enemy mobile units render only when their position is logically visible.
[√] Enemy/static buildings render if their footprint intersects explored memory.
[√] Resources and terrain render if explored.

## Update Algorithm

Each fog update:

1. Clear the visible mask only.
2. Gather player vision sources: living player units and completed player buildings.
3. For each source, convert sight radius to mask-pixel radius.
4. Paint only the source bounding box, using distance squared.
5. Write visible = 255 inside the core radius.
6. Write a feather band outside the core radius only to visual strength channels.
7. explored = max(explored, visible).
8. Upload the Image to an existing ImageTexture with `Update`, avoiding texture recreation.

Do not allocate per-cell snapshot lists during normal gameplay.

## Performance Rules

[√] Do not draw fog per cell in `_Draw`.
[√] Do not call `Snapshot()` every frame for world fog or minimap fog.
[√] Recompute fog only when the update timer expires or vision sources move enough.
[√] Reuse byte arrays, Image, and ImageTexture.
[√] Target one world fog draw call and one minimap fog draw call.
[] Add a debug counter for mask update time and texture upload time before adding polish.

## Godot Implementation Plan

### Phase 1: Mask Data

[√] Replace or wrap `FogOfWarMap` with `FogVisibilityMap`.
[√] Add stable mask dimensions, cell size, visible/explored buffers, and fast query methods.
[√] Keep current public gameplay calls: `IsVisible`, `IsExplored`, `AnyVisible`, `AnyExplored`.
[√] Add `VisibilityTexture` or `GetMaskImageTexture()` for renderers.

### Phase 2: World Shader Layer

[√] Replace `FogOfWarLayer` cell drawing with one world-sized shader surface.
[√] Shader samples mask texture by world UV.
[√] Shader applies:
   unexplored alpha near 1.0
   explored-memory alpha around 0.48-0.62
   visible alpha 0
   3x3 or 5-tap blur only in shader
[√] Keep the result tactical and obvious, not smoky or noisy.

### Phase 3: Minimap Integration

[√] Minimap draws the same mask texture scaled to minimap bounds.
[√] Avoid per-cell minimap loops.
[√] Units/buildings/resources keep current visibility filtering rules.

### Phase 4: QA

[√] Deterministic test: unexplored/visible/explored transitions.
[√] Deterministic test: mobile enemy hidden in explored memory.
[√] Deterministic test: enemy building remains in explored memory.
[√] Performance smoke: 100 vision sources across repeated mask updates, with no runtime world/minimap fog snapshot calls.
[] Visual QA: fog edge is soft under camera pan/zoom and does not appear as large square blocks.

## Deferred Polish

[] Terrain/building line-of-sight blockers.
[] Different sensor types: radar, air vision, stealth detection.
[] Temporary scan reveal.
[] Team-shared vision.
[] Optional shader noise only if it costs no extra draw calls and remains readable.
