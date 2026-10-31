using MasterBuilderHeeler.Configuration;
using MasterBuilderHeeler.Core.Placement;
using MasterBuilderHeeler.Core.State;
using UnityEngine;

namespace MasterBuilderHeeler.Core.Translation
{
    public static class PieceTranslationEngine
    {
        public static Vector3 ActiveOffset { get; private set; }

        public static void ResetOffset() => ActiveOffset = Vector3.zero;

        public static void ResetNudge()
        {
            PlacementEditState editState = BuildStateManager.EditState;
            switch (editState)
            {
                case PlacementEditState.GhostFrozen:
                    BuildStateManager.ResetFrozenGhostPosition();
                    break;
                case PlacementEditState.InPlaceEdit:
                    BuildStateManager.ResetEditedPiecePosition();
                    break;
                default:
                    ActiveOffset = Vector3.zero;
                    break;
            }

            if (ModConfig.IsDebugLogging)
            {
                Plugin.LogDebug($"[PieceTranslationEngine] Reset nudge offset in state {editState}.");
            }
        }

        public static void UpdateNudgeInput(Player player, Vector3Int rawNudgeSigns)
        {
            if (rawNudgeSigns == Vector3Int.zero)
            {
                return;
            }

            GameObject? targetObject = BuildStateManager.GetActiveTargetObject(player);
            if (!targetObject || !targetObject.activeInHierarchy)
            {
                return;
            }

            Quaternion targetRotation = targetObject.transform.rotation;
            float step = BuildStateManager.GetActiveTranslationStep();
            BuildFrameBasis.ResolveTranslationAxes(
                targetRotation,
                out Vector3 rightAxis,
                out Vector3 upAxis,
                out Vector3 forwardAxis);

            Vector3 delta = ((rightAxis * rawNudgeSigns.x) + (upAxis * rawNudgeSigns.y) + (forwardAxis * rawNudgeSigns.z)) * step;
            if (delta == Vector3.zero)
            {
                return;
            }

            if (ModConfig.IsDebugLogging)
            {
                Plugin.LogDebug($"[PieceTranslationEngine] Nudge applied: delta={delta}, step={step:F2}m, preset={BuildStateManager.ActiveStepPreset}.");
            }

            PlacementEditState editState = BuildStateManager.EditState;
            Vector3 effectiveDelta = delta;

            if (BuildStateManager.IsGridSnapActive && targetObject.TryGetComponent(out Piece targetPiece))
            {
                Vector3 basePosition = editState switch
                {
                    PlacementEditState.GhostFrozen => BuildStateManager.FrozenGhostPosition,
                    PlacementEditState.InPlaceEdit => BuildStateManager.EditedPiecePosition,
                    _ => targetObject.transform.position
                };
                Vector3 steppedPosition = GridAlignmentEngine.AlignAndStepPosition(
                    targetPiece,
                    basePosition,
                    targetRotation,
                    delta,
                    out Vector3 alignedBasePosition);
                // so originally i had a bug here that kept adding grid snaps twice because reasons. bug dead. we gucci
                effectiveDelta = editState == PlacementEditState.Normal
                    ? steppedPosition - alignedBasePosition
                    : steppedPosition - basePosition;
            }

            ApplyTranslatedPosition(editState, effectiveDelta);
        }

        public static Vector3 ResolveTargetPosition(Piece piece, Vector3 rawPosition, Quaternion targetRotation)
        {
            Vector3 basePosition = BuildStateManager.EditState switch
            {
                PlacementEditState.GhostFrozen => BuildStateManager.FrozenGhostPosition,
                PlacementEditState.InPlaceEdit => BuildStateManager.EditedPiecePosition,
                _ => rawPosition + ActiveOffset
            };

            return BuildStateManager.IsGridSnapActive
                ? GridAlignmentEngine.AlignPositionToGrid(piece, basePosition, targetRotation)
                : basePosition;
        }

        public static void AdjustPlacementGhost(GameObject placementGhost)
        {
            if (!placementGhost.TryGetComponent(out Piece piece))
            {
                return;
            }

            Vector3 currentPosition = placementGhost.transform.position;
            Vector3 adjustedPosition = ResolveTargetPosition(piece, currentPosition, placementGhost.transform.rotation);

            if (adjustedPosition != currentPosition)
            {
                placementGhost.transform.position = adjustedPosition;
            }
        }

        private static void ApplyTranslatedPosition(PlacementEditState editState, Vector3 delta)
        {
            switch (editState)
            {
                case PlacementEditState.GhostFrozen:
                    BuildStateManager.TranslateFrozenGhost(delta);
                    break;
                case PlacementEditState.InPlaceEdit:
                    BuildStateManager.TranslateEditedPiece(delta);
                    break;
                default:
                    ActiveOffset += delta;
                    break;
            }
        }
    }
}
