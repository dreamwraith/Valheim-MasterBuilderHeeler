using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using Jotunn.Managers;
using MasterBuilderHeeler.Configuration;
using MasterBuilderHeeler.Core.Input;
using MasterBuilderHeeler.Utilities;

namespace MasterBuilderHeeler.Core.UI
{
    public static class HudKeybindFormatter
    {
        private static readonly Dictionary<BuildAction, string> GamepadLabelCache = new Dictionary<BuildAction, string>(40);

        private static string? _cachedForwardNudgeLabel;
        private static string? _cachedSideNudgeLabel;
        private static string? _cachedVerticalNudgeLabel;

        public static int KeybindVersion { get; private set; }

        public static void Initialize()
        {
            Cleanup();
            IReadOnlyList<BuildAction> actions = BuildActions.All;
            for (int index = 0; index < actions.Count; index++)
            {
                if (actions[index].HasKey)
                {
                    actions[index].Key.SettingChanged += OnShortcutSettingChanged;
                }
            }

            IReadOnlyList<ConfigEntry<InputManager.GamepadButton>> gamepadButtons = ModConfig.Input.AllButtons;
            for (int index = 0; index < gamepadButtons.Count; index++)
            {
                gamepadButtons[index].SettingChanged += OnShortcutSettingChanged;
            }

            // showing or hiding a key on a hud row changes the row text and the panel width the way a rebind does
            IReadOnlyList<ConfigEntry<bool>> keymapFlags = ModConfig.Hud.KeymapFlags;
            for (int index = 0; index < keymapFlags.Count; index++)
            {
                keymapFlags[index].SettingChanged += OnShortcutSettingChanged;
            }

            ModConfig.Input.EnableGamepadControls.SettingChanged += OnShortcutSettingChanged;
            ModConfig.Hud.KeymapDisplay.SettingChanged += OnShortcutSettingChanged;
            // fires when the player goes from keyboard to gamepad or back and when the controller layout changes
            ZInput.OnInputLayoutChanged += InvalidateCache;
            HudTheme.ThemeChanged += ClearFormattedNudgeLabels;

            InvalidateCache();
        }

        public static void Cleanup()
        {
            HudTheme.ThemeChanged -= ClearFormattedNudgeLabels;
            ZInput.OnInputLayoutChanged -= InvalidateCache;
            ModConfig.Hud.KeymapDisplay.SettingChanged -= OnShortcutSettingChanged;
            ModConfig.Input.EnableGamepadControls.SettingChanged -= OnShortcutSettingChanged;

            IReadOnlyList<ConfigEntry<bool>> keymapFlags = ModConfig.Hud.KeymapFlags;
            for (int index = 0; index < keymapFlags.Count; index++)
            {
                keymapFlags[index].SettingChanged -= OnShortcutSettingChanged;
            }

            IReadOnlyList<ConfigEntry<InputManager.GamepadButton>> gamepadButtons = ModConfig.Input.AllButtons;
            for (int index = 0; index < gamepadButtons.Count; index++)
            {
                gamepadButtons[index].SettingChanged -= OnShortcutSettingChanged;
            }

            IReadOnlyList<BuildAction> actions = BuildActions.All;
            for (int index = 0; index < actions.Count; index++)
            {
                if (actions[index].HasKey)
                {
                    actions[index].Key.SettingChanged -= OnShortcutSettingChanged;
                }
            }

            GamepadLabelCache.Clear();
        }

        public static string Format(BuildAction action) =>
            JoinDevices(
                IsKeyboardShown ? KeyLabelFormatter.Format(action.Key) : null,
                IsGamepadShown ? GetGamepadLabel(action) : null);

        public static string FormatPair(BuildAction first, BuildAction second) =>
            JoinDevices(
                IsKeyboardShown ? JoinPair(KeyLabelFormatter.Format(first.Key), KeyLabelFormatter.Format(second.Key)) : null,
                IsGamepadShown ? JoinPair(GetGamepadLabel(first), GetGamepadLabel(second)) : null);

        public static string FormatRotate() =>
            JoinDevices(
                IsKeyboardShown ? "Scroll" : null,
                IsGamepadShown ? FormatGamepadRotate() : null);

        public static string FormatForwardNudge()
        {
            if (_cachedForwardNudgeLabel == null)
            {
                _cachedForwardNudgeLabel = FormatNudgePair(
                    BuildActions.NudgeForward,
                    HudGlyphs.TriangleUpSolid,
                    BuildActions.NudgeBackward,
                    HudGlyphs.TriangleDownSolid,
                    HudTheme.ArrowForwardHex);
            }
            return _cachedForwardNudgeLabel;
        }

        public static string FormatSideNudge()
        {
            if (_cachedSideNudgeLabel == null)
            {
                _cachedSideNudgeLabel = FormatNudgePair(
                    BuildActions.NudgeLeft,
                    HudGlyphs.TriangleLeftSolid,
                    BuildActions.NudgeRight,
                    HudGlyphs.TriangleRightSolid,
                    HudTheme.ArrowRightHex);
            }
            return _cachedSideNudgeLabel;
        }

        public static string FormatVerticalNudge()
        {
            if (_cachedVerticalNudgeLabel == null)
            {
                _cachedVerticalNudgeLabel = FormatNudgePair(
                    BuildActions.NudgeUp,
                    HudGlyphs.TriangleUpHollow,
                    BuildActions.NudgeDown,
                    HudGlyphs.TriangleDownHollow,
                    HudTheme.ArrowUpHex);
            }
            return _cachedVerticalNudgeLabel;
        }

        private static bool IsGamepadShown =>
            ModConfig.Input.EnableGamepadControls.Value &&
            ModConfig.Hud.KeymapDisplay.Value switch
            {
                KeymapDisplayMode.CurrentInput => GamepadChordLayer.IsGamepadActive,
                KeymapDisplayMode.KeyboardOnly => false,
                _ => true
            };

        private static bool IsKeyboardShown =>
            ModConfig.Hud.KeymapDisplay.Value == KeymapDisplayMode.BothAlways || !IsGamepadShown;

        private static string GetGamepadLabel(BuildAction action)
        {
            if (GamepadLabelCache.TryGetValue(action, out string cachedLabel))
            {
                return cachedLabel;
            }

            string formatted = FormatGamepad(action);
            GamepadLabelCache[action] = formatted;
            return formatted;
        }

        // commit and cancel follow the layouts place and remove buttons. an action with a gamepad button shows its chord
        // (layer button plus that button). everything else only lives in the build radial so it shows how to open that
        private static string FormatGamepad(BuildAction action)
        {
            if (ReferenceEquals(action, BuildActions.CommitPieceEdit))
            {
                return GamepadButtonMap.GetButtonLabel("JoyPlace");
            }

            if (ReferenceEquals(action, BuildActions.CancelPieceEdit))
            {
                return GamepadButtonMap.GetButtonLabel("JoyRemove");
            }

            BuildAction gamepadAction = action.GamepadStandIn == null ? action : action.GamepadStandIn;
            string layerLabel = GamepadButtonMap.GetButtonLabel(ModConfig.Input.LayerButton.Value);
            // this text always ends up in a keycap and the keycap is bold, so the tags take the wheel back out of it
            return gamepadAction.GamepadButton != null
                ? $"{layerLabel}+{GamepadButtonMap.GetButtonLabel(gamepadAction.GamepadButton.Value)}"
                : $"</b>{HudGlyphs.Wheel}<b> {layerLabel}";
        }

        private static string FormatGamepadRotate()
        {
            string rotateLabel = GamepadButtonMap.GetButtonLabel("JoyRotate");
            return ZInput.InputLayout == InputLayout.Default
                ? $"{rotateLabel}+{GamepadButtonMap.GetPathLabel(GamepadButtonMap.RightStickPath)}"
                : $"{rotateLabel}{HudTheme.PipeSeparator}{GamepadButtonMap.GetButtonLabel("JoyRotateRight")}";
        }

        // the key stays the normal key color and only the symbol is colored like on the axis rows. the symbol takes
        // the color of the gizmo arrow that the nudge moves along
        private static string FormatNudgePair(
            BuildAction firstAction,
            string firstGlyph,
            BuildAction secondAction,
            string secondGlyph,
            string arrowColorHex)
        {
            string firstSymbol = $"<color={arrowColorHex}>{firstGlyph}</color>";
            string secondSymbol = $"<color={arrowColorHex}>{secondGlyph}</color>";
            return JoinDevices(
                IsKeyboardShown
                    ? JoinPair($"{KeyLabelFormatter.Format(firstAction.Key)} {firstSymbol}", $"{KeyLabelFormatter.Format(secondAction.Key)} {secondSymbol}")
                    : null,
                IsGamepadShown
                    ? JoinPair($"{GetGamepadLabel(firstAction)} {firstSymbol}", $"{GetGamepadLabel(secondAction)} {secondSymbol}")
                    : null);
        }

        private static string JoinPair(string firstLabel, string secondLabel) =>
            $"{firstLabel}{HudTheme.PipeSeparator}{secondLabel}";

        // null means that device is not on show and one of the two always is. the keyboard set goes first when both show
        private static string JoinDevices(string? keyboardText, string? gamepadText)
        {
            if (keyboardText == null)
            {
                return gamepadText!;
            }

            return gamepadText == null
                ? keyboardText
                : $"{keyboardText}{HudTheme.VerticalEllipsisSeparator}{gamepadText}";
        }

        private static void OnShortcutSettingChanged(object sender, EventArgs eventArgs) => InvalidateCache();

        private static void ClearFormattedNudgeLabels()
        {
            _cachedForwardNudgeLabel = null;
            _cachedSideNudgeLabel = null;
            _cachedVerticalNudgeLabel = null;
        }

        private static void InvalidateCache()
        {
            KeyLabelFormatter.ClearCache();
            GamepadLabelCache.Clear();
            ClearFormattedNudgeLabels();
            KeybindVersion++;
        }
    }
}
