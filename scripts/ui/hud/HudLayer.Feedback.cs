using Godot;

namespace ProceduralRts.Ui;

public partial class HudLayer : CanvasLayer
{
    private void UpdateProductionFeedback(float dt)
    {
        if (_productionStatusPulse > 0)
        {
            _productionStatusPulse = Mathf.Max(0, _productionStatusPulse - dt * 2.8f);
            var lift = _productionStatusPulse * 0.14f;
            _productionValue.Modulate = new Color(1f + lift, 1f + lift, 1f + lift, 1);
        }
        else if (_productionValue is not null)
        {
            _productionValue.Modulate = Colors.White;
        }

        if (_queueStatusPulse > 0)
        {
            _queueStatusPulse = Mathf.Max(0, _queueStatusPulse - dt * 2.8f);
            var lift = _queueStatusPulse * 0.18f;
            _queueValue.Modulate = new Color(1f + lift, 1f + lift, 1f + lift, 1);
            _cancelProduction.Modulate = new Color(1f + lift, 1f + lift * 0.6f, 1f + lift * 0.3f, 1);
        }
        else
        {
            _queueValue.Modulate = Colors.White;
            _cancelProduction.Modulate = Colors.White;
        }

        if (_commandResultPulse > 0)
        {
            _commandResultPulse = Mathf.Max(0, _commandResultPulse - dt * 2.8f);
            var lift = _commandResultPulse * 0.18f;
            _commandResultValue.Modulate = new Color(1f + lift, 1f + lift, 1f + lift, 1);
        }
        else if (_commandResultValue is not null)
        {
            _commandResultValue.Modulate = Colors.White;
        }
    }
}
