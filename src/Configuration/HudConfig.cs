using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Configuration;

namespace MasterBuilderHeeler.Configuration
{
    public record struct KeymapRowSetting(string DisplayName, ConfigEntry<bool> ShowKeymap);

    public sealed class HudConfig(
        ConfigSection hud,
        ConfigSection globalPanel,
        ConfigSection globalLegend,
        ConfigSection precisionPanel,
        ConfigSection precisionLegend,
        ConfigSection hoverPanel,
        ConfigSection placementPanel)
    {
        public ConfigEntry<bool> ShowInfoPanelBackground { get; } = hud.Client("ShowInfoPanelBackground", true,
            "Draws a rounded background behind HUD panels. Disable to show text without panel backgrounds.");

        public ConfigEntry<float> InfoUpdateInterval { get; } = hud.Client("InfoUpdateInterval", 0.25f,
            "Seconds between updates on the Hover and Placement piece info panels.",
            new AcceptableValueRange<float>(0.05f, 1.0f));

        public ConfigEntry<bool> HideNativeCrosshairHealthBar { get; } = hud.Client("HideNativeCrosshairHealthBar", false,
            "Hides the base game crosshair piece health bar while the Hover panel shows piece health.");

        public ConfigEntry<KeymapDisplayMode> KeymapDisplay { get; } = hud.Client("KeymapDisplay", KeymapDisplayMode.CurrentInput,
            "Selects the bindings that the HUD panels show (CurrentInput = the input device in use, KeyboardOnly = keyboard and mouse, GamepadOnly = gamepad, BothAlways = keyboard and gamepad side by side). Gamepad bindings show only while EnableGamepadControls is on.");

        public PanelLayoutSettings GlobalPanelLayout { get; } = new PanelLayoutSettings(
            globalPanel,
            "Global controls panel",
            "bottom-right corner",
            defaultOffsetX: -380f,
            defaultOffsetY: 100f,
            defaultFontSize: 13);

        // each legend binds right after the layout settings of its panel, so the legend section sits under the
        // panel section in the settings list
        private readonly KeymapRowSetting[] _globalRows =
            BindRows(globalLegend, "Global controls panel", Enum.GetNames(typeof(GlobalKeymapRow)));

        public PanelLayoutSettings PrecisionPanelLayout { get; } = new PanelLayoutSettings(
            precisionPanel,
            "Precision controls panel",
            "bottom-right corner",
            defaultOffsetX: -40f,
            defaultOffsetY: 100f,
            defaultFontSize: 13);

        private readonly KeymapRowSetting[] _precisionRows =
            BindRows(precisionLegend, "Precision controls panel", Enum.GetNames(typeof(PrecisionKeymapRow)));

        public IReadOnlyList<ConfigEntry<bool>> KeymapFlags { get; } =
            Join(globalLegend.All<bool>(), precisionLegend.All<bool>());

        public PanelLayoutSettings HoverPanelLayout { get; } = new PanelLayoutSettings(
            hoverPanel,
            "hovered piece info panel",
            "crosshair",
            defaultOffsetX: 225f,
            defaultOffsetY: -30f,
            defaultFontSize: 13);

        public ConfigEntry<bool> ShowHoverName { get; } = hoverPanel.Client("ShowHoverName", true,
            "Displays the name of the hovered piece.");

        public ConfigEntry<bool> ShowHoverPrefab { get; } = hoverPanel.Client("ShowHoverPrefab", false,
            "Displays the internal prefab ID of the hovered piece.");

        public ConfigEntry<bool> ShowHoverHealth { get; } = hoverPanel.Client("ShowHoverHealth", true,
            "Displays current health, max health, and health percentage of the hovered piece.");

        public ConfigEntry<bool> ShowHoverSupport { get; } = hoverPanel.Client("ShowHoverSupport", true,
            "Displays structural support and stability percentage of the hovered piece.");

        public ConfigEntry<bool> ShowHoverEuler { get; } = hoverPanel.Client("ShowHoverEuler", true,
            "Displays rotation angles in degrees of the hovered piece.");

        public ConfigEntry<bool> ShowHoverQuaternion { get; } = hoverPanel.Client("ShowHoverQuaternion", false,
            "Displays Quaternion rotation values (x, y, z, w) of the hovered piece.");

        public PanelLayoutSettings PlacementPanelLayout { get; } = new PanelLayoutSettings(
            placementPanel,
            "active placement piece info panel",
            "crosshair",
            defaultOffsetX: -110f,
            defaultOffsetY: -150f,
            defaultFontSize: 13);

        public ConfigEntry<bool> ShowGhostName { get; } = placementPanel.Client("ShowGhostName", true,
            "Displays the name of the active placement ghost or edited piece.");

        public ConfigEntry<bool> ShowGhostPrefab { get; } = placementPanel.Client("ShowGhostPrefab", false,
            "Displays the internal prefab ID of the active placement ghost or edited piece.");

        public ConfigEntry<bool> ShowGhostEuler { get; } = placementPanel.Client("ShowGhostEuler", true,
            "Displays rotation angles in degrees of the active placement ghost or edited piece.");

        public ConfigEntry<bool> ShowGhostQuaternion { get; } = placementPanel.Client("ShowGhostQuaternion", false,
            "Displays Quaternion rotation values (x, y, z, w) of the active placement ghost or edited piece.");

        public ConfigEntry<bool> ShowGhostDistance { get; } = placementPanel.Client("ShowGhostDistance", true,
            "Displays distance in meters from the player to the target placement position.");

        public ConfigEntry<bool> ShowGhostWorldPosition { get; } = placementPanel.Client("ShowGhostWorldPosition", false,
            "Displays world coordinates (X, Y, Z) of the active placement ghost or edited piece.");

        public KeymapRowSetting Get(GlobalKeymapRow row) => _globalRows[(int)row];

        public KeymapRowSetting Get(PrecisionKeymapRow row) => _precisionRows[(int)row];

        // a row enum makes one setting per row so nothing here is written out per row
        private static KeymapRowSetting[] BindRows(ConfigSection section, string panelDescription, string[] rowNames)
        {
            KeymapRowSetting[] rows = new KeymapRowSetting[rowNames.Length];
            for (int index = 0; index < rowNames.Length; index++)
            {
                string displayName = ToDisplayName(rowNames[index]);
                ConfigEntry<bool> flag = section.Client(rowNames[index], true,
                    $"Displays the key binding on the {displayName} row of the {panelDescription}. A status or value on the row stays visible. A row that shows only a key binding is removed.",
                    attributes: new ConfigurationManagerAttributes { DispName = displayName });
                rows[index] = new KeymapRowSetting(displayName, flag);
            }

            return rows;
        }

        // FreezeGhost to Freeze Ghost
        private static string ToDisplayName(string rowName)
        {
            StringBuilder builder = new StringBuilder(rowName.Length + 4);
            for (int index = 0; index < rowName.Length; index++)
            {
                if (index > 0 && char.IsUpper(rowName[index]))
                {
                    builder.Append(' ');
                }
                builder.Append(rowName[index]);
            }

            return builder.ToString();
        }

        private static List<ConfigEntry<bool>> Join(IReadOnlyList<ConfigEntry<bool>> first, IReadOnlyList<ConfigEntry<bool>> second)
        {
            List<ConfigEntry<bool>> joined = new List<ConfigEntry<bool>>(first.Count + second.Count);
            joined.AddRange(first);
            joined.AddRange(second);
            return joined;
        }
    }
}
