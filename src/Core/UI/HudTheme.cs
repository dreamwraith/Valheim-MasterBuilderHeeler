using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using MasterBuilderHeeler.Configuration;
using MasterBuilderHeeler.Core.State;
using UnityEngine;

namespace MasterBuilderHeeler.Core.UI
{
    public static class HudTheme
    {
        private static string _keycapOpen = string.Empty;
        private static string _keycapClose = string.Empty;
        private static string _measureOpen = string.Empty;
        private static string _measureClose = string.Empty;

        public static event Action? ThemeChanged;

        public static int ThemeVersion { get; private set; }

        public static Color HeaderColor { get; private set; }
        public static Color LabelColor { get; private set; }
        public static Color PrimaryValueColor { get; private set; }
        public static Color SecondaryValueColor { get; private set; }

        public static string SecondaryValueHex { get; private set; } = string.Empty;
        public static string MaxHealthHex { get; private set; } = string.Empty;

        public static string AxisPitchHex { get; private set; } = string.Empty;
        public static string AxisYawHex { get; private set; } = string.Empty;
        public static string AxisRollHex { get; private set; } = string.Empty;

        public static string StepFineHex { get; private set; } = string.Empty;
        public static string StepCoarseHex { get; private set; } = string.Empty;
        public static string StepCustomHex { get; private set; } = string.Empty;
        public static string StepStandardHex { get; private set; } = string.Empty;

        public static Gradient HealthPercentGradient { get; private set; } = null!;
        public static Gradient StabilityPercentGradient { get; private set; } = null!;

        public static string PipeSeparator { get; private set; } = string.Empty;
        public static string BrokenBarSeparator { get; private set; } = string.Empty;
        public static string SlashSeparator { get; private set; } = string.Empty;

        public static string StatusOn { get; private set; } = string.Empty;
        public static string StatusOff { get; private set; } = string.Empty;
        public static string StatusBuildCameraOn { get; private set; } = string.Empty;
        public static string StatusGhostFreezeOn { get; private set; } = string.Empty;

        public static string StatusRotationLocal { get; private set; } = string.Empty;
        public static string StatusRotationWorld { get; private set; } = string.Empty;
        public static string StatusNudgeLocal { get; private set; } = string.Empty;
        public static string StatusNudgeWorld { get; private set; } = string.Empty;
        public static string StatusGridWorld { get; private set; } = string.Empty;
        public static string StatusGridLocal { get; private set; } = string.Empty;
        public static string StatusGridTerrain { get; private set; } = string.Empty;
        public static string StatusGridFlat { get; private set; } = string.Empty;

        public static void Initialize()
        {
            Cleanup();

            ModConfig.WorldGridColor.SettingChanged += OnColorSettingChanged;
            ModConfig.LocalGridColor.SettingChanged += OnColorSettingChanged;

            IReadOnlyList<ConfigEntry<Color>> hudColors = ThemeColorConfig.AllHudColors;
            for (int index = 0; index < hudColors.Count; index++)
            {
                hudColors[index].SettingChanged += OnColorSettingChanged;
            }

            RebuildThemeStrings();
        }

        public static void Cleanup()
        {
            ModConfig.WorldGridColor.SettingChanged -= OnColorSettingChanged;
            ModConfig.LocalGridColor.SettingChanged -= OnColorSettingChanged;

            IReadOnlyList<ConfigEntry<Color>> hudColors = ThemeColorConfig.AllHudColors;
            for (int index = 0; index < hudColors.Count; index++)
            {
                hudColors[index].SettingChanged -= OnColorSettingChanged;
            }
        }

        public static void RebuildThemeStrings()
        {
            HeaderColor = ThemeColorConfig.HudHeaderColor.Value;
            LabelColor = ThemeColorConfig.HudLabelColor.Value;
            PrimaryValueColor = ThemeColorConfig.HudPrimaryValueColor.Value;
            SecondaryValueColor = ThemeColorConfig.HudSecondaryValueColor.Value;

            string worldGridHex = ToHex(ModConfig.WorldGridColor.Value);
            string localGridHex = ToHex(ModConfig.LocalGridColor.Value);
            string bracketHex = ToHex(ThemeColorConfig.HudBracketColor.Value);
            string statusOnHex = ToHex(ThemeColorConfig.HudStatusOnColor.Value);
            string statusOffHex = ToHex(ThemeColorConfig.HudStatusOffColor.Value);
            string buildCameraOnHex = ToHex(ThemeColorConfig.HudBuildCameraOnColor.Value);
            string ghostFreezeOnHex = ToHex(ThemeColorConfig.HudGhostFreezeOnColor.Value);
            string activeFrameHex = ToHex(ThemeColorConfig.HudActiveFrameColor.Value);

            SecondaryValueHex = ToHex(SecondaryValueColor);
            MaxHealthHex = ToHex(ThemeColorConfig.HudMaxHealthColor.Value);
            AxisPitchHex = ToHex(ThemeColorConfig.HudAxisPitchColor.Value);
            AxisYawHex = ToHex(ThemeColorConfig.HudAxisYawColor.Value);
            AxisRollHex = ToHex(ThemeColorConfig.HudAxisRollColor.Value);
            StepFineHex = ToHex(ThemeColorConfig.HudFineStepColor.Value);
            StepStandardHex = ToHex(ThemeColorConfig.HudStandardStepColor.Value);
            StepCoarseHex = ToHex(ThemeColorConfig.HudCoarseStepColor.Value);
            StepCustomHex = ToHex(ThemeColorConfig.HudCustomStepColor.Value);

            Color lowColor = ThemeColorConfig.HudCriticalLowColor.Value;
            Color midColor = ThemeColorConfig.HudWarningMidColor.Value;
            HealthPercentGradient = CreatePercentGradient(lowColor, midColor, ThemeColorConfig.HudHealthHighColor.Value);
            StabilityPercentGradient = CreatePercentGradient(lowColor, midColor, ThemeColorConfig.HudStabilityHighColor.Value);

            _keycapOpen = $"<color={bracketHex}>{HudGlyphs.RawKeycapOpen}</color>";
            _keycapClose = $"<color={bracketHex}>{HudGlyphs.RawKeycapClose}</color>";
            _measureOpen = $"<color={bracketHex}>{HudGlyphs.RawMeasureOpen}</color>";
            _measureClose = $"<color={bracketHex}>{HudGlyphs.RawMeasureClose}</color>";

            PipeSeparator = $" <color={bracketHex}>{HudGlyphs.RawPipe}</color> ";
            BrokenBarSeparator = $" <color={bracketHex}>{HudGlyphs.RawBrokenBar}</color> ";
            SlashSeparator = $" <color={bracketHex}>/</color> ";

            StatusOn = $"<color={statusOnHex}><b>{HudGlyphs.ToggleActive} ON</b></color>";
            StatusOff = $"<color={statusOffHex}>{HudGlyphs.ToggleInactive} OFF</color>";
            StatusBuildCameraOn = $"<color={buildCameraOnHex}><b>{HudGlyphs.ToggleActive} ON</b></color>";
            StatusGhostFreezeOn = $"<color={ghostFreezeOnHex}><b>{HudGlyphs.ToggleActive} ON</b></color>";

            StatusRotationLocal = $"<color={activeFrameHex}><b>{HudGlyphs.ToggleActive} Local</b></color>";
            StatusRotationWorld = $"<color={SecondaryValueHex}>{HudGlyphs.ToggleInactive} World</color>";
            StatusNudgeLocal = $"<color={SecondaryValueHex}>{HudGlyphs.ToggleInactive} Local</color>";
            StatusNudgeWorld = $"<color={activeFrameHex}><b>{HudGlyphs.ToggleActive} World</b></color>";
            StatusGridWorld = $"<color={worldGridHex}>{HudGlyphs.GridOrthogonal} World</color>";
            StatusGridLocal = $"<color={localGridHex}><b>{HudGlyphs.GridDiagonal} Local</b></color>";
            StatusGridTerrain = $"<color={SecondaryValueHex}>{HudGlyphs.SurfaceTerrain} Terrain</color>";
            StatusGridFlat = $"<color={activeFrameHex}><b>{HudGlyphs.SurfaceFlat} Flat</b></color>";

            ThemeVersion++;

            if (ThemeChanged != null)
            {
                ThemeChanged();
            }
        }

        public static string GetAxisColorHex(RotationAxis axis) => axis switch
        {
            RotationAxis.Pitch => AxisPitchHex,
            RotationAxis.Roll => AxisRollHex,
            _ => AxisYawHex
        };

        public static string GetStepColorHex(StepPreset preset) => preset switch
        {
            StepPreset.Fine => StepFineHex,
            StepPreset.Coarse => StepCoarseHex,
            _ => StepStandardHex
        };

        public static string Keycap(string keyContent) =>
            $"<b>{_keycapOpen}{keyContent}{_keycapClose}</b>";

        public static string Measure(string measureContent) =>
            $"{_measureOpen}{measureContent}{_measureClose}";

        public static string FormatKeycapToggle(
            string keyContent,
            bool isActive,
            string activeColorHex,
            string activeGlyph,
            string inactiveGlyph) =>
            isActive
                ? $"{Keycap(keyContent)} <color={activeColorHex}><b>{activeGlyph}</b></color>"
                : $"{Keycap(keyContent)} {inactiveGlyph}";

        // the explicit zero sections stop tiny negatives showing up as -0.0 or -0.00. meaningless to everyone but me.
        // the Quantize multipliers below ignore float drift smaller than whats on screen because im weird.
        public static string FormatEuler(Vector3 euler) =>
            Measure($"{euler.x:0.0##;-0.0##;0.0}{HudGlyphs.Degree}, {euler.y:0.0##;-0.0##;0.0}{HudGlyphs.Degree}, {euler.z:0.0##;-0.0##;0.0}{HudGlyphs.Degree}");

        public static string FormatCoordinates(Vector3 position) =>
            Measure($"{position.x:0.00;-0.00;0.00}, {position.y:0.00;-0.00;0.00}, {position.z:0.00;-0.00;0.00}");

        public static string FormatQuaternion(Quaternion rotation) =>
            Measure($"{rotation.x:0.0000;-0.0000;0.0000}, {rotation.y:0.0000;-0.0000;0.0000}, {rotation.z:0.0000;-0.0000;0.0000}, {rotation.w:0.0000;-0.0000;0.0000}");

        public static Vector3Int QuantizeEuler(Vector3 euler) =>
            Vector3Int.RoundToInt(euler * 1000f);

        public static Vector3Int QuantizeCoordinates(Vector3 position) =>
            Vector3Int.RoundToInt(position * 100f);

        public static QuantizedQuaternion QuantizeQuaternion(Quaternion rotation) =>
            new QuantizedQuaternion(
                Mathf.RoundToInt(rotation.x * 10000f),
                Mathf.RoundToInt(rotation.y * 10000f),
                Mathf.RoundToInt(rotation.z * 10000f),
                Mathf.RoundToInt(rotation.w * 10000f));

        private static void OnColorSettingChanged(object sender, EventArgs eventArgs) =>
            RebuildThemeStrings();

        private static Gradient CreatePercentGradient(Color lowColor, Color midColor, Color highColor)
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(lowColor, 0.25f),
                    new GradientColorKey(midColor, 0.50f),
                    new GradientColorKey(highColor, 1.00f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1.0f, 0.0f),
                    new GradientAlphaKey(1.0f, 1.0f)
                });
            return gradient;
        }

        private static string ToHex(Color color) => "#" + ColorUtility.ToHtmlStringRGB(color);

        public readonly struct QuantizedQuaternion : IEquatable<QuantizedQuaternion>
        {
            public readonly int X;
            public readonly int Y;
            public readonly int Z;
            public readonly int W;

            public QuantizedQuaternion(int componentX, int componentY, int componentZ, int componentW)
            {
                X = componentX;
                Y = componentY;
                Z = componentZ;
                W = componentW;
            }

            public bool Equals(QuantizedQuaternion other) =>
                X == other.X && Y == other.Y && Z == other.Z && W == other.W;
        }
    }
}
