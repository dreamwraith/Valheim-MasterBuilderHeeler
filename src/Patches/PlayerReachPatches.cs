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

        // First prefix and Last finalizer wrap other mods so their prefixes see expanded reach and base reach restores last
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static void Prefix(Player __instance, out float __state) =>
            __state = BuildReachService.ApplyReachExpansion(__instance);

        // Last prefix reapplies expansion if another mods prefix lowered m_maxPlaceDistance
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void EnforcePrefix(Player __instance) =>
            BuildReachService.ApplyReachExpansion(__instance);

        [HarmonyFinalizer]
        [HarmonyPriority(Priority.Last)]
        private static void Finalizer(Player __instance, float __state) =>
            BuildReachService.RestoreReach(__instance, __state);
    }
}
