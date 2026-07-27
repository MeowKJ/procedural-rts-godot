using Godot;
using ProceduralRts.Core;
using CoreOwner = ProceduralRts.Core.Owner;

namespace ProceduralRts.World;

public partial class FootprintLayer
{
    private void UpdateMarks(float dt)
    {
        for (var i = _marks.Count - 1; i >= 0; i--)
        {
            var mark = _marks[i] with { Age = _marks[i].Age + dt };
            if (mark.Age >= mark.Lifetime)
            {
                _marks.RemoveAt(i);
                continue;
            }

            _marks[i] = mark;
        }
    }

    private void EmitMarks(float dt)
    {
        _liveUnitIds.Clear();
        foreach (var unit in UnitBattlefield.Units)
        {
            if (unit.Hp <= 0)
            {
                continue;
            }

            _liveUnitIds.Add(unit.Id);
            if (!TryResolveFootprintSpecStyle(unit, out var specStyle))
            {
                ResetTrailState(unit);
                continue;
            }

            if (!ShouldEmit(unit, specStyle))
            {
                ResetTrailState(unit);
                continue;
            }

            var state = _trailStates.TryGetValue(unit.Id, out var trailState)
                ? trailState
                : new TrailState(unit.Position, 0, false);
            var moved = unit.Position.DistanceTo(state.LastPosition);
            var accumulated = state.AccumulatedDistance + moved;
            var alternate = state.Alternate;
            var direction = unit.Velocity.LengthSquared() > 1 ? unit.Velocity.Normalized() : Vector2.Right.Rotated(unit.Facing);

            while (accumulated >= specStyle.Footprint.Spacing)
            {
                accumulated -= specStyle.Footprint.Spacing;
                alternate = !alternate;
                AddMark(unit, specStyle, direction, alternate);
            }

            _trailStates[unit.Id] = new TrailState(unit.Position, accumulated, alternate);
        }

        _expiredTrailIds.Clear();
        foreach (var unitId in _trailStates.Keys)
        {
            if (!_liveUnitIds.Contains(unitId))
            {
                _expiredTrailIds.Add(unitId);
            }
        }

        foreach (var unitId in _expiredTrailIds)
        {
            _trailStates.Remove(unitId);
        }
    }

    private void ResetTrailState(UnitInstance unit)
    {
        _trailStates[unit.Id] = new TrailState(
            unit.Position,
            0,
            _trailStates.TryGetValue(unit.Id, out var oldState) && oldState.Alternate);
    }

    private bool ShouldEmit(UnitInstance unit, FootprintSpecStyle specStyle)
    {
        if (specStyle.MovementDomain == MovementDomain.Air)
        {
            return unit.Velocity.Length() >= MinimumSpeed * 1.6f;
        }

        if (!IsVisibleToPlayer(unit.Position))
        {
            return false;
        }

        return unit.Velocity.Length() >= MinimumSpeed && specStyle.Footprint.MarkKind != FootprintMarkKind.Contrail;
    }

    private void AddMark(UnitInstance unit, FootprintSpecStyle specStyle, Vector2 direction, bool alternate)
    {
        var side = new Vector2(-direction.Y, direction.X);
        var style = specStyle.Footprint;
        var basePosition = unit.Position - direction * specStyle.Radius * 0.55f;
        var mark = new FootprintMark(
            style.MarkKind,
            unit.PlayerSlotId == PlayerSlotId.One ? CoreOwner.Player : CoreOwner.Enemy,
            basePosition,
            direction,
            side,
            style.Color,
            style.Width,
            style.Length,
            style.LateralOffset,
            style.Lifetime,
            0,
            alternate);
        _marks.Add(mark);
        ApplyMarkBudget();
    }

    private void ApplyMarkBudget()
    {
        if (_marks.Count > SoftMaxMarks)
        {
            var overflow = _marks.Count - SoftMaxMarks;
            for (var index = 0; index < overflow && index < _marks.Count; index++)
            {
                var mark = _marks[index];
                var fadeAge = Mathf.Max(mark.Age, mark.Lifetime - UnderLoadFadeSeconds);
                _marks[index] = mark with { Age = fadeAge };
            }
        }

        if (_marks.Count > MaxMarks)
        {
            _marks.RemoveRange(0, _marks.Count - MaxMarks);
        }
    }
}
