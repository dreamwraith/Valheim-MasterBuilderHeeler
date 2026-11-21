using HarmonyLib;
using MasterBuilderHeeler.Core.Camera;

namespace MasterBuilderHeeler.Patches
{
    [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.UpdateCamera))]
    public static class GameCameraPatches
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.VeryHigh)]
        private static bool UpdateCameraPrefix(float dt, GameCamera __instance, out float __state) =>
            BuildCameraController.InterceptCameraUpdate(dt, __instance, out __state);

        [HarmonyPostfix]
        private static void UpdateCameraPostfix(GameCamera __instance, float __state) =>
            BuildCameraController.RestoreZoomSensitivity(__instance, __state);

        // Last so the build camera pose goes on after azumatts firstpersonmode postfix moves the camera
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void UpdateCameraFlightPostfix(GameCamera __instance) =>
            BuildCameraController.ApplyFlightPose(__instance);
    }
}
