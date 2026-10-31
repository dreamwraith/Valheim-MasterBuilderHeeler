using System;
using TMPro;
using UnityEngine.UI;

namespace MasterBuilderHeeler.Core.UI
{
    public sealed class HudLabelRow
    {
        public HudLabelRow(
            TextMeshProUGUI label,
            TextMeshProUGUI value,
            LayoutElement leftLayoutElement,
            LayoutElement rightLayoutElement,
            bool isSecondaryValue = false)
        {
            Label = label;
            Value = value;
            LeftLayoutElement = leftLayoutElement;
            RightLayoutElement = rightLayoutElement;
            IsSecondaryValue = isSecondaryValue;
        }

        public TextMeshProUGUI Label { get; }

        public TextMeshProUGUI Value { get; }

        public LayoutElement LeftLayoutElement { get; }

        public LayoutElement RightLayoutElement { get; }

        public bool IsSecondaryValue { get; }

        public void SetActive(bool active)
        {
            if (Label.gameObject.activeSelf != active)
            {
                Label.gameObject.SetActive(active);
            }

            if (Value.gameObject.activeSelf != active)
            {
                Value.gameObject.SetActive(active);
            }
        }

        public void SetText(string label, string value)
        {
            if (!string.Equals(Label.text, label, StringComparison.Ordinal))
            {
                Label.text = label;
            }

            SetValue(value);
        }

        public void SetValue(string value)
        {
            if (!string.Equals(Value.text, value, StringComparison.Ordinal))
            {
                Value.text = value;
            }
        }
    }
}
