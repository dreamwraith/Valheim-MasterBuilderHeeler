using System;
using System.Collections.Generic;
using Jotunn.Managers;
using MasterBuilderHeeler.Configuration;
using MasterBuilderHeeler.Core.Placement;
using MasterBuilderHeeler.Core.Rotation;
using MasterBuilderHeeler.Core.Sampling;
using MasterBuilderHeeler.Core.State;
using MasterBuilderHeeler.Core.Translation;
using UnityEngine;

namespace MasterBuilderHeeler.Core.Input
{
    // every build action is declared once in here. the key settings show in this order in the config file and
    // ConfigurationManager and the router checks presses in it, so dont shuffle it
    // a dedicated server builds this list for the config, so handlers are lambdas and nothing they call gets
    // pulled in until a key is pressed
    public static class BuildActions
    {
        // static fields get set top to bottom so this has to stay above the actions
        private static readonly List<BuildAction> Actions = new List<BuildAction>(38);

        private static readonly Func<bool> IsPrecisionActive = () => BuildStateManager.IsPrecisionModeActive;
        private static readonly Func<bool> CanTogglePrecision = () =>
            ModConfig.General.EnablePrecision.Value && BuildStateManager.EditState != PlacementEditState.InPlaceEdit;
        private static readonly Func<bool> IsBuildCameraEnabled = () => ModConfig.General.EnableBuildCamera.Value;
        private static readonly Func<bool> IsGridEnabled = () => ModConfig.General.EnableGrid.Value;
        private static readonly Func<bool> IsSamplingAvailable = () => PieceEyedropper.IsCustomSamplingAvailable();

        // the two gamepad only actions. they come first because key actions further down point at them for the button
        // the hud shows on a gamepad. they have no key, so they dont change the order of the key settings
        public static readonly BuildAction CycleStepPreset = Add(new BuildAction(
            "CycleStepPreset",
            "Cycle Step Preset",
            KeyCode.None,
            "Selects the next larger rotation and nudge step preset while the layer button is held. Coarse wraps to UltraFine.",
            _ => BuildStateManager.ShiftStepPreset(1),
            IsPrecisionActive,
            gamepad: new GamepadBinding(InputManager.GamepadButton.LeftStickButton)));

        public static readonly BuildAction CycleAxis = Add(new BuildAction(
            "CycleAxis",
            "Cycle Rotation Axis",
            KeyCode.None,
            "Selects the next rotation axis (Yaw, Pitch, Roll) while the layer button is held.",
            _ => BuildStateManager.CycleAxis(),
            IsPrecisionActive,
            gamepad: new GamepadBinding(InputManager.GamepadButton.RightTrigger)));

        public static readonly BuildAction TogglePrecisionMode = Add(new BuildAction(
            "TogglePrecisionMode",
            "Toggle Precision Mode",
            KeyCode.F6,
            "Toggles Precision Mode (pitch, yaw, and roll rotation gizmo, piece nudging, and Precision controls panel).",
            player => PlacementGhostController.TogglePrecisionMode(player),
            CanTogglePrecision));

        public static readonly BuildAction ToggleBuildCamera = Add(new BuildAction(
            "ToggleBuildCamera",
            "Toggle Build Camera",
            KeyCode.B,
            "Toggles the Build Camera while a build tool is equipped.",
            _ => BuildStateManager.ToggleBuildCamera(),
            IsBuildCameraEnabled));

        public static readonly BuildAction ToggleGhostFreeze = Add(new BuildAction(
            "ToggleGhostFreeze",
            "Toggle Freeze Ghost",
            KeyCode.F7,
            "Pins or unpins the active placement ghost in place for inspection, rotation, and nudging.",
            player => PlacementGhostController.ToggleGhostFreeze(player),
            IsPrecisionActive,
            gamepad: new GamepadBinding(
                InputManager.GamepadButton.LeftTrigger,
                "Pins or unpins the active placement ghost while the layer button is held.")));

        public static readonly BuildAction DecreaseStepPreset = Add(new BuildAction(
            "DecreaseStepPreset",
            "Decrease Step Preset",
            KeyCode.Minus,
            "Selects the next smaller rotation and nudge step preset. UltraFine wraps to Coarse.",
            _ => BuildStateManager.ShiftStepPreset(-1),
            IsPrecisionActive));

        public static readonly BuildAction IncreaseStepPreset = Add(new BuildAction(
            "IncreaseStepPreset",
            "Increase Step Preset",
            KeyCode.Equals,
            "Selects the next larger rotation and nudge step preset. Coarse wraps to UltraFine.",
            _ => BuildStateManager.ShiftStepPreset(1),
            IsPrecisionActive,
            gamepadStandIn: CycleStepPreset));

        public static readonly BuildAction DoubleStepMultiplier = Add(new BuildAction(
            "DoubleStepMultiplier",
            "Double Step Multiplier",
            KeyCode.RightBracket,
            "Doubles the active rotation and nudge step multiplier (up to 8x).",
            _ => BuildStateManager.DoubleStepMultiplier(),
            IsPrecisionActive));

        public static readonly BuildAction HalveStepMultiplier = Add(new BuildAction(
            "HalveStepMultiplier",
            "Halve Step Multiplier",
            KeyCode.LeftBracket,
            "Halves the active rotation and nudge step multiplier (down to 0.125x).",
            _ => BuildStateManager.HalveStepMultiplier(),
            IsPrecisionActive));

        public static readonly BuildAction ResetStepMultiplier = Add(new BuildAction(
            "ResetStepMultiplier",
            "Reset Step Multiplier",
            KeyCode.Backslash,
            "Resets the rotation and nudge step multiplier to 1x.",
            _ => BuildStateManager.ResetStepMultiplier(),
            IsPrecisionActive));

        public static readonly BuildAction SelectYawAxis = Add(new BuildAction(
            "SelectYawAxis",
            "Select Yaw Axis",
            KeyCode.H,
            "Selects the horizontal (Yaw) rotation axis.",
            _ => BuildStateManager.SelectYawAxis(),
            IsPrecisionActive,
            gamepadStandIn: CycleAxis));

        public static readonly BuildAction SelectPitchAxis = Add(new BuildAction(
            "SelectPitchAxis",
            "Select Pitch Axis",
            KeyCode.J,
            "Selects the forward/backward tilt (Pitch) rotation axis, or returns to Yaw when pressed again.",
            _ => BuildStateManager.TogglePitchAxis(),
            IsPrecisionActive,
            gamepadStandIn: CycleAxis));

        public static readonly BuildAction SelectRollAxis = Add(new BuildAction(
            "SelectRollAxis",
            "Select Roll Axis",
            KeyCode.K,
            "Selects the side-to-side tilt (Roll) rotation axis, or returns to Yaw when pressed again.",
            _ => BuildStateManager.ToggleRollAxis(),
            IsPrecisionActive,
            gamepadStandIn: CycleAxis));

        public static readonly BuildAction CycleRotationFrame = Add(new BuildAction(
            "CycleRotationFrame",
            "Cycle Rotation Frame",
            KeyCode.Alpha0,
            "Switches between World and Local rotation frames.",
            _ => BuildStateManager.CycleRotationFrame(),
            IsPrecisionActive));

        public static readonly BuildAction ToggleRoofMode = Add(new BuildAction(
            "ToggleRoofMode",
            "Toggle Roof Mode",
            KeyCode.L,
            "Aligns rotation and nudge axes along 45-degree corner roof slopes.",
            _ => BuildStateManager.ToggleRoofMode(),
            IsPrecisionActive));

        public static readonly BuildAction ResetActiveAxis = Add(new BuildAction(
            "ResetActiveAxis",
            "Reset Active Axis",
            KeyCode.Alpha9,
            "Resets rotation on the selected axis.",
            _ => PieceRotationEngine.ResetAxis(BuildStateManager.ActiveAxis),
            IsPrecisionActive));

        public static readonly BuildAction ResetAllAxes = Add(new BuildAction(
            "ResetAllAxes",
            "Reset All Axes",
            KeyCode.Backspace,
            "Resets piece rotation on all axes.",
            _ => PieceRotationEngine.ResetAllRotators(),
            IsPrecisionActive));

        public static readonly BuildAction NudgeForward = Add(new BuildAction(
            "NudgeForward",
            "Nudge Forward",
            KeyCode.UpArrow,
            "Nudges the active placement ghost or edited piece forward in the active nudge frame.",
            gamepad: new GamepadBinding(
                InputManager.GamepadButton.DPadUp,
                "Nudges the active placement ghost or edited piece forward while the layer button is held.")));

        public static readonly BuildAction NudgeBackward = Add(new BuildAction(
            "NudgeBackward",
            "Nudge Backward",
            KeyCode.DownArrow,
            "Nudges the active placement ghost or edited piece backward in the active nudge frame.",
            gamepad: new GamepadBinding(
                InputManager.GamepadButton.DPadDown,
                "Nudges the active placement ghost or edited piece backward while the layer button is held.")));

        public static readonly BuildAction NudgeLeft = Add(new BuildAction(
            "NudgeLeft",
            "Nudge Left",
            KeyCode.LeftArrow,
            "Nudges the active placement ghost or edited piece left in the active nudge frame.",
            gamepad: new GamepadBinding(
                InputManager.GamepadButton.DPadLeft,
                "Nudges the active placement ghost or edited piece left while the layer button is held.")));

        public static readonly BuildAction NudgeRight = Add(new BuildAction(
            "NudgeRight",
            "Nudge Right",
            KeyCode.RightArrow,
            "Nudges the active placement ghost or edited piece right in the active nudge frame.",
            gamepad: new GamepadBinding(
                InputManager.GamepadButton.DPadRight,
                "Nudges the active placement ghost or edited piece right while the layer button is held.")));

        public static readonly BuildAction NudgeUp = Add(new BuildAction(
            "NudgeUp",
            "Nudge Up",
            KeyCode.PageUp,
            "Nudges the active placement ghost or edited piece upward.",
            gamepad: new GamepadBinding(
                InputManager.GamepadButton.RightShoulder,
                "Nudges the active placement ghost or edited piece upward while the layer button is held.")));

        public static readonly BuildAction NudgeDown = Add(new BuildAction(
            "NudgeDown",
            "Nudge Down",
            KeyCode.PageDown,
            "Nudges the active placement ghost or edited piece downward.",
            gamepad: new GamepadBinding(
                InputManager.GamepadButton.LeftShoulder,
                "Nudges the active placement ghost or edited piece downward while the layer button is held.")));

        public static readonly BuildAction CycleTranslationFrame = Add(new BuildAction(
            "CycleTranslationFrame",
            "Cycle Nudge Frame",
            KeyCode.End,
            "Switches nudge directions between Local (piece) and World frames.",
            _ => BuildStateManager.CycleTranslationFrame(),
            IsPrecisionActive));

        public static readonly BuildAction ResetNudge = Add(new BuildAction(
            "ResetNudge",
            "Reset Nudge",
            KeyCode.Home,
            "Resets piece nudge offset to its starting position.",
            _ => PieceTranslationEngine.ResetNudge(),
            IsPrecisionActive));

        public static readonly BuildAction ResetBuildSettings = Add(new BuildAction(
            "ResetBuildSettings",
            "Reset Build Settings",
            KeyCode.F10,
            "Resets piece rotation, nudge, axis, rotation and nudge frames, Roof Mode, step preset, step multiplier, and grid settings to their defaults.",
            _ => PlacementGhostController.ResetBuildSettings(),
            IsPrecisionActive));

        public static readonly BuildAction ToggleGridSnap = Add(new BuildAction(
            "ToggleGridSnap",
            "Toggle Grid Snap",
            KeyCode.Y,
            "Toggles grid snapping.",
            _ => BuildStateManager.ToggleGridSnap(),
            IsGridEnabled));

        public static readonly BuildAction ToggleGridOverlay = Add(new BuildAction(
            "ToggleGridOverlay",
            "Toggle Grid Overlay",
            KeyCode.U,
            "Shows or hides the grid overlay independently of grid snapping.",
            player => PlacementGhostController.ToggleGridOverlay(player),
            IsGridEnabled));

        public static readonly BuildAction ToggleLocalGrid = Add(new BuildAction(
            "ToggleLocalGrid",
            "Toggle Local Grid",
            KeyCode.I,
            "Anchors the grid origin and horizontal angle to the piece you are aiming at, or switches back to the World grid.",
            player => PlacementGhostController.ToggleLocalGrid(player),
            IsGridEnabled));

        public static readonly BuildAction ToggleGridSurface = Add(new BuildAction(
            "ToggleGridSurface",
            "Toggle Grid Surface",
            KeyCode.O,
            "Switches the grid overlay between Terrain (follows ground height) and Flat.",
            _ => BuildStateManager.ToggleGridSurface(),
            IsGridEnabled));

        public static readonly BuildAction CycleGridSize = Add(new BuildAction(
            "CycleGridSize",
            "Cycle Grid Size",
            KeyCode.P,
            "Cycles grid cell size through 0.5m, 1.0m, 2.0m, and 4.0m.",
            _ => BuildStateManager.CycleGridSize(),
            IsGridEnabled));

        public static readonly BuildAction SamplePiece = Add(new BuildAction(
            "SamplePiece",
            "Sample Piece",
            KeyCode.Z,
            "Selects the piece you are aiming at in the active build menu.",
            player => PieceEyedropper.SampleTargetPiece(player),
            IsSamplingAvailable));

        public static readonly BuildAction MatchPieceRotation = Add(new BuildAction(
            "MatchPieceRotation",
            "Match Piece Rotation",
            KeyCode.N,
            "Copies the rotation of the piece you are aiming at onto the active placement ghost.",
            player => PieceEyedropper.SampleTargetRotation(player),
            IsSamplingAvailable));

        public static readonly BuildAction SamplePieceAndRotation = Add(new BuildAction(
            "SamplePieceAndRotation",
            "Sample Piece and Rotation",
            KeyCode.Semicolon,
            "Selects the piece you are aiming at in the active build menu and copies its rotation.",
            player => PieceEyedropper.SampleTargetPieceAndRotation(player),
            IsSamplingAvailable));

        public static readonly BuildAction TogglePieceEdit = Add(new BuildAction(
            "TogglePieceEdit",
            "Toggle In-Place Edit",
            KeyCode.F8,
            "Starts moving the piece you are aiming at, or saves the move when pressed again.",
            player => PieceEditController.TryTogglePieceEdit(player),
            IsPrecisionActive));

        public static readonly BuildAction CommitPieceEdit = Add(new BuildAction(
            "CommitPieceEdit",
            "Commit In-Place Edit",
            KeyCode.Insert,
            "Saves the new position and rotation of the edited piece."));

        public static readonly BuildAction CancelPieceEdit = Add(new BuildAction(
            "CancelPieceEdit",
            "Cancel In-Place Edit",
            KeyCode.Delete,
            "Cancels moving the piece and restores its original position and rotation."));

        public static readonly BuildAction OpenBuildRadial = Add(new BuildAction(
            "OpenBuildRadial",
            "Open Build Radial",
            KeyCode.F4,
            "Opens or closes the build radial menu while a build tool is equipped."));

        private static readonly BuildAction[] GamepadSettingOrder =
        {
            NudgeForward,
            NudgeBackward,
            NudgeLeft,
            NudgeRight,
            NudgeUp,
            NudgeDown,
            CycleAxis,
            ToggleGhostFreeze,
            CycleStepPreset
        };

        public static IReadOnlyList<BuildAction> All => Actions;

        public static void BindConfig(ConfigSection section)
        {
            for (int index = 0; index < Actions.Count; index++)
            {
                Actions[index].BindConfig(section);
            }
        }

        // the second loop picks up an action that has a gamepad button but isnt in GamepadSettingOrder, so a new one
        // cant be left unbound by forgetting that list
        public static void BindGamepadConfig(ConfigSection section)
        {
            for (int index = 0; index < GamepadSettingOrder.Length; index++)
            {
                GamepadSettingOrder[index].BindGamepadConfig(section);
            }

            for (int index = 0; index < Actions.Count; index++)
            {
                Actions[index].BindGamepadConfig(section);
            }
        }

        public static void RegisterButtons()
        {
            for (int index = 0; index < Actions.Count; index++)
            {
                Actions[index].RegisterButton();
            }
        }

        private static BuildAction Add(BuildAction action)
        {
            Actions.Add(action);
            return action;
        }
    }
}
