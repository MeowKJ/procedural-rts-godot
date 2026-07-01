namespace ProceduralRts.Core;

public readonly record struct PresentationMetricsSnapshot(
    int SampleCount,
    double LastFrameMs,
    double AverageFrameMs,
    double OnePercentLowFrameMs,
    double OnePercentLowFps,
    double LastProcessMs,
    double AverageProcessMs,
    double LastRenderEstimateMs,
    double AverageRenderEstimateMs,
    double LastSimStepMs,
    double AverageSimStepMs);

public sealed class PresentationMetrics
{
    private readonly double[] _frameMs;
    private readonly double[] _processMs;
    private readonly double[] _renderEstimateMs;
    private readonly double[] _simStepMs;
    private int _next;
    private int _count;
    private double _frameTotalMs;
    private double _processTotalMs;
    private double _renderEstimateTotalMs;
    private double _simStepTotalMs;

    public PresentationMetrics(int capacity = 300)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Metric capacity must be positive.");
        }

        _frameMs = new double[capacity];
        _processMs = new double[capacity];
        _renderEstimateMs = new double[capacity];
        _simStepMs = new double[capacity];
    }

    public int Capacity => _frameMs.Length;
    public int SampleCount => _count;
    public double LastFrameMs { get; private set; }
    public double LastProcessMs { get; private set; }
    public double LastRenderEstimateMs { get; private set; }
    public double LastSimStepMs { get; private set; }
    public double AverageFrameMs => _count <= 0 ? 0 : _frameTotalMs / _count;
    public double AverageProcessMs => _count <= 0 ? 0 : _processTotalMs / _count;
    public double AverageRenderEstimateMs => _count <= 0 ? 0 : _renderEstimateTotalMs / _count;
    public double AverageSimStepMs => _count <= 0 ? 0 : _simStepTotalMs / _count;

    public void Clear()
    {
        Array.Clear(_frameMs);
        Array.Clear(_processMs);
        Array.Clear(_renderEstimateMs);
        Array.Clear(_simStepMs);
        _next = 0;
        _count = 0;
        _frameTotalMs = 0;
        _processTotalMs = 0;
        _renderEstimateTotalMs = 0;
        _simStepTotalMs = 0;
        LastFrameMs = 0;
        LastProcessMs = 0;
        LastRenderEstimateMs = 0;
        LastSimStepMs = 0;
    }

    public void RecordFrame(double frameMs, double processMs = 0, double simStepMs = 0)
    {
        frameMs = SanitizedMs(frameMs);
        processMs = SanitizedMs(processMs);
        simStepMs = SanitizedMs(simStepMs);
        var renderEstimateMs = Math.Max(0, frameMs - processMs);

        if (_count == _frameMs.Length)
        {
            _frameTotalMs -= _frameMs[_next];
            _processTotalMs -= _processMs[_next];
            _renderEstimateTotalMs -= _renderEstimateMs[_next];
            _simStepTotalMs -= _simStepMs[_next];
        }
        else
        {
            _count++;
        }

        _frameMs[_next] = frameMs;
        _processMs[_next] = processMs;
        _renderEstimateMs[_next] = renderEstimateMs;
        _simStepMs[_next] = simStepMs;
        _frameTotalMs += frameMs;
        _processTotalMs += processMs;
        _renderEstimateTotalMs += renderEstimateMs;
        _simStepTotalMs += simStepMs;

        LastFrameMs = frameMs;
        LastProcessMs = processMs;
        LastRenderEstimateMs = renderEstimateMs;
        LastSimStepMs = simStepMs;
        _next = (_next + 1) % _frameMs.Length;
    }

    public PresentationMetricsSnapshot Snapshot()
    {
        if (_count <= 0)
        {
            return new PresentationMetricsSnapshot(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        }

        var onePercentLowFrameMs = OnePercentLowFrameMs();
        var onePercentLowFps = onePercentLowFrameMs <= 0 ? 0 : 1000.0 / onePercentLowFrameMs;
        return new PresentationMetricsSnapshot(
            _count,
            LastFrameMs,
            AverageFrameMs,
            onePercentLowFrameMs,
            onePercentLowFps,
            LastProcessMs,
            AverageProcessMs,
            LastRenderEstimateMs,
            AverageRenderEstimateMs,
            LastSimStepMs,
            AverageSimStepMs);
    }

    private double OnePercentLowFrameMs()
    {
        var samples = new double[_count];
        Array.Copy(_frameMs, samples, _count);
        Array.Sort(samples);

        var worstCount = Math.Max(1, (int)Math.Ceiling(_count * 0.01));
        var total = 0.0;
        for (var index = samples.Length - worstCount; index < samples.Length; index++)
        {
            total += samples[index];
        }

        return total / worstCount;
    }

    private static double SanitizedMs(double value)
    {
        return double.IsFinite(value) && value > 0 ? value : 0;
    }
}
