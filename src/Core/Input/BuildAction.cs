using System;
using BepInEx.Configuration;
using Jotunn.Configs;
using Jotunn.Managers;
using MasterBuilderHeeler.Configuration;
using UnityEngine;

namespace MasterBuilderHeeler.Core.Input
{
    // the gamepad button an action starts on. it only counts while the layer button is held
    // leave the description out and the setting uses the description of the action
    public record struct GamepadBinding(InputManager.GamepadButton DefaultButton, string? Description = null);

    public sealed class BuildAction
    {
        private static readonly Func<bool> AlwaysAvailable = () => true;

        public BuildAction(
            string name,
            string displayName,
            KeyCode defaultKey,
            string description,
            Action<Player>? onPressed = null,
            Func<bool>? isAvailable = null,
            GamepadBinding? gamepad = null,
            BuildAction? gamepadStandIn = null)
        {
            Name = name;
            DisplayName = displayName;
            DefaultKey = defaultKey;
            Description = description;
            OnPressed = onPressed;
            IsAvailable = isAvailable == null ? AlwaysAvailable : isAvailable;
            Gamepad = gamepad;
            GamepadStandIn = gamepadStandIn;
        }

        public string Name { get; }

        public string DisplayName { get; }

        public KeyCode DefaultKey { get; }

        public string Description { get; }

        // null for a key the router reads itself: the held nudge keys, commit and cancel, and the radial key
        public Action<Player>? OnPressed { get; }

        public Func<bool> IsAvailable { get; }

        public GamepadBinding? Gamepad { get; }

        public BuildAction? GamepadStandIn { get; }

        // KeyCode.None makes the action gamepad only. it gets no key setting and no jotunn button, so Key and Button
        // stay unset
        public bool HasKey => DefaultKey != KeyCode.None;

        public ConfigEntry<KeyboardShortcut> Key { get; private set; } = null!;

        public ButtonConfig Button { get; private set; } = null!;

        public ConfigEntry<InputManager.GamepadButton>? GamepadButton { get; private set; }

        public void BindConfig(ConfigSection section)
        {
            if (!HasKey)
            {
                return;
            }

            Key = section.Client(
                $"{Name}Key",
                new KeyboardShortcut(DefaultKey),
                Description,
                attributes: new ConfigurationManagerAttributes { DispName = DisplayName });
        }

        // BuildActions.BindGamepadConfig can call this twice for one action so the second call does nothing
        public void BindGamepadConfig(ConfigSection section)
        {
            if (!Gamepad.HasValue || GamepadButton != null)
            {
                return;
            }

            GamepadBinding binding = Gamepad.Value;
            GamepadButton = section.Client(
                $"{Name}Button",
                binding.DefaultButton,
                binding.Description == null ? Description : binding.Description,
                attributes: new ConfigurationManagerAttributes { DispName = DisplayName });
        }

        public void RegisterButton()
        {
            if (!HasKey)
            {
                return;
            }

            Button = new ButtonConfig
            {
                Name = Name,
                ShortcutConfig = Key,
                ActiveInGUI = false,
                ActiveInCustomGUI = false,
                BlockOtherInputs = false,
                HintToken = null
            };

            InputManager.Instance.AddButton(Plugin.ModGUID, Button);
        }
    }
}
