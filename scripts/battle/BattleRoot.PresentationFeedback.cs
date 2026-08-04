using Godot;
using ProceduralRts.Core;
using ProceduralRts.Ui;

namespace ProceduralRts;

public partial class BattleRoot
{
    private void PlayDeathCue(IReadOnlyList<UnitInstanceDeathInfo> deaths)
    {
        if (deaths.Count > 0)
        {
            PlayAudioCue(TacticalAudioCue.Death, deaths[0].Position);
        }
    }

    private void PlayDeathCue(IReadOnlyList<UnitBattlefieldBuildingDeathInfo> deaths)
    {
        if (deaths.Count > 0)
        {
            PlayAudioCue(TacticalAudioCue.Death, deaths[0].Position);
        }
    }

    private void PlayDeathCue(Vector2? worldPosition)
    {
        if (worldPosition is { } position)
        {
            PlayAudioCue(TacticalAudioCue.Death, position);
        }
    }

    private void PlayAudioCue(TacticalAudioCue cue, Vector2? worldPosition = null)
        => _audio?.Play(cue, worldPosition, _camera is null ? null : _camera.VisibleWorldRect());

    private void RequestImpactShake(Vector2 position, ImpactVfxStyle style)
    {
        if (DisplayAudioSettings.ImpactScreenShake)
        {
            _camera.RequestImpactShake(position, style);
        }
    }
}
