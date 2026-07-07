using Godot;
using ProceduralRts.Controllers;
using ProceduralRts.Core;
using ProceduralRts.Ui;
using ProceduralRts.World;

namespace ProceduralRts;

public partial class BattleRoot
{
    private readonly List<HudLayer.AlertLine> _alertLineBuffer = [];

    private void RefreshAlerts(float delta)
    {
        for (var index = _alerts.Count - 1; index >= 0; index--)
        {
            _alerts[index].Age += delta;
            if (_alerts[index].Age >= _alerts[index].Lifetime)
            {
                _alerts.RemoveAt(index);
            }
        }

        _alertLineBuffer.Clear();
        for (var index = 0; index < _alerts.Count && _alertLineBuffer.Count < 4; index++)
        {
            var alert = _alerts[index];
            _alertLineBuffer.Add(new HudLayer.AlertLine(
                alert.Kind,
                alert.FactionId,
                alert.Text,
                1 - Mathf.Clamp(alert.Age / alert.Lifetime, 0, 1)));
        }

        _hud.SetAlerts(_alertLineBuffer);
    }

    private void RefreshCommandPreview()
    {
        var hasSelectedBuildings = UseUnitDesignRuntime
            ? _unitBattlefield.HasSelectedBuildings(PlayerSlotId.One)
            : HasSelectedLegacyBuildings();
        _hud.SetCommandPreview(_buildPlacement.IsActive ? _buildPlacement.PreviewState : _selection.PreviewState);
        _hud.SetHudContext(
            _state.SelectedCount() > 0 || _unitBattlefield.SelectedCount(PlayerSlotId.One) > 0,
            hasSelectedBuildings,
            _buildPlacement.IsActive);
    }

    private void AddStatusAlert(string status)
    {
        if (IsInsufficientCreditsStatus(status))
        {
            AddInsufficientCreditsAlert();
            return;
        }
    }

    private void AddInsufficientCreditsAlert()
    {
        if (!TryUseAlertCooldown("status:insufficient-credits", InsufficientCreditsAlertCooldown))
        {
            return;
        }

        AddAlert(AlertKind.Economy, GameText.T("ui.alert.insufficientCredits"));
    }

    private static bool IsInsufficientCreditsStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return false;
        }

        var mentionsCredits = status.Contains("credits", StringComparison.OrdinalIgnoreCase)
            || status.Contains(GameText.T("ui.top.credits"), StringComparison.OrdinalIgnoreCase);
        if (!mentionsCredits)
        {
            return false;
        }

        return ContainsLocalizedNeedCreditsPrefix(status, "ui.needCredits")
            || ContainsLocalizedNeedCreditsPrefix(status, "production.needCredits");
    }

    private static bool ContainsLocalizedNeedCreditsPrefix(string status, string key)
    {
        var prefix = GameText.T(key).Split('{')[0].Trim();
        return !string.IsNullOrWhiteSpace(prefix)
            && status.Contains(prefix, StringComparison.OrdinalIgnoreCase);
    }

    private void AddAlert(AlertKind kind, string text, Vector2? worldPosition = null)
    {
        _alerts.Insert(0, new AlertEntry(kind, null, CompactAlertText(text), worldPosition, _elapsed));
        if (_alerts.Count > 10)
        {
            _alerts.RemoveRange(10, _alerts.Count - 10);
        }
    }

    private void AddProductionCompleteAlert(string designId, string label, Vector2 worldPosition)
    {
        if (!TryUseAlertCooldown($"production-complete:{designId}", ProductionAlertCooldown))
        {
            return;
        }

        AddAlert(AlertKind.Production, GameText.Format("ui.production.deployed", label), worldPosition);
    }

    private bool TryJumpToLatestPositionedAlert()
    {
        for (var index = 0; index < _alerts.Count; index++)
        {
            if (_alerts[index].WorldPosition is not { } worldPosition)
            {
                continue;
            }

            OnMinimapJumpRequested(worldPosition);
            return true;
        }

        return false;
    }

    private bool TryUseAlertCooldown(string key, float cooldown)
    {
        if (_alertCooldowns.TryGetValue(key, out var lastTime) && _elapsed - lastTime < cooldown)
        {
            return false;
        }

        _alertCooldowns[key] = _elapsed;
        return true;
    }

    private void UpdateIdleHarvesterAlert()
    {
        Vector2? firstIdleHarvesterWorldPosition;
        int idleHarvesters;
        if (UseUnitDesignRuntime)
        {
            idleHarvesters = RuntimeIdleHarvesterCount(out firstIdleHarvesterWorldPosition);
        }
        else
        {
            idleHarvesters = IdleLegacyHarvesterCount();
            firstIdleHarvesterWorldPosition = FirstIdleLegacyHarvesterPosition();
        }

        if (idleHarvesters == 0 || _elapsed - _idleHarvesterAlertAt < IdleHarvesterAlertCooldown)
        {
            return;
        }

        _idleHarvesterAlertAt = _elapsed;
        AddAlert(
            AlertKind.Harvester,
            idleHarvesters == 1 ? GameText.T("ui.alert.idleHarvester.one") : GameText.Format("ui.alert.idleHarvester.many", idleHarvesters),
            firstIdleHarvesterWorldPosition);
    }

    private int RuntimeIdleHarvesterCount(out Vector2? firstWorldPosition)
    {
        return _unitBattlefield.IdleHarvesterCount(PlayerSlotId.One, out firstWorldPosition);
    }

    private int IdleLegacyHarvesterCount()
    {
        var count = 0;
        foreach (var unit in _state.Units)
        {
            if (unit.Owner == ProceduralRts.Core.Owner.Player
                && IsHarvestWorker(unit)
                && unit.Hp > 0
                && unit.HarvesterMode == HarvesterMode.Idle
                && unit.MoveTarget is null)
            {
                count++;
            }
        }

        return count;
    }

    private Vector2? FirstIdleLegacyHarvesterPosition()
    {
        foreach (var unit in _state.Units)
        {
            if (unit.Owner == ProceduralRts.Core.Owner.Player
                && IsHarvestWorker(unit)
                && unit.Hp > 0
                && unit.HarvesterMode == HarvesterMode.Idle
                && unit.MoveTarget is null)
            {
                return unit.Position;
            }
        }

        return null;
    }

    private void UpdatePowerAlert(bool force)
    {
        var powerStable = UseUnitDesignRuntime
            ? _unitBattlefield.PowerStatus(PlayerSlotId.One).IsStable
            : HasLegacyPlayerPowerPlant();
        if (!force && powerStable == _powerStable)
        {
            return;
        }

        _powerStable = powerStable;
        AddAlert(AlertKind.Power, powerStable ? GameText.T("ui.alert.powerStable") : GameText.T("ui.alert.powerOffline"), PowerAlertWorldPosition());
        if (!powerStable)
        {
            PlayAudioCue(TacticalAudioCue.LowPower);
        }
    }

    private Vector2? PowerAlertWorldPosition()
    {
        return UseUnitDesignRuntime
            ? RuntimePowerAlertWorldPosition()
            : LegacyPowerAlertWorldPosition();
    }

    private Vector2? RuntimePowerAlertWorldPosition()
    {
        var center = Vector2.Zero;
        var liveCount = 0;
        foreach (var building in _unitBattlefield.BuildingSnapshots())
        {
            if (building.PlayerSlotId != PlayerSlotId.One || building.Hp <= 0)
            {
                continue;
            }

            if (building.Kind == BuildingDesignIds.PowerPlant)
            {
                return building.Position;
            }

            center += building.Position;
            liveCount++;
        }

        return liveCount == 0 ? null : center / liveCount;
    }

    private Vector2? LegacyPowerAlertWorldPosition()
    {
        var center = Vector2.Zero;
        var liveCount = 0;
        foreach (var building in _state.Buildings)
        {
            if (building.Owner != ProceduralRts.Core.Owner.Player || building.Hp <= 0)
            {
                continue;
            }

            if (building.Kind == BuildingDesignIds.PowerPlant)
            {
                return building.Position;
            }

            center += building.Position;
            liveCount++;
        }

        return liveCount == 0 ? null : center / liveCount;
    }

    private bool HasSelectedLegacyBuildings()
    {
        foreach (var building in _state.Buildings)
        {
            if (building.Owner == ProceduralRts.Core.Owner.Player && building.Selected)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasLegacyPlayerPowerPlant()
    {
        foreach (var building in _state.Buildings)
        {
            if (building.Owner == ProceduralRts.Core.Owner.Player
                && building.Hp > 0
                && building.Kind == BuildingDesignIds.PowerPlant)
            {
                return true;
            }
        }

        return false;
    }

    private static string CompactAlertText(string text)
    {
        return text.Length <= 42 ? text : text[..39] + "...";
    }
    private sealed class AlertEntry(AlertKind kind, FactionId? factionId, string text, Vector2? worldPosition, float createdAt)
    {
        public AlertKind Kind { get; } = kind;
        public FactionId? FactionId { get; } = factionId;
        public string Text { get; } = text;
        public Vector2? WorldPosition { get; } = worldPosition;
        public float CreatedAt { get; } = createdAt;
        public float Age { get; set; }
        public float Lifetime { get; } = AlertLifetime;
    }
}
