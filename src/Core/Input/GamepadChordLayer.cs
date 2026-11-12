using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using Jotunn.Managers;
using MasterBuilderHeeler.Configuration;
using MasterBuilderHeeler.Core.UI.Radial;
using MasterBuilderHeeler.Utilities;
using UnityEngine;

namespace MasterBuilderHeeler.Core.Input
{
    public static class GamepadChordLayer
    {
        // same tap and hold cutoff the game uses for its own gamepad buttons
        public const float TapThreshold = 0.33f;

        // these act on release or while held and share a physical button with a radial control in some layout (snap
        // cycling on a stick click, remove on LB in alternative 2, rotate on LT in alternative 1). a press that starts
        // in the radial would still count after it closes, so they get reset while its open
        private static readonly string[] RadialSuppressedButtons =
        {
            "JoyNextSnap",
            "JoyPrevSnap",
            "JoyRemove",
            "JoyRotate",
            "JoyRotateRight"
        };

        private static readonly List<ZInput.ButtonDef> SuppressedButtons = new List<ZInput.ButtonDef>(48);
        private static readonly HashSet<string> OwnButtonNames = new HashSet<string>(StringComparer.Ordinal);
        private static readonly HashSet<string> OwnButtonPaths = new HashSet<string>(StringComparer.Ordinal);

        private static ZInput.ButtonDef? _suppressionAnchor;
        private static bool _suppressionDirty = true;
        private static bool _chordUsed;
        private static float _pressTime;

        public static bool IsHeld { get; private set; }
        public static bool WasTapped { get; private set; }

        public static bool IsGamepadActive =>
            ModConfig.General.EnableMod.Value && ModConfig.Input.EnableGamepadControls.Value && ZInput.IsGamepadActive();

        // the sticks and triggers drive the map while its open and PlayerController.TakeInput stops gamepad gameplay,
        // so the gamepad build controls stop too
        public static bool IsBuildInputActive => IsGamepadActive && !Minimap.IsOpen();

        public static void Initialize()
        {
            Cleanup();
            IReadOnlyList<ConfigEntry<InputManager.GamepadButton>> buttons = ModConfig.Input.AllButtons;
            for (int index = 0; index < buttons.Count; index++)
            {
                buttons[index].SettingChanged += OnButtonSettingChanged;
            }
        }

        public static void Cleanup()
        {
            IReadOnlyList<ConfigEntry<InputManager.GamepadButton>> buttons = ModConfig.Input.AllButtons;
            for (int index = 0; index < buttons.Count; index++)
            {
                buttons[index].SettingChanged -= OnButtonSettingChanged;
            }

            IsHeld = false;
            WasTapped = false;
            _suppressionAnchor = null;
            _suppressionDirty = true;
            SuppressedButtons.Clear();
        }

        // runs right after ZInput ticks its buttons. press and release edges are made in that tick, so whatever order
        // the game runs its Update methods in the reset here clears them before anything reads them
        public static void OnInputUpdated()
        {
            BuildRadialMenu.RefreshOpenState();
            UpdateLayerState();
            SuppressBaseButtons();
        }

        public static void SuppressBaseButtons()
        {
            if (IsHeld)
            {
                for (int index = 0; index < SuppressedButtons.Count; index++)
                {
                    SuppressedButtons[index].ResetState();
                }
            }

            if (BuildRadialMenu.IsOpen)
            {
                for (int index = 0; index < RadialSuppressedButtons.Length; index++)
                {
                    ZInput.ResetButtonStatus(RadialSuppressedButtons[index]);
                }
            }
        }

        public static bool IsLayerButtonPressed() =>
            ZInput.GetButtonDown(GamepadButtonMap.GetRawButtonName(ModConfig.Input.LayerButton.Value));

        public static bool IsChordHeld(BuildAction action) =>
            IsHeld &&
            action.GamepadButton != null &&
            ZInput.GetButton(GamepadButtonMap.GetRawButtonName(action.GamepadButton.Value));

        private static void UpdateLayerState()
        {
            WasTapped = false;
            if (!IsLayerContextValid())
            {
                IsHeld = false;
                return;
            }

            string layerName = GamepadButtonMap.GetRawButtonName(ModConfig.Input.LayerButton.Value);
            if (!IsHeld)
            {
                if (!ZInput.GetButtonDown(layerName))
                {
                    return;
                }

                IsHeld = true;
                _chordUsed = false;
                _pressTime = Time.time;
            }
            else if (!ZInput.GetButton(layerName))
            {
                IsHeld = false;
                WasTapped = !_chordUsed && Time.time - _pressTime < TapThreshold;
                return;
            }

            RefreshSuppressedButtons(layerName);
            if (!_chordUsed)
            {
                _chordUsed = IsAnyChordButtonHeld();
            }
        }

        // one bump of the mouse flips the active device for a frame. dropping the layer for that would send the next
        // chord press to the base game with the button still down, so ZInput.IsGamepadActive is left out on purpose
        private static bool IsLayerContextValid()
        {
            if (!ModConfig.General.EnableMod.Value || !ModConfig.Input.EnableGamepadControls.Value || Minimap.IsOpen())
            {
                return false;
            }

            Player player = Player.m_localPlayer;
            return player && player.InPlaceMode() && !InputUtils.IsTextInputOrMenuOpen();
        }

        private static bool IsAnyChordButtonHeld()
        {
            IReadOnlyList<ConfigEntry<InputManager.GamepadButton>> buttons = ModConfig.Input.AllButtons;
            for (int index = 0; index < buttons.Count; index++)
            {
                ConfigEntry<InputManager.GamepadButton> button = buttons[index];
                if (!ReferenceEquals(button, ModConfig.Input.LayerButton) &&
                    ZInput.GetButton(GamepadButtonMap.GetRawButtonName(button.Value)))
                {
                    return true;
                }
            }

            return false;
        }

        // every layout refresh throws away all gamepad buttons and makes new ones, so the layer button object changing
        // means the list is stale
        private static void RefreshSuppressedButtons(string layerName)
        {
            Dictionary<string, ZInput.ButtonDef> gameButtons = ZInput.instance.m_buttons;
            gameButtons.TryGetValue(layerName, out ZInput.ButtonDef layerButton);
            if (!_suppressionDirty && ReferenceEquals(layerButton, _suppressionAnchor))
            {
                return;
            }

            _suppressionAnchor = layerButton;
            _suppressionDirty = false;
            SuppressedButtons.Clear();
            OwnButtonNames.Clear();
            OwnButtonPaths.Clear();

            IReadOnlyList<ConfigEntry<InputManager.GamepadButton>> ownButtons = ModConfig.Input.AllButtons;
            for (int index = 0; index < ownButtons.Count; index++)
            {
                string ownName = GamepadButtonMap.GetRawButtonName(ownButtons[index].Value);
                if (gameButtons.TryGetValue(ownName, out ZInput.ButtonDef ownButton) && ownButton.ButtonAction.bindings.Count > 0)
                {
                    OwnButtonNames.Add(ownName);
                    OwnButtonPaths.Add(ownButton.GetActionPath());
                }
            }

            // anything else the game or another mod put on the same physical button gets reset while the layer is held
            // the raw buttons i read myself stay out of the list or the chords would never fire
            foreach (ZInput.ButtonDef gameButton in gameButtons.Values)
            {
                if (gameButton.Source == ZInput.InputSource.Gamepad &&
                    gameButton.ButtonAction.bindings.Count > 0 &&
                    !OwnButtonNames.Contains(gameButton.Name) &&
                    OwnButtonPaths.Contains(gameButton.GetActionPath()))
                {
                    SuppressedButtons.Add(gameButton);
                }
            }
        }

        private static void OnButtonSettingChanged(object sender, EventArgs eventArgs) =>
            _suppressionDirty = true;
    }
}
