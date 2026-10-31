using HarmonyLib;
using MasterBuilderHeeler.Core.Rotation;
using MasterBuilderHeeler.Core.Sampling;

namespace MasterBuilderHeeler.Patches
{
    [HarmonyPatch(typeof(Player), nameof(Player.CopyPiece))]
    public static class PieceCopyPatches
    {
        [HarmonyPrefix]
        private static bool Prefix(Player __instance) =>
            PieceEyedropper.ResolveNativeCopy(__instance);

        [HarmonyPostfix]
        private static void Postfix(Player __instance, bool __result)
        {
            if (__result)
            {
                PieceRotationEngine.SyncNativeYaw(__instance);
            }
        }
    }
}
