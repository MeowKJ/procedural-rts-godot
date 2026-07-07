using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public partial class HudLayer : CanvasLayer
{
    private partial class ProductionProviderLaneButton : Button
    {
        public required int Index { get; init; }
        public ProductionProviderLaneState State { get; private set; } = new(
            ProductionProviderLaneScope.Auto,
            0,
            "",
            "",
            "",
            0,
            0,
            0,
            false,
            "");
        private Color _accent = Cyan;
        private bool _selected;

        public void SetState(ProductionProviderLaneState state, bool selected, bool enabled, bool constructionMode)
        {
            State = state;
            _selected = selected;
            _accent = state.Scope switch
            {
                ProductionProviderLaneScope.Auto => Cyan,
                ProductionProviderLaneScope.All => Mint,
                _ => Amber,
            };
            Text = state.ShortLabel;
            Disabled = !enabled;
            TooltipText = enabled
                ? GameText.Format(
                    constructionMode ? "ui.constructionProviderLane.tooltip" : "ui.providerLane.tooltip",
                    state.Label,
                    state.ProviderCount,
                    state.QueueCount)
                : LocalizedDisabledReason(state.DisabledReasonKey, 0);
            QueueRedraw();
        }

        public override void _Draw()
        {
            base._Draw();
            var alpha = Disabled ? 0.32f : 0.86f;
            if (_selected)
            {
                DrawRect(new Rect2(new Vector2(3, 3), Size - new Vector2(6, 6)), new Color(_accent, 0.18f), true);
                DrawRect(new Rect2(new Vector2(2, 2), Size - new Vector2(4, 4)), new Color(_accent, 0.76f), false, 1.4f);
            }

            if (State.ActiveProgress > 0)
            {
                DrawRect(new Rect2(3, Size.Y - 5, (Size.X - 6) * State.ActiveProgress, 2), new Color(_accent, alpha), true);
            }

            if (State.QueueCount > 0)
            {
                DrawCircle(new Vector2(Size.X - 6, 6), 3.2f, new Color(_accent, alpha));
            }
        }
    }
}
