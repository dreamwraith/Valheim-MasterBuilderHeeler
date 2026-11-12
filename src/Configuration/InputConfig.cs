using System.Collections.Generic;
using BepInEx.Configuration;
using Jotunn.Managers;
using MasterBuilderHeeler.Core.Input;

namespace MasterBuilderHeeler.Configuration
{
    public sealed class InputConfig
    {
        // the action catalog owns the key and the gamepad button of each action, so it binds in between the settings
        // in here and the two sections come out in the order of the settings list
        public InputConfig(ConfigSection keyboard, ConfigSection gamepad)
        {
            BuildActions.BindConfig(keyboard);

            EnableGamepadControls = gamepad.Client("EnableGamepadControls", true,
                "Enables gamepad controls in build mode. Keyboard and mouse bindings stay active.",
                attributes: new ConfigurationManagerAttributes { DispName = "Enable Gamepad Controls" });

            LayerButton = gamepad.Client("LayerButton", InputManager.GamepadButton.ButtonWest,
                "Layer button for build mode. Hold it and press another button to use the bindings in this section. Tap it to open the build radial menu.",
                attributes: new ConfigurationManagerAttributes { DispName = "Layer Button" });

            BuildActions.BindGamepadConfig(gamepad);
            AllButtons = gamepad.All<InputManager.GamepadButton>();
        }

        public ConfigEntry<bool> EnableGamepadControls { get; }

        public ConfigEntry<InputManager.GamepadButton> LayerButton { get; }

        public IReadOnlyList<ConfigEntry<InputManager.GamepadButton>> AllButtons { get; }
    }
}
