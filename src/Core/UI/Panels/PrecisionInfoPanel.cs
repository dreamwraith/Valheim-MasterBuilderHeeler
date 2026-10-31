using System;
using MasterBuilderHeeler.Configuration;
using MasterBuilderHeeler.Core.State;
using MasterBuilderHeeler.Utilities;
using UnityEngine;

namespace MasterBuilderHeeler.Core.UI.Panels
{
    public sealed class PrecisionInfoPanel : HudPanelBase
    {
        private readonly HudLabelRow _stepSummaryRow;
        private readonly HudLabelRow _fineStepRow;
        private readonly HudLabelRow _coarseStepRow;
        private readonly HudLabelRow _adjustStepRow;
        private readonly HudLabelRow _resetStepRow;
        private readonly HudLabelRow _rotationFrameRow;
        private readonly HudLabelRow _roofModeRow;
        private readonly HudLabelRow _rotateRow;
        private readonly HudLabelRow _yawAxisRow;
        private readonly HudLabelRow _pitchAxisRow;
        private readonly HudLabelRow _rollAxisRow;
        private readonly HudLabelRow _resetAxisRow;
        private readonly HudLabelRow _resetAllRow;
        private readonly HudLabelRow _nudgeFrameRow;
        private readonly HudLabelRow _nudgeRow;
        private readonly HudLabelRow _sideNudgeRow;
        private readonly HudLabelRow _verticalNudgeRow;
        private readonly HudLabelRow _resetNudgeRow;

        private PanelStateSnapshot _lastSnapshot;
        private bool _hasValidSnapshot;
        private int _preseededKeybindVersion = -1;

        public PrecisionInfoPanel(Transform parentTransform)
            : base(parentTransform, "MBH_PrecisionInfoPanel", ModConfig.PrecisionPanelLayout)
        {
            Panel.SetFloatingHeader("Precision");

            _stepSummaryRow = Panel.AddRow("Step Size:");
            _fineStepRow = Panel.AddRow("Fine Step:");
            _coarseStepRow = Panel.AddRow("Coarse Step:");
            _adjustStepRow = Panel.AddRow("Adjust Step:");
            _resetStepRow = Panel.AddRow("Reset Step:");
            _rotationFrameRow = Panel.AddRow("Rotation Frame:");
            _roofModeRow = Panel.AddRow("Roof Mode:");
            _rotateRow = Panel.AddRow("Rotate:");
            _yawAxisRow = Panel.AddRow("Yaw Axis:");
            _pitchAxisRow = Panel.AddRow("Pitch Axis:");
            _rollAxisRow = Panel.AddRow("Roll Axis:");
            _resetAxisRow = Panel.AddRow("Reset Axis:");
            _resetAllRow = Panel.AddRow("Reset All Axes:");
            _nudgeFrameRow = Panel.AddRow("Nudge Frame:");
            _nudgeRow = Panel.AddRow("Nudge:");
            _sideNudgeRow = Panel.AddRow(string.Empty);
            _verticalNudgeRow = Panel.AddRow(string.Empty);
            _resetNudgeRow = Panel.AddRow("Reset Nudge:");

            Panel.SetAnchors(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f));

            PreseedConstantWidth();
            Panel.SetActive(false);
        }

        public void Refresh(Player localPlayer)
        {
            if (!ModConfig.PrecisionPanelLayout.Show.Value)
            {
                SetActive(false);
                return;
            }

            if (!BuildStateManager.IsPrecisionModeActive || !localPlayer.InPlaceMode())
            {
                SetActive(false);
                return;
            }

            PlacementEditState editState = BuildStateManager.EditState;

            Panel.SetActive(true);

            int keybindVersion = HudKeybindFormatter.KeybindVersion;
            if (_preseededKeybindVersion != keybindVersion)
            {
                Panel.ResetClampedWidth();
                PreseedConstantWidth();
            }

            PanelStateSnapshot snapshot = new PanelStateSnapshot(
                editState,
                BuildStateManager.IsTerrainOpGhostActive,
                BuildStateManager.IsCornerRoofTarget,
                BuildStateManager.IsRoofModeActive,
                BuildStateManager.ActiveStepPreset,
                BuildStateManager.GetActiveRotationStep(),
                BuildStateManager.GetActiveTranslationStep(),
                BuildStateManager.StepMultiplier,
                BuildStateManager.ActiveRotationFrame,
                BuildStateManager.ActiveAxis,
                BuildStateManager.ActiveTranslationFrame,
                keybindVersion,
                HudTheme.ThemeVersion);

            if (_hasValidSnapshot && snapshot.Equals(_lastSnapshot))
            {
                return;
            }

            _lastSnapshot = snapshot;
            _hasValidSnapshot = true;

            string fineKey = KeyLabelFormatter.Format(KeyboardMouseConfig.ToggleFineStepKey);
            string coarseKey = KeyLabelFormatter.Format(KeyboardMouseConfig.ToggleCoarseStepKey);
            string halveKey = KeyLabelFormatter.Format(KeyboardMouseConfig.HalveStepMultiplierKey);
            string doubleKey = KeyLabelFormatter.Format(KeyboardMouseConfig.DoubleStepMultiplierKey);
            string resetScaleKey = KeyLabelFormatter.Format(KeyboardMouseConfig.ResetStepMultiplierKey);

            _fineStepRow.SetValue(HudTheme.FormatKeycapToggle(
                fineKey,
                snapshot.ActiveStepPreset == StepPreset.Fine,
                HudTheme.StepFineHex,
                HudGlyphs.ToggleActive,
                HudGlyphs.ToggleInactive));

            _coarseStepRow.SetValue(HudTheme.FormatKeycapToggle(
                coarseKey,
                snapshot.ActiveStepPreset == StepPreset.Coarse,
                HudTheme.StepCoarseHex,
                HudGlyphs.ToggleActive,
                HudGlyphs.ToggleInactive));

            _adjustStepRow.SetValue(HudTheme.Keycap($"{halveKey}{HudTheme.PipeSeparator}{doubleKey}"));
            _resetStepRow.SetValue(HudTheme.Keycap(resetScaleKey));

            bool isCustomMultiplier = !Mathf.Approximately(snapshot.StepMultiplier, 1f);
            string scaleSuffix = isCustomMultiplier
                ? $"{HudTheme.BrokenBarSeparator}{snapshot.StepMultiplier:0.###}x"
                : string.Empty;
            string stepSummary = snapshot.IsTerrainOp
                ? $"{snapshot.TranslationStep:0.###}m{scaleSuffix}"
                : $"{snapshot.RotationStep:0.#}{HudGlyphs.Degree}{HudTheme.BrokenBarSeparator}{snapshot.TranslationStep:0.###}m{scaleSuffix}";
            string stepLabel = isCustomMultiplier ? "Custom" : snapshot.ActiveStepPreset.ToString();
            string stepColor = isCustomMultiplier ? HudTheme.StepCustomHex : HudTheme.GetStepColorHex(snapshot.ActiveStepPreset);

            _stepSummaryRow.SetValue($"<color={stepColor}><b>{stepLabel}</b></color> <color={HudTheme.SecondaryValueHex}>{HudTheme.Measure(stepSummary)}</color>");

            bool showRotation = !snapshot.IsTerrainOp;
            bool showRoof = showRotation && snapshot.IsCornerRoof;
            _rotationFrameRow.SetActive(showRotation);
            _roofModeRow.SetActive(showRoof);
            _rotateRow.SetActive(showRotation);
            _yawAxisRow.SetActive(showRotation);
            _pitchAxisRow.SetActive(showRotation);
            _rollAxisRow.SetActive(showRotation);
            _resetAxisRow.SetActive(showRotation);
            _resetAllRow.SetActive(showRotation);

            if (showRotation)
            {
                string cycleFrameKey = KeyLabelFormatter.Format(KeyboardMouseConfig.CycleRotationFrameKey);
                string frameStatus = snapshot.ActiveRotationFrame == RotationFrame.Local
                    ? HudTheme.StatusRotationLocal
                    : HudTheme.StatusRotationWorld;
                _rotationFrameRow.SetValue($"{HudTheme.Keycap(cycleFrameKey)} {frameStatus}");

                if (showRoof)
                {
                    string roofModeKey = KeyLabelFormatter.Format(KeyboardMouseConfig.ToggleRoofModeKey);
                    string roofStatus = snapshot.IsRoofModeActive ? HudTheme.StatusOn : HudTheme.StatusOff;
                    _roofModeRow.SetValue($"{HudTheme.Keycap(roofModeKey)} {roofStatus}");
                }

                RotationAxis activeAxis = snapshot.ActiveAxis;
                string axisColorHex = HudTheme.GetAxisColorHex(activeAxis);
                string activeAxisGlyph = activeAxis switch
                {
                    RotationAxis.Pitch => HudGlyphs.PitchActive,
                    RotationAxis.Roll => HudGlyphs.CircleBullseye,
                    _ => HudGlyphs.YawActive
                };

                string yawKey = KeyLabelFormatter.Format(KeyboardMouseConfig.SelectYawAxisKey);
                string pitchKey = KeyLabelFormatter.Format(KeyboardMouseConfig.SelectPitchAxisKey);
                string rollKey = KeyLabelFormatter.Format(KeyboardMouseConfig.SelectRollAxisKey);
                string resetAxisKey = KeyLabelFormatter.Format(KeyboardMouseConfig.ResetActiveAxisKey);
                string resetAllKey = KeyLabelFormatter.Format(KeyboardMouseConfig.ResetAllAxesKey);

                _rotateRow.SetValue($"{HudTheme.Keycap("Scroll")} <color={axisColorHex}><b>{activeAxisGlyph} {activeAxis}</b></color>");

                _yawAxisRow.SetValue(HudTheme.FormatKeycapToggle(
                    yawKey,
                    (activeAxis & RotationAxis.Yaw) != 0,
                    HudTheme.AxisYawHex,
                    HudGlyphs.YawActive,
                    HudGlyphs.YawInactive));

                _pitchAxisRow.SetValue(HudTheme.FormatKeycapToggle(
                    pitchKey,
                    (activeAxis & RotationAxis.Pitch) != 0,
                    HudTheme.AxisPitchHex,
                    HudGlyphs.PitchActive,
                    HudGlyphs.PitchInactive));

                _rollAxisRow.SetValue(HudTheme.FormatKeycapToggle(
                    rollKey,
                    (activeAxis & RotationAxis.Roll) != 0,
                    HudTheme.AxisRollHex,
                    HudGlyphs.CircleBullseye,
                    HudGlyphs.CircleHollow));

                _resetAxisRow.SetText($"Reset <color={axisColorHex}><b>{activeAxis}</b></color>:", HudTheme.Keycap(resetAxisKey));
                _resetAllRow.SetValue(HudTheme.Keycap(resetAllKey));
            }

            string cycleTranslationFrameKey = KeyLabelFormatter.Format(KeyboardMouseConfig.CycleTranslationFrameKey);
            string nudgeFrameStatus = snapshot.ActiveTranslationFrame == TranslationFrame.World
                ? HudTheme.StatusNudgeWorld
                : HudTheme.StatusNudgeLocal;
            _nudgeFrameRow.SetValue($"{HudTheme.Keycap(cycleTranslationFrameKey)} {nudgeFrameStatus}");
            _nudgeRow.SetValue(HudTheme.Keycap(HudKeybindFormatter.FormatForwardNudge()));
            _sideNudgeRow.SetValue(HudTheme.Keycap(HudKeybindFormatter.FormatSideNudge()));
            _verticalNudgeRow.SetValue(HudTheme.Keycap(HudKeybindFormatter.FormatVerticalNudge()));
            _resetNudgeRow.SetValue(HudTheme.Keycap(KeyLabelFormatter.Format(KeyboardMouseConfig.ResetNudgeKey)));

            Panel.UpdateClampedWidth();
        }

        public override void SetActive(bool active)
        {
            if (!active)
            {
                _hasValidSnapshot = false;
            }

            base.SetActive(active);
        }

        protected override void OnFontSizeChanged()
        {
            _hasValidSnapshot = false;
            PreseedConstantWidth();
        }

        // measures the widest text each row can show so the panel width stays put when values change
        // i need to figure out a better way to do this when i refactor how i handle the display objects. 
        // fuhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhh
        private void PreseedConstantWidth()
        {
            _preseededKeybindVersion = HudKeybindFormatter.KeybindVersion;
            Panel.ObserveAllRowLabels();
            Panel.ObserveSampleWidths($"Reset <color={HudTheme.AxisPitchHex}><b>Pitch</b></color>:", string.Empty);

            string halveKey = KeyLabelFormatter.Format(KeyboardMouseConfig.HalveStepMultiplierKey);
            string doubleKey = KeyLabelFormatter.Format(KeyboardMouseConfig.DoubleStepMultiplierKey);
            string cycleFrameKey = KeyLabelFormatter.Format(KeyboardMouseConfig.CycleRotationFrameKey);
            string roofModeKey = KeyLabelFormatter.Format(KeyboardMouseConfig.ToggleRoofModeKey);
            string cycleTranslationFrameKey = KeyLabelFormatter.Format(KeyboardMouseConfig.CycleTranslationFrameKey);
            string stepSample = $"5.6{HudGlyphs.Degree}{HudTheme.BrokenBarSeparator}0.125m{HudTheme.BrokenBarSeparator}0.125x";

            Panel.ObserveSampleWidths(string.Empty, $"<color={HudTheme.StepCustomHex}><b>Custom</b></color> <color={HudTheme.SecondaryValueHex}>{HudTheme.Measure(stepSample)}</color>");
            Panel.ObserveSampleWidths(string.Empty, HudTheme.Keycap($"{halveKey}{HudTheme.PipeSeparator}{doubleKey}"));
            Panel.ObserveSampleWidths(string.Empty, $"{HudTheme.Keycap(cycleFrameKey)} {HudTheme.StatusRotationWorld}");
            Panel.ObserveSampleWidths(string.Empty, $"{HudTheme.Keycap(roofModeKey)} {HudTheme.StatusOff}");
            Panel.ObserveSampleWidths(string.Empty, $"{HudTheme.Keycap("Scroll")} <color={HudTheme.AxisPitchHex}><b>{HudGlyphs.PitchActive} Pitch</b></color>");
            Panel.ObserveSampleWidths(string.Empty, $"{HudTheme.Keycap(cycleTranslationFrameKey)} {HudTheme.StatusNudgeWorld}");
            Panel.ObserveSampleWidths(string.Empty, HudTheme.Keycap(HudKeybindFormatter.FormatForwardNudge()));
            Panel.ObserveSampleWidths(string.Empty, HudTheme.Keycap(HudKeybindFormatter.FormatSideNudge()));
            Panel.ObserveSampleWidths(string.Empty, HudTheme.Keycap(HudKeybindFormatter.FormatVerticalNudge()));
        }

        private readonly struct PanelStateSnapshot : IEquatable<PanelStateSnapshot>
        {
            public readonly PlacementEditState EditState;
            public readonly bool IsTerrainOp;
            public readonly bool IsCornerRoof;
            public readonly bool IsRoofModeActive;
            public readonly StepPreset ActiveStepPreset;
            public readonly float RotationStep;
            public readonly float TranslationStep;
            public readonly float StepMultiplier;
            public readonly RotationFrame ActiveRotationFrame;
            public readonly RotationAxis ActiveAxis;
            public readonly TranslationFrame ActiveTranslationFrame;
            public readonly int KeybindVersion;
            public readonly int ThemeVersion;

            public PanelStateSnapshot(
                PlacementEditState editState,
                bool isTerrainOp,
                bool isCornerRoof,
                bool isRoofModeActive,
                StepPreset activeStepPreset,
                float rotationStep,
                float translationStep,
                float stepMultiplier,
                RotationFrame activeRotationFrame,
                RotationAxis activeAxis,
                TranslationFrame activeTranslationFrame,
                int keybindVersion,
                int themeVersion)
            {
                EditState = editState;
                IsTerrainOp = isTerrainOp;
                IsCornerRoof = isCornerRoof;
                IsRoofModeActive = isRoofModeActive;
                ActiveStepPreset = activeStepPreset;
                RotationStep = rotationStep;
                TranslationStep = translationStep;
                StepMultiplier = stepMultiplier;
                ActiveRotationFrame = activeRotationFrame;
                ActiveAxis = activeAxis;
                ActiveTranslationFrame = activeTranslationFrame;
                KeybindVersion = keybindVersion;
                ThemeVersion = themeVersion;
            }

            public bool Equals(PanelStateSnapshot other) =>
                EditState == other.EditState &&
                IsTerrainOp == other.IsTerrainOp &&
                IsCornerRoof == other.IsCornerRoof &&
                IsRoofModeActive == other.IsRoofModeActive &&
                ActiveStepPreset == other.ActiveStepPreset &&
                Mathf.Approximately(RotationStep, other.RotationStep) &&
                Mathf.Approximately(TranslationStep, other.TranslationStep) &&
                Mathf.Approximately(StepMultiplier, other.StepMultiplier) &&
                ActiveRotationFrame == other.ActiveRotationFrame &&
                ActiveAxis == other.ActiveAxis &&
                ActiveTranslationFrame == other.ActiveTranslationFrame &&
                KeybindVersion == other.KeybindVersion &&
                ThemeVersion == other.ThemeVersion;
        }
    }
}
