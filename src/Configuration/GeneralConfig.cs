using BepInEx.Configuration;

namespace MasterBuilderHeeler.Configuration
{
    public sealed class GeneralConfig(ConfigSection section)
    {
        public ConfigEntry<bool> EnableMod { get; } = section.Synced("EnableMod", true,
            "Enables or disables MasterBuilderHeeler.");

        public ConfigEntry<bool> EnablePrecision { get; } = section.Synced("EnablePrecision", true,
            "Enables precision placement, 3-axis rotation, nudging, ghost freeze, in-place piece edit, piece sampling, the gizmo, and the Precision panel.");

        public ConfigEntry<bool> EnableBuildCamera { get; } = section.Synced("EnableBuildCamera", true,
            "Enables the Build Camera, its spotlight, mist clearing, and item auto-pickup.");

        public ConfigEntry<bool> EnableGrid { get; } = section.Synced("EnableGrid", true,
            "Enables grid snapping and the grid overlay.");

        public ConfigEntry<bool> EnableInfoPanels { get; } = section.Synced("EnableInfoPanels", true,
            "Displays all HUD control and piece info panels (Global, Precision, Hover, and Placement). Disable to remove the panels and stop their updates.");

        public ConfigEntry<bool> EnableExtendedAvatarReach { get; } = section.Synced("EnableExtendedAvatarReach", false,
            "Extends player placement, repair, dismantle, and in-place edit reach to MaxBuildReachDistance.");

        public ConfigEntry<bool> ValidateStationRangeAtPieceLocation { get; } = section.Synced("ValidateStationRangeAtPieceLocation", false,
            "Checks crafting station build range at the target piece position instead of the player position during standard placement, repair, and dismantle. The Build Camera always uses the target piece position.");

        public ConfigEntry<bool> RequireHammerForHoverInspection { get; } = section.Synced("RequireHammerForHoverInspection", true,
            "Requires an equipped build tool to display the hovered piece info panel. Disable to show hovered piece info at all times.");

        public ConfigEntry<ModLogLevel> LoggingLevel { get; } = section.Client("LoggingLevel", ModLogLevel.Info,
            "BepInEx log detail level (None = silent, Info = startup and edits, Debug = detailed actions).");
    }
}
