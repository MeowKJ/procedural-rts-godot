using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Controllers;

public partial class ControlGroupController : Node
{
    private const double RecallCenterDoubleTapSeconds = 0.45;

    public required UnitBattlefield UnitBattlefield { get; init; }
    public PlayerSlotId LocalPlayerSlotId { get; init; } = PlayerSlotId.One;
    public Action<int>? SelectionChanged { get; init; }
    public Action<Vector2>? FocusRequested { get; init; }
    public Action<string>? StatusChanged { get; init; }

    private readonly Dictionary<int, List<int>> _groups = [];
    private readonly List<int> _emptySelection = [];
    private readonly float[] _feedbackPulses = new float[10];
    private int? _lastRecalledGroup;
    private double _lastRecallSeconds;

    public override void _Process(double delta)
    {
        var dt = (float)delta;
        for (var index = 1; index < _feedbackPulses.Length; index++)
        {
            _feedbackPulses[index] = Mathf.Max(0, _feedbackPulses[index] - dt * 2.8f);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey key || !key.Pressed || key.Echo)
        {
            return;
        }

        var groupNumber = NumberFromKey(key.Keycode);
        if (groupNumber is null)
        {
            return;
        }

        if (key.CtrlPressed)
        {
            SaveGroup(groupNumber.Value);
            GetViewport().SetInputAsHandled();
            return;
        }

        RecallGroup(groupNumber.Value);
        GetViewport().SetInputAsHandled();
    }

    private bool UseUnitBattlefieldGroups()
    {
        return true;
    }

    private static int? NumberFromKey(Key key)
    {
        return key switch
        {
            Key.Key1 or Key.Kp1 => 1,
            Key.Key2 or Key.Kp2 => 2,
            Key.Key3 or Key.Kp3 => 3,
            Key.Key4 or Key.Kp4 => 4,
            Key.Key5 or Key.Kp5 => 5,
            Key.Key6 or Key.Kp6 => 6,
            Key.Key7 or Key.Kp7 => 7,
            Key.Key8 or Key.Kp8 => 8,
            Key.Key9 or Key.Kp9 => 9,
            _ => null,
        };
    }

}
