using System;
using MasterBuilderHeeler.Configuration;
using MasterBuilderHeeler.Core.Input;
using MasterBuilderHeeler.Core.State;
using UnityEngine;

namespace MasterBuilderHeeler.Core.UI.Panels
{
    public sealed class PrecisionInfoPanel : HudKeymapPanel
    {
        // a terrain tool has nothing to rotate
        private static readonly Func<bool> ShowsRotation = () => !BuildStateManager.IsTerrainOpGhostActive;
        private static readonly Func<bool> ShowsRoofMode = () =>
            !BuildStateManager.IsTerrainOpGhostActive && BuildStateManager.IsCornerRoofTarget;
        // the symbol on the reset axis row only shows which axis the key resets. thats not a status so the row
        // still goes away when its key is hidden
        private static readonly Func<bool> ShowsResetAxis = () =>
            !BuildStateManager.IsTerrainOpGhostActive && ModConfig.Hud.Get(PrecisionKeymapRow.ResetAxis).ShowKeymap.Value;

        private readonly HudLabelRow _stepSummaryRow;

        private PanelStateSnapshot _lastSnapshot;
        private bool _hasValidSnapshot;
        private int _preseededKeybindVersion = -1;

        public PrecisionInfoPanel(Transform parentTransform)
            : base(parentTransform, "MBH_PrecisionInfoPanel", ModConfig.Hud.PrecisionPanelLayout)
        {
            Panel.SetFloatingHeader("Precision");

            // step size has no key, so it isnt a keymap row and has no setting
            _stepSummaryRow = Panel.AddRow("Step Size:");

            Row(PrecisionKeymapRow.StepPreset,
                () => HudKeybindFormatter.FormatPair(BuildActions.DecreaseStepPreset, BuildActions.IncreaseStepPreset),
                indicator: new HudIndicator(() => FormatStepPips(BuildStateManager.ActiveStepPreset), () => FormatStepPips(StepPreset.Fine)));
            Row(PrecisionKeymapRow.AdjustStep,
                () => HudKeybindFormatter.FormatPair(BuildActions.HalveStepMultiplier, BuildActions.DoubleStepMultiplier));
            Row(PrecisionKeymapRow.ResetStep, BuildActions.ResetStepMultiplier);
            Row(PrecisionKeymapRow.RotationFrame, BuildActions.CycleRotationFrame, ShowsRotation,
                HudIndicator.Choice(
                    () => BuildStateManager.ActiveRotationFrame == RotationFrame.Local,
                    () => HudTheme.StatusRotationLocal,
                    () => HudTheme.StatusRotationWorld));
            Row(PrecisionKeymapRow.RoofMode, BuildActions.ToggleRoofMode, ShowsRoofMode,
                HudIndicator.OnOff(() => BuildStateManager.IsRoofModeActive));
            Row(PrecisionKeymapRow.Rotate, HudKeybindFormatter.FormatRotate, ShowsRotation,
                new HudIndicator(() => FormatAxisName(BuildStateManager.ActiveAxis), () => FormatAxisName(RotationAxis.Pitch)));
            Row(PrecisionKeymapRow.YawAxis, BuildActions.SelectYawAxis, ShowsRotation,
                AxisIndicator(RotationAxis.Yaw, HudGlyphs.YawInactive));
            Row(PrecisionKeymapRow.PitchAxis, BuildActions.SelectPitchAxis, ShowsRotation,
                AxisIndicator(RotationAxis.Pitch, HudGlyphs.PitchInactive));
            Row(PrecisionKeymapRow.RollAxis, BuildActions.SelectRollAxis, ShowsRotation,
                AxisIndicator(RotationAxis.Roll, HudGlyphs.CircleHollow));
            Row(PrecisionKeymapRow.ResetAxis, BuildActions.ResetActiveAxis, ShowsResetAxis,
                new HudIndicator(() => FormatAxisSymbol(BuildStateManager.ActiveAxis), () => FormatAxisSymbol(RotationAxis.Pitch)),
                FormatResetAxisLabel);
            Row(PrecisionKeymapRow.ResetAllAxes, BuildActions.ResetAllAxes, ShowsRotation);
            Row(PrecisionKeymapRow.NudgeFrame, BuildActions.CycleTranslationFrame,
                indicator: HudIndicator.Choice(
                    () => BuildStateManager.ActiveTranslationFrame == TranslationFrame.World,
                    () => HudTheme.StatusNudgeWorld,
                    () => HudTheme.StatusNudgeLocal));
            // three rows on one setting. the two under the first have no label of their own
            Row(PrecisionKeymapRow.Nudge, HudKeybindFormatter.FormatForwardNudge);
            Row(PrecisionKeymapRow.Nudge, HudKeybindFormatter.FormatSideNudge, label: string.Empty);
            Row(PrecisionKeymapRow.Nudge, HudKeybindFormatter.FormatVerticalNudge, label: string.Empty);
            Row(PrecisionKeymapRow.ResetNudge, BuildActions.ResetNudge);
            Row(PrecisionKeymapRow.ResetSettings, BuildActions.ResetBuildSettings);

            Panel.SetAnchors(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f));

            PreseedConstantWidth();
            Panel.SetActive(false);
        }

        public void Refresh(Player localPlayer)
        {
            if (!ModConfig.Hud.PrecisionPanelLayout.Show.Value)
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

            // same check as the Global panel. the step size row is the one thing here that takes values from it
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

            RefreshKeymapRows();
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

        // the symbol of an axis in the color of its gizmo ring. a ring color can be too dark to read as text so only
        // symbols take it
        private static string FormatAxisSymbol(RotationAxis axis)
        {
            string axisGlyph = axis switch
            {
                RotationAxis.Pitch => HudGlyphs.PitchActive,
                RotationAxis.Roll => HudGlyphs.CircleBullseye,
                _ => HudGlyphs.YawActive
            };

            return $"<color={HudTheme.GetAxisColorHex(axis)}><b>{axisGlyph}</b></color>";
        }

        private static string FormatAxisName(RotationAxis axis) =>
            $"{FormatAxisSymbol(axis)} <b>{axis}</b>";

        private static HudIndicator AxisIndicator(RotationAxis axis, string inactiveGlyph) =>
            HudIndicator.Choice(
                () => (BuildStateManager.ActiveAxis & axis) != 0,
                () => FormatAxisSymbol(axis),
                () => inactiveGlyph);

        private static string FormatResetAxisLabel() =>
            $"Reset {BuildStateManager.ActiveAxis}:";

        private static string FormatStepPips(StepPreset activePreset)
        {
            string pips = string.Empty;
            for (int presetIndex = (int)StepPreset.UltraFine; presetIndex <= (int)StepPreset.Coarse; presetIndex++)
            {
                StepPreset preset = (StepPreset)presetIndex;
                pips += preset == activePreset
                    ? $"<color={HudTheme.GetStepColorHex(preset)}><b>{HudGlyphs.ToggleActive}</b></color>"
                    : HudGlyphs.ToggleInactive;
            }

            return pips;
        }

        private void Row(
            PrecisionKeymapRow row,
            BuildAction action,
            Func<bool>? isVisible = null,
            HudIndicator? indicator = null,
            Func<string>? getLabel = null) =>
            AddKeymapRow(
                ModConfig.Hud.Get(row),
                () => HudKeybindFormatter.Format(action),
                isVisible: isVisible,
                indicator: indicator,
                getLabel: getLabel);

        // for a row whose key text is not one action: a pair of keys, the rotate row, the nudge rows
        private void Row(
            PrecisionKeymapRow row,
            Func<string> getKeyText,
            Func<bool>? isVisible = null,
            HudIndicator? indicator = null,
            string? label = null) =>
            AddKeymapRow(ModConfig.Hud.Get(row), getKeyText, isVisible: isVisible, indicator: indicator, label: label);

        private void PreseedConstantWidth()
        {
            _preseededKeybindVersion = HudKeybindFormatter.KeybindVersion;
            Panel.ObserveSampleWidths("Reset Pitch:", string.Empty);

            string stepSample = $"5.6{HudGlyphs.Degree}{HudTheme.BrokenBarSeparator}0.125m{HudTheme.BrokenBarSeparator}0.125x";
            Panel.ObserveSampleWidths(
                _stepSummaryRow.Label.text,
                $"<color={HudTheme.StepCustomHex}><b>Custom</b></color> <color={HudTheme.SecondaryValueHex}>{HudTheme.Measure(stepSample)}</color>");

            ObserveKeymapRowWidths();
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
