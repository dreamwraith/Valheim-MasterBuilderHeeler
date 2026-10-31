using MasterBuilderHeeler.Configuration;
using UnityEngine;

namespace MasterBuilderHeeler.Core.UI
{
    public abstract class HudPanelBase
    {
        protected HudPanelBase(
            Transform parentTransform,
            string panelName,
            PanelLayoutSettings layoutSettings)
        {
            Panel = new HudPanelView(
                parentTransform,
                panelName,
                layoutSettings,
                OnFontSizeChanged,
                () => SetActive(false));
        }

        protected HudPanelView Panel { get; }

        public virtual void SetActive(bool active) => Panel.SetActive(active);

        public virtual void Destroy() => Panel.Destroy();

        protected virtual void OnFontSizeChanged()
        {
        }
    }
}
