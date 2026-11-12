using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MasterBuilderHeeler.Core.UI.Radial
{
    // the game fades a picked elements icon to the selected color and back to white (GroupElement.ChangeColor) even
    // when its hidden. these elements hide the icon and show a label in its place, so the label just copies its color
    public sealed class BuildRadialLabelTint : MonoBehaviour
    {
        private Image _icon = null!;
        private TextMeshProUGUI _label = null!;

        public void Initialize(Image icon, TextMeshProUGUI label)
        {
            _icon = icon;
            _label = label;
        }

        private void LateUpdate() => _label.color = _icon.color;
    }
}
