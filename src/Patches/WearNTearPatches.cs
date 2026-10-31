using HarmonyLib;
using MasterBuilderHeeler.Core.Placement;

namespace MasterBuilderHeeler.Patches
{
    [HarmonyPatch(typeof(WearNTearUpdater), nameof(WearNTearUpdater.Update))]
    public static class WearNTearPatches
    {
        [HarmonyPrefix]
        private static void UpdatePrefix(WearNTearUpdater __instance) =>
            PieceEditController.DeferWearNTearIfEditing(__instance);
    }
}
