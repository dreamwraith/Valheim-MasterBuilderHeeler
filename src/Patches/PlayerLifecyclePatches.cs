using HarmonyLib;
using MasterBuilderHeeler.Core.Camera;
using MasterBuilderHeeler.Core.Input;
using MasterBuilderHeeler.Core.Placement;

namespace MasterBuilderHeeler.Patches
{
    [HarmonyPatch]
    public static class PlayerLifecyclePatches
    {
        [HarmonyPatch(typeof(Player), nameof(Player.SetLocalPlayer))]
        [HarmonyPostfix]
        private static void SetLocalPlayerPostfix(Player __instance) =>
            PlacementGhostController.OnLocalPlayerReady(__instance);

        [HarmonyPatch(typeof(Player), nameof(Player.OnDestroy))]
        [HarmonyPrefix]
        private static void OnDestroyPrefix(Player __instance) =>
            PlacementGhostController.OnPlayerDestroyed(__instance);

        [HarmonyPatch(typeof(Player), nameof(Player.Update))]
        [HarmonyPostfix]
        private static void UpdatePostfix(Player __instance)
        {
            PlacementGhostController.UpdateSessionLifecycle(__instance);
            BuildInputRouter.ProcessInput(__instance);
        }

        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.TakeInput))]
        [HarmonyPostfix]
        private static void TakeInputPostfix(ref bool __result) =>
            BuildCameraController.OverrideTakeInput(ref __result);
    }
}
