using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace MasterBuilderHeeler.Configuration
{
    public sealed class BuildConfig(
        ConfigSection precision,
        ConfigSection gizmo,
        ConfigSection grid,
        ConfigSection camera,
        ConfigSection cameraLight,
        ConfigSection cameraExtras)
    {
        public ConfigEntry<bool> EnablePrecisionModeByDefault { get; } = precision.Conditional("EnablePrecisionModeByDefault", true,
            "Enables Precision Mode at login and after respawn.");

        public ConfigEntry<bool> EnableRoofModeByDefault { get; } = precision.Conditional("EnableRoofModeByDefault", false,
            "Aligns rotation and nudge axes along 45-degree corner roof slopes at login, after respawn, and on Reset Build Settings.");

        public ConfigEntry<PrecisionCopyMode> PrecisionCopyBehavior { get; } = precision.Conditional("PrecisionCopyBehavior", PrecisionCopyMode.SideBySide,
            "Sets how base game Copy Piece and the sample keys operate while Precision Mode is on (SideBySide = keep both, OverrideNative = base game copy also copies full rotation, DisableNative = sample keys only, DisableCustom = base game copy only).");

        public ConfigEntry<float> RotationStepUltraFine { get; } = precision.Conditional("RotationStepUltraFine", 1.0f,
            "Rotation step in degrees when the UltraFine step preset is active.",
            new AcceptableValueRange<float>(0.1f, 5.0f));

        public ConfigEntry<float> RotationStepExtraFine { get; } = precision.Conditional("RotationStepExtraFine", 2.5f,
            "Rotation step in degrees when the ExtraFine step preset is active.",
            new AcceptableValueRange<float>(0.5f, 10.0f));

        public ConfigEntry<float> RotationStepFine { get; } = precision.Conditional("RotationStepFine", 7.5f,
            "Rotation step in degrees when the Fine step preset is active.",
            new AcceptableValueRange<float>(1.0f, 15.0f));

        public ConfigEntry<float> RotationStepStandard { get; } = precision.Conditional("RotationStepStandard", 22.5f,
            "Rotation step in degrees when the Standard step preset is active.",
            new AcceptableValueRange<float>(5.0f, 45.0f));

        public ConfigEntry<float> RotationStepCoarse { get; } = precision.Conditional("RotationStepCoarse", 45.0f,
            "Rotation step in degrees when the Coarse step preset is active.",
            new AcceptableValueRange<float>(15.0f, 90.0f));

        public ConfigEntry<float> TranslationStepUltraFine { get; } = precision.Conditional("TranslationStepUltraFine", 0.02f,
            "Nudge step in meters when the UltraFine step preset is active.",
            new AcceptableValueRange<float>(0.005f, 0.1f));

        public ConfigEntry<float> TranslationStepExtraFine { get; } = precision.Conditional("TranslationStepExtraFine", 0.05f,
            "Nudge step in meters when the ExtraFine step preset is active.",
            new AcceptableValueRange<float>(0.01f, 0.2f));

        public ConfigEntry<float> TranslationStepFine { get; } = precision.Conditional("TranslationStepFine", 0.125f,
            "Nudge step in meters when the Fine step preset is active.",
            new AcceptableValueRange<float>(0.02f, 0.5f));

        public ConfigEntry<float> TranslationStepStandard { get; } = precision.Conditional("TranslationStepStandard", 0.25f,
            "Nudge step in meters when the Standard step preset is active.",
            new AcceptableValueRange<float>(0.05f, 1.0f));

        public ConfigEntry<float> TranslationStepCoarse { get; } = precision.Conditional("TranslationStepCoarse", 1.0f,
            "Nudge step in meters when the Coarse step preset is active.",
            new AcceptableValueRange<float>(0.25f, 5.0f));

        public ConfigEntry<GizmoStyle> GizmoRendererStyle { get; } = gizmo.Client("GizmoRendererStyle", GizmoStyle.Solid,
            "Visual style for the rotation gizmo (set to Disabled to hide the gizmo).");

        public ConfigEntry<float> GizmoScale { get; } = gizmo.Client("GizmoScale", 1.0f,
            "Size multiplier for the rotation gizmo.",
            new AcceptableValueRange<float>(0.25f, 4.0f));

        public ConfigEntry<bool> EnableGizmoLighting { get; } = gizmo.Client("EnableGizmoLighting", true,
            "Lights the rotation gizmo with world light, shadows, and fog. Disable to use fixed shading from overhead that the world does not change. Applies only to solid gizmo styles.");

        public ConfigEntry<Color> GizmoPitchRingColor { get; } = gizmo.Client<Color>("GizmoPitchRingColor", new Color32(245, 26, 26, 255),
            "Color and opacity of the Pitch rotation ring on the 3D gizmo. The Precision panel shows the Pitch axis symbol in the same color.");

        public ConfigEntry<Color> GizmoYawRingColor { get; } = gizmo.Client<Color>("GizmoYawRingColor", new Color32(0, 180, 0, 255),
            "Color and opacity of the Yaw rotation ring on the 3D gizmo. The Precision panel shows the Yaw axis symbol in the same color.");

        public ConfigEntry<Color> GizmoRollRingColor { get; } = gizmo.Client<Color>("GizmoRollRingColor", new Color32(26, 26, 204, 255),
            "Color and opacity of the Roll rotation ring on the 3D gizmo. The Precision panel shows the Roll axis symbol in the same color.");

        public ConfigEntry<Color> GizmoRightArrowColor { get; } = gizmo.Client<Color>("GizmoRightArrowColor", new Color32(207, 0, 191, 217),
            "Color and opacity of the Right direction arrow on the 3D gizmo. The Precision panel shows the left and right nudge symbols in the same color.");

        public ConfigEntry<Color> GizmoUpArrowColor { get; } = gizmo.Client<Color>("GizmoUpArrowColor", new Color32(222, 102, 0, 217),
            "Color and opacity of the Up direction arrow on the 3D gizmo. The Precision panel shows the up and down nudge symbols in the same color.");

        public ConfigEntry<Color> GizmoForwardArrowColor { get; } = gizmo.Client<Color>("GizmoForwardArrowColor", new Color32(0, 191, 191, 217),
            "Color and opacity of the Forward direction arrow on the 3D gizmo. The Precision panel shows the forward and backward nudge symbols in the same color.");

        public IReadOnlyList<ConfigEntry<Color>> GizmoColors { get; } = gizmo.All<Color>();

        public ConfigEntry<float> DefaultGridSize { get; } = grid.Conditional("DefaultGridSize", 1.0f,
            "Starting grid cell size in meters.",
            new AcceptableValueList<float>(0.5f, 1.0f, 2.0f, 4.0f));

        public ConfigEntry<bool> SnapVerticalToGrid { get; } = grid.Conditional("SnapVerticalToGrid", false,
            "Snaps piece height to the grid. Furniture, ground, and water pieces keep their height. Disable to keep the base game placement height.");

        public ConfigEntry<float> GridOverlayRadius { get; } = grid.Client("GridOverlayRadius", 14.0f,
            "Display radius in meters around the active focus point for the grid overlay.",
            new AcceptableValueRange<float>(4.0f, 64.0f));

        public ConfigEntry<float> TerrainGridHeight { get; } = grid.Client("TerrainGridHeight", 0.25f,
            "Height in meters above the ground for the terrain-following grid overlay.",
            new AcceptableValueRange<float>(0.04f, 1.0f));

        public ConfigEntry<float> WorldFlatGridHeight { get; } = grid.Client("WorldFlatGridHeight", 0.15f,
            "Vertical offset in meters from the player for the flat World grid overlay.",
            new AcceptableValueRange<float>(-1.0f, 5.0f));

        public ConfigEntry<float> LocalFlatGridHeight { get; } = grid.Client("LocalFlatGridHeight", 0.0f,
            "Vertical offset in meters from the anchored piece for the flat Local grid overlay.",
            new AcceptableValueRange<float>(-1.0f, 5.0f));

        public ConfigEntry<Color> WorldGridColor { get; } = grid.Client<Color>("WorldGridColor", new Color32(0, 255, 255, 178),
            "Color and opacity of the World grid overlay.");

        public ConfigEntry<Color> LocalGridColor { get; } = grid.Client<Color>("LocalGridColor", new Color32(255, 0, 255, 178),
            "Color and opacity of the Local grid overlay.");

        public ConfigEntry<GridStyle> GridOverlayStyle { get; } = grid.Client("GridOverlayStyle", GridStyle.Ribbon,
            "Render style of the grid overlay. Wireframe draws thin lines with a constant width on the screen and has a lower performance cost. Ribbon draws flat lines with a width in meters.");

        public ConfigEntry<float> RibbonGridMinorWidth { get; } = grid.Client("RibbonGridMinorWidth", 0.025f,
            "Width in meters of the standard grid lines. Applies only to the Ribbon grid style.",
            new AcceptableValueRange<float>(0.01f, 0.25f));

        public ConfigEntry<float> RibbonGridMajorWidth { get; } = grid.Client("RibbonGridMajorWidth", 0.05f,
            "Width in meters of every fourth grid line. Applies only to the Ribbon grid style.",
            new AcceptableValueRange<float>(0.01f, 0.25f));

        public ConfigEntry<float> RibbonGridOriginWidth { get; } = grid.Client("RibbonGridOriginWidth", 0.075f,
            "Width in meters of the two lines that cross at the grid origin. The World grid origin is world X 0, Z 0. The Local grid origin is the anchored piece. Applies only to the Ribbon grid style.",
            new AcceptableValueRange<float>(0.01f, 0.25f));

        public ConfigEntry<float> BuildCameraMoveSpeed { get; } = camera.Conditional("BuildCameraMoveSpeed", 10.0f,
            "Standard movement speed in meters per second for the Build Camera.",
            new AcceptableValueRange<float>(1.0f, 100.0f));

        public ConfigEntry<float> BuildCameraFastMoveSpeed { get; } = camera.Conditional("BuildCameraFastMoveSpeed", 25.0f,
            "Fast movement speed in meters per second while holding the Run key in the Build Camera.",
            new AcceptableValueRange<float>(2.0f, 200.0f));

        public ConfigEntry<float> BuildCameraSmoothTime { get; } = camera.Client("BuildCameraSmoothTime", 0.12f,
            "Smoothing time in seconds for Build Camera movement acceleration and stopping.",
            new AcceptableValueRange<float>(0.01f, 1.0f));

        public ConfigEntry<float> BuildCameraLookSensitivity { get; } = camera.Client("BuildCameraLookSensitivity", 2.0f,
            "Mouse look sensitivity multiplier for the Build Camera.",
            new AcceptableValueRange<float>(0.1f, 10.0f));

        public ConfigEntry<float> BuildCameraLookSmoothTime { get; } = camera.Client("BuildCameraLookSmoothTime", 0.05f,
            "Smoothing time in seconds for Build Camera mouse look.",
            new AcceptableValueRange<float>(0.01f, 0.5f));

        public ConfigEntry<float> BuildCameraTetherRadius { get; } = camera.Synced("BuildCameraTetherRadius", 50.0f,
            "Maximum distance in meters the Build Camera can move from the player.",
            new AcceptableValueRange<float>(10.0f, 200.0f));

        public ConfigEntry<bool> BuildCameraIgnoreCollision { get; } = camera.Synced("BuildCameraIgnoreCollision", false,
            "Lets the Build Camera pass through build pieces and trees. Terrain and rocks still block the camera.");

        public ConfigEntry<float> MaxBuildReachDistance { get; } = camera.Synced("MaxBuildReachDistance", 50.0f,
            "Maximum reach in meters for extended player reach, in-place edit targeting, piece sampling, and hovered piece info. Build Camera reach is the base game 50m raycast.",
            new AcceptableValueRange<float>(10.0f, 50.0f));

        public ConfigEntry<bool> EnableCameraLight { get; } = cameraLight.Conditional("EnableCameraLight", true,
            "Enables the spotlight attached to the Build Camera.");

        public ConfigEntry<Color> CameraLightColor { get; } = cameraLight.Conditional("CameraLightColor", Color.white,
            "Color of the Build Camera spotlight.");

        public ConfigEntry<float> CameraLightIntensity { get; } = cameraLight.Conditional("CameraLightIntensity", 1.5f,
            "Brightness of the Build Camera spotlight.",
            new AcceptableValueRange<float>(0.1f, 10.0f));

        public ConfigEntry<float> CameraLightRange { get; } = cameraLight.Conditional("CameraLightRange", 30.0f,
            "Maximum range in meters of the Build Camera spotlight.",
            new AcceptableValueRange<float>(5.0f, 100.0f));

        public ConfigEntry<float> CameraLightSpotAngle { get; } = cameraLight.Conditional("CameraLightSpotAngle", 90.0f,
            "Cone angle in degrees of the Build Camera spotlight.",
            new AcceptableValueRange<float>(30.0f, 160.0f));

        public ConfigEntry<bool> EnableCameraDemister { get; } = cameraExtras.Synced("EnableCameraDemister", true,
            "Clears Mistlands mist around the Build Camera.");

        public ConfigEntry<bool> RequireWisplightForDemister { get; } = cameraExtras.Synced("RequireWisplightForDemister", true,
            "Requires an active Wisplight effect on the player before the Build Camera clears mist.");

        public ConfigEntry<float> CameraDemisterRadius { get; } = cameraExtras.Synced("CameraDemisterRadius", 15.0f,
            "Mist-clearing radius in meters around the Build Camera (base game Wisplight radius is 15).",
            new AcceptableValueRange<float>(5.0f, 60.0f));

        public ConfigEntry<bool> EnableAutoPickup { get; } = cameraExtras.Synced("EnableAutoPickup", true,
            "Collects nearby dropped items around the Build Camera into the player inventory.");

        public ConfigEntry<bool> RequireComfortForAutoPickup { get; } = cameraExtras.Synced("RequireComfortForAutoPickup", true,
            "Requires an active Resting (Comfort) effect on the player before Build Camera auto-pickup collects items.");

        public ConfigEntry<float> AutoPickupRadius { get; } = cameraExtras.Synced("AutoPickupRadius", 5.0f,
            "Item pickup radius in meters around the Build Camera.",
            new AcceptableValueRange<float>(1.0f, 20.0f));

        public ConfigEntry<bool> EnforcePrivateAreaCheck { get; } = cameraExtras.Synced("EnforcePrivateAreaCheck", true,
            "Requires ward access at the item position before Build Camera auto-pickup collects an item.");
    }
}
