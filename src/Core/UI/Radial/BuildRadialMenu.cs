using System;
using MasterBuilderHeeler.Core.Input;
using MasterBuilderHeeler.Core.State;
using UnityEngine;
using Valheim.UI;

namespace MasterBuilderHeeler.Core.UI.Radial
{
    public static class BuildRadialMenu
    {
        private static BuildRadialPage _rootPage = null!;
        private static int _toggleKeyFrame = -1;

        public static bool IsOpen { get; private set; }

        public static void Initialize()
        {
            BuildRadialPage rotationPage = new BuildRadialPage("Rotation", new[]
            {
                BuildRadialEntry.Toggle("Rotation Frame", BuildActions.CycleRotationFrame, () => BuildStateManager.ActiveRotationFrame.ToString()),
                BuildRadialEntry.Toggle("Roof Mode", BuildActions.ToggleRoofMode, () => FormatOnOff(BuildStateManager.IsRoofModeActive)),
                BuildRadialEntry.OneShot("Reset Axis", BuildActions.ResetActiveAxis, () => BuildStateManager.ActiveAxis.ToString()),
                BuildRadialEntry.OneShot("Reset All Axes", BuildActions.ResetAllAxes)
            });

            BuildRadialPage nudgePage = new BuildRadialPage("Nudge", new[]
            {
                BuildRadialEntry.Toggle("Nudge Frame", BuildActions.CycleTranslationFrame, () => BuildStateManager.ActiveTranslationFrame.ToString()),
                BuildRadialEntry.OneShot("Reset Nudge", BuildActions.ResetNudge)
            });

            BuildRadialPage stepPage = new BuildRadialPage("Step", new[]
            {
                BuildRadialEntry.Toggle("Larger Step", BuildActions.IncreaseStepPreset, FormatStepPreset),
                BuildRadialEntry.Toggle("Smaller Step", BuildActions.DecreaseStepPreset, FormatStepPreset),
                BuildRadialEntry.Toggle("Double Step", BuildActions.DoubleStepMultiplier, FormatStepMultiplier),
                BuildRadialEntry.Toggle("Halve Step", BuildActions.HalveStepMultiplier, FormatStepMultiplier),
                BuildRadialEntry.Toggle("Reset Step Scale", BuildActions.ResetStepMultiplier, FormatStepMultiplier)
            });

            BuildRadialPage gridPage = new BuildRadialPage("Grid", new[]
            {
                BuildRadialEntry.Toggle("Grid Snap", BuildActions.ToggleGridSnap, () => FormatOnOff(BuildStateManager.IsGridSnapActive)),
                BuildRadialEntry.Toggle("Grid Overlay", BuildActions.ToggleGridOverlay, () => FormatOnOff(BuildStateManager.IsGridOverlayVisible)),
                BuildRadialEntry.Toggle("Grid Frame", BuildActions.ToggleLocalGrid, () => BuildStateManager.ActiveGridFrame.ToString()),
                BuildRadialEntry.Toggle("Grid Surface", BuildActions.ToggleGridSurface, () => BuildStateManager.ActiveGridSurface.ToString()),
                BuildRadialEntry.Toggle("Grid Size", BuildActions.CycleGridSize, () => $"{BuildStateManager.GridCellSize:F1}m")
            });

            BuildRadialPage samplePage = new BuildRadialPage("Sample", new[]
            {
                BuildRadialEntry.OneShot("Sample Piece", BuildActions.SamplePiece),
                BuildRadialEntry.OneShot("Sample Rotation", BuildActions.MatchPieceRotation),
                BuildRadialEntry.OneShot("Sample Both", BuildActions.SamplePieceAndRotation)
            });

            BuildRadialPage editPage = new BuildRadialPage("Edit", new[]
            {
                BuildRadialEntry.Toggle("Freeze Ghost", BuildActions.ToggleGhostFreeze, () => FormatOnOff(BuildStateManager.EditState == PlacementEditState.GhostFrozen)),
                BuildRadialEntry.OneShot("Edit Piece", BuildActions.TogglePieceEdit, () => FormatOnOff(BuildStateManager.EditState == PlacementEditState.InPlaceEdit)),
                BuildRadialEntry.OneShot("Reset Settings", BuildActions.ResetBuildSettings)
            });

            _rootPage = new BuildRadialPage("Master Builder", new[]
            {
                BuildRadialEntry.Toggle("Precision", BuildActions.TogglePrecisionMode, () => FormatOnOff(BuildStateManager.IsPrecisionModeActive)),
                BuildRadialEntry.Group(rotationPage),
                BuildRadialEntry.Group(nudgePage),
                BuildRadialEntry.Group(stepPage),
                BuildRadialEntry.Group(gridPage),
                BuildRadialEntry.Group(samplePage),
                BuildRadialEntry.Group(editPage),
                BuildRadialEntry.Toggle("Build Camera", BuildActions.ToggleBuildCamera, () => FormatOnOff(BuildStateManager.IsBuildCameraActive))
            });
        }

        public static void Open(Player player)
        {
            if (!_rootPage.HasAvailableEntries)
            {
                return;
            }

            // the radial object ships inactive and RadialData.SO stays null until it wakes up for the first time, so wake
            // it before anything reads the radial settings. RadialBase.Awake switches it right back off that first time
            // and the game gets around that by activating it twice on its first open. this does the same
            RadialBase radial = Hud.instance.m_radialMenu;
            radial.Active = true;

            // same setup OpenRadialConfig does for the game radial. a page of mine isnt an OpenRadialConfig, so
            // RadialBase.Open skips all of it
            radial.CanOpen = true;
            radial.m_animationManager.EndAll();
            radial.LoadSettings();
            radial.ResetAllVariables();
            radial.OnInteractionDelay = PlayerController.SetTakeInputDelay;
            radial.ShouldAnimateIn = true;

            // LoadSettings makes a fresh GetClose every time the game radial opens, so this wrap only lives for this open
            Func<bool> gameClose = radial.GetClose;
            radial.GetClose = () => gameClose() || GamepadChordLayer.IsLayerButtonPressed() || TryConsumeToggleKey();

            // RadialBase.Open refuses to open in place mode unless the radial is already active. thats the only thing
            // keeping it out of build mode, so it has to be active going in
            radial.Active = true;
            player.m_onDeath += radial.InstantClose;
            radial.Open(_rootPage);
            IsOpen = true;
        }

        // Hud.InRadial stays true through the close animation, so input stays suppressed until the radial is really gone
        public static void RefreshOpenState()
        {
            if (IsOpen && !Hud.InRadial())
            {
                IsOpen = false;
            }
        }

        // one press of the keyboard key only counts once. the radial reads its close buttons in its own Update and the
        // router reads this key in the player Update. i dont know which of those runs first in a frame, so without
        // this the press that opens the radial could shut it again, or the press that closes it could open it again
        public static bool TryConsumeToggleKey()
        {
            if (Time.frameCount == _toggleKeyFrame || !ZInput.GetButtonDown(BuildActions.OpenBuildRadial.Button.Name))
            {
                return false;
            }

            _toggleKeyFrame = Time.frameCount;
            return true;
        }

        private static string FormatOnOff(bool isOn) => isOn ? "On" : "Off";

        private static string FormatStepPreset() => BuildStateManager.ActiveStepPreset.ToString();

        private static string FormatStepMultiplier() => $"{BuildStateManager.StepMultiplier:0.###}x";
    }
}
