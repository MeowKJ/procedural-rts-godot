using Godot;
using ProceduralRts.Core;
using CoreOwner = ProceduralRts.Core.Owner;

namespace ProceduralRts.World;

public partial class FootprintLayer : Node2D
{
    private const float MinimumSpeed = 20;
    private const float RedrawIntervalSeconds = 1f / 30f;
    private const int SoftMaxMarks = 620;
    private const int MaxMarks = 760;
    private const float UnderLoadFadeSeconds = 0.45f;

    private readonly Dictionary<int, TrailState> _trailStates = [];
    private readonly List<FootprintMark> _marks = [];
    private readonly HashSet<int> _liveUnitIds = [];
    private readonly List<int> _expiredTrailIds = [];
    private float _redrawTimer;

    public required GameState State { get; init; }
    public Rect2? CullingWorldRect { get; set; }
    public int ActiveMarkCount => _marks.Count;

    public override void _Process(double delta)
    {
        var dt = (float)delta;
        UpdateMarks(dt);
        EmitMarks(dt);
        _redrawTimer -= dt;
        if (_redrawTimer <= 0)
        {
            _redrawTimer = RedrawIntervalSeconds;
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        foreach (var mark in _marks)
        {
            if (!IsVisible(mark.Position))
            {
                continue;
            }

            if (!IsMarkVisibleToPlayer(mark))
            {
                continue;
            }

            DrawMark(mark);
        }
    }

    private bool IsVisible(Vector2 position)
    {
        return CullingWorldRect is not { } rect || rect.HasPoint(position);
    }

    private bool IsMarkVisibleToPlayer(FootprintMark mark)
    {
        return State.OwnerRelation(CoreOwner.Player, mark.Owner) is PlayerRelation.Self or PlayerRelation.Allied
            || State.IsVisibleToPlayer(mark.Position);
    }
}
