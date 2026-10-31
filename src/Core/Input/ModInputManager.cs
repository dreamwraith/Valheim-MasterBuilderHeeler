using BepInEx.Configuration;
using Jotunn.Configs;
using Jotunn.Managers;
using MasterBuilderHeeler.Configuration;

namespace MasterBuilderHeeler.Core.Input
{
    public static class ModInputManager
    {
        public static ButtonConfig TogglePrecisionModeButton { get; private set; } = null!;
        public static ButtonConfig ToggleBuildCameraButton { get; private set; } = null!;
        public static ButtonConfig ToggleGhostFreezeButton { get; private set; } = null!;

        public static ButtonConfig ToggleFineStepButton { get; private set; } = null!;
        public static ButtonConfig ToggleCoarseStepButton { get; private set; } = null!;
        public static ButtonConfig DoubleStepMultiplierButton { get; private set; } = null!;
        public static ButtonConfig HalveStepMultiplierButton { get; private set; } = null!;
        public static ButtonConfig ResetStepMultiplierButton { get; private set; } = null!;

        public static ButtonConfig SelectYawAxisButton { get; private set; } = null!;
        public static ButtonConfig SelectPitchAxisButton { get; private set; } = null!;
        public static ButtonConfig SelectRollAxisButton { get; private set; } = null!;
        public static ButtonConfig CycleRotationFrameButton { get; private set; } = null!;
        public static ButtonConfig ToggleRoofModeButton { get; private set; } = null!;
        public static ButtonConfig ResetActiveAxisButton { get; private set; } = null!;
        public static ButtonConfig ResetAllAxesButton { get; private set; } = null!;

        public static ButtonConfig NudgeForwardButton { get; private set; } = null!;
        public static ButtonConfig NudgeBackwardButton { get; private set; } = null!;
        public static ButtonConfig NudgeLeftButton { get; private set; } = null!;
        public static ButtonConfig NudgeRightButton { get; private set; } = null!;
        public static ButtonConfig NudgeUpButton { get; private set; } = null!;
        public static ButtonConfig NudgeDownButton { get; private set; } = null!;
        public static ButtonConfig CycleTranslationFrameButton { get; private set; } = null!;
        public static ButtonConfig ResetNudgeButton { get; private set; } = null!;

        public static ButtonConfig ToggleGridSnapButton { get; private set; } = null!;
        public static ButtonConfig ToggleGridOverlayButton { get; private set; } = null!;
        public static ButtonConfig ToggleLocalGridButton { get; private set; } = null!;
        public static ButtonConfig ToggleGridSurfaceButton { get; private set; } = null!;
        public static ButtonConfig CycleGridSizeButton { get; private set; } = null!;
        public static ButtonConfig SamplePieceButton { get; private set; } = null!;
        public static ButtonConfig MatchPieceRotationButton { get; private set; } = null!;
        public static ButtonConfig SamplePieceAndRotationButton { get; private set; } = null!;

        public static ButtonConfig TogglePieceEditButton { get; private set; } = null!;
        public static ButtonConfig CommitPieceEditButton { get; private set; } = null!;
        public static ButtonConfig CancelPieceEditButton { get; private set; } = null!;

        public static void Initialize()
        {
            TogglePrecisionModeButton = RegisterButton("TogglePrecisionMode", KeyboardMouseConfig.TogglePrecisionModeKey);
            ToggleBuildCameraButton = RegisterButton("ToggleBuildCamera", KeyboardMouseConfig.ToggleBuildCameraKey);
            ToggleGhostFreezeButton = RegisterButton("ToggleGhostFreeze", KeyboardMouseConfig.ToggleGhostFreezeKey);

            ToggleFineStepButton = RegisterButton("ToggleFineStep", KeyboardMouseConfig.ToggleFineStepKey);
            ToggleCoarseStepButton = RegisterButton("ToggleCoarseStep", KeyboardMouseConfig.ToggleCoarseStepKey);
            DoubleStepMultiplierButton = RegisterButton("DoubleStepMultiplier", KeyboardMouseConfig.DoubleStepMultiplierKey);
            HalveStepMultiplierButton = RegisterButton("HalveStepMultiplier", KeyboardMouseConfig.HalveStepMultiplierKey);
            ResetStepMultiplierButton = RegisterButton("ResetStepMultiplier", KeyboardMouseConfig.ResetStepMultiplierKey);

            SelectYawAxisButton = RegisterButton("SelectYawAxis", KeyboardMouseConfig.SelectYawAxisKey);
            SelectPitchAxisButton = RegisterButton("SelectPitchAxis", KeyboardMouseConfig.SelectPitchAxisKey);
            SelectRollAxisButton = RegisterButton("SelectRollAxis", KeyboardMouseConfig.SelectRollAxisKey);
            CycleRotationFrameButton = RegisterButton("CycleRotationFrame", KeyboardMouseConfig.CycleRotationFrameKey);
            ToggleRoofModeButton = RegisterButton("ToggleRoofMode", KeyboardMouseConfig.ToggleRoofModeKey);
            ResetActiveAxisButton = RegisterButton("ResetActiveAxis", KeyboardMouseConfig.ResetActiveAxisKey);
            ResetAllAxesButton = RegisterButton("ResetAllAxes", KeyboardMouseConfig.ResetAllAxesKey);

            NudgeForwardButton = RegisterButton("NudgeForward", KeyboardMouseConfig.NudgeForwardKey);
            NudgeBackwardButton = RegisterButton("NudgeBackward", KeyboardMouseConfig.NudgeBackwardKey);
            NudgeLeftButton = RegisterButton("NudgeLeft", KeyboardMouseConfig.NudgeLeftKey);
            NudgeRightButton = RegisterButton("NudgeRight", KeyboardMouseConfig.NudgeRightKey);
            NudgeUpButton = RegisterButton("NudgeUp", KeyboardMouseConfig.NudgeUpKey);
            NudgeDownButton = RegisterButton("NudgeDown", KeyboardMouseConfig.NudgeDownKey);
            CycleTranslationFrameButton = RegisterButton("CycleTranslationFrame", KeyboardMouseConfig.CycleTranslationFrameKey);
            ResetNudgeButton = RegisterButton("ResetNudge", KeyboardMouseConfig.ResetNudgeKey);

            ToggleGridSnapButton = RegisterButton("ToggleGridSnap", KeyboardMouseConfig.ToggleGridSnapKey);
            ToggleGridOverlayButton = RegisterButton("ToggleGridOverlay", KeyboardMouseConfig.ToggleGridOverlayKey);
            ToggleLocalGridButton = RegisterButton("ToggleLocalGrid", KeyboardMouseConfig.ToggleLocalGridKey);
            ToggleGridSurfaceButton = RegisterButton("ToggleGridSurface", KeyboardMouseConfig.ToggleGridSurfaceKey);
            CycleGridSizeButton = RegisterButton("CycleGridSize", KeyboardMouseConfig.CycleGridSizeKey);
            SamplePieceButton = RegisterButton("SamplePiece", KeyboardMouseConfig.SamplePieceKey);
            MatchPieceRotationButton = RegisterButton("MatchPieceRotation", KeyboardMouseConfig.MatchPieceRotationKey);
            SamplePieceAndRotationButton = RegisterButton("SamplePieceAndRotation", KeyboardMouseConfig.SamplePieceAndRotationKey);

            TogglePieceEditButton = RegisterButton("TogglePieceEdit", KeyboardMouseConfig.TogglePieceEditKey);
            CommitPieceEditButton = RegisterButton("CommitPieceEdit", KeyboardMouseConfig.CommitPieceEditKey);
            CancelPieceEditButton = RegisterButton("CancelPieceEdit", KeyboardMouseConfig.CancelPieceEditKey);
        }

        private static ButtonConfig RegisterButton(
            string actionName,
            ConfigEntry<KeyboardShortcut> shortcutConfig)
        {
            ButtonConfig buttonConfig = new ButtonConfig
            {
                Name = actionName,
                ShortcutConfig = shortcutConfig,
                ActiveInGUI = false,
                ActiveInCustomGUI = false,
                BlockOtherInputs = false,
                HintToken = null
            };

            InputManager.Instance.AddButton(Plugin.ModGUID, buttonConfig);
            return buttonConfig;
        }
    }
}
