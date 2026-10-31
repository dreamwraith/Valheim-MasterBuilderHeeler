using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace MasterBuilderHeeler.Configuration
{
    public static class ThemeColorConfig
    {
        private const string GizmoSectionName = "Gizmo Colors";
        private const string HudSectionName = "HUD Colors";

        private static readonly List<ConfigEntry<Color>> HudColors = new List<ConfigEntry<Color>>(24);

        public static ConfigEntry<Color> GizmoPitchRingColor { get; private set; } = null!;
        public static ConfigEntry<Color> GizmoYawRingColor { get; private set; } = null!;
        public static ConfigEntry<Color> GizmoRollRingColor { get; private set; } = null!;
        public static ConfigEntry<Color> GizmoRightArrowColor { get; private set; } = null!;
        public static ConfigEntry<Color> GizmoUpArrowColor { get; private set; } = null!;
        public static ConfigEntry<Color> GizmoForwardArrowColor { get; private set; } = null!;

        public static ConfigEntry<Color> HudHeaderColor { get; private set; } = null!;
        public static ConfigEntry<Color> HudLabelColor { get; private set; } = null!;
        public static ConfigEntry<Color> HudPrimaryValueColor { get; private set; } = null!;
        public static ConfigEntry<Color> HudSecondaryValueColor { get; private set; } = null!;
        public static ConfigEntry<Color> HudBracketColor { get; private set; } = null!;

        public static ConfigEntry<Color> HudStatusOnColor { get; private set; } = null!;
        public static ConfigEntry<Color> HudStatusOffColor { get; private set; } = null!;
        public static ConfigEntry<Color> HudBuildCameraOnColor { get; private set; } = null!;
        public static ConfigEntry<Color> HudGhostFreezeOnColor { get; private set; } = null!;
        public static ConfigEntry<Color> HudActiveFrameColor { get; private set; } = null!;

        public static ConfigEntry<Color> HudAxisPitchColor { get; private set; } = null!;
        public static ConfigEntry<Color> HudAxisYawColor { get; private set; } = null!;
        public static ConfigEntry<Color> HudAxisRollColor { get; private set; } = null!;

        public static ConfigEntry<Color> HudFineStepColor { get; private set; } = null!;
        public static ConfigEntry<Color> HudStandardStepColor { get; private set; } = null!;
        public static ConfigEntry<Color> HudCoarseStepColor { get; private set; } = null!;
        public static ConfigEntry<Color> HudCustomStepColor { get; private set; } = null!;

        public static ConfigEntry<Color> HudMaxHealthColor { get; private set; } = null!;
        public static ConfigEntry<Color> HudHealthHighColor { get; private set; } = null!;
        public static ConfigEntry<Color> HudStabilityHighColor { get; private set; } = null!;
        public static ConfigEntry<Color> HudWarningMidColor { get; private set; } = null!;
        public static ConfigEntry<Color> HudCriticalLowColor { get; private set; } = null!;

        public static IReadOnlyList<ConfigEntry<Color>> AllHudColors => HudColors;

        public static void Initialize(ConfigFile config)
        {
            HudColors.Clear();

            GizmoPitchRingColor = BindGizmoColor(config, "GizmoPitchRingColor",
                new Color(1.0f, 0.25f, 0.25f, 1.0f),
                "Color and opacity of the Pitch rotation ring on the 3D gizmo.");

            GizmoYawRingColor = BindGizmoColor(config, "GizmoYawRingColor",
                new Color(0.25f, 1.0f, 0.25f, 1.0f),
                "Color and opacity of the Yaw rotation ring on the 3D gizmo.");

            GizmoRollRingColor = BindGizmoColor(config, "GizmoRollRingColor",
                new Color(0.25f, 0.65f, 1.0f, 1.0f),
                "Color and opacity of the Roll rotation ring on the 3D gizmo.");

            GizmoRightArrowColor = BindGizmoColor(config, "GizmoRightArrowColor",
                new Color(1.0f, 0.35f, 0.35f, 0.85f),
                "Color and opacity of the Right direction arrow on the 3D gizmo.");

            GizmoUpArrowColor = BindGizmoColor(config, "GizmoUpArrowColor",
                new Color(0.35f, 1.0f, 0.35f, 0.85f),
                "Color and opacity of the Up direction arrow on the 3D gizmo.");

            GizmoForwardArrowColor = BindGizmoColor(config, "GizmoForwardArrowColor",
                new Color(0.35f, 0.75f, 1.0f, 0.85f),
                "Color and opacity of the Forward direction arrow on the 3D gizmo.");

            HudHeaderColor = BindHudColor(config, "HudHeaderColor",
                Color.white,
                "Text color for floating panel headers (Global and Precision).");

            HudLabelColor = BindHudColor(config, "HudLabelColor",
                new Color(1.0f, 0.79f, 0.15f, 1.0f),
                "Text color for row labels on the left side of HUD panels.");

            HudPrimaryValueColor = BindHudColor(config, "HudPrimaryValueColor",
                Color.white,
                "Primary text color for keybind labels and active values on HUD panels.");

            HudSecondaryValueColor = BindHudColor(config, "HudSecondaryValueColor",
                (Color)new Color32(0xCF, 0xD6, 0xDC, 0xFF),
                "Secondary text color for piece names, coordinates, angles, measurements, and default frame labels.");

            HudBracketColor = BindHudColor(config, "HudBracketColor",
                (Color)new Color32(0x80, 0x80, 0x80, 0xFF),
                "Color for keycap brackets, measurement parentheses, and value separators on HUD panels.");

            HudStatusOnColor = BindHudColor(config, "HudStatusOnColor",
                (Color)new Color32(0x76, 0xFF, 0x03, 0xFF),
                "Text color for standard ON toggle indicators on HUD panels.");

            HudStatusOffColor = BindHudColor(config, "HudStatusOffColor",
                (Color)new Color32(0xFF, 0x52, 0x52, 0xFF),
                "Text color for OFF toggle indicators on HUD panels.");

            HudBuildCameraOnColor = BindHudColor(config, "HudBuildCameraOnColor",
                (Color)new Color32(0x00, 0xE5, 0xFF, 0xFF),
                "Text color for the Build Camera ON indicator on the Global panel.");

            HudGhostFreezeOnColor = BindHudColor(config, "HudGhostFreezeOnColor",
                (Color)new Color32(0xE0, 0x40, 0xFB, 0xFF),
                "Text color for the Freeze Ghost ON indicator on the Global panel.");

            HudActiveFrameColor = BindHudColor(config, "HudActiveFrameColor",
                (Color)new Color32(0x80, 0xD8, 0xFF, 0xFF),
                "Text color for non-default frame and surface indicators (Local rotation, World nudge, and Flat surface).");

            HudAxisPitchColor = BindHudColor(config, "HudAxisPitchColor",
                (Color)new Color32(0xFF, 0x52, 0x52, 0xFF),
                "Text color for Pitch rotation axis indicators on the Precision panel.");

            HudAxisYawColor = BindHudColor(config, "HudAxisYawColor",
                (Color)new Color32(0x76, 0xFF, 0x03, 0xFF),
                "Text color for Yaw rotation axis indicators on the Precision panel.");

            HudAxisRollColor = BindHudColor(config, "HudAxisRollColor",
                (Color)new Color32(0x40, 0xC4, 0xFF, 0xFF),
                "Text color for Roll rotation axis indicators on the Precision panel.");

            HudFineStepColor = BindHudColor(config, "HudFineStepColor",
                (Color)new Color32(0x80, 0xD8, 0xFF, 0xFF),
                "Text color for the Fine Step indicator on the Precision panel.");

            HudStandardStepColor = BindHudColor(config, "HudStandardStepColor",
                Color.white,
                "Text color for the Standard Step indicator on the Precision panel.");

            HudCoarseStepColor = BindHudColor(config, "HudCoarseStepColor",
                (Color)new Color32(0xFF, 0xAB, 0x40, 0xFF),
                "Text color for the Coarse Step indicator on the Precision panel.");

            HudCustomStepColor = BindHudColor(config, "HudCustomStepColor",
                (Color)new Color32(0xEA, 0x80, 0xFC, 0xFF),
                "Text color for the Custom step multiplier indicator on the Precision panel.");

            HudMaxHealthColor = BindHudColor(config, "HudMaxHealthColor",
                (Color)new Color32(0xFA, 0xFA, 0xFA, 0xFF),
                "Text color for maximum piece health on the Hover panel.");

            HudHealthHighColor = BindHudColor(config, "HudHealthHighColor",
                new Color(0.612f, 0.800f, 0.396f, 1.0f),
                "Text color for full (100%) piece health on the Hover panel.");

            HudStabilityHighColor = BindHudColor(config, "HudStabilityHighColor",
                new Color(0.392f, 0.710f, 0.965f, 1.0f),
                "Text color for full (100%) structural stability on the Hover panel.");

            HudWarningMidColor = BindHudColor(config, "HudWarningMidColor",
                new Color(1.000f, 0.933f, 0.345f, 1.0f),
                "Text color for medium (50%) piece health and stability on the Hover panel.");

            HudCriticalLowColor = BindHudColor(config, "HudCriticalLowColor",
                new Color(0.937f, 0.325f, 0.314f, 1.0f),
                "Text color for low (25% and below) piece health and stability on the Hover panel.");
        }

        private static ConfigEntry<Color> BindGizmoColor(
            ConfigFile config,
            string key,
            Color defaultValue,
            string description) =>
            ModConfig.BindClient(config, GizmoSectionName, key, defaultValue, description);

        private static ConfigEntry<Color> BindHudColor(
            ConfigFile config,
            string key,
            Color defaultValue,
            string description)
        {
            ConfigEntry<Color> entry = ModConfig.BindClient(config, HudSectionName, key, defaultValue, description);
            HudColors.Add(entry);
            return entry;
        }
    }
}
