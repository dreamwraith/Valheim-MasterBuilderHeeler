using BepInEx.Configuration;
using UnityEngine;

namespace MasterBuilderHeeler.Configuration
{
    public sealed class PanelLayoutSettings
    {
        public PanelLayoutSettings(
            ConfigEntry<bool> show,
            ConfigEntry<float> offsetX,
            ConfigEntry<float> offsetY,
            ConfigEntry<int> fontSize,
            ConfigEntry<Color> backgroundColor)
        {
            Show = show;
            OffsetX = offsetX;
            OffsetY = offsetY;
            FontSize = fontSize;
            BackgroundColor = backgroundColor;
        }

        public ConfigEntry<bool> Show { get; }
        public ConfigEntry<float> OffsetX { get; }
        public ConfigEntry<float> OffsetY { get; }
        public ConfigEntry<int> FontSize { get; }
        public ConfigEntry<Color> BackgroundColor { get; }

        public Vector2 Offset => new Vector2(OffsetX.Value, OffsetY.Value);
    }
}
