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

    // bad name, base game repair calls the same thing, so we can patch just the one spot.
    [HarmonyPatch(typeof(Player), nameof(Player.CheckCanRemovePiece))]
    public static class PlayerStationCheckPatches
    {
        [HarmonyPrefix]
        private static void Prefix(Piece piece) => PlacementValidator.RepairOrRemoveTarget = piece;

        [HarmonyFinalizer]
        private static void Finalizer() => PlacementValidator.RepairOrRemoveTarget = null;
    }
}
