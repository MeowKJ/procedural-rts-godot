using Godot;

namespace ProceduralRts.Core;

/// <summary>
/// Recomputes per-owner gameplay visibility each tick. An entity is visible to a
/// viewer-owner if it is owned/allied (always visible) or a friendly Vision
/// entity is within sight range. Writes to <see cref="EntityWorld.Visibility"/>.
///
/// Broadphase (perf): allied visibility is resolved per-owner (owners are few),
/// and hostile range checks use a spatial grid sized to the max sight range so
/// each subject only tests nearby viewers instead of every viewer. Purely
/// deterministic — visibility is a set, so insertion order does not change it.
///
/// Gameplay visibility only — the visual fog mask is presentation
/// (docs/RTS99Design.md "视野和迷雾").
/// </summary>
public sealed class VisionSystem : ISimSystem
{
    private readonly record struct Viewer(OwnerId Owner, Vector2 Position, float Range);
    private readonly SortedSet<int> _owners = [];
    private readonly List<Viewer> _viewers = [];
    private readonly SpatialGrid<Viewer> _viewerGrid = new();

    public void Step(SimContext context)
    {
        var world = context.World;
        world.Visibility.Clear();

        _owners.Clear();
        _viewers.Clear();

        var maxRange = 1f;

        foreach (var entity in world.OrderedEntities)
        {
            _owners.Add(entity.OwnerId.Value);
            if (entity.Components.TryGet<VisionComponentState>(out var vision))
            {
                var sightRange = UpgradeResolver.SightRange(world, entity, vision.SightRange);
                _viewers.Add(new Viewer(entity.OwnerId, entity.Transform.Position, sightRange));
                if (sightRange > maxRange)
                {
                    maxRange = sightRange;
                }
            }

            if (entity.Components.TryGet<ScanRevealComponentState>(out var scanReveal)
                && scanReveal.Radius > 0
                && scanReveal.DurationRemaining > 0)
            {
                _viewers.Add(new Viewer(entity.OwnerId, entity.Transform.Position, scanReveal.Radius));
                if (scanReveal.Radius > maxRange)
                {
                    maxRange = scanReveal.Radius;
                }
            }
        }

        // 1. Allied/self visibility: per owner, cheap because owners are few.
        foreach (var ownerValue in _owners)
        {
            var owner = new OwnerId(ownerValue);
            foreach (var subject in world.OrderedEntities)
            {
                var relation = world.Relations.Relation(owner, subject.OwnerId);
                if (relation is PlayerRelation.Self or PlayerRelation.Allied)
                {
                    world.Visibility.MarkVisible(owner, subject.Id);
                }
            }
        }

        if (_viewers.Count == 0)
        {
            return;
        }

        // 2. Hostile range visibility via a spatial grid of viewers. Cell size is
        // the max sight range, so a subject only needs its own + 8 neighbor cells.
        _viewerGrid.Reset(maxRange);
        foreach (var viewer in _viewers)
        {
            _viewerGrid.Add(viewer.Position, viewer);
        }

        foreach (var subject in world.OrderedEntities)
        {
            foreach (var viewer in _viewerGrid.Neighbors(subject.Transform.Position))
            {
                // Allied already handled; only hostile/neutral need a range test.
                if (world.Relations.Relation(viewer.Owner, subject.OwnerId) is PlayerRelation.Self or PlayerRelation.Allied)
                {
                    continue;
                }

                if (viewer.Position.DistanceSquaredTo(subject.Transform.Position) <= viewer.Range * viewer.Range)
                {
                    world.Visibility.MarkVisible(viewer.Owner, subject.Id);
                }
            }
        }
    }
}
