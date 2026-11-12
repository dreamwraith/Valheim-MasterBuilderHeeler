using System;
using BepInEx.Configuration;

namespace MasterBuilderHeeler.Core.UI
{
    // a hud row that shows a key. turning its setting off hides the key and never the indicator, so a row with
    // only a key goes away completely
    public sealed class HudKeymapRow
    {
        private static readonly Func<bool> Always = () => true;

        private readonly HudLabelRow _row;
        private readonly ConfigEntry<bool> _showKeymap;
        private readonly Func<string> _getKeyText;
        private readonly Func<bool> _isEnabled;
        private readonly Func<bool> _isVisible;
        private readonly HudIndicator? _indicator;
        private readonly Func<string>? _getLabel;

        // isEnabled is the feature switch the row belongs to and isVisible is the build state it shows in. the panel
        // width is sized for every row of an enabled feature even when its not on screen, so the two are kept apart
        public HudKeymapRow(
            HudLabelRow row,
            ConfigEntry<bool> showKeymap,
            Func<string> getKeyText,
            Func<bool>? isEnabled,
            Func<bool>? isVisible,
            HudIndicator? indicator,
            Func<string>? getLabel)
        {
            _row = row;
            _showKeymap = showKeymap;
            _getKeyText = getKeyText;
            _isEnabled = isEnabled == null ? Always : isEnabled;
            _isVisible = isVisible == null ? Always : isVisible;
            _indicator = indicator;
            _getLabel = getLabel;
        }

        private bool HasContent => _showKeymap.Value || _indicator != null;

        public void Refresh()
        {
            bool isActive = _isEnabled() && _isVisible() && HasContent;
            _row.SetActive(isActive);
            if (!isActive)
            {
                return;
            }

            string value = Compose(_indicator == null ? null : _indicator.Text);
            if (_getLabel == null)
            {
                _row.SetValue(value);
            }
            else
            {
                _row.SetText(_getLabel(), value);
            }
        }

        // measures every text the row can show, so the panel width stays put when the value changes
        public void ObserveWidths(HudPanelView panel)
        {
            if (!_isEnabled() || !HasContent)
            {
                return;
            }

            string label = _row.Label.text;
            if (_indicator == null)
            {
                panel.ObserveSampleWidths(label, Compose(null));
                return;
            }

            Func<string>[] samples = _indicator.WidthSamples;
            for (int index = 0; index < samples.Length; index++)
            {
                panel.ObserveSampleWidths(label, Compose(samples[index]()));
            }
        }

        // only called for a row with content, so with the key hidden there is always an indicator
        private string Compose(string? indicatorText)
        {
            if (!_showKeymap.Value)
            {
                return indicatorText == null ? string.Empty : indicatorText;
            }

            string keycap = HudTheme.Keycap(_getKeyText());
            return indicatorText == null ? keycap : $"{keycap} {indicatorText}";
        }
    }
}
