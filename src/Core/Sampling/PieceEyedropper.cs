using MasterBuilderHeeler.Configuration;
using MasterBuilderHeeler.Core.Placement;
using MasterBuilderHeeler.Core.Rotation;
using MasterBuilderHeeler.Core.State;
using MasterBuilderHeeler.Utilities;
using UnityEngine;

namespace MasterBuilderHeeler.Core.Sampling
{
    public static class PieceEyedropper
    {
        public static bool IsCustomSamplingAvailable() =>
            BuildStateManager.IsPrecisionModeActive &&
            BuildStateManager.EditState != PlacementEditState.InPlaceEdit &&
            ModConfig.PrecisionCopyBehavior.Value != PrecisionCopyMode.DisableCustom;

        public static bool ResolveNativeCopy(Player player)
        {
            if (!ModConfig.EnableMod.Value)
            {
                return true;
            }

            if (BuildStateManager.EditState == PlacementEditState.InPlaceEdit)
            {
                return false;
            }

            if (!BuildStateManager.IsPrecisionModeActive)
            {
                return true;
            }

            // turns out the game had piece pick this whole time, and my dumb ass didnt even know. wtf.
            // its keybind is hardcoded and cant be remapped, so first, what the fuck iron gate, 
            // but also, this just lets you choose which one wins. this one is for Searica <3
            switch (ModConfig.PrecisionCopyBehavior.Value)
            {
                case PrecisionCopyMode.OverrideNative:
                    SampleTargetPieceAndRotation(player);
                    return false;
                case PrecisionCopyMode.DisableNative:
                    return false;
                default:
                    return true;
            }
        }

        public static void SampleTargetPiece(Player player)
        {
            if (!TryGetSelectableTarget(player, out Piece target))
            {
                return;
            }

            CopyPiece(player, target);
            HudMessageUtils.ShowTopLeft($"Sampled: {PieceUtils.GetDisplayName(target)}");
        }

        public static void SampleTargetRotation(Player player)
        {
            if (!TryGetAimedTarget(player, out Piece target))
            {
                return;
            }

            if (!player.m_placementGhost)
            {
                Plugin.LogDebug("[PieceEyedropper] Rotation copy rejected: no active placement ghost.");
                HudMessageUtils.ShowTopLeft("Select a build piece first to copy rotation onto.");
                return;
            }

            CopyRotation(player, target);
            string eulerText = MathUtils.FormatEulerPlain(target.transform.rotation.eulerAngles);
            HudMessageUtils.ShowTopLeft($"Copied rotation {eulerText} from {PieceUtils.GetDisplayName(target)}");
        }

        public static void SampleTargetPieceAndRotation(Player player)
        {
            if (!TryGetSelectableTarget(player, out Piece target))
            {
                return;
            }

            CopyPiece(player, target);
            CopyRotation(player, target);
            string eulerText = MathUtils.FormatEulerPlain(target.transform.rotation.eulerAngles);
            HudMessageUtils.ShowTopLeft($"Sampled: {PieceUtils.GetDisplayName(target)} ({eulerText})");
        }

        private static bool TryGetAimedTarget(Player player, out Piece target)
        {
            target = BuildReachService.GetAimedPiece(player)!;
            if (target)
            {
                return true;
            }

            Plugin.LogDebug("[PieceEyedropper] Sample rejected: no piece targeted in aim raycast.");
            HudMessageUtils.ShowTopLeft("Aim at a placed piece to sample.");
            return false;
        }

        private static bool TryGetSelectableTarget(Player player, out Piece target)
        {
            if (!TryGetAimedTarget(player, out target))
            {
                return false;
            }

            if (player.m_buildPieces.GetPieceIndex(target, out _, out _))
            {
                return true;
            }

            string pieceName = PieceUtils.GetDisplayName(target);
            if (ModConfig.IsDebugLogging)
            {
                Plugin.LogDebug($"[PieceEyedropper] Sample rejected: '{pieceName}' not available in active build table.");
            }
            HudMessageUtils.ShowTopLeft($"'{pieceName}' is not in the equipped tool's build menu.");
            return false;
        }

        private static void CopyPiece(Player player, Piece target)
        {
            player.SetSelectedPiece(target);

            if (Hud.instance && Hud.instance.m_buildUi && Hud.IsPieceSelectionVisible())
            {
                Hud.instance.m_buildUi.UpdatePieceSelection(target);
            }

            if (ModConfig.IsDebugLogging)
            {
                Plugin.LogDebug($"[PieceEyedropper] Sampled piece {Utils.GetPrefabName(target.gameObject)} into active build tool.");
            }
        }

        private static void CopyRotation(Player player, Piece target)
        {
            PieceRotationEngine.MatchPieceRotation(target);

            Quaternion targetRotation = target.transform.rotation;
            // m_placeRotation counts steps not degrees, convert yaw to the closest step so game rotation picks up from the sampled angle.
            // wrap it so it stays in range... idk. i think people are crazy for toggling shit, just stay in precision, but watev
            float stepDegrees = Mathf.Max(1f, player.m_placeRotationDegrees);
            int stepCount = Mathf.Max(1, Mathf.RoundToInt(360f / stepDegrees));
            player.m_placeRotation = ((Mathf.RoundToInt(targetRotation.eulerAngles.y / stepDegrees) % stepCount) + stepCount) % stepCount;
            player.m_placementGhost.transform.rotation = targetRotation;

            if (ModConfig.IsDebugLogging)
            {
                Plugin.LogDebug($"[PieceEyedropper] Copied rotation from {Utils.GetPrefabName(target.gameObject)}: euler={targetRotation.eulerAngles}");
            }
        }
    }
}
