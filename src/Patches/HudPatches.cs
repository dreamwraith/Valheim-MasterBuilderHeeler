using HarmonyLib;
using MasterBuilderHeeler.Core.UI;

namespace MasterBuilderHeeler.Patches
{
    [HarmonyPatch(typeof(Hud))]
    public static class HudPatches
    {
        [HarmonyPatch(nameof(Hud.Awake))]
        [HarmonyPostfix]
        private static void AwakePostfix(Hud __instance) =>
            HudManager.AttachToHud(__instance);

        [HarmonyPatch(nameof(Hud.UpdateCrosshair))]
        [HarmonyPostfix]
        private static void UpdateCrosshairPostfix(Hud __instance) =>
            HudManager.UpdateNativePieceHealthBar(__instance);
    }
}
