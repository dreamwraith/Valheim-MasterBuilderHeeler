using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace MasterBuilderHeeler.Configuration
{
    public static class KeyboardMouseConfig
    {
        private const string SectionName = "Keybindings (Keyboard & Mouse)";

        private static readonly List<ConfigEntry<KeyboardShortcut>> Shortcuts = new List<ConfigEntry<KeyboardShortcut>>(34);

        public static ConfigEntry<KeyboardShortcut> TogglePrecisionModeKey { get; private set; } = null!;
        public static ConfigEntry<KeyboardShortcut> ToggleBuildCameraKey { get; private set; } = null!;
        public static ConfigEntry<KeyboardShortcut> ToggleGhostFreezeKey { get; private set; } = null!;

        public static ConfigEntry<KeyboardShortcut> ToggleFineStepKey { get; private set; } = null!;
        public static ConfigEntry<KeyboardShortcut> ToggleCoarseStepKey { get; private set; } = null!;
        public static ConfigEntry<KeyboardShortcut> DoubleStepMultiplierKey { get; private set; } = null!;
        public static ConfigEntry<KeyboardShortcut> HalveStepMultiplierKey { get; private set; } = null!;
        public static ConfigEntry<KeyboardShortcut> ResetStepMultiplierKey { get; private set; } = null!;

        public static ConfigEntry<KeyboardShortcut> SelectYawAxisKey { get; private set; } = null!;
        public static ConfigEntry<KeyboardShortcut> SelectPitchAxisKey { get; private set; } = null!;
        public static ConfigEntry<KeyboardShortcut> SelectRollAxisKey { get; private set; } = null!;
        public static ConfigEntry<KeyboardShortcut> CycleRotationFrameKey { get; private set; } = null!;
        public static ConfigEntry<KeyboardShortcut> ToggleRoofModeKey { get; private set; } = null!;
        public static ConfigEntry<KeyboardShortcut> ResetActiveAxisKey { get; private set; } = null!;
        public static ConfigEntry<KeyboardShortcut> ResetAllAxesKey { get; private set; } = null!;

        public static ConfigEntry<KeyboardShortcut> NudgeForwardKey { get; private set; } = null!;
        public static ConfigEntry<KeyboardShortcut> NudgeBackwardKey { get; private set; } = null!;
        public static ConfigEntry<KeyboardShortcut> NudgeLeftKey { get; private set; } = null!;
        public static ConfigEntry<KeyboardShortcut> NudgeRightKey { get; private set; } = null!;
        public static ConfigEntry<KeyboardShortcut> NudgeUpKey { get; private set; } = null!;
        public static ConfigEntry<KeyboardShortcut> NudgeDownKey { get; private set; } = null!;
        public static ConfigEntry<KeyboardShortcut> CycleTranslationFrameKey { get; private set; } = null!;
        public static ConfigEntry<KeyboardShortcut> ResetNudgeKey { get; private set; } = null!;

        public static ConfigEntry<KeyboardShortcut> ToggleGridSnapKey { get; private set; } = null!;
        public static ConfigEntry<KeyboardShortcut> ToggleGridOverlayKey { get; private set; } = null!;
        public static ConfigEntry<KeyboardShortcut> ToggleLocalGridKey { get; private set; } = null!;
        public static ConfigEntry<KeyboardShortcut> ToggleGridSurfaceKey { get; private set; } = null!;
        public static ConfigEntry<KeyboardShortcut> CycleGridSizeKey { get; private set; } = null!;
        public static ConfigEntry<KeyboardShortcut> SamplePieceKey { get; private set; } = null!;
        public static ConfigEntry<KeyboardShortcut> MatchPieceRotationKey { get; private set; } = null!;
        public static ConfigEntry<KeyboardShortcut> SamplePieceAndRotationKey { get; private set; } = null!;

        public static ConfigEntry<KeyboardShortcut> TogglePieceEditKey { get; private set; } = null!;
        public static ConfigEntry<KeyboardShortcut> CommitPieceEditKey { get; private set; } = null!;
        public static ConfigEntry<KeyboardShortcut> CancelPieceEditKey { get; private set; } = null!;

        public static IReadOnlyList<ConfigEntry<KeyboardShortcut>> AllShortcuts => Shortcuts;

        public static void Initialize(ConfigFile config)
        {
            Shortcuts.Clear();

            TogglePrecisionModeKey = BindShortcut(config, "TogglePrecisionModeKey", "Toggle Precision Mode",
                new KeyboardShortcut(KeyCode.F6),
                "Toggles Precision Mode (pitch, yaw, and roll rotation gizmo, piece nudging, and Precision controls panel).");

            ToggleBuildCameraKey = BindShortcut(config, "ToggleBuildCameraKey", "Toggle Build Camera",
                new KeyboardShortcut(KeyCode.B),
                "Toggles the Build Camera while a build tool is equipped.");

            ToggleGhostFreezeKey = BindShortcut(config, "ToggleGhostFreezeKey", "Toggle Freeze Ghost",
                new KeyboardShortcut(KeyCode.F7),
                "Pins or unpins the active placement ghost in place for inspection, rotation, and nudging.");

            ToggleFineStepKey = BindShortcut(config, "ToggleFineStepKey", "Toggle Fine Step",
                new KeyboardShortcut(KeyCode.Minus),
                "Toggles the Fine Step preset for small rotation and nudge increments.");

            ToggleCoarseStepKey = BindShortcut(config, "ToggleCoarseStepKey", "Toggle Coarse Step",
                new KeyboardShortcut(KeyCode.Equals),
                "Toggles the Coarse Step preset for large rotation and nudge increments.");

            DoubleStepMultiplierKey = BindShortcut(config, "DoubleStepMultiplierKey", "Double Step Multiplier",
                new KeyboardShortcut(KeyCode.RightBracket),
                "Doubles the active rotation and nudge step multiplier (up to 8x).");

            HalveStepMultiplierKey = BindShortcut(config, "HalveStepMultiplierKey", "Halve Step Multiplier",
                new KeyboardShortcut(KeyCode.LeftBracket),
                "Halves the active rotation and nudge step multiplier (down to 0.125x).");

            ResetStepMultiplierKey = BindShortcut(config, "ResetStepMultiplierKey", "Reset Step Multiplier",
                new KeyboardShortcut(KeyCode.Backslash),
                "Resets the rotation and nudge step multiplier to 1x.");

            SelectYawAxisKey = BindShortcut(config, "SelectYawAxisKey", "Select Yaw Axis",
                new KeyboardShortcut(KeyCode.H),
                "Selects the horizontal (Yaw) rotation axis.");

            SelectPitchAxisKey = BindShortcut(config, "SelectPitchAxisKey", "Select Pitch Axis",
                new KeyboardShortcut(KeyCode.J),
                "Selects the forward/backward tilt (Pitch) rotation axis, or returns to Yaw when pressed again.");

            SelectRollAxisKey = BindShortcut(config, "SelectRollAxisKey", "Select Roll Axis",
                new KeyboardShortcut(KeyCode.K),
                "Selects the side-to-side tilt (Roll) rotation axis, or returns to Yaw when pressed again.");

            CycleRotationFrameKey = BindShortcut(config, "CycleRotationFrameKey", "Cycle Rotation Frame",
                new KeyboardShortcut(KeyCode.Alpha0),
                "Switches between World and Local rotation frames.");

            ToggleRoofModeKey = BindShortcut(config, "ToggleRoofModeKey", "Toggle Roof Mode",
                new KeyboardShortcut(KeyCode.L),
                "Aligns rotation and nudge axes along 45-degree corner roof slopes.");

            ResetActiveAxisKey = BindShortcut(config, "ResetActiveAxisKey", "Reset Active Axis",
                new KeyboardShortcut(KeyCode.Alpha9),
                "Resets rotation on the selected axis.");

            ResetAllAxesKey = BindShortcut(config, "ResetAllAxesKey", "Reset All Axes",
                new KeyboardShortcut(KeyCode.Backspace),
                "Resets piece rotation on all axes.");

            NudgeForwardKey = BindShortcut(config, "NudgeForwardKey", "Nudge Forward",
                new KeyboardShortcut(KeyCode.UpArrow),
                "Nudges the active placement ghost or edited piece forward in the active nudge frame.");

            NudgeBackwardKey = BindShortcut(config, "NudgeBackwardKey", "Nudge Backward",
                new KeyboardShortcut(KeyCode.DownArrow),
                "Nudges the active placement ghost or edited piece backward in the active nudge frame.");

            NudgeLeftKey = BindShortcut(config, "NudgeLeftKey", "Nudge Left",
                new KeyboardShortcut(KeyCode.LeftArrow),
                "Nudges the active placement ghost or edited piece left in the active nudge frame.");

            NudgeRightKey = BindShortcut(config, "NudgeRightKey", "Nudge Right",
                new KeyboardShortcut(KeyCode.RightArrow),
                "Nudges the active placement ghost or edited piece right in the active nudge frame.");

            NudgeUpKey = BindShortcut(config, "NudgeUpKey", "Nudge Up",
                new KeyboardShortcut(KeyCode.PageUp),
                "Nudges the active placement ghost or edited piece upward.");

            NudgeDownKey = BindShortcut(config, "NudgeDownKey", "Nudge Down",
                new KeyboardShortcut(KeyCode.PageDown),
                "Nudges the active placement ghost or edited piece downward.");

            CycleTranslationFrameKey = BindShortcut(config, "CycleTranslationFrameKey", "Cycle Nudge Frame",
                new KeyboardShortcut(KeyCode.End),
                "Switches nudge directions between Local (piece) and World frames.");

            ResetNudgeKey = BindShortcut(config, "ResetNudgeKey", "Reset Nudge",
                new KeyboardShortcut(KeyCode.Home),
                "Resets piece nudge offset to its starting position.");

            ToggleGridSnapKey = BindShortcut(config, "ToggleGridSnapKey", "Toggle Grid Snap",
                new KeyboardShortcut(KeyCode.Y),
                "Toggles grid snapping.");

            ToggleGridOverlayKey = BindShortcut(config, "ToggleGridOverlayKey", "Toggle Grid Overlay",
                new KeyboardShortcut(KeyCode.U),
                "Shows or hides the grid overlay independently of grid snapping.");

            ToggleLocalGridKey = BindShortcut(config, "ToggleLocalGridKey", "Toggle Local Grid",
                new KeyboardShortcut(KeyCode.I),
                "Anchors the grid origin and horizontal angle to the piece you are aiming at, or switches back to the World grid.");

            ToggleGridSurfaceKey = BindShortcut(config, "ToggleGridSurfaceKey", "Toggle Grid Surface",
                new KeyboardShortcut(KeyCode.O),
                "Switches the grid overlay between Terrain (follows ground height) and Flat.");

            CycleGridSizeKey = BindShortcut(config, "CycleGridSizeKey", "Cycle Grid Size",
                new KeyboardShortcut(KeyCode.P),
                "Cycles grid cell size through 0.5m, 1.0m, 2.0m, and 4.0m.");

            SamplePieceKey = BindShortcut(config, "SamplePieceKey", "Sample Piece",
                new KeyboardShortcut(KeyCode.Z),
                "Selects the piece you are aiming at in the Hammer build menu.");

            MatchPieceRotationKey = BindShortcut(config, "MatchPieceRotationKey", "Match Piece Rotation",
                new KeyboardShortcut(KeyCode.N),
                "Copies the rotation of the piece you are aiming at onto the active placement ghost.");

            SamplePieceAndRotationKey = BindShortcut(config, "SamplePieceAndRotationKey", "Sample Piece and Rotation",
                new KeyboardShortcut(KeyCode.Semicolon),
                "Selects the piece you are aiming at in the Hammer build menu and copies its rotation.");

            TogglePieceEditKey = BindShortcut(config, "TogglePieceEditKey", "Toggle In-Place Edit",
                new KeyboardShortcut(KeyCode.F8),
                "Starts moving the piece you are aiming at, or saves the move when pressed again.");

            CommitPieceEditKey = BindShortcut(config, "CommitPieceEditKey", "Commit In-Place Edit",
                new KeyboardShortcut(KeyCode.Insert),
                "Saves the new position and rotation of the edited piece.");

            CancelPieceEditKey = BindShortcut(config, "CancelPieceEditKey", "Cancel In-Place Edit",
                new KeyboardShortcut(KeyCode.Delete),
                "Cancels moving the piece and restores its original position and rotation.");
        }

        private static ConfigEntry<KeyboardShortcut> BindShortcut(
            ConfigFile config,
            string key,
            string displayName,
            KeyboardShortcut defaultShortcut,
            string description)
        {
            ConfigEntry<KeyboardShortcut> entry = ModConfig.BindClient(
                config,
                SectionName,
                key,
                defaultShortcut,
                description,
                attributes: new ConfigurationManagerAttributes { DispName = displayName });
            Shortcuts.Add(entry);
            return entry;
        }
    }
}
