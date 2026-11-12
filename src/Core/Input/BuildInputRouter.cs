using System;
using System.Collections.Generic;
using MasterBuilderHeeler.Configuration;
using MasterBuilderHeeler.Core.Placement;
using MasterBuilderHeeler.Core.Rotation;
using MasterBuilderHeeler.Core.State;
using MasterBuilderHeeler.Core.Translation;
using MasterBuilderHeeler.Core.UI.Radial;
using MasterBuilderHeeler.Utilities;
using UnityEngine;

namespace MasterBuilderHeeler.Core.Input
{
    public static class BuildInputRouter
    {
        private const float NudgeRepeatDelay = 0.12f;

        // which chords were held last frame. one slot per action in the order of BuildActions.All
        private static readonly bool[] ChordHeld = new bool[BuildActions.All.Count];

        private static float _nextNudgeTime;

        public static bool TryPress(BuildAction action, Player player)
        {
            if (action.OnPressed == null || !action.IsAvailable())
            {
                return false;
            }

            if (ModConfig.IsDebugLogging)
            {
                Plugin.LogDebug($"[BuildInputRouter] Action captured: '{action.Name}'.");
            }
            action.OnPressed(player);
            return true;
        }

        public static void ProcessInput(Player player)
        {
            if (!ModConfig.General.EnableMod.Value || player != Player.m_localPlayer || !player.InPlaceMode())
            {
                return;
            }

            if (InputUtils.IsTextInputOrMenuOpen())
            {
                return;
            }

            if (GamepadChordLayer.WasTapped || BuildRadialMenu.TryConsumeToggleKey())
            {
                Plugin.LogDebug("[BuildInputRouter] Action captured: 'OpenBuildRadial'.");
                BuildRadialMenu.Open(player);
                return;
            }

            if (BuildStateManager.EditState == PlacementEditState.InPlaceEdit && CheckPieceEditCommitCancel())
            {
                return;
            }

            // keeps running with precision toggled off so game scroll still gets tracked for when it comes back on
            if (ModConfig.General.EnablePrecision.Value)
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

            if (!CheckChordActions(player))
            {
                CheckKeyActions(player);
            }
        }

        private static int GetRotationStepSign()
        {
            float scroll = ZInput.GetMouseScrollWheel();
            if (Mathf.Abs(scroll) > 0.001f)
            {
                return Math.Sign(scroll);
            }

            return GamepadRotationGesture.ReadStepSign(Time.deltaTime);
        }

        private static Vector3Int GetRawNudgeSigns()
        {
            int right = 0;
            int up = 0;
            int forward = 0;

            if (IsNudgeHeld(BuildActions.NudgeForward)) forward += 1;
            if (IsNudgeHeld(BuildActions.NudgeBackward)) forward -= 1;
            if (IsNudgeHeld(BuildActions.NudgeRight)) right += 1;
            if (IsNudgeHeld(BuildActions.NudgeLeft)) right -= 1;
            if (IsNudgeHeld(BuildActions.NudgeUp)) up += 1;
            if (IsNudgeHeld(BuildActions.NudgeDown)) up -= 1;

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

        private static bool IsNudgeHeld(BuildAction action) =>
            ZInput.GetButton(action.Button.Name) || GamepadChordLayer.IsChordHeld(action);

        // the layouts place and remove buttons. the layer check is here because those same buttons are chords
        // while the layer is held
        private static bool IsGamepadCommitPressed() =>
            GamepadChordLayer.IsBuildInputActive &&
            !GamepadChordLayer.IsHeld &&
            ZInput.GetButtonDown("JoyPlace") &&
            !ZInput.GetButton("JoyAltKeys");

        // game removes on release so cancel does too. short taps only, because on alternative 1 the remove button
        // is also the alt keys button and letting go of a long hold shouldnt throw the edit away
        private static bool IsGamepadCancelPressed() =>
            GamepadChordLayer.IsBuildInputActive &&
            !GamepadChordLayer.IsHeld &&
            ZInput.GetButtonUp("JoyRemove") &&
            ZInput.GetButtonLastPressedTimer("JoyRemove") < GamepadChordLayer.TapThreshold;

        private static bool CheckPieceEditCommitCancel()
        {
            if (ZInput.GetButtonDown(BuildActions.CommitPieceEdit.Button.Name) || IsGamepadCommitPressed())
            {
                Plugin.LogDebug("[BuildInputRouter] Action captured: 'CommitPieceEdit'.");
                PieceEditController.CommitEdit();
                return true;
            }

            if (ZInput.GetButtonDown(BuildActions.CancelPieceEdit.Button.Name) || IsGamepadCancelPressed())
            {
                Plugin.LogDebug("[BuildInputRouter] Action captured: 'CancelPieceEdit'.");
                PieceEditController.CancelEdit();
                return true;
            }

            return false;
        }

        // the d-pad buttons auto repeat in ZInput and a gamepad button moved onto the d-pad would fire over and over
        // while held, so press edges are tracked here and not with GetButtonDown
        private static bool CheckChordActions(Player player)
        {
            IReadOnlyList<BuildAction> actions = BuildActions.All;
            BuildAction? pressedAction = null;
            for (int index = 0; index < actions.Count; index++)
            {
                BuildAction action = actions[index];
                if (action.OnPressed == null)
                {
                    continue;
                }

                bool isHeld = GamepadChordLayer.IsChordHeld(action);
                if (isHeld && !ChordHeld[index] && pressedAction == null)
                {
                    pressedAction = action;
                }
                ChordHeld[index] = isHeld;
            }

            return pressedAction != null && TryPress(pressedAction, player);
        }

        // jotunn allocates a string every time one of these misses. thats jotunn issue #385 so im leaving it alone
        private static void CheckKeyActions(Player player)
        {
            IReadOnlyList<BuildAction> actions = BuildActions.All;
            for (int index = 0; index < actions.Count; index++)
            {
                BuildAction action = actions[index];
                if (action.OnPressed != null && action.HasKey && ZInput.GetButtonDown(action.Button.Name) && TryPress(action, player))
                {
                    return;
                }
            }
        }
    }
}
