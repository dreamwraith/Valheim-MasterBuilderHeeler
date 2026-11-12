using System.Collections.Generic;
using MasterBuilderHeeler.Configuration;
using MasterBuilderHeeler.Core.State;
using MasterBuilderHeeler.Utilities;
using UnityEngine;

namespace MasterBuilderHeeler.Core.Translation
{
    public static class GridAlignmentEngine
    {
        // i just learned what the word epsilon actually meant so im using it everywhere now.
        private const float AxisDeltaEpsilon = 0.0001f;
        private const float BoundaryEpsilon = 0.001f;

        private static readonly List<Transform> ReusableSnapPoints = new List<Transform>(16);

        public static Vector3 AlignPositionToGrid(Piece piece, Vector3 position, Quaternion rotation)
        {
            if (!BuildStateManager.IsGridSnapActive)
            {
                return position;
            }

            (Vector3 gridOrigin, Quaternion gridBasis, float step, bool excludeVertical) = ResolveGridAxes(piece);
            Vector3 snapOffset = GetActiveSnapOffset(piece, rotation);
            Vector3 anchorPosition = position + snapOffset;

            Quaternion inverseBasis = Quaternion.Inverse(gridBasis);
            Vector3 gridCoords = inverseBasis * (anchorPosition - gridOrigin);
            gridCoords.x = MathUtils.SnapScalar(gridCoords.x, step);
            if (!excludeVertical)
            {
                gridCoords.y = MathUtils.SnapScalar(gridCoords.y, step);
            }
            gridCoords.z = MathUtils.SnapScalar(gridCoords.z, step);

            Vector3 alignedAnchor = gridOrigin + (gridBasis * gridCoords);
            return alignedAnchor - snapOffset;
        }

        public static (Vector3 SteppedPosition, Vector3 AlignedPosition) AlignAndStepPosition(
            Piece piece,
            Vector3 currentPosition,
            Quaternion rotation,
            Vector3 worldDelta)
        {
            (Vector3 gridOrigin, Quaternion gridBasis, float step, bool excludeVertical) = ResolveGridAxes(piece);
            Vector3 snapOffset = GetActiveSnapOffset(piece, rotation);
            Vector3 anchorPosition = currentPosition + snapOffset;

            Quaternion inverseBasis = Quaternion.Inverse(gridBasis);
            Vector3 rawCoords = inverseBasis * (anchorPosition - gridOrigin);
            Vector3 alignedCoords = new Vector3(
                MathUtils.SnapScalar(rawCoords.x, step),
                excludeVertical ? rawCoords.y : MathUtils.SnapScalar(rawCoords.y, step),
                MathUtils.SnapScalar(rawCoords.z, step));

            Vector3 alignedPosition = (gridOrigin + (gridBasis * alignedCoords)) - snapOffset;

            Vector3 localDelta = inverseBasis * worldDelta;
            Vector3 steppedCoords = alignedCoords;

            // a slightly rotated local nudge lands on both x and z, ignore the cross axis part
            float horizontalThreshold = Mathf.Max(Mathf.Abs(localDelta.x), Mathf.Abs(localDelta.z)) * 0.5f;
            if (horizontalThreshold > AxisDeltaEpsilon)
            {
                if (Mathf.Abs(localDelta.x) >= horizontalThreshold)
                {
                    steppedCoords.x = StepCoordinate(alignedCoords.x, step, localDelta.x > 0f);
                }

                if (Mathf.Abs(localDelta.z) >= horizontalThreshold)
                {
                    steppedCoords.z = StepCoordinate(alignedCoords.z, step, localDelta.z > 0f);
                }
            }

            if (Mathf.Abs(localDelta.y) > AxisDeltaEpsilon)
            {
                steppedCoords.y = excludeVertical
                    ? alignedCoords.y + localDelta.y
                    : StepCoordinate(alignedCoords.y, step, localDelta.y > 0f);
            }

            Vector3 steppedAnchor = gridOrigin + (gridBasis * steppedCoords);
            return (steppedAnchor - snapOffset, alignedPosition);
        }

        // with a manual snap point picked, that point is what lines up with the grid instead of the piece origin
        private static Vector3 GetActiveSnapOffset(Piece piece, Quaternion rotation)
        {
            Player localPlayer = Player.m_localPlayer;
            if (!localPlayer || piece.gameObject != localPlayer.m_placementGhost)
            {
                return Vector3.zero;
            }

            int manualSnapIndex = localPlayer.m_manualSnapPoint;
            if (manualSnapIndex < 0)
            {
                return Vector3.zero;
            }

            ReusableSnapPoints.Clear();
            piece.GetSnapPoints(ReusableSnapPoints);
            if (manualSnapIndex >= ReusableSnapPoints.Count)
            {
                ReusableSnapPoints.Clear();
                return Vector3.zero;
            }

            Vector3 snapLocalPosition = ReusableSnapPoints[manualSnapIndex].localPosition;
            ReusableSnapPoints.Clear();
            return rotation * Vector3.Scale(snapLocalPosition, piece.transform.lossyScale);
        }

        // bias by BoundaryEpsilon so float noise on an exact grid line still steps to the next line
        private static float StepCoordinate(float value, float step, bool positive) => positive
            ? (Mathf.Floor((value / step) + BoundaryEpsilon) + 1f) * step
            : (Mathf.Ceil((value / step) - BoundaryEpsilon) - 1f) * step;

        private static GridAxes ResolveGridAxes(Piece piece)
        {
            float step = BuildStateManager.GridCellSize;
            bool excludeVertical = !ModConfig.Build.SnapVerticalToGrid.Value ||
                piece.m_category == Piece.PieceCategory.Furniture || piece.m_groundPiece || piece.m_waterPiece;

            return BuildStateManager.ActiveGridFrame == GridFrame.Local
                ? new GridAxes(BuildStateManager.LocalGridOrigin, BuildStateManager.LocalGridRotation, step, excludeVertical)
                : new GridAxes(Vector3.zero, Quaternion.identity, step, excludeVertical);
        }

        private record struct GridAxes(Vector3 Origin, Quaternion Basis, float Step, bool ExcludeVertical);
    }
}
