using Godot;
using ProceduralRts.Controllers;
using ProceduralRts.Core;
using ProceduralRts.Ui;
using ProceduralRts.World;

namespace ProceduralRts;

public partial class BattleRoot
{
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

        _hud.SetAlerts(_alerts
            .OrderByDescending(alert => alert.CreatedAt)
            .Take(4)
            .Select(alert => new HudLayer.AlertLine(
                alert.Kind,
                alert.FactionId,
                alert.Text,
                1 - Mathf.Clamp(alert.Age / alert.Lifetime, 0, 1)))
            .ToList());
    }

    private void RefreshCommandPreview()
    {
        var hasSelectedBuildings = UseUnitDesignRuntime
            ? _unitBattlefield.HasSelectedBuildings(PlayerSlotId.One)
            : _state.SelectedBuildings().Any();
        _hud.SetCommandPreview(_buildPlacement.IsActive ? _buildPlacement.PreviewState : _selection.PreviewState);
        _hud.SetHudContext(
            _state.SelectedCount() > 0 || _unitBattlefield.SelectedCount(PlayerSlotId.One) > 0,
            hasSelectedBuildings,
            _buildPlacement.IsActive);
    }

    private void AddStatusAlert(string status)
    {
        if (!TryUseAlertCooldown($"status:{status}", 1.2f))
        {
            return;
        }

        var mentionsCredits = status.Contains("credits", StringComparison.OrdinalIgnoreCase)
            || status.Contains(GameText.T("ui.top.credits"), StringComparison.OrdinalIgnoreCase);
        var looksLikeActionableEconomyStatus = status.StartsWith("Need ", StringComparison.OrdinalIgnoreCase)
            || status.Contains("available", StringComparison.OrdinalIgnoreCase)
            || status.Contains(GameText.T("ui.producerUnavailable"), StringComparison.OrdinalIgnoreCase)
            || status.Contains(GameText.T("production.needCredits").Split('{')[0].Trim(), StringComparison.OrdinalIgnoreCase);

        if (mentionsCredits && looksLikeActionableEconomyStatus)
        {
            AddAlert(AlertKind.Economy, status);
        }
    }

    private void AddAlert(AlertKind kind, string text, Vector2? worldPosition = null)
    {
        _alerts.Insert(0, new AlertEntry(kind, null, CompactAlertText(text), worldPosition, _elapsed));
        if (_alerts.Count > 10)
        {
            _alerts.RemoveRange(10, _alerts.Count - 10);
        }
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
        if (UseUnitDesignRuntime)
        {
            return;
        }

        var idleHarvesters = _state.Units.Count(unit =>
            unit.Owner == ProceduralRts.Core.Owner.Player
            && IsHarvestWorker(unit)
            && unit.Hp > 0
            && unit.HarvesterMode == HarvesterMode.Idle
            && unit.MoveTarget is null);

        if (idleHarvesters == 0 || _elapsed - _idleHarvesterAlertAt < IdleHarvesterAlertCooldown)
        {
            return;
        }

        _idleHarvesterAlertAt = _elapsed;
        AddAlert(AlertKind.Harvester, idleHarvesters == 1 ? GameText.T("ui.alert.idleHarvester.one") : GameText.Format("ui.alert.idleHarvester.many", idleHarvesters));
    }

    private void UpdatePowerAlert(bool force)
    {
        var powerStable = UseUnitDesignRuntime
            ? _unitBattlefield.PowerStatus(PlayerSlotId.One).IsStable
            : _state.Buildings
                .Where(building => building.Owner == ProceduralRts.Core.Owner.Player && building.Hp > 0)
                .Any(building => building.Kind == BuildingDesignIds.PowerPlant);
        if (!force && powerStable == _powerStable)
        {
            return;
        }

        _powerStable = powerStable;
        AddAlert(AlertKind.Power, powerStable ? GameText.T("ui.alert.powerStable") : GameText.T("ui.alert.powerOffline"));
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
