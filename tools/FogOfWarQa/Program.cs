using System.Diagnostics;
using Godot;
using ProceduralRts.Core;

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void AssertMask(Color pixel, float red, float green, float alpha, string label)
{
    const float tolerance = 0.001f;
    Assert(MathF.Abs(pixel.R - red) <= tolerance, $"{label} red channel expected {red}, got {pixel.R}");
    Assert(MathF.Abs(pixel.G - green) <= tolerance, $"{label} green channel expected {green}, got {pixel.G}");
    Assert(MathF.Abs(pixel.A - alpha) <= tolerance, $"{label} alpha expected {alpha}, got {pixel.A}");
}

static void AssertBetween(float value, float min, float max, string label)
{
    Assert(value > min && value < max, $"{label} expected between {min} and {max}, got {value}");
}

static void Advance(WorldPresentationEnvironment environment, UnitBattlefield battlefield, float seconds)
{
    for (var elapsed = 0f; elapsed < seconds; elapsed += 0.05f)
    {
        environment.Update(0.05, battlefield, PlayerSlotId.One);
    }
}

static Rect2 BuildingRect(UnitBattlefieldBuildingSnapshot building)
{
    var spec = BuildSpecCatalog.For(building.Kind);
    return new Rect2(building.Position - spec.Footprint / 2f, spec.Footprint);
}

static string RepoRoot()
{
    var directory = new DirectoryInfo(System.Environment.CurrentDirectory);
    while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ProceduralRts.csproj")))
    {
        directory = directory.Parent;
    }

    return directory?.FullName ?? throw new InvalidOperationException("could not locate ProceduralRts.csproj");
}

static string ReadSourceWithPartials(string sourcePath)
{
    var parts = new List<string>();
    var addedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    if (File.Exists(sourcePath))
    {
        parts.Add(File.ReadAllText(sourcePath));
        addedPaths.Add(sourcePath);
    }

    var directory = Path.GetDirectoryName(sourcePath);
    var sourceName = Path.GetFileNameWithoutExtension(sourcePath);
    if (directory is not null && Directory.Exists(directory))
    {
        foreach (var partialPath in Directory.EnumerateFiles(directory, $"{sourceName}.*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path))
        {
            if (addedPaths.Add(partialPath))
            {
                parts.Add(File.ReadAllText(partialPath));
            }
        }
    }

    return string.Join("\n\n", parts);
}

var fog = new FogOfWarMap(24);
var worldSize = new Vector2(3600, 2400);
var firstScout = new Vector2(600, 600);
var secondScout = new Vector2(1260, 840);
var featherEdgePoint = firstScout + new Vector2(218, 0);
var unseenPoint = new Vector2(3220, 2120);

var expectedMaskSize = FogOfWarVisualPolicy.MaskSize(worldSize);
var lowMaskSize = FogOfWarVisualPolicy.MaskSize(worldSize, FogQualityTier.Low);
var highMaskSize = FogOfWarVisualPolicy.MaskSize(worldSize, FogQualityTier.High);
Assert(expectedMaskSize.X == 150 && expectedMaskSize.Y == 100, "fog visual policy should derive a 150x100 mask for the default 3600x2400 world");
Assert(lowMaskSize.X < expectedMaskSize.X && lowMaskSize.Y < expectedMaskSize.Y, "low fog quality should use a lower-resolution mask");
Assert(highMaskSize.X > expectedMaskSize.X && highMaskSize.Y > expectedMaskSize.Y, "high fog quality should use a higher-resolution mask");
Assert(FogOfWarVisualPolicy.WorldRedrawIntervalFor(FogQualityTier.Low) > FogOfWarVisualPolicy.WorldRedrawIntervalFor(FogQualityTier.Medium), "low fog quality should redraw less often than medium");
Assert(FogOfWarVisualPolicy.WorldRedrawIntervalFor(FogQualityTier.High) < FogOfWarVisualPolicy.WorldRedrawIntervalFor(FogQualityTier.Medium), "high fog quality should redraw more often than medium");
Assert(FogOfWarVisualPolicy.CameraScopedUploadWorldStepFor(FogQualityTier.Medium) >= FogOfWarVisualPolicy.CellSizeFor(FogQualityTier.Medium), "camera-scoped fog upload step should be cell-size aware");
Assert(CameraInputMath.StableVisualDelta(0.25f) <= CameraInputMath.MaxVisualDeltaSeconds, "camera visual delta should clamp frame hitches before pan smoothing");
Assert(MathF.Abs(CameraInputMath.StableVisualDelta(1f / 60f) - (1f / 60f)) <= 0.0001f, "camera visual delta should preserve normal 60hz frames");
AssertMask(FogOfWarVisualPolicy.MaskPixel(0, 0), 0, 0, FogOfWarVisualPolicy.UnexploredAlpha, "policy unexplored mask pixel");
AssertMask(FogOfWarVisualPolicy.MaskPixel(0, 1), 0, 1, FogOfWarVisualPolicy.ExploredMemoryAlpha, "policy explored memory mask pixel");
AssertMask(FogOfWarVisualPolicy.MaskPixel(1, 1), 1, 1, FogOfWarVisualPolicy.VisibleAlpha, "policy visible mask pixel");

fog.Update(worldSize, [(firstScout, 180f)]);
var initialStats = fog.Stats();
var firstRevision = fog.MaskRevision;
Assert(initialStats.Columns == expectedMaskSize.X && initialStats.Rows == expectedMaskSize.Y, "default 3600x2400 fog mask should follow the visual policy size");
Assert(initialStats.VisibleCells > 0, "first vision update should mark visible cells");
Assert(initialStats.VisibleCells == initialStats.ExploredCells, "fresh reveal should make visible cells explored");
Assert(fog.IsVisible(firstScout) && fog.IsExplored(firstScout), "scout position should be visible and explored");
Assert(!fog.IsVisible(unseenPoint) && !fog.IsExplored(unseenPoint), "distant unseen point should remain concealed");
AssertMask(fog.DebugMaskPixel(firstScout), 1, 1, 0, "visible mask pixel");
AssertMask(fog.DebugMaskPixel(unseenPoint), 0, 0, 0.97f, "unexplored mask pixel");
var featherEdge = fog.DebugMaskPixel(featherEdgePoint);
Assert(!fog.IsVisible(featherEdgePoint), "visual feather edge should not count as gameplay visibility");
AssertBetween(featherEdge.R, 0.08f, 0.92f, "visual feather red channel");
AssertBetween(featherEdge.G, 0.08f, 0.92f, "visual feather green channel");
AssertBetween(featherEdge.A, 0.08f, 0.92f, "visual feather alpha");

fog.Update(worldSize, [(firstScout, 180f)]);
Assert(fog.MaskRevision == firstRevision, "unchanged fog vision should not dirty the mask revision");

fog.Update(worldSize, [(secondScout, 180f)]);
Assert(fog.MaskRevision > firstRevision, "changed fog vision should dirty the mask revision");
Assert(!fog.IsVisible(firstScout) && fog.IsExplored(firstScout), "old scout position should become explored memory");
Assert(fog.IsVisible(secondScout) && fog.IsExplored(secondScout), "new scout position should be live visible");
AssertMask(fog.DebugMaskPixel(firstScout), 0, 1, 0.54f, "explored memory mask pixel");
AssertMask(fog.DebugMaskPixel(secondScout), 1, 1, 0, "new visible mask pixel");
var featherMemory = fog.DebugMaskPixel(featherEdgePoint);
AssertBetween(featherMemory.G, 0.08f, 0.92f, "visual feather memory green channel");
AssertBetween(featherMemory.A, 0.54f, 0.97f, "visual feather memory alpha");

var battlefield = new UnitBattlefield { WorldSize = worldSize };
battlefield.Relations.Set(PlayerSlotId.One, PlayerSlotId.Two, PlayerRelation.Hostile);
var state = new WorldPresentationEnvironment(worldSize);
var highQualityState = new WorldPresentationEnvironment(worldSize, FogQualityTier.High);
Assert(highQualityState.FogQuality == FogQualityTier.High, "presentation environment should retain the configured fog quality tier");
Assert(MathF.Abs(highQualityState.FogOfWar.CellSize - FogOfWarVisualPolicy.CellSizeFor(FogQualityTier.High)) < 0.001f, "presentation fog map should use quality-specific cell size");
state.SetVisualTheme(WorldVisualTheme.DayCommand, "fog-qa", transitionProgress: 1);
state.FogOfWar.ClearMemory();

var playerScout = battlefield.Spawn("generic.infantry", PlayerSlotId.One, firstScout);
var enemyMobile = battlefield.Spawn("generic.light_tank", PlayerSlotId.Two, firstScout + new Vector2(40, 0));
var enemyStructure = battlefield.UpsertBuildingTarget(
    3,
    BuildingDesignIds.PowerPlant,
    PlayerSlotId.Two,
    UnitFactionId.Cat,
    firstScout + new Vector2(96, 0),
    0,
    BuildSpecCatalog.For(BuildingDesignIds.PowerPlant).MaxHp);
Advance(state, battlefield, 0.2f);

Assert(state.IsVisible(enemyMobile.Position), "enemy mobile unit should be visible in live vision");
Assert(state.FogOfWar.AnyExplored(BuildingRect(enemyStructure)), "enemy static building should be explored while scouted");

var playerScoutEntity = battlefield.UnitEntityByInstanceId(playerScout.Id)
    ?? throw new InvalidOperationException("player scout entity should exist");
playerScoutEntity.Transform = EntityTransform.At(secondScout, playerScoutEntity.Transform.Facing);
battlefield.Update(0);
Advance(state, battlefield, 0.2f);

Assert(!state.IsVisible(enemyMobile.Position), "enemy mobile unit should be hidden in explored memory outside live vision");
Assert(state.FogOfWar.AnyExplored(BuildingRect(enemyStructure)), "enemy static building should remain in explored memory");
Assert(!state.FogOfWar.AnyVisible(BuildingRect(enemyStructure)), "enemy static building memory should not be treated as live vision");

var hiddenMinimapBuildings = battlefield.BuildingMinimapProjections(PlayerSlotId.One, _ => false);
var exploredMinimapBuildings = battlefield.BuildingMinimapProjections(PlayerSlotId.One, state.FogOfWar.AnyExplored);
Assert(hiddenMinimapBuildings.All(building => building.Id != enemyStructure.Id), "enemy static building should not enter UnitBattlefield minimap projections while unexplored");
Assert(exploredMinimapBuildings.Any(building => building.Id == enemyStructure.Id), "enemy static building should enter UnitBattlefield minimap projections from explored memory");

var stressFog = new FogOfWarMap();
var sources = Enumerable.Range(0, 100)
    .Select(index =>
    {
        var x = 160 + index % 20 * 165;
        var y = 180 + index / 20 * 380;
        return (new Vector2(x, y), 220f);
    })
    .ToArray();
stressFog.Update(worldSize, sources);
_ = stressFog.Stats();
var stressRevisionBefore = stressFog.MaskRevision;
var elapsed = Stopwatch.StartNew();
for (var i = 0; i < 1200; i++)
{
    stressFog.Update(worldSize, sources);
    _ = stressFog.Stats();
}

elapsed.Stop();
Console.WriteLine($"fog 100-source unchanged-source performance smoke: {elapsed.ElapsedMilliseconds}ms (<3500ms)");
Assert(elapsed.ElapsedMilliseconds < 3500, $"fog 100-source performance smoke took {elapsed.ElapsedMilliseconds}ms");
Assert(stressFog.MaskRevision == stressRevisionBefore, "unchanged fog vision sources should skip repeated reveal work");

var root = RepoRoot();
var runtimeSnapshotUsers = Directory
    .EnumerateFiles(Path.Combine(root, "scripts"), "*.cs", SearchOption.AllDirectories)
    .Where(path => !path.EndsWith("FogOfWarMap.cs", StringComparison.OrdinalIgnoreCase))
    .Where(path =>
    {
        var text = File.ReadAllText(path);
        return text.Contains("FogOfWar.Snapshot()", StringComparison.Ordinal)
            || text.Contains(".FogOfWar.Snapshot()", StringComparison.Ordinal);
    })
    .ToArray();
Assert(runtimeSnapshotUsers.Length == 0, "runtime scripts should not call FogOfWar.Snapshot() for normal world/minimap rendering");

var fogLayer = File.ReadAllText(Path.Combine(root, "scripts", "world", "FogOfWarLayer.cs"));
Assert(fogLayer.Contains("DrawTextureRect(texture", StringComparison.Ordinal), "world fog layer should render a single mask texture");
Assert(!fogLayer.Contains("FogOfWarCell", StringComparison.Ordinal), "world fog layer should not draw per-cell fog");
Assert(fogLayer.Contains("TextureFilter = TextureFilterEnum.Linear", StringComparison.Ordinal), "world fog mask should use linear texture filtering for non-blocky boundaries");
Assert(fogLayer.Contains("FogOfWarVisualPolicy.WorldRedrawIntervalFor", StringComparison.Ordinal), "world fog redraw throttling should use the selected fog quality tier");
Assert(fogLayer.Contains("FogOfWarVisualPolicy.CameraScopedUploadWorldStepFor", StringComparison.Ordinal), "world fog scoped upload movement should use the visual policy");
Assert(fogLayer.Contains("ShouldQueueFogRedraw", StringComparison.Ordinal)
    && fogLayer.Contains("CameraScopedRectMoved", StringComparison.Ordinal), "world fog layer should redraw only for mask revision or camera-scoped upload movement");
Assert(fogLayer.Contains("FogOfWarVisualPolicy.UnexploredOverlay", StringComparison.Ordinal)
    && fogLayer.Contains("FogOfWarVisualPolicy.ExploredMemoryOverlay", StringComparison.Ordinal)
    && fogLayer.Contains("visibility_smoothstep", StringComparison.Ordinal), "world fog shader parameters should come from the visual policy");

var fogMap = ReadSourceWithPartials(Path.Combine(root, "scripts", "core", "fog", "FogOfWarMap.cs"));
Assert(fogMap.Contains("FogOfWarVisualPolicy.MaskSize", StringComparison.Ordinal)
    && fogMap.Contains("FogOfWarVisualPolicy.MaskPixel", StringComparison.Ordinal), "fog data mask sizing and alpha semantics should be centralized through the visual policy");
Assert(fogMap.Contains("FogOfWarVisualPolicy.CellSizeFor", StringComparison.Ordinal), "fog map should support quality-specific mask resolution");
Assert(fogMap.Contains("MaskChangedSincePreviousUpdate", StringComparison.Ordinal)
    && fogMap.Contains("MaskRevision", StringComparison.Ordinal)
    && fogMap.Contains("MaskTextureUploadCount", StringComparison.Ordinal), "fog mask texture upload should be gated by actual mask changes");
Assert(fogMap.Contains("MaskTexture(Rect2? updateWorldRect", StringComparison.Ordinal)
    && fogMap.Contains("CellRangeFor(updateWorldRect)", StringComparison.Ordinal), "fog texture updates should support camera-scoped mask-cell ranges");
Assert(fogMap.Contains("_maskTextureDirty = !range.Covers", StringComparison.Ordinal), "camera-scoped fog texture update should leave off-screen dirty memory pending");
Assert(fogMap.Contains("_dirtyMaskRange", StringComparison.Ordinal)
    && fogMap.Contains("HasPendingMaskTextureUpload", StringComparison.Ordinal)
    && fogMap.Contains("VisionSourceSignature", StringComparison.Ordinal)
    && fogMap.Contains("canSkipUnchangedSources", StringComparison.Ordinal), "fog map should track dirty ranges, expose pending upload tests, and skip unchanged vision-source updates");

var cameraController = File.ReadAllText(Path.Combine(root, "scripts", "controllers", "CameraController.cs"));
var cameraMath = File.ReadAllText(Path.Combine(root, "scripts", "core", "commands", "CameraInputMath.cs"));
Assert(cameraMath.Contains("StableVisualDelta", StringComparison.Ordinal)
    && cameraMath.Contains("MaxVisualDeltaSeconds", StringComparison.Ordinal), "camera math should clamp visual dt for pan/zoom hitch resistance");
Assert(cameraController.Contains("CameraInputMath.StableVisualDelta", StringComparison.Ordinal), "camera controller should use stable visual dt for pan/zoom integration");

var battleRoot = ReadSourceWithPartials(Path.Combine(root, "scripts", "BattleRoot.cs"));
Assert(battleRoot.Contains("IsVisibleToPlayer = _presentationEnvironment.IsVisible", StringComparison.Ordinal), "mobile non-allied presentation should use live fog visibility");
Assert(battleRoot.Contains("ExploredProvider = _presentationEnvironment.FogOfWar.AnyExplored", StringComparison.Ordinal), "building presentation should use explored fog memory");
Assert(battleRoot.Contains("BuildingMinimapProjections(PlayerSlotId.One, _presentationEnvironment.FogOfWar.AnyExplored)", StringComparison.Ordinal), "live building minimap projections should be filtered by explored fog memory");
Assert(battleRoot.Contains("_presentationEnvironment.FogOfWar.MaskTexture()", StringComparison.Ordinal), "minimap should consume the cached fog mask texture");
Assert(battleRoot.Contains("_fogOfWar.VisibleWorldRect = visibleRect", StringComparison.Ordinal), "battle root should feed the camera culling rect to fog rendering");
Assert(fogLayer.Contains("VisibleWorldRect", StringComparison.Ordinal)
    && fogLayer.Contains("MaskTexture(VisibleWorldRect)", StringComparison.Ordinal), "world fog layer should request camera-scoped mask texture updates");
Assert(fogLayer.Contains("ShouldQueueImmediateFogUpload", StringComparison.Ordinal)
    && fogLayer.Contains("HasPendingMaskTextureUpload(VisibleWorldRect)", StringComparison.Ordinal), "world fog layer should upload changed visible fog promptly without camera-only redraw storms");

Console.WriteLine("Fog-of-war QA passed: mask channels, feathered edges, explored memory, hidden mobile enemies, static memory, camera-scoped texture updates, 100-source smoke, and no runtime Snapshot rendering");
