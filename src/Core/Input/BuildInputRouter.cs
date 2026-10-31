using System;
using Jotunn.Configs;
using MasterBuilderHeeler.Configuration;
using MasterBuilderHeeler.Core.Placement;
using MasterBuilderHeeler.Core.Rotation;
using MasterBuilderHeeler.Core.Sampling;
using MasterBuilderHeeler.Core.State;
using MasterBuilderHeeler.Core.Translation;
using MasterBuilderHeeler.Utilities;
using UnityEngine;

namespace MasterBuilderHeeler.Core.Input
{
    public static class BuildInputRouter
    {
        private const float NudgeRepeatDelay = 0.12f;

        private static ToggleActionBinding[] _toggleActions = Array.Empty<ToggleActionBinding>();
        private static float _nextNudgeTime;

        public static void Initialize()
        {
            Func<bool> isPrecisionActive = () => BuildStateManager.IsPrecisionModeActive;
            Func<bool> canTogglePrecision = () => ModConfig.EnablePrecision.Value && BuildStateManager.EditState != PlacementEditState.InPlaceEdit;
            Func<bool> isBuildCameraEnabled = () => ModConfig.EnableBuildCamera.Value;
            Func<bool> isGridEnabled = () => ModConfig.EnableGrid.Value;

            _toggleActions = new ToggleActionBinding[]
            {
                new ToggleActionBinding(ModInputManager.TogglePrecisionModeButton, PlacementGhostController.TogglePrecisionMode, canTogglePrecision),
                new ToggleActionBinding(ModInputManager.ToggleBuildCameraButton, _ => BuildStateManager.ToggleBuildCamera(), isBuildCameraEnabled),
                new ToggleActionBinding(ModInputManager.ToggleGhostFreezeButton, PlacementGhostController.ToggleGhostFreeze, isPrecisionActive),

                new ToggleActionBinding(ModInputManager.ToggleFineStepButton, _ => BuildStateManager.ToggleFineStep(), isPrecisionActive),
                new ToggleActionBinding(ModInputManager.ToggleCoarseStepButton, _ => BuildStateManager.ToggleCoarseStep(), isPrecisionActive),
                new ToggleActionBinding(ModInputManager.DoubleStepMultiplierButton, _ => BuildStateManager.DoubleStepMultiplier(), isPrecisionActive),
                new ToggleActionBinding(ModInputManager.HalveStepMultiplierButton, _ => BuildStateManager.HalveStepMultiplier(), isPrecisionActive),
                new ToggleActionBinding(ModInputManager.ResetStepMultiplierButton, _ => BuildStateManager.ResetStepMultiplier(), isPrecisionActive),

                new ToggleActionBinding(ModInputManager.SelectYawAxisButton, _ => BuildStateManager.SelectYawAxis(), isPrecisionActive),
                new ToggleActionBinding(ModInputManager.SelectPitchAxisButton, _ => BuildStateManager.TogglePitchAxis(), isPrecisionActive),
                new ToggleActionBinding(ModInputManager.SelectRollAxisButton, _ => BuildStateManager.ToggleRollAxis(), isPrecisionActive),
                new ToggleActionBinding(ModInputManager.CycleRotationFrameButton, _ => BuildStateManager.CycleRotationFrame(), isPrecisionActive),
                new ToggleActionBinding(ModInputManager.ToggleRoofModeButton, _ => BuildStateManager.ToggleRoofMode(), isPrecisionActive),
                new ToggleActionBinding(ModInputManager.ResetActiveAxisButton, _ => PieceRotationEngine.ResetAxis(BuildStateManager.ActiveAxis), isPrecisionActive),
                new ToggleActionBinding(ModInputManager.ResetAllAxesButton, _ => PieceRotationEngine.ResetAllRotators(), isPrecisionActive),

                new ToggleActionBinding(ModInputManager.CycleTranslationFrameButton, _ => BuildStateManager.CycleTranslationFrame(), isPrecisionActive),
                new ToggleActionBinding(ModInputManager.ResetNudgeButton, _ => PieceTranslationEngine.ResetNudge(), isPrecisionActive),

                new ToggleActionBinding(ModInputManager.ToggleGridSnapButton, _ => BuildStateManager.ToggleGridSnap(), isGridEnabled),
                new ToggleActionBinding(ModInputManager.ToggleGridOverlayButton, PlacementGhostController.ToggleGridOverlay, isGridEnabled),
                new ToggleActionBinding(ModInputManager.ToggleLocalGridButton, PlacementGhostController.ToggleLocalGrid, isGridEnabled),
                new ToggleActionBinding(ModInputManager.ToggleGridSurfaceButton, _ => BuildStateManager.ToggleGridSurface(), isGridEnabled),
                new ToggleActionBinding(ModInputManager.CycleGridSizeButton, _ => BuildStateManager.CycleGridSize(), isGridEnabled),
                new ToggleActionBinding(ModInputManager.SamplePieceButton, PieceEyedropper.SampleTargetPiece, PieceEyedropper.IsCustomSamplingAvailable),
                new ToggleActionBinding(ModInputManager.MatchPieceRotationButton, PieceEyedropper.SampleTargetRotation, PieceEyedropper.IsCustomSamplingAvailable),
                new ToggleActionBinding(ModInputManager.SamplePieceAndRotationButton, PieceEyedropper.SampleTargetPieceAndRotation, PieceEyedropper.IsCustomSamplingAvailable),

                new ToggleActionBinding(ModInputManager.TogglePieceEditButton, PieceEditController.TryTogglePieceEdit, isPrecisionActive)
            };
        }

        public static void ProcessInput(Player player)
        {
            if (!ModConfig.EnableMod.Value || player != Player.m_localPlayer || !player.InPlaceMode())
            {
                return;
            }

            if (InputUtils.IsTextInputOrMenuOpen())
            {
                return;
            }

            if (BuildStateManager.EditState == PlacementEditState.InPlaceEdit && CheckPieceEditCommitCancel())
            {
                return;
            }

            // keeps running with precision toggled off so game scroll still gets tracked for when it comes back on
            if (ModConfig.EnablePrecision.Value)
            {
                int rotationStepSign = GetRotationStepSign();
                PieceRotationEngine.UpdatePlacementInput(player, rotationStepSign);
            }

            if (BuildStateManager.IsPrecisionModeActive)
            {
                Vector3Int rawNudgeSigns = GetRawNudgeSigns();
                PieceTranslationEngine.UpdateNudgeInput(player, rawNudgeSigns);
            }

            if (BuildStateManager.EditState == PlacementEditState.InPlaceEdit)
            {
                PieceEditController.UpdateEditMode(player);
            }

            CheckToggleActions(player);
        }

        private static int GetRotationStepSign()
        {
            float scroll = ZInput.GetMouseScrollWheel();
            return Mathf.Abs(scroll) > 0.001f ? Math.Sign(scroll) : 0;
        }

        private static Vector3Int GetRawNudgeSigns()
        {
            int right = 0;
            int up = 0;
            int forward = 0;

            if (GetButton(ModInputManager.NudgeForwardButton)) forward += 1;
            if (GetButton(ModInputManager.NudgeBackwardButton)) forward -= 1;
            if (GetButton(ModInputManager.NudgeRightButton)) right += 1;
            if (GetButton(ModInputManager.NudgeLeftButton)) right -= 1;
            if (GetButton(ModInputManager.NudgeUpButton)) up += 1;
            if (GetButton(ModInputManager.NudgeDownButton)) up -= 1;

            if (right == 0 && up == 0 && forward == 0)
            {
                _nextNudgeTime = 0f;
                return Vector3Int.zero;
            }

            if (Time.time < _nextNudgeTime)
            {
                return Vector3Int.zero;
            }

            _nextNudgeTime = Time.time + NudgeRepeatDelay;
            return new Vector3Int(right, up, forward);
        }

        private static bool GetButtonDown(ButtonConfig button) =>
            ZInput.GetButtonDown(button.Name);

        private static bool GetButton(ButtonConfig button) =>
            ZInput.GetButton(button.Name);

        private static bool CheckPieceEditCommitCancel()
        {
            if (GetButtonDown(ModInputManager.CommitPieceEditButton))
            {
                Plugin.LogDebug("[BuildInputRouter] Action captured: 'CommitPieceEdit'.");
                PieceEditController.CommitEdit();
                return true;
            }

            if (GetButtonDown(ModInputManager.CancelPieceEditButton))
            {
                Plugin.LogDebug("[BuildInputRouter] Action captured: 'CancelPieceEdit'.");
                PieceEditController.CancelEdit();
                return true;
            }

            return false;
        }

        private static void CheckToggleActions(Player player)
        {
            for (int index = 0; index < _toggleActions.Length; index++)
            {
                ToggleActionBinding binding = _toggleActions[index];
                if (GetButtonDown(binding.Button) && binding.IsAvailable())
                {
                    if (ModConfig.IsDebugLogging)
                    {
                        Plugin.LogDebug($"[BuildInputRouter] Action captured: '{binding.Button.Name}'.");
                    }
                    binding.Callback(player);
                    return;
                }
            }
        }

        private readonly struct ToggleActionBinding
        {
            public readonly ButtonConfig Button;
            public readonly Action<Player> Callback;
            public readonly Func<bool> IsAvailable;

            public ToggleActionBinding(
                ButtonConfig button,
                Action<Player> callback,
                Func<bool> isAvailable)
            {
                Button = button;
                Callback = callback;
                IsAvailable = isAvailable;
            }
        }
    }
}
