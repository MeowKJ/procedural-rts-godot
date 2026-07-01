using Godot;

namespace ProceduralRts.Ui;

public partial class TacticalAudioLayer : Node
{
    private const int MixRate = 22050;
    private const float DefaultDb = -13f;
    private readonly Dictionary<TacticalAudioCue, AudioStreamWav> _streams = [];
    private readonly List<AudioStreamPlayer> _players = [];
    private int _nextPlayer;

    public override void _Ready()
    {
        _streams[TacticalAudioCue.Selection] = Tone([(740f, 0.045f), (980f, 0.05f)], 0.24f);
        _streams[TacticalAudioCue.Move] = Tone([(420f, 0.045f), (560f, 0.055f)], 0.22f);
        _streams[TacticalAudioCue.Attack] = Tone([(190f, 0.05f), (96f, 0.075f), (280f, 0.035f)], 0.30f, square: true);
        _streams[TacticalAudioCue.Alert] = Tone([(880f, 0.04f), (660f, 0.04f), (880f, 0.05f)], 0.26f, square: true);
        _streams[TacticalAudioCue.Production] = Tone([(520f, 0.045f), (780f, 0.045f), (1040f, 0.08f)], 0.24f);
        _streams[TacticalAudioCue.OutcomeVictory] = Tone([(520f, 0.08f), (780f, 0.08f), (1040f, 0.14f)], 0.28f);
        _streams[TacticalAudioCue.OutcomeDefeat] = Tone([(360f, 0.08f), (220f, 0.10f), (120f, 0.16f)], 0.30f, square: true);
        _streams[TacticalAudioCue.Invalid] = Tone([(110f, 0.055f), (82f, 0.08f)], 0.26f, square: true);

        for (var index = 0; index < 6; index++)
        {
            var player = new AudioStreamPlayer
            {
                Name = $"CuePlayer_{index}",
                Bus = "Master",
                VolumeDb = DefaultDb,
            };
            _players.Add(player);
            AddChild(player);
        }
    }

    public void Play(TacticalAudioCue cue)
    {
        if (!_streams.TryGetValue(cue, out var stream) || _players.Count == 0)
        {
            return;
        }

        var player = _players[_nextPlayer];
        _nextPlayer = (_nextPlayer + 1) % _players.Count;
        player.Stop();
        player.Stream = stream;
        player.PitchScale = cue == TacticalAudioCue.Alert ? 1.04f : 1f;
        player.VolumeDb = cue is TacticalAudioCue.OutcomeVictory or TacticalAudioCue.OutcomeDefeat ? -10f : DefaultDb;
        player.Play();
    }

    public void ReleaseManagedResources()
    {
        foreach (var player in _players)
        {
            player.Stop();
            player.Stream = null;
        }

        foreach (var stream in _streams.Values)
        {
            ManagedGodotResourceCleanup.DisposeGodotObject(stream);
        }

        _streams.Clear();
    }

    private static AudioStreamWav Tone(IReadOnlyList<(float Frequency, float Duration)> notes, float amplitude, bool square = false)
    {
        var sampleCount = notes.Sum(note => Mathf.CeilToInt(note.Duration * MixRate));
        var data = new byte[sampleCount * 2];
        var cursor = 0;

        foreach (var (frequency, duration) in notes)
        {
            var noteSamples = Mathf.CeilToInt(duration * MixRate);
            for (var index = 0; index < noteSamples; index++)
            {
                var t = index / (float)MixRate;
                var phase = Mathf.Tau * frequency * t;
                var wave = square
                    ? MathF.Sign(MathF.Sin(phase)) * 0.74f + MathF.Sin(phase * 2.01f) * 0.16f
                    : MathF.Sin(phase) + MathF.Sin(phase * 2.005f) * 0.18f;
                var envelope = Envelope(index, noteSamples);
                var sample = (short)Mathf.Clamp(wave * amplitude * envelope * short.MaxValue, short.MinValue, short.MaxValue);
                data[cursor++] = (byte)(sample & 0xff);
                data[cursor++] = (byte)((sample >> 8) & 0xff);
            }
        }

        return new AudioStreamWav
        {
            Format = AudioStreamWav.FormatEnum.Format16Bits,
            MixRate = MixRate,
            Stereo = false,
            Data = data,
        };
    }

    private static float Envelope(int sampleIndex, int sampleCount)
    {
        var attack = Mathf.Max(1, Mathf.RoundToInt(sampleCount * 0.16f));
        var release = Mathf.Max(1, Mathf.RoundToInt(sampleCount * 0.28f));
        if (sampleIndex < attack)
        {
            return sampleIndex / (float)attack;
        }

        var remaining = sampleCount - sampleIndex;
        return remaining < release ? remaining / (float)release : 1f;
    }
}
