using System;
using System.Collections.Generic;
using MasterBuilderHeeler.Configuration;
using UnityEngine;

namespace MasterBuilderHeeler.Core.UI
{
    public abstract class HudKeymapPanel : HudPanelBase
    {
        private readonly List<HudKeymapRow> _keymapRows = new List<HudKeymapRow>(18);

        protected HudKeymapPanel(
            Transform parentTransform,
            string panelName,
            PanelLayoutSettings layoutSettings)
            : base(parentTransform, panelName, layoutSettings)
        {
        }

        // the row label comes from the name of the setting. pass a label only for a row that shares a setting with
        // the row above it and so has no label of its own
        protected void AddKeymapRow(
            KeymapRowSetting setting,
            Func<string> getKeyText,
            Func<bool>? isEnabled = null,
            Func<bool>? isVisible = null,
            HudIndicator? indicator = null,
            Func<string>? getLabel = null,
            string? label = null)
        {
            HudLabelRow row = Panel.AddRow(label == null ? $"{setting.DisplayName}:" : label);
            _keymapRows.Add(new HudKeymapRow(row, setting.ShowKeymap, getKeyText, isEnabled, isVisible, indicator, getLabel));
        }

        protected void RefreshKeymapRows()
        {
            for (int index = 0; index < _keymapRows.Count; index++)
            {
                _keymapRows[index].Refresh();
            }
        }

        protected void ObserveKeymapRowWidths()
        {
            for (int index = 0; index < _keymapRows.Count; index++)
            {
                _keymapRows[index].ObserveWidths(Panel);
            }
        }
    }
}
