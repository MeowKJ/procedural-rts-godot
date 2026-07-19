using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Ui;

public partial class HudLayer : CanvasLayer
{
    private partial class AbilityCard : Button
    {
        public required AbilityKind Kind { get; init; }
        private AbilityCardState _state;
        public string InspectorText { get; private set; } = "";

        public void SetState(AbilityCardState state)
        {
            _state = state;
            Disabled = state.CooldownRemaining > 0.01f && !state.IsActive;
            InspectorText = AbilityInspectorText(state);
            Text = $"\n\n{AbilityCommandGrammar(state.Ability)} {AbilityStateCode(state)}";
            QueueRedraw();
        }

        public override void _Draw()
        {
            base._Draw();
            var size = Size;
            var metrics = HudVisualFoundation.MetricsFor(HudVisualPrimitive.CommandCard);
            var style = UiFactory.GetHudCommandButtonOverlayStyle(Disabled, CurrentPalette);
            var accent = AbilityAccent(_state.Ability.Kind);
            var ready = _state.CooldownRemaining <= 0.01f;
            var iconAccent = new Color(accent, ready || _state.IsActive ? 0.96f : 0.48f);
            var iconRect = new Rect2(
                new Vector2(metrics.ContentPadding * 2, metrics.ContentPadding - 1),
                new Vector2(size.X - metrics.ContentPadding * 4, 32));
            DynamicUnitIcon.DrawFallbackIcon(
                this,
                iconRect,
                AbilityIcon(_state.Ability.Kind),
                iconAccent,
                framed: false);

            DrawString(
                UiFontProfile.DrawFont(metrics.DetailFontRole),
                new Vector2(8, size.Y - 12),
                AbilityShortCode(_state.Ability.Kind),
                HorizontalAlignment.Left,
                size.X - 16,
                metrics.DetailFontSize,
                style.ShortLabel);

            var metric = AbilityMetricLine(_state.Ability);
            if (!string.IsNullOrWhiteSpace(metric))
            {
                DrawString(
                    UiFontProfile.DrawFont(metrics.DetailFontRole),
                    new Vector2(8, 17),
                    metric,
                    HorizontalAlignment.Left,
                    size.X - 16,
                    metrics.DetailFontSize,
                    new Color(CurrentPalette.TextMuted, ready ? 0.74f : 0.52f));
            }

            if (!ready && !_state.IsActive)
            {
                DrawRect(new Rect2(0, size.Y - 5, size.X, 5), new Color(CurrentPalette.Danger, 0.46f), true);
                DrawRect(new Rect2(Vector2.Zero, size), new Color(CurrentPalette.PanelSubtleFill, 0.34f), true);
            }

            if (_state.IsActive)
            {
                DrawRect(
                    new Rect2(Vector2.Zero, size).Grow(-metrics.ItemSpacing),
                    new Color(accent, 0.36f),
                    false,
                    1.8f,
                    true);
            }
        }

        private static string AbilityTooltip(AbilityCardState state)
        {
            var metric = AbilityMetricLine(state.Ability);
            return string.IsNullOrWhiteSpace(metric)
                ? $"{AbilityLabel(state.Ability.Kind)} - {AbilityCommandGrammar(state.Ability)} - {AbilityStatusText(state)}"
                : $"{AbilityLabel(state.Ability.Kind)} - {AbilityCommandGrammar(state.Ability)} - {AbilityStatusText(state)} - {metric}";
        }

        private static string AbilityInspectorText(AbilityCardState state)
        {
            var metric = AbilityMetricLine(state.Ability);
            var status = AbilityStatusText(state);
            return GameText.Format(
                "ui.catalog.inspectAbility",
                AbilityLabel(state.Ability.Kind),
                AbilityCommandGrammar(state.Ability),
                string.IsNullOrWhiteSpace(metric) ? "--" : metric,
                status);
        }

        private static string AbilityCommandGrammar(AbilitySpec ability)
        {
            return AbilityTargetRuleFor(ability) switch
            {
                AbilityTargetRule.Self => GameText.T("ui.ability.grammar.self"),
                AbilityTargetRule.Point => GameText.T("ui.ability.grammar.point"),
                AbilityTargetRule.Entity => GameText.T("ui.ability.grammar.target"),
                AbilityTargetRule.FriendlyEntity => GameText.T("ui.ability.grammar.friendly"),
                AbilityTargetRule.HostileEntity => GameText.T("ui.ability.grammar.hostile"),
                AbilityTargetRule.PointOrEntity => GameText.T("ui.ability.grammar.target"),
                AbilityTargetRule.FriendlyPointOrEntity => GameText.T("ui.ability.grammar.friendly"),
                AbilityTargetRule.HostilePointOrEntity => GameText.T("ui.ability.grammar.hostile"),
                _ => GameText.T("ui.ability.grammar.target"),
            };
        }

        private static AbilityTargetRule AbilityTargetRuleFor(AbilitySpec ability)
        {
            if (ability.TargetRule != AbilityTargetRule.Auto)
            {
                return ability.TargetRule;
            }

            return ability.Kind switch
            {
                AbilityKind.Deploy => AbilityTargetRule.Self,
                AbilityKind.Scan => AbilityTargetRule.Point,
                AbilityKind.RepairField => AbilityTargetRule.FriendlyPointOrEntity,
                AbilityKind.ShieldField => AbilityTargetRule.FriendlyPointOrEntity,
                _ => AbilityTargetRule.PointOrEntity,
            };
        }

        private static string AbilityStateCode(AbilityCardState state)
        {
            if (state.IsActive)
            {
                return GameText.T("ui.ability.state.active");
            }

            return state.CooldownRemaining > 0.01f
                ? GameText.T("ui.ability.state.cooldown")
                : GameText.T("ui.ability.state.ready");
        }

        private static string AbilityStatusText(AbilityCardState state)
        {
            if (state.IsActive)
            {
                return GameText.T("ui.ability.active");
            }

            return state.CooldownRemaining > 0.01f
                ? GameText.Format("ui.ability.cooldown", Mathf.CeilToInt(state.CooldownRemaining))
                : GameText.T("ui.status.ready");
        }

        private static string AbilityMetricLine(AbilitySpec ability)
        {
            var radius = ability.Radius > 0 ? $"R{Mathf.RoundToInt(ability.Radius)}" : "";
            var value = AbilityValueLabel(ability);
            if (string.IsNullOrWhiteSpace(radius))
            {
                return value;
            }

            return string.IsNullOrWhiteSpace(value) ? radius : $"{radius}  {value}";
        }

        private static string AbilityValueLabel(AbilitySpec ability)
        {
            return ability.Kind switch
            {
                AbilityKind.RepairField => ability.Value > 0 ? $"+{Mathf.CeilToInt(ability.Value)}/s" : "",
                AbilityKind.ShieldField => ability.Value <= 0 ? "" : ability.Value <= 1 ? $"{Mathf.RoundToInt(ability.Value * 100)}%" : Mathf.CeilToInt(ability.Value).ToString(),
                AbilityKind.Scan => ability.Value > 0 ? $"{Mathf.CeilToInt(ability.Value)}s" : "",
                AbilityKind.Deploy => ability.Value > 0 ? $"{ability.Value:0.#}x" : "",
                _ => "",
            };
        }

        private static string AbilityLabel(AbilityKind kind)
        {
            return kind switch
            {
                AbilityKind.RepairField => GameText.T("ui.ability.repairField"),
                AbilityKind.ShieldField => GameText.T("ui.ability.shieldField"),
                AbilityKind.Scan => GameText.T("ui.ability.scan"),
                AbilityKind.Deploy => GameText.T("ui.ability.deploy"),
                _ => kind.ToString(),
            };
        }

        private static string AbilityShortCode(AbilityKind kind)
        {
            return kind switch
            {
                AbilityKind.RepairField => GameText.T("ui.ability.short.repairField"),
                AbilityKind.ShieldField => GameText.T("ui.ability.short.shieldField"),
                AbilityKind.Scan => GameText.T("ui.ability.short.scan"),
                AbilityKind.Deploy => GameText.T("ui.ability.short.deploy"),
                _ => "---",
            };
        }

        private static IconGlyph AbilityIcon(AbilityKind kind)
        {
            return kind switch
            {
                AbilityKind.RepairField => IconGlyph.Repair,
                AbilityKind.ShieldField => IconGlyph.Shield,
                AbilityKind.Scan => IconGlyph.Scan,
                AbilityKind.Deploy => IconGlyph.Deploy,
                _ => IconGlyph.Ability,
            };
        }

        private static Color AbilityAccent(AbilityKind kind)
        {
            return kind switch
            {
                AbilityKind.RepairField => Mint,
                AbilityKind.ShieldField => Cyan,
                AbilityKind.Scan => Amber,
                AbilityKind.Deploy => CurrentPalette.DogCommand,
                _ => InkMuted,
            };
        }
    }
}
