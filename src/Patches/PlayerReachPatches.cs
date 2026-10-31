using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MasterBuilderHeeler.Core.Placement;

namespace MasterBuilderHeeler.Patches
{
    [HarmonyPatch]
    public static class PlayerReachPatches
    {
        [HarmonyTargetMethods]
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.DeclaredMethod(typeof(Player), nameof(Player.PieceRayTest));
            yield return AccessTools.DeclaredMethod(typeof(Player), nameof(Player.UpdateWearNTearHover));
            yield return AccessTools.DeclaredMethod(typeof(Player), nameof(Player.CopyPiece));
            yield return AccessTools.DeclaredMethod(typeof(Player), nameof(Player.RemovePiece));
        }

        // has to run first so __state always gets set, even if another prefix skips the original. postfix restores from it
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static void Prefix(Player __instance, out float __state) =>
            BuildReachService.ApplyReachExpansion(__instance, out __state);

        [HarmonyPostfix]
        private static void Postfix(Player __instance, float __state) =>
            BuildReachService.RestoreReach(__instance, __state);
    }
}
