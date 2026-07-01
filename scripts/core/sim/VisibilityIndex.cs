using Godot;

namespace ProceduralRts.Core;

/// <summary>
/// Per-owner gameplay visibility computed each tick by <see cref="VisionSystem"/>.
/// This is the authoritative "what can owner X currently see" used for fog,
/// minimap, target legality in fog, and AI — distinct from the visual fog mask in
/// presentation (docs/RTS99Design.md "视野和迷雾": gameplay visibility and visual
/// fog are separate). Read-only to presentation.
/// </summary>
public sealed class VisibilityIndex
{
    // owner -> set of entity ids that owner can currently see.
    private readonly Dictionary<int, HashSet<int>> _visible = [];

    public void Clear()
    {
        foreach (var set in _visible.Values)
        {
            set.Clear();
        }
    }

    public void MarkVisible(OwnerId viewer, EntityId entity)
    {
        if (!_visible.TryGetValue(viewer.Value, out var set))
        {
            set = [];
            _visible[viewer.Value] = set;
        }

        set.Add(entity.Value);
    }

    public bool IsVisible(OwnerId viewer, EntityId entity)
    {
        return _visible.TryGetValue(viewer.Value, out var set) && set.Contains(entity.Value);
    }

    public int VisibleCount(OwnerId viewer)
    {
        return _visible.TryGetValue(viewer.Value, out var set) ? set.Count : 0;
    }
}
