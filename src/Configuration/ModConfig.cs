using BepInEx.Configuration;
using ConditionalConfigSync;

namespace MasterBuilderHeeler.Configuration
{
    public static class ModConfig
    {
        public static ConfigSync ConfigSync { get; private set; } = null!;

        public static GeneralConfig General { get; private set; } = null!;
        public static BuildConfig Build { get; private set; } = null!;
        public static HudConfig Hud { get; private set; } = null!;
        public static InputConfig Input { get; private set; } = null!;
        public static HudColorConfig HudColors { get; private set; } = null!;

        public static bool IsInfoLogging => General.LoggingLevel.Value >= ModLogLevel.Info;
        public static bool IsDebugLogging => General.LoggingLevel.Value >= ModLogLevel.Debug;

        // jotunn numbers a section when its first setting binds, so the order in here is the order of the settings list
        // inside a section the settings are in the order they bind, top to bottom in each class
        public static void Initialize(ConfigFile config)
        {
            // bepinex writes the whole file on every bind while this is on, so its off until the last bind is in
            config.SaveOnConfigSet = false;

            ConfigSync = new ConfigSync(Plugin.ModGUID)
            {
                DisplayName = Plugin.ModName,
                CurrentVersion = Plugin.ModVersion,
                MinimumRequiredVersion = Plugin.MinimumCompatibleVersion,
                ModRequired = false,
                ModRequirementMode = ModRequirementMode.Conditional
            };

            General = new GeneralConfig(new ConfigSection(config, "General"));

            Build = new BuildConfig(
                new ConfigSection(config, "Precision Settings"),
                new ConfigSection(config, "Gizmo Settings"),
                new ConfigSection(config, "Grid Settings"),
                new ConfigSection(config, "Build Camera"),
                new ConfigSection(config, "Build Camera Light"),
                new ConfigSection(config, "Build Camera Extras"));

            Hud = new HudConfig(
                new ConfigSection(config, "HUD & Info Panels"),
                new ConfigSection(config, "Global Panel"),
                new ConfigSection(config, "Global Panel Legend"),
                new ConfigSection(config, "Precision Panel"),
                new ConfigSection(config, "Precision Panel Legend"),
                new ConfigSection(config, "Hover Panel"),
                new ConfigSection(config, "Placement Panel"));

            Input = new InputConfig(
                new ConfigSection(config, "Keyboard & Mouse Bindings"),
                new ConfigSection(config, "Gamepad Bindings"));

            HudColors = new HudColorConfig(new ConfigSection(config, "HUD Colors"));

            ConfigMigration.MigrateMovedSections(config);

            config.Save();
            config.SaveOnConfigSet = true;
        }
    }
}
