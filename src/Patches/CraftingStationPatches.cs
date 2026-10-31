using HarmonyLib;
using MasterBuilderHeeler.Core.Validation;
using UnityEngine;

namespace MasterBuilderHeeler.Patches
{
    [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.HaveBuildStationInRange))]
    public static class CraftingStationPatches
    {
        [HarmonyPrefix]
        private static void HaveBuildStationInRangePrefix(ref Vector3 point) =>
            PlacementValidator.OverrideStationRangePoint(ref point);
    }
}
