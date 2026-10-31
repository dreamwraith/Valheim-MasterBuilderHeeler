using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MasterBuilderHeeler.Core.Placement;
using MasterBuilderHeeler.Core.Rotation;
using UnityEngine;

namespace MasterBuilderHeeler.Patches
{
    [HarmonyPatch(typeof(Player))]
    public static class PlacementGhostPatches
    {
        private static readonly FieldInfo PlayerPlaceRotationField = AccessTools.Field(typeof(Player), nameof(Player.m_placeRotation));
        private static readonly MethodInfo QuaternionEulerMethod = AccessTools.Method(typeof(Quaternion), nameof(Quaternion.Euler), new[] { typeof(float), typeof(float), typeof(float) });
        private static readonly MethodInfo ResolveTargetRotationMethod = AccessTools.Method(typeof(PieceRotationEngine), nameof(PieceRotationEngine.ResolveTargetRotation));

        [HarmonyPatch(nameof(Player.UpdatePlacement))]
        [HarmonyPrefix]
        private static void UpdatePlacementPrefix(Player __instance) =>
            PlacementGhostController.SuppressPlacementInputIfEditing(__instance);

        [HarmonyPatch(nameof(Player.SetSelectedPiece), typeof(Vector2Int))]
        [HarmonyPrefix]
        private static void SetSelectedPiecePrefix(Player __instance, Vector2Int p) =>
            PlacementGhostController.OnSelectedPieceChanging(__instance, p);

        [HarmonyPatch(nameof(Player.SetupPlacementGhost))]
        [HarmonyPrefix]
        private static void SetupPlacementGhostPrefix(Player __instance) =>
            PlacementGhostController.BeforeSetupPlacementGhost(__instance);

        [HarmonyPatch(nameof(Player.SetupPlacementGhost))]
        [HarmonyPostfix]
        private static void SetupPlacementGhostPostfix(Player __instance) =>
            PlacementGhostController.AfterSetupPlacementGhost(__instance);

        [HarmonyPatch(nameof(Player.TryPlacePiece))]
        [HarmonyPostfix]
        private static void TryPlacePiecePostfix(Player __instance, bool __result) =>
            PlacementGhostController.OnTryPlacePieceCompleted(__instance, __result);

        [HarmonyPatch(nameof(Player.PlacePiece))]
        [HarmonyPostfix]
        private static void PlacePiecePostfix() =>
            PlacementGhostController.OnPiecePlaced();

        [HarmonyPatch(nameof(Player.UpdatePlacementGhost))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static bool UpdatePlacementGhostPrefix(Player __instance, bool flashGuardStone) =>
            PlacementGhostController.TryOverridePlacementGhost(__instance, flashGuardStone);

        // VeryHigh so ValidateGhost sets status before other mods postfixes read it or mess with it
        [HarmonyPatch(nameof(Player.UpdatePlacementGhost))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.VeryHigh)]
        private static void UpdatePlacementGhostPostfix(Player __instance, bool flashGuardStone) =>
            PlacementGhostController.ApplyPrecisionGhostOffset(__instance, flashGuardStone);

        [HarmonyPatch(nameof(Player.CanRotatePiece))]
        [HarmonyPostfix]
        private static void CanRotatePiecePostfix(ref bool __result) =>
            PieceRotationEngine.OverrideCanRotatePiece(ref __result);

        // here there be dragons... my only transpiler, probably the most risky, but eh... whatever. i think its fine.
        // the game builds the ghost rotation here from yaw only, then positions and snaps the ghost with it
        // swap in the 3 axis rotation right after Quaternion.Euler. a postfix is too late, snapping already ran
        [HarmonyPatch(nameof(Player.UpdatePlacementGhost))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> UpdatePlacementGhostTranspiler(IEnumerable<CodeInstruction> instructions) =>
            new CodeMatcher(instructions)
                .MatchEndForward(
                    new CodeMatch(OpCodes.Ldfld, PlayerPlaceRotationField),
                    new CodeMatch(OpCodes.Conv_R4),
                    new CodeMatch(OpCodes.Mul),
                    new CodeMatch(OpCodes.Ldc_R4),
                    new CodeMatch(OpCodes.Call, QuaternionEulerMethod))
                .ThrowIfInvalid("Could not locate Player.UpdatePlacementGhost placeRotation Euler calculation.")
                .Advance(1)
                .InsertAndAdvance(new CodeInstruction(OpCodes.Call, ResolveTargetRotationMethod))
                .InstructionEnumeration();
    }
}
