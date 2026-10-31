using BepInEx.Configuration;
using ConditionalConfigSync;
using Jotunn.Extensions;
using UnityEngine;

namespace MasterBuilderHeeler.Configuration
{
    public static class ModConfig
    {
        private static readonly Color DefaultPanelBackgroundColor = new Color(0.08f, 0.08f, 0.08f, 0.75f);

        public static ConfigSync ConfigSync { get; private set; } = null!;

        public static ConfigEntry<bool> LockConfiguration { get; private set; } = null!;
        public static ConfigEntry<bool> EnableMod { get; private set; } = null!;
        public static ConfigEntry<bool> EnablePrecision { get; private set; } = null!;
        public static ConfigEntry<bool> EnableBuildCamera { get; private set; } = null!;
        public static ConfigEntry<bool> EnableGrid { get; private set; } = null!;
        public static ConfigEntry<bool> EnableInfoPanels { get; private set; } = null!;
        public static ConfigEntry<bool> EnableExtendedAvatarReach { get; private set; } = null!;
        public static ConfigEntry<bool> ValidateStationRangeAtPieceLocation { get; private set; } = null!;
        public static ConfigEntry<bool> RequireHammerForHoverInspection { get; private set; } = null!;
        public static ConfigEntry<GizmoStyle> GizmoRendererStyle { get; private set; } = null!;
        public static ConfigEntry<ModLogLevel> LoggingLevel { get; private set; } = null!;

        public static ConfigEntry<bool> EnablePrecisionModeByDefault { get; private set; } = null!;
        public static ConfigEntry<bool> EnableRoofModeByDefault { get; private set; } = null!;
        public static ConfigEntry<PrecisionCopyMode> PrecisionCopyBehavior { get; private set; } = null!;
        public static ConfigEntry<float> FineRotationStep { get; private set; } = null!;
        public static ConfigEntry<float> StandardRotationStep { get; private set; } = null!;
        public static ConfigEntry<float> CoarseRotationStep { get; private set; } = null!;
        public static ConfigEntry<float> FineTranslationStep { get; private set; } = null!;
        public static ConfigEntry<float> StandardTranslationStep { get; private set; } = null!;
        public static ConfigEntry<float> CoarseTranslationStep { get; private set; } = null!;

        public static ConfigEntry<float> DefaultGridSize { get; private set; } = null!;
        public static ConfigEntry<bool> SnapVerticalToGrid { get; private set; } = null!;
        public static ConfigEntry<float> GridOverlayRadius { get; private set; } = null!;
        public static ConfigEntry<float> TerrainGridHeight { get; private set; } = null!;
        public static ConfigEntry<float> WorldFlatGridHeight { get; private set; } = null!;
        public static ConfigEntry<float> LocalFlatGridHeight { get; private set; } = null!;
        public static ConfigEntry<Color> WorldGridColor { get; private set; } = null!;
        public static ConfigEntry<Color> LocalGridColor { get; private set; } = null!;

        public static ConfigEntry<float> BuildCameraMoveSpeed { get; private set; } = null!;
        public static ConfigEntry<float> BuildCameraFastMoveSpeed { get; private set; } = null!;
        public static ConfigEntry<float> BuildCameraSmoothTime { get; private set; } = null!;
        public static ConfigEntry<float> BuildCameraLookSensitivity { get; private set; } = null!;
        public static ConfigEntry<float> BuildCameraLookSmoothTime { get; private set; } = null!;
        public static ConfigEntry<float> BuildCameraTetherRadius { get; private set; } = null!;
        public static ConfigEntry<float> MaxBuildReachDistance { get; private set; } = null!;

        public static ConfigEntry<bool> EnableCameraLight { get; private set; } = null!;
        public static ConfigEntry<Color> CameraLightColor { get; private set; } = null!;
        public static ConfigEntry<float> CameraLightIntensity { get; private set; } = null!;
        public static ConfigEntry<float> CameraLightRange { get; private set; } = null!;
        public static ConfigEntry<float> CameraLightSpotAngle { get; private set; } = null!;

        public static ConfigEntry<bool> EnableCameraDemister { get; private set; } = null!;
        public static ConfigEntry<bool> RequireWisplightForDemister { get; private set; } = null!;
        public static ConfigEntry<float> CameraDemisterRadius { get; private set; } = null!;
        public static ConfigEntry<bool> EnableAutoPickup { get; private set; } = null!;
        public static ConfigEntry<bool> RequireComfortForAutoPickup { get; private set; } = null!;
        public static ConfigEntry<float> AutoPickupRadius { get; private set; } = null!;
        public static ConfigEntry<bool> EnforcePrivateAreaCheck { get; private set; } = null!;

        public static ConfigEntry<bool> ShowInfoPanelBackground { get; private set; } = null!;
        public static ConfigEntry<float> InfoUpdateInterval { get; private set; } = null!;
        public static ConfigEntry<bool> HideNativeCrosshairHealthBar { get; private set; } = null!;

        public static PanelLayoutSettings GlobalPanelLayout { get; private set; } = null!;
        public static PanelLayoutSettings PrecisionPanelLayout { get; private set; } = null!;

        public static PanelLayoutSettings HoverPanelLayout { get; private set; } = null!;
        public static ConfigEntry<bool> ShowHoverName { get; private set; } = null!;
        public static ConfigEntry<bool> ShowHoverPrefab { get; private set; } = null!;
        public static ConfigEntry<bool> ShowHoverHealth { get; private set; } = null!;
        public static ConfigEntry<bool> ShowHoverSupport { get; private set; } = null!;
        public static ConfigEntry<bool> ShowHoverEuler { get; private set; } = null!;
        public static ConfigEntry<bool> ShowHoverQuaternion { get; private set; } = null!;

        public static PanelLayoutSettings PlacementPanelLayout { get; private set; } = null!;
        public static ConfigEntry<bool> ShowGhostName { get; private set; } = null!;
        public static ConfigEntry<bool> ShowGhostPrefab { get; private set; } = null!;
        public static ConfigEntry<bool> ShowGhostEuler { get; private set; } = null!;
        public static ConfigEntry<bool> ShowGhostQuaternion { get; private set; } = null!;
        public static ConfigEntry<bool> ShowGhostDistance { get; private set; } = null!;
        public static ConfigEntry<bool> ShowGhostWorldPosition { get; private set; } = null!;

        public static bool IsInfoLogging => LoggingLevel.Value >= ModLogLevel.Info;
        public static bool IsDebugLogging => LoggingLevel.Value >= ModLogLevel.Debug;

        public static void Initialize(ConfigFile config)
        {
            ConfigSync = new ConfigSync(Plugin.ModGUID)
            {
                DisplayName = Plugin.ModName,
                CurrentVersion = Plugin.ModVersion,
                MinimumRequiredVersion = Plugin.MinimumCompatibleVersion,
                ModRequired = false,
                ModRequirementMode = ModRequirementMode.Conditional
            };

            LockConfiguration = BindLocking(config, "General", "LockConfiguration", true,
                "Locks server-controlled settings so only server administrators can change them.");

            EnableMod = BindSynced(config, "General", "EnableMod", true,
                "Enables or disables MasterBuilderHeeler.");

            EnablePrecision = BindSynced(config, "General", "EnablePrecision", true,
                "Enables precision placement, 3-axis rotation, nudging, ghost freeze, in-place piece edit, piece sampling, the gizmo, and the Precision panel.");

            EnableBuildCamera = BindSynced(config, "General", "EnableBuildCamera", true,
                "Enables the Build Camera, its spotlight, mist clearing, and item auto-pickup.");

            EnableGrid = BindSynced(config, "General", "EnableGrid", true,
                "Enables grid snapping and the grid overlay.");

            EnableInfoPanels = BindSynced(config, "General", "EnableInfoPanels", true,
                "Displays all HUD control and piece info panels (Global, Precision, Hover, and Placement). Disable to remove the panels and stop their updates.");

            EnableExtendedAvatarReach = BindSynced(config, "General", "EnableExtendedAvatarReach", false,
                "Extends player placement, repair, dismantle, and in-place edit reach to MaxBuildReachDistance.");

            ValidateStationRangeAtPieceLocation = BindSynced(config, "General", "ValidateStationRangeAtPieceLocation", false,
                "Checks crafting station build range at the target piece position instead of the player position during standard placement. The Build Camera always uses the target piece position.");

            RequireHammerForHoverInspection = BindSynced(config, "General", "RequireHammerForHoverInspection", false,
                "Requires an equipped build tool to display the hovered piece info panel. Disable to show hovered piece info at all times.");

            GizmoRendererStyle = BindClient(config, "General", "GizmoRendererStyle", GizmoStyle.ProceduralWireframe,
                "Visual style for the rotation gizmo (set to Disabled to hide the gizmo).");

            LoggingLevel = BindClient(config, "General", "LoggingLevel", ModLogLevel.Info,
                "BepInEx log detail level (None = silent, Info = startup and edits, Debug = detailed actions).");

            EnablePrecisionModeByDefault = BindConditionalClient(config, "Precision Settings", "EnablePrecisionModeByDefault", true,
                "Enables Precision Mode automatically when equipping a build tool.");

            EnableRoofModeByDefault = BindConditionalClient(config, "Precision Settings", "EnableRoofModeByDefault", false,
                "Aligns rotation and nudge axes along 45-degree corner roof slopes automatically when equipping a build tool.");

            PrecisionCopyBehavior = BindConditionalClient(config, "Precision Settings", "PrecisionCopyBehavior", PrecisionCopyMode.SideBySide,
                "Sets how native Copy Piece and the sample keys operate while Precision Mode is on (SideBySide = keep both, OverrideNative = native copy also copies full rotation, DisableNative = sample keys only, DisableCustom = native copy only).");

            FineRotationStep = BindConditionalClient(config, "Precision Settings", "FineRotationStep", 1.0f,
                "Rotation step in degrees when Fine Step is active.",
                new AcceptableValueRange<float>(0.1f, 10.0f));

            StandardRotationStep = BindConditionalClient(config, "Precision Settings", "StandardRotationStep", 15.0f,
                "Rotation step in degrees when Standard Step is active.",
                new AcceptableValueRange<float>(1.0f, 90.0f));

            CoarseRotationStep = BindConditionalClient(config, "Precision Settings", "CoarseRotationStep", 45.0f,
                "Rotation step in degrees when Coarse Step is active.",
                new AcceptableValueRange<float>(5.0f, 90.0f));

            FineTranslationStep = BindConditionalClient(config, "Precision Settings", "FineTranslationStep", 0.02f,
                "Nudge step in meters when Fine Step is active.",
                new AcceptableValueRange<float>(0.005f, 0.2f));

            StandardTranslationStep = BindConditionalClient(config, "Precision Settings", "StandardTranslationStep", 0.25f,
                "Nudge step in meters when Standard Step is active.",
                new AcceptableValueRange<float>(0.05f, 2.0f));

            CoarseTranslationStep = BindConditionalClient(config, "Precision Settings", "CoarseTranslationStep", 1.0f,
                "Nudge step in meters when Coarse Step is active.",
                new AcceptableValueRange<float>(0.2f, 5.0f));

            DefaultGridSize = BindConditionalClient(config, "Grid Settings", "DefaultGridSize", 1.0f,
                "Starting grid cell size in meters.",
                new AcceptableValueList<float>(0.5f, 1.0f, 2.0f, 4.0f));

            SnapVerticalToGrid = BindConditionalClient(config, "Grid Settings", "SnapVerticalToGrid", false,
                "Snaps piece height to the grid. Furniture, ground, and water pieces keep their height. Disable to keep the native placement height.");

            GridOverlayRadius = BindClient(config, "Grid Settings", "GridOverlayRadius", 14.0f,
                "Display radius in meters around the active focus point for the grid overlay.",
                new AcceptableValueRange<float>(4.0f, 64.0f));

            TerrainGridHeight = BindClient(config, "Grid Settings", "TerrainGridHeight", 0.075f,
                "Height in meters above the ground for the terrain-following grid overlay.",
                new AcceptableValueRange<float>(0.04f, 1.0f));

            WorldFlatGridHeight = BindClient(config, "Grid Settings", "WorldFlatGridHeight", 0.02f,
                "Height offset in meters above the player for the flat World grid overlay.",
                new AcceptableValueRange<float>(0.02f, 5.0f));

            LocalFlatGridHeight = BindClient(config, "Grid Settings", "LocalFlatGridHeight", 0.0f,
                "Vertical offset in meters from the anchored piece for the flat Local grid overlay.",
                new AcceptableValueRange<float>(-1.0f, 5.0f));

            WorldGridColor = BindClient(config, "Grid Settings", "WorldGridColor", (Color)new Color32(0x00, 0xFF, 0xFF, 0xB2),
                "Color and opacity of the World grid overlay.");

            LocalGridColor = BindClient(config, "Grid Settings", "LocalGridColor", (Color)new Color32(0xFF, 0x00, 0xFF, 0xB2),
                "Color and opacity of the Local grid overlay.");

            BuildCameraMoveSpeed = BindConditionalClient(config, "Build Camera", "BuildCameraMoveSpeed", 10.0f,
                "Standard movement speed in meters per second for the Build Camera.",
                new AcceptableValueRange<float>(1.0f, 100.0f));

            BuildCameraFastMoveSpeed = BindConditionalClient(config, "Build Camera", "BuildCameraFastMoveSpeed", 25.0f,
                "Fast movement speed in meters per second while holding the Run key in the Build Camera.",
                new AcceptableValueRange<float>(2.0f, 200.0f));

            BuildCameraSmoothTime = BindClient(config, "Build Camera", "BuildCameraSmoothTime", 0.12f,
                "Smoothing time in seconds for Build Camera movement acceleration and stopping.",
                new AcceptableValueRange<float>(0.01f, 1.0f));

            BuildCameraLookSensitivity = BindClient(config, "Build Camera", "BuildCameraLookSensitivity", 2.0f,
                "Mouse look sensitivity multiplier for the Build Camera.",
                new AcceptableValueRange<float>(0.1f, 10.0f));

            BuildCameraLookSmoothTime = BindClient(config, "Build Camera", "BuildCameraLookSmoothTime", 0.05f,
                "Smoothing time in seconds for Build Camera mouse look.",
                new AcceptableValueRange<float>(0.01f, 0.5f));

            BuildCameraTetherRadius = BindSynced(config, "Build Camera", "BuildCameraTetherRadius", 50.0f,
                "Maximum distance in meters the Build Camera can move from the player.",
                new AcceptableValueRange<float>(10.0f, 200.0f));

            MaxBuildReachDistance = BindSynced(config, "Build Camera", "MaxBuildReachDistance", 50.0f,
                "Maximum targeting and build reach in meters for the Build Camera, extended player reach, piece sampling, and hovered piece info.",
                new AcceptableValueRange<float>(10.0f, 50.0f));

            EnableCameraLight = BindConditionalClient(config, "Build Camera Light", "EnableCameraLight", true,
                "Enables the spotlight attached to the Build Camera.");

            CameraLightColor = BindConditionalClient(config, "Build Camera Light", "CameraLightColor", Color.white,
                "Color of the Build Camera spotlight.");

            CameraLightIntensity = BindConditionalClient(config, "Build Camera Light", "CameraLightIntensity", 1.5f,
                "Brightness of the Build Camera spotlight.",
                new AcceptableValueRange<float>(0.1f, 10.0f));

            CameraLightRange = BindConditionalClient(config, "Build Camera Light", "CameraLightRange", 30.0f,
                "Maximum range in meters of the Build Camera spotlight.",
                new AcceptableValueRange<float>(5.0f, 100.0f));

            CameraLightSpotAngle = BindConditionalClient(config, "Build Camera Light", "CameraLightSpotAngle", 90.0f,
                "Cone angle in degrees of the Build Camera spotlight.",
                new AcceptableValueRange<float>(30.0f, 160.0f));

            EnableCameraDemister = BindSynced(config, "Build Camera Demister & AutoPickup", "EnableCameraDemister", true,
                "Clears Mistlands mist around the Build Camera.");

            RequireWisplightForDemister = BindSynced(config, "Build Camera Demister & AutoPickup", "RequireWisplightForDemister", true,
                "Requires an active Wisplight effect on the player before the Build Camera clears mist.");

            CameraDemisterRadius = BindSynced(config, "Build Camera Demister & AutoPickup", "CameraDemisterRadius", 15.0f,
                "Mist-clearing radius in meters around the Build Camera (native Wisplight radius is 15).",
                new AcceptableValueRange<float>(5.0f, 60.0f));

            EnableAutoPickup = BindSynced(config, "Build Camera Demister & AutoPickup", "EnableAutoPickup", true,
                "Collects nearby dropped items around the Build Camera into the player inventory.");

            RequireComfortForAutoPickup = BindSynced(config, "Build Camera Demister & AutoPickup", "RequireComfortForAutoPickup", true,
                "Requires an active Resting (Comfort) effect on the player before Build Camera auto-pickup collects items.");

            AutoPickupRadius = BindSynced(config, "Build Camera Demister & AutoPickup", "AutoPickupRadius", 5.0f,
                "Item pickup radius in meters around the Build Camera.",
                new AcceptableValueRange<float>(1.0f, 20.0f));

            EnforcePrivateAreaCheck = BindSynced(config, "Build Camera Demister & AutoPickup", "EnforcePrivateAreaCheck", true,
                "Requires ward access at the item position before Build Camera auto-pickup collects an item.");

            ShowInfoPanelBackground = BindClient(config, "HUD & Info Panels", "ShowInfoPanelBackground", true,
                "Draws a rounded background behind HUD panels. Disable to show text without panel backgrounds.");

            InfoUpdateInterval = BindClient(config, "HUD & Info Panels", "InfoUpdateInterval", 0.25f,
                "Seconds between updates on the Hover and Placement piece info panels.",
                new AcceptableValueRange<float>(0.05f, 1.0f));

            HideNativeCrosshairHealthBar = BindClient(config, "HUD & Info Panels", "HideNativeCrosshairHealthBar", false,
                "Hides the native crosshair piece health bar while HUD panels are enabled.");

            GlobalPanelLayout = BindPanelLayout(
                config,
                "Global Panel",
                "Global controls panel",
                "bottom-right corner",
                defaultOffsetX: -380f,
                defaultOffsetY: 100f,
                defaultFontSize: 13);

            PrecisionPanelLayout = BindPanelLayout(
                config,
                "Precision Panel",
                "Precision controls panel",
                "bottom-right corner",
                defaultOffsetX: -40f,
                defaultOffsetY: 100f,
                defaultFontSize: 13);

            HoverPanelLayout = BindPanelLayout(
                config,
                "Hover Panel",
                "hovered piece info panel",
                "crosshair",
                defaultOffsetX: 225f,
                defaultOffsetY: -30f,
                defaultFontSize: 13);

            ShowHoverName = BindClient(config, "Hover Panel", "ShowHoverName", true,
                "Displays the name of the hovered piece.");

            ShowHoverPrefab = BindClient(config, "Hover Panel", "ShowHoverPrefab", false,
                "Displays the internal prefab ID of the hovered piece.");

            ShowHoverHealth = BindClient(config, "Hover Panel", "ShowHoverHealth", true,
                "Displays current health, max health, and health percentage of the hovered piece.");

            ShowHoverSupport = BindClient(config, "Hover Panel", "ShowHoverSupport", true,
                "Displays structural support and stability percentage of the hovered piece.");

            ShowHoverEuler = BindClient(config, "Hover Panel", "ShowHoverEuler", true,
                "Displays rotation angles in degrees of the hovered piece.");

            ShowHoverQuaternion = BindClient(config, "Hover Panel", "ShowHoverQuaternion", false,
                "Displays Quaternion rotation values (x, y, z, w) of the hovered piece.");

            PlacementPanelLayout = BindPanelLayout(
                config,
                "Placement Panel",
                "active placement piece info panel",
                "crosshair",
                defaultOffsetX: -110f,
                defaultOffsetY: -150f,
                defaultFontSize: 13);

            ShowGhostName = BindClient(config, "Placement Panel", "ShowGhostName", true,
                "Displays the name of the active placement ghost or edited piece.");

            ShowGhostPrefab = BindClient(config, "Placement Panel", "ShowGhostPrefab", false,
                "Displays the internal prefab ID of the active placement ghost or edited piece.");

            ShowGhostEuler = BindClient(config, "Placement Panel", "ShowGhostEuler", true,
                "Displays rotation angles in degrees of the active placement ghost or edited piece.");

            ShowGhostQuaternion = BindClient(config, "Placement Panel", "ShowGhostQuaternion", false,
                "Displays Quaternion rotation values (x, y, z, w) of the active placement ghost or edited piece.");

            ShowGhostDistance = BindClient(config, "Placement Panel", "ShowGhostDistance", true,
                "Displays distance in meters from the player to the target placement position.");

            ShowGhostWorldPosition = BindClient(config, "Placement Panel", "ShowGhostWorldPosition", false,
                "Displays world coordinates (X, Y, Z) of the active placement ghost or edited piece.");

            KeyboardMouseConfig.Initialize(config);
            ThemeColorConfig.Initialize(config);
        }

        internal static ConfigEntry<T> BindClient<T>(
            ConfigFile config,
            string section,
            string key,
            T defaultValue,
            string description,
            AcceptableValueBase? acceptableValues = null,
            ConfigurationManagerAttributes? attributes = null)
        {
            ConfigEntry<T> entry = BindOrdered(config, section, key, defaultValue, description, acceptableValues, attributes);
            ConfigSync.AddConfigEntry(entry, ConfigSyncMode.AlwaysClientControlled);
            return entry;
        }

        private static PanelLayoutSettings BindPanelLayout(
            ConfigFile config,
            string section,
            string panelDescription,
            string anchorDescription,
            float defaultOffsetX,
            float defaultOffsetY,
            int defaultFontSize)
        {
            ConfigEntry<bool> show = BindClient(config, section, "ShowPanel", true,
                $"Displays the {panelDescription}.");

            ConfigEntry<float> offsetX = BindClient(config, section, "OffsetX", defaultOffsetX,
                $"Horizontal offset in pixels from the {anchorDescription} for the {panelDescription}.",
                new AcceptableValueRange<float>(-5000f, 5000f));

            ConfigEntry<float> offsetY = BindClient(config, section, "OffsetY", defaultOffsetY,
                $"Vertical offset in pixels from the {anchorDescription} for the {panelDescription}.",
                new AcceptableValueRange<float>(-3000f, 3000f));

            ConfigEntry<int> fontSize = BindClient(config, section, "FontSize", defaultFontSize,
                $"Font size in pixels for the {panelDescription}.",
                new AcceptableValueRange<int>(8, 32));

            ConfigEntry<Color> backgroundColor = BindClient(config, section, "BackgroundColor", DefaultPanelBackgroundColor,
                $"Background color and opacity for the {panelDescription}.");

            return new PanelLayoutSettings(show, offsetX, offsetY, fontSize, backgroundColor);
        }

        private static ConfigEntry<bool> BindLocking(
            ConfigFile config,
            string section,
            string key,
            bool defaultValue,
            string description)
        {
            ConfigEntry<bool> entry = BindOrdered(config, section, key, defaultValue, description, null, null);
            ConfigSync.AddLockingConfigEntry(entry);
            return entry;
        }

        private static ConfigEntry<T> BindSynced<T>(
            ConfigFile config,
            string section,
            string key,
            T defaultValue,
            string description,
            AcceptableValueBase? acceptableValues = null)
        {
            ConfigEntry<T> entry = BindOrdered(config, section, key, defaultValue, description, acceptableValues, attributes: null);
            ConfigSync.AddConfigEntry(entry, ConfigSyncMode.Conditional, serverControlledByDefault: true);
            return entry;
        }

        private static ConfigEntry<T> BindConditionalClient<T>(
            ConfigFile config,
            string section,
            string key,
            T defaultValue,
            string description,
            AcceptableValueBase? acceptableValues = null)
        {
            ConfigEntry<T> entry = BindOrdered(config, section, key, defaultValue, description, acceptableValues, attributes: null);
            ConfigSync.AddConfigEntry(entry, ConfigSyncMode.Conditional, serverControlledByDefault: false);
            return entry;
        }

        // some wont like this but i dont really prefer jotunn's implementation for sync, so i stick with ccs
        // maybe jotunn can pull in some of ccs features at some point, because its nice for server side toggles
        // that said, jotunn ordering is pretty slick, so im still using that shit
        private static ConfigEntry<T> BindOrdered<T>(
            ConfigFile config,
            string section,
            string key,
            T defaultValue,
            string description,
            AcceptableValueBase? acceptableValues,
            ConfigurationManagerAttributes? attributes) =>
            config.BindConfigInOrder(
                section,
                key,
                defaultValue,
                description,
                synced: false,
                sectionOrder: true,
                settingOrder: true,
                acceptableValues: acceptableValues,
                configAttributes: attributes);
    }
}
