using BepInEx.Configuration;
using UnityEngine;

namespace MasterBuilderHeeler.Configuration
{
    public sealed class PanelLayoutSettings(
        ConfigSection section,
        string panelDescription,
        string anchorDescription,
        float defaultOffsetX,
        float defaultOffsetY,
        int defaultFontSize)
    {
        public ConfigEntry<bool> Show { get; } = section.Client("ShowPanel", true,
            $"Displays the {panelDescription}.");

        public ConfigEntry<float> OffsetX { get; } = section.Client("OffsetX", defaultOffsetX,
            $"Horizontal offset in pixels from the {anchorDescription} for the {panelDescription}.",
            new AcceptableValueRange<float>(-5000f, 5000f));

        public ConfigEntry<float> OffsetY { get; } = section.Client("OffsetY", defaultOffsetY,
            $"Vertical offset in pixels from the {anchorDescription} for the {panelDescription}.",
            new AcceptableValueRange<float>(-3000f, 3000f));

        public ConfigEntry<int> FontSize { get; } = section.Client("FontSize", defaultFontSize,
            $"Font size in pixels for the {panelDescription}.",
            new AcceptableValueRange<int>(8, 32));

        public ConfigEntry<Color> BackgroundColor { get; } = section.Client<Color>("BackgroundColor", new Color32(20, 20, 20, 191),
            $"Background color and opacity for the {panelDescription}.");

        public Vector2 Offset => new Vector2(OffsetX.Value, OffsetY.Value);
    }
}
