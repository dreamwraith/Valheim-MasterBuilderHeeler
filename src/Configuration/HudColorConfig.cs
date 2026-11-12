using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace MasterBuilderHeeler.Configuration
{
    public sealed class HudColorConfig(ConfigSection section)
    {
        public ConfigEntry<Color> HudHeaderColor { get; } = section.Client("HudHeaderColor", Color.white,
            "Text color for floating panel headers (Global and Precision).");

        public ConfigEntry<Color> HudLabelColor { get; } = section.Client<Color>("HudLabelColor", new Color32(255, 201, 38, 255),
            "Text color for row labels on the left side of HUD panels.");

        public ConfigEntry<Color> HudPrimaryValueColor { get; } = section.Client("HudPrimaryValueColor", Color.white,
            "Primary text color for keybind labels and active values on HUD panels.");

        public ConfigEntry<Color> HudSecondaryValueColor { get; } = section.Client<Color>("HudSecondaryValueColor", new Color32(207, 214, 220, 255),
            "Secondary text color for piece names, coordinates, angles, measurements, and default frame labels.");

        public ConfigEntry<Color> HudBracketColor { get; } = section.Client<Color>("HudBracketColor", new Color32(128, 128, 128, 255),
            "Color for keycap brackets, measurement parentheses, and value separators on HUD panels.");

        public ConfigEntry<Color> HudStatusOnColor { get; } = section.Client<Color>("HudStatusOnColor", new Color32(118, 255, 3, 255),
            "Text color for standard ON toggle indicators on HUD panels.");

        public ConfigEntry<Color> HudStatusOffColor { get; } = section.Client<Color>("HudStatusOffColor", new Color32(255, 82, 82, 255),
            "Text color for OFF toggle indicators on HUD panels.");

        public ConfigEntry<Color> HudBuildCameraOnColor { get; } = section.Client<Color>("HudBuildCameraOnColor", new Color32(0, 229, 255, 255),
            "Text color for the Build Camera ON indicator on the Global panel.");

        public ConfigEntry<Color> HudGhostFreezeOnColor { get; } = section.Client<Color>("HudGhostFreezeOnColor", new Color32(224, 64, 251, 255),
            "Text color for the Freeze Ghost ON indicator on the Global panel.");

        public ConfigEntry<Color> HudActiveFrameColor { get; } = section.Client<Color>("HudActiveFrameColor", new Color32(128, 216, 255, 255),
            "Text color for non-default frame and surface indicators (Local rotation, World nudge, and Flat surface).");

        public ConfigEntry<Color> HudUltraFineStepColor { get; } = section.Client<Color>("HudUltraFineStepColor", new Color32(68, 138, 255, 255),
            "Text color for the UltraFine step preset indicators on the Precision panel.");

        public ConfigEntry<Color> HudExtraFineStepColor { get; } = section.Client<Color>("HudExtraFineStepColor", new Color32(64, 196, 255, 255),
            "Text color for the ExtraFine step preset indicators on the Precision panel.");

        public ConfigEntry<Color> HudFineStepColor { get; } = section.Client<Color>("HudFineStepColor", new Color32(128, 216, 255, 255),
            "Text color for the Fine step preset indicators on the Precision panel.");

        public ConfigEntry<Color> HudStandardStepColor { get; } = section.Client("HudStandardStepColor", Color.white,
            "Text color for the Standard step preset indicators on the Precision panel.");

        public ConfigEntry<Color> HudCoarseStepColor { get; } = section.Client<Color>("HudCoarseStepColor", new Color32(255, 171, 64, 255),
            "Text color for the Coarse step preset indicators on the Precision panel.");

        public ConfigEntry<Color> HudCustomStepColor { get; } = section.Client<Color>("HudCustomStepColor", new Color32(234, 128, 252, 255),
            "Text color for the Custom step multiplier indicator on the Precision panel.");

        public ConfigEntry<Color> HudMaxHealthColor { get; } = section.Client<Color>("HudMaxHealthColor", new Color32(250, 250, 250, 255),
            "Text color for maximum piece health on the Hover panel.");

        public ConfigEntry<Color> HudHealthHighColor { get; } = section.Client<Color>("HudHealthHighColor", new Color32(156, 204, 101, 255),
            "Text color for full (100%) piece health on the Hover panel.");

        public ConfigEntry<Color> HudStabilityHighColor { get; } = section.Client<Color>("HudStabilityHighColor", new Color32(100, 181, 246, 255),
            "Text color for full (100%) structural stability on the Hover panel.");

        public ConfigEntry<Color> HudWarningMidColor { get; } = section.Client<Color>("HudWarningMidColor", new Color32(255, 238, 88, 255),
            "Text color for medium (50%) piece health and stability on the Hover panel.");

        public ConfigEntry<Color> HudCriticalLowColor { get; } = section.Client<Color>("HudCriticalLowColor", new Color32(239, 83, 80, 255),
            "Text color for low (25% and below) piece health and stability on the Hover panel.");

        public IReadOnlyList<ConfigEntry<Color>> All { get; } = section.All<Color>();
    }
}
