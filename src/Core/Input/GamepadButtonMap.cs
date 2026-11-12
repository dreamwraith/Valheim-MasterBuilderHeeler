using System.Collections.Generic;
using Jotunn.Managers;
using Valheim.SettingsGui;

namespace MasterBuilderHeeler.Core.Input
{
    public static class GamepadButtonMap
    {
        public const string Unbound = "Unbound";

        // the stick and not its click. classic rotate is a push of this stick
        public const string RightStickPath = "<Gamepad>/rightStick";

        // these are the raw buttons the game registers in every controller layout, so they survive layout changes
        // unlike anything jotunn registers. unknown names just read as not pressed
        public static string GetRawButtonName(InputManager.GamepadButton button) => button switch
        {
            InputManager.GamepadButton.DPadUp => "JoyDPadUp",
            InputManager.GamepadButton.DPadDown => "JoyDPadDown",
            InputManager.GamepadButton.DPadLeft => "JoyDPadLeft",
            InputManager.GamepadButton.DPadRight => "JoyDPadRight",
            InputManager.GamepadButton.ButtonSouth => "JoyButtonA",
            InputManager.GamepadButton.ButtonEast => "JoyButtonB",
            InputManager.GamepadButton.ButtonWest => "JoyButtonX",
            InputManager.GamepadButton.ButtonNorth => "JoyButtonY",
            InputManager.GamepadButton.LeftShoulder => "JoyLBumper",
            InputManager.GamepadButton.RightShoulder => "JoyRBumper",
            InputManager.GamepadButton.LeftTrigger => "JoyLTrigger",
            InputManager.GamepadButton.RightTrigger => "JoyRTrigger",
            InputManager.GamepadButton.SelectButton => "JoyBack",
            InputManager.GamepadButton.StartButton => "JoyStart",
            InputManager.GamepadButton.LeftStickButton => "JoyLStick",
            InputManager.GamepadButton.RightStickButton => "JoyRStick",
            _ => string.Empty
        };

        public static string GetButtonLabel(InputManager.GamepadButton button) =>
            GetButtonLabel(GetRawButtonName(button));

        // label comes from the path the button is bound to right now, so layout changes and the swap settings are covered
        // a button can exist with an empty binding. the games MapKeyFromPath throws on an empty path so those stop here
        public static string GetButtonLabel(string buttonName)
        {
            if (!ZInput.instance.m_buttons.TryGetValue(buttonName, out ZInput.ButtonDef button) ||
                button.ButtonAction.bindings.Count == 0)
            {
                return Unbound;
            }

            string path = button.GetActionPath();
            return string.IsNullOrEmpty(path) ? Unbound : GetPathLabel(path);
        }

        // same lookup ZInput.GetBoundKeyString does for the games own button sprites. the tag names the sprite
        // set and TMP finds a named set on its own. text is only for a binding the table doesnt have
        public static string GetPathLabel(string path)
        {
            string spriteSet = SelectForController("xbox", "ps5", "switch2");
            if (ZInput.s_gamepadSpriteMap.TryGetValue(ZInput.instance.MapKeyFromPath(path), out Dictionary<string, string> spriteNames) &&
                spriteNames.TryGetValue(spriteSet, out string spriteName))
            {
                return $"<sprite=\"{spriteSet}\" name=\"{spriteName}\">";
            }

            return GetPathText(path);
        }

        private static string GetPathText(string path) => path switch
        {
            "<Gamepad>/buttonSouth" => SelectForController("A", "Cross", "B"),
            "<Gamepad>/buttonEast" => SelectForController("B", "Circle", "A"),
            "<Gamepad>/buttonWest" => SelectForController("X", "Square", "Y"),
            "<Gamepad>/buttonNorth" => SelectForController("Y", "Triangle", "X"),
            "<Gamepad>/leftShoulder" => SelectForController("LB", "L1", "L"),
            "<Gamepad>/rightShoulder" => SelectForController("RB", "R1", "R"),
            "<Gamepad>/leftTrigger" => SelectForController("LT", "L2", "ZL"),
            "<Gamepad>/rightTrigger" => SelectForController("RT", "R2", "ZR"),
            "<Gamepad>/leftStickPress" => SelectForController("LS", "L3", "LS"),
            "<Gamepad>/rightStickPress" => SelectForController("RS", "R3", "RS"),
            RightStickPath => "R Stick",
            "<Gamepad>/dpad/up" => "DPad Up",
            "<Gamepad>/dpad/down" => "DPad Down",
            "<Gamepad>/dpad/left" => "DPad Left",
            "<Gamepad>/dpad/right" => "DPad Right",
            "<Gamepad>/select" => "Select",
            "<Gamepad>/start" => "Start",
            "<DualShockGamepad>/touchpadButton" => "Touchpad",
            _ => Unbound
        };

        private static string SelectForController(string xboxValue, string playStationValue, string switchValue)
        {
            GamepadGlyphs glyphs = ZInput.CurrentGlyph == GamepadGlyphs.Auto
                ? ZInput.ConnectedGamepadTypeGlyphs()
                : ZInput.CurrentGlyph;

            return glyphs switch
            {
                GamepadGlyphs.Playstation => playStationValue,
                GamepadGlyphs.Switch => switchValue,
                _ => xboxValue
            };
        }
    }
}
