using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;
using MasterBuilderHeeler.Configuration;
using MasterBuilderHeeler.Core.Camera;
using MasterBuilderHeeler.Core.Input;
using MasterBuilderHeeler.Core.Placement;
using MasterBuilderHeeler.Core.State;
using MasterBuilderHeeler.Core.UI;
using MasterBuilderHeeler.Core.Visuals.Gizmo;
using MasterBuilderHeeler.Core.Visuals.Grid;

namespace MasterBuilderHeeler
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    [BepInDependency(Jotunn.Main.ModGuid, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("_shudnal.ConditionalConfigSync", BepInDependency.DependencyFlags.HardDependency)]
    [BepInIncompatibility("Azumatt_and_ValheimPlusDevs.PerfectPlacement")]
    [BepInIncompatibility("Azumatt.BuildCameraCHE")]
    public class Plugin : BaseUnityPlugin
    {
        public const string ModGUID = "dreamwraith.MasterBuilderHeeler";
        public const string ModName = "MasterBuilderHeeler";
        public const string ModVersion = VersionInfo.Version;
        public const string MinimumCompatibleVersion = "0.0.1";

        private Harmony? _harmony;

        internal static ManualLogSource Log { get; private set; } = null!;

        private void Awake()
        {
            Log = Logger;

            ModConfig.Initialize(Config);
            new ConfigFileWatcher(Config);

            if (GUIManager.IsHeadless())
            {
                return;
            }

            ModInputManager.Initialize();
            HudTheme.Initialize();
            HudKeybindFormatter.Initialize();
            BuildInputRouter.Initialize();
            BuildStateManager.Initialize();
            PlacementGhostController.Initialize();
            BuildCameraRig.Initialize();
            GizmoRendering.Initialize();
            HudManager.Initialize();

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), ModGUID);
            Log.LogInfo($"{ModName} v{ModVersion} initialized.");
        }

        private void OnDestroy()
        {
            if (GUIManager.IsHeadless())
            {
                return;
            }

            if (_harmony != null)
            {
                _harmony.UnpatchSelf();
            }

            PlacementGhostController.ResetBuildSession();
            PlacementGhostController.Cleanup();
            HudManager.Cleanup();
            HudKeybindFormatter.Cleanup();
            HudTheme.Cleanup();
            GizmoRendering.Cleanup();
            GridOverlayRenderer.Cleanup();
            BuildCameraRig.Cleanup();
            BuildStateManager.Cleanup();
        }

        internal static void LogInfo(string message)
        {
            if (ModConfig.IsInfoLogging)
            {
                Log.LogInfo(message);
            }
        }

        internal static void LogDebug(string message)
        {
            if (ModConfig.IsDebugLogging)
            {
                Log.LogInfo($"[DEBUG] {message}");
            }
        }
    }
}
