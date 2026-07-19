using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using ProceduralRts.Core;

internal sealed record BattleHudVisualArtifactStructuralResult(
    bool Passed,
    IReadOnlyList<string> Checks,
    IReadOnlyList<BattleHudRuntimeControlEvidence> Controls);

internal sealed record BattleHudVisualArtifactCapture(
    string State,
    string CaptureId,
    string SourceKind,
    string CommandIntent,
    int Width,
    int Height,
    string FileName,
    long Bytes,
    string Sha256,
    IReadOnlyList<string> RequiredControls,
    IReadOnlyList<string> RequiredSignals,
    BattleHudVisualArtifactStructuralResult StructuralResult);

internal sealed record BattleHudVisualArtifactManifest(
    int SchemaVersion,
    string ExactCommit,
    string Scenario,
    string StructuralEvidenceFile,
    string Language,
    int StartingCredits,
    int MapSeed,
    string EnemyDifficulty,
    string LaunchMode,
    string Theme,
    int SettleFrames,
    int RenderFlushFrames,
    int TotalFramesPerCapture,
    int CaptureCount,
    IReadOnlyList<BattleHudVisualArtifactCapture> Captures);

internal static class BattleHudVisualArtifactManifestWriter
{
    public static void Write(
        string manifestPath,
        string structuralEvidencePath,
        string exactCommit,
        IReadOnlyList<BattleHudVisualGateCase> gateCases)
    {
        if (exactCommit.Length != 40 || !exactCommit.All(Uri.IsHexDigit))
        {
            throw new InvalidOperationException("Battle HUD artifact manifest requires a 40-character exact commit SHA.");
        }

        if (gateCases.Count != BattleHudRuntimeStateCatalog.States.Count
            * BattleHudRuntimeStateCatalog.Resolutions.Count)
        {
            throw new InvalidOperationException($"Battle HUD artifact manifest expected 18 gate cases, found {gateCases.Count}.");
        }

        var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(manifestPath))
            ?? throw new InvalidOperationException("Battle HUD artifact manifest path requires a parent directory.");
        var evidenceDirectory = Path.GetDirectoryName(Path.GetFullPath(structuralEvidencePath))
            ?? throw new InvalidOperationException("Battle HUD structural evidence path requires a parent directory.");
        if (!File.Exists(structuralEvidencePath))
        {
            throw new InvalidOperationException($"Battle HUD structural evidence is missing: {structuralEvidencePath}");
        }

        var structuralEvidence = JsonSerializer.Deserialize<List<BattleHudRuntimeStructuralEvidence>>(
                File.ReadAllText(structuralEvidencePath),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Battle HUD structural evidence could not be decoded.");
        if (structuralEvidence.Count != gateCases.Count
            || structuralEvidence.Select(item => item.FileName).Distinct(StringComparer.Ordinal).Count() != structuralEvidence.Count)
        {
            throw new InvalidOperationException(
                $"Battle HUD structural evidence must contain {gateCases.Count} unique capture results.");
        }

        var evidenceByFileName = structuralEvidence.ToDictionary(
            item => item.FileName,
            StringComparer.Ordinal);
        var captures = new List<BattleHudVisualArtifactCapture>(gateCases.Count);
        foreach (var gateCase in gateCases)
        {
            if (!evidenceByFileName.TryGetValue(gateCase.FileName, out var structural))
            {
                throw new InvalidOperationException(
                    $"Battle HUD structural evidence is missing capture {gateCase.FileName}.");
            }

            ValidateStructuralEvidence(gateCase, structural);
            var path = Path.Combine(evidenceDirectory, gateCase.FileName);
            var file = new FileInfo(path);
            if (!file.Exists || file.Length <= 4096)
            {
                throw new InvalidOperationException($"Battle HUD capture is missing or empty: {path}");
            }

            var (width, height) = ReadPngDimensions(path);
            if (width != gateCase.Resolution.Width || height != gateCase.Resolution.Height)
            {
                throw new InvalidOperationException(
                    $"Battle HUD capture {gateCase.FileName} is {width}x{height}, expected {gateCase.Resolution.Suffix}.");
            }

            using var stream = File.OpenRead(path);
            var sha256 = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
            var state = BattleHudRuntimeStateCatalog.For(gateCase.State);
            captures.Add(new BattleHudVisualArtifactCapture(
                gateCase.State.ToString(),
                gateCase.CaptureId,
                state.SourceKind.ToString(),
                state.CommandIntent.ToString(),
                width,
                height,
                gateCase.FileName,
                file.Length,
                sha256,
                gateCase.RequiredControls,
                gateCase.RequiredSignals,
                new BattleHudVisualArtifactStructuralResult(
                    structural.Passed,
                    structural.Checks,
                    structural.Controls)));
        }

        if (captures.Select(capture => capture.Sha256).Distinct(StringComparer.Ordinal).Count() != captures.Count)
        {
            throw new InvalidOperationException("Battle HUD artifact manifest rejects duplicate capture hashes.");
        }

        var config = BattleHudRuntimeStateCatalog.CaptureConfig;
        var manifest = new BattleHudVisualArtifactManifest(
            SchemaVersion: 1,
            ExactCommit: exactCommit.ToLowerInvariant(),
            Scenario: BattleHudRuntimeStateCatalog.Scenario,
            StructuralEvidenceFile: Path.GetFileName(structuralEvidencePath),
            Language: config.Language.ToString(),
            StartingCredits: config.StartingCredits,
            MapSeed: config.MapSeed,
            EnemyDifficulty: config.EnemyDifficulty.ToString(),
            LaunchMode: config.LaunchMode.ToString(),
            Theme: config.Theme.ToString(),
            SettleFrames: config.SettleFrames,
            RenderFlushFrames: config.RenderFlushFrames,
            TotalFramesPerCapture: config.SettleFrames + config.RenderFlushFrames,
            CaptureCount: captures.Count,
            Captures: captures);
        Directory.CreateDirectory(outputDirectory);
        File.WriteAllText(
            manifestPath,
            JsonSerializer.Serialize(manifest, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
            }) + Environment.NewLine);
    }

    private static void ValidateStructuralEvidence(
        BattleHudVisualGateCase gateCase,
        BattleHudRuntimeStructuralEvidence structural)
    {
        if (!structural.Passed
            || structural.Scenario != gateCase.Scenario
            || structural.State != gateCase.State.ToString()
            || structural.CaptureId != gateCase.CaptureId
            || structural.Width != gateCase.Resolution.Width
            || structural.Height != gateCase.Resolution.Height)
        {
            throw new InvalidOperationException(
                $"Battle HUD structural evidence does not match {gateCase.FileName}.");
        }

        var actualControls = structural.Controls
            .Select(control => control.ControlId)
            .ToHashSet(StringComparer.Ordinal);
        if (actualControls.Count != structural.Controls.Count
            || !actualControls.SetEquals(gateCase.RequiredControls))
        {
            throw new InvalidOperationException(
                $"Battle HUD structural evidence has the wrong critical controls for {gateCase.FileName}.");
        }

        foreach (var control in gateCase.RequiredControls)
        {
            foreach (var prefix in new[] { "visible:", "alpha:", "nonzero:", "viewport-contains:" })
            {
                if (!structural.Checks.Contains(prefix + control, StringComparer.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Battle HUD structural evidence lacks {prefix}{control} for {gateCase.FileName}.");
                }
            }

            if (IsInteractiveControl(control))
            {
                var controlEvidence = structural.Controls.Single(item => item.ControlId == control);
                if (controlEvidence.Width < 44
                    || controlEvidence.Height < 44
                    || !structural.Checks.Contains($"hit-target:{control}", StringComparer.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Battle HUD structural evidence has an undersized interactive control {control} in {gateCase.FileName}.");
                }
            }
        }

        foreach (var signal in gateCase.RequiredSignals)
        {
            if (!structural.Checks.Contains($"signal:{signal}", StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Battle HUD structural evidence lacks signal:{signal} for {gateCase.FileName}.");
            }
        }
    }

    private static bool IsInteractiveControl(string controlId) =>
        controlId is nameof(BattleHudRuntimeControlId.StanceHold)
            or nameof(BattleHudRuntimeControlId.ProductionProviderLane0)
            or nameof(BattleHudRuntimeControlId.ProductionCard)
            or nameof(BattleHudRuntimeControlId.CancelProduction);

    private static (int Width, int Height) ReadPngDimensions(string path)
    {
        Span<byte> header = stackalloc byte[24];
        using var stream = File.OpenRead(path);
        if (stream.Read(header) != header.Length
            || !header[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
        {
            throw new InvalidOperationException($"Battle HUD capture is not a PNG: {path}");
        }

        return (
            BinaryPrimitives.ReadInt32BigEndian(header.Slice(16, 4)),
            BinaryPrimitives.ReadInt32BigEndian(header.Slice(20, 4)));
    }
}
