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

        public static string ArrowRightHex { get; private set; } = string.Empty;
        public static string ArrowUpHex { get; private set; } = string.Empty;
        public static string ArrowForwardHex { get; private set; } = string.Empty;

        public static string StepUltraFineHex { get; private set; } = string.Empty;
        public static string StepExtraFineHex { get; private set; } = string.Empty;
        public static string StepFineHex { get; private set; } = string.Empty;
        public static string StepCoarseHex { get; private set; } = string.Empty;
        public static string StepCustomHex { get; private set; } = string.Empty;
        public static string StepStandardHex { get; private set; } = string.Empty;

        public static Gradient HealthPercentGradient { get; private set; } = null!;
        public static Gradient StabilityPercentGradient { get; private set; } = null!;

        public static string PipeSeparator { get; private set; } = string.Empty;
        public static string BrokenBarSeparator { get; private set; } = string.Empty;
        public static string SlashSeparator { get; private set; } = string.Empty;
        public static string VerticalEllipsisSeparator { get; private set; } = string.Empty;

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

            ModConfig.Build.WorldGridColor.SettingChanged += OnColorSettingChanged;
            ModConfig.Build.LocalGridColor.SettingChanged += OnColorSettingChanged;

            // the axis and nudge rows on the precision panel take their colors from the gizmo rings and arrows
            IReadOnlyList<ConfigEntry<Color>> gizmoColors = ModConfig.Build.GizmoColors;
            for (int index = 0; index < gizmoColors.Count; index++)
            {
                gizmoColors[index].SettingChanged += OnColorSettingChanged;
            }

            IReadOnlyList<ConfigEntry<Color>> hudColors = ModConfig.HudColors.All;
            for (int index = 0; index < hudColors.Count; index++)
            {
                hudColors[index].SettingChanged += OnColorSettingChanged;
            }

            RebuildThemeStrings();
        }

        public static void Cleanup()
        {
            ModConfig.Build.WorldGridColor.SettingChanged -= OnColorSettingChanged;
            ModConfig.Build.LocalGridColor.SettingChanged -= OnColorSettingChanged;

            IReadOnlyList<ConfigEntry<Color>> gizmoColors = ModConfig.Build.GizmoColors;
            for (int index = 0; index < gizmoColors.Count; index++)
            {
                gizmoColors[index].SettingChanged -= OnColorSettingChanged;
            }

            IReadOnlyList<ConfigEntry<Color>> hudColors = ModConfig.HudColors.All;
            for (int index = 0; index < hudColors.Count; index++)
            {
                hudColors[index].SettingChanged -= OnColorSettingChanged;
            }
        }

        public static void RebuildThemeStrings()
        {
            HeaderColor = ModConfig.HudColors.HudHeaderColor.Value;
            LabelColor = ModConfig.HudColors.HudLabelColor.Value;
            PrimaryValueColor = ModConfig.HudColors.HudPrimaryValueColor.Value;
            SecondaryValueColor = ModConfig.HudColors.HudSecondaryValueColor.Value;

            string worldGridHex = ToHex(ModConfig.Build.WorldGridColor.Value);
            string localGridHex = ToHex(ModConfig.Build.LocalGridColor.Value);
            string bracketHex = ToHex(ModConfig.HudColors.HudBracketColor.Value);
            string statusOnHex = ToHex(ModConfig.HudColors.HudStatusOnColor.Value);
            string statusOffHex = ToHex(ModConfig.HudColors.HudStatusOffColor.Value);
            string buildCameraOnHex = ToHex(ModConfig.HudColors.HudBuildCameraOnColor.Value);
            string ghostFreezeOnHex = ToHex(ModConfig.HudColors.HudGhostFreezeOnColor.Value);
            string activeFrameHex = ToHex(ModConfig.HudColors.HudActiveFrameColor.Value);

            SecondaryValueHex = ToHex(SecondaryValueColor);
            MaxHealthHex = ToHex(ModConfig.HudColors.HudMaxHealthColor.Value);
            AxisPitchHex = ToHex(ModConfig.Build.GizmoPitchRingColor.Value);
            AxisYawHex = ToHex(ModConfig.Build.GizmoYawRingColor.Value);
            AxisRollHex = ToHex(ModConfig.Build.GizmoRollRingColor.Value);
            ArrowRightHex = ToHex(ModConfig.Build.GizmoRightArrowColor.Value);
            ArrowUpHex = ToHex(ModConfig.Build.GizmoUpArrowColor.Value);
            ArrowForwardHex = ToHex(ModConfig.Build.GizmoForwardArrowColor.Value);
            StepUltraFineHex = ToHex(ModConfig.HudColors.HudUltraFineStepColor.Value);
            StepExtraFineHex = ToHex(ModConfig.HudColors.HudExtraFineStepColor.Value);
            StepFineHex = ToHex(ModConfig.HudColors.HudFineStepColor.Value);
            StepStandardHex = ToHex(ModConfig.HudColors.HudStandardStepColor.Value);
            StepCoarseHex = ToHex(ModConfig.HudColors.HudCoarseStepColor.Value);
            StepCustomHex = ToHex(ModConfig.HudColors.HudCustomStepColor.Value);

            Color lowColor = ModConfig.HudColors.HudCriticalLowColor.Value;
            Color midColor = ModConfig.HudColors.HudWarningMidColor.Value;
            HealthPercentGradient = CreatePercentGradient(lowColor, midColor, ModConfig.HudColors.HudHealthHighColor.Value);
            StabilityPercentGradient = CreatePercentGradient(lowColor, midColor, ModConfig.HudColors.HudStabilityHighColor.Value);

            _keycapOpen = $"<color={bracketHex}>{HudGlyphs.RawKeycapOpen}</color>";
            _keycapClose = $"<color={bracketHex}>{HudGlyphs.RawKeycapClose}</color>";
            _measureOpen = $"<color={bracketHex}>{HudGlyphs.RawMeasureOpen}</color>";
            _measureClose = $"<color={bracketHex}>{HudGlyphs.RawMeasureClose}</color>";

            PipeSeparator = $" <color={bracketHex}>{HudGlyphs.RawPipe}</color> ";
            BrokenBarSeparator = $" <color={bracketHex}>{HudGlyphs.RawBrokenBar}</color> ";
            SlashSeparator = $" <color={bracketHex}>/</color> ";
            VerticalEllipsisSeparator = $" <color={bracketHex}>{HudGlyphs.RawVerticalEllipsis}</color> ";

            StatusOn = $"<color={statusOnHex}><b>{HudGlyphs.ToggleActive} ON</b></color>";
            StatusOff = $"<color={statusOffHex}>{HudGlyphs.ToggleInactive} OFF</color>";
            StatusBuildCameraOn = $"<color={buildCameraOnHex}><b>{HudGlyphs.ToggleActive} ON</b></color>";
            StatusGhostFreezeOn = $"<color={ghostFreezeOnHex}><b>{HudGlyphs.ToggleActive} ON</b></color>";

            StatusRotationLocal = $"<color={activeFrameHex}><b>{HudGlyphs.HouseBuilding} Local</b></color>";
            StatusRotationWorld = $"<color={SecondaryValueHex}>{HudGlyphs.MirrorBall} World</color>";
            StatusNudgeLocal = $"<color={SecondaryValueHex}>{HudGlyphs.HouseBuilding} Local</color>";
            StatusNudgeWorld = $"<color={activeFrameHex}><b>{HudGlyphs.MirrorBall} World</b></color>";
            StatusGridWorld = $"<color={worldGridHex}>{HudGlyphs.Globe} World</color>";
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
            StepPreset.UltraFine => StepUltraFineHex,
            StepPreset.ExtraFine => StepExtraFineHex,
            StepPreset.Fine => StepFineHex,
            StepPreset.Coarse => StepCoarseHex,
            _ => StepStandardHex
        };

        public static string Keycap(string keyContent) =>
            $"<b>{_keycapOpen}{keyContent}{_keycapClose}</b>";

        public static string Measure(string measureContent) =>
            $"{_measureOpen}{measureContent}{_measureClose}";

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
