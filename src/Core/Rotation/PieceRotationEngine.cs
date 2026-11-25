using MasterBuilderHeeler.Configuration;
using MasterBuilderHeeler.Core.Placement;
using MasterBuilderHeeler.Core.Rotation.Rotators;
using MasterBuilderHeeler.Core.State;
using MasterBuilderHeeler.Utilities;
using UnityEngine;

namespace MasterBuilderHeeler.Core.Rotation
{
    public static class PieceRotationEngine
    {
        private static readonly WorldEulerRotator EulerRotator = new WorldEulerRotator();
        private static readonly LocalFrameRotator LocalRotator = new LocalFrameRotator();
        private static readonly CornerRoofRotator RoofRotator = new CornerRoofRotator();

        private static int _currentTargetInstanceId;
        private static bool _dirtyFromNativeScroll;
        private static Quaternion _savedRotation = Quaternion.identity;

        public static IRotator GetActiveRotator()
        {
            if (BuildStateManager.IsRoofModeEffective)
            {
                return RoofRotator;
            }

            return BuildStateManager.ActiveRotationFrame == RotationFrame.Local
                ? LocalRotator
                : EulerRotator;
        }

        // a ring sits on the axis its key really turns the piece around. local and roof rotation turn around the
        // pieces own axes so all three rings follow the piece. world rotation steps euler angles and unity applies
        // those as roll then pitch then yaw, so yaw is around world up, pitch is around the side axis after yaw and
        // only roll is around the pieces own axis
        public static (Quaternion Pitch, Quaternion Yaw, Quaternion Roll) ResolveRingBases(Quaternion pieceRotation)
        {
            Quaternion pieceBasis = BuildFrameBasis.ResolveGizmoBasis(pieceRotation);
            if (!ReferenceEquals(GetActiveRotator(), EulerRotator))
            {
                return (pieceBasis, pieceBasis, pieceBasis);
            }

            Vector3 eulerAngles = EulerRotator.GetEulerAngles();
            return (
                Quaternion.Euler(eulerAngles.x, eulerAngles.y, 0f),
                Quaternion.Euler(0f, eulerAngles.y, 0f),
                pieceBasis);
        }

        public static void UpdatePlacementInput(Player player, int rotationStepSign)
        {
            if (!TryGetRotatableTarget(player, out _) || rotationStepSign == 0)
            {
                return;
            }

            if (!BuildStateManager.IsPrecisionModeActive && BuildStateManager.EditState == PlacementEditState.Normal)
            {
                // precision is off so games scroll owns the rotation, flagging it so the rotators grab the new yaw when
                // precision comes back on
                _dirtyFromNativeScroll = true;
                return;
            }

            RotationAxis currentAxis = BuildStateManager.ActiveAxis;
            if (currentAxis == RotationAxis.None)
            {
                return;
            }

            float signedStep = BuildStateManager.GetActiveRotationStep() * rotationStepSign;
            IRotator activeRotator = RotateActiveRotator(currentAxis, signedStep);

            if (ModConfig.IsDebugLogging)
            {
                Plugin.LogDebug($"[PieceRotationEngine] Applied rotation: axis={currentAxis} step={signedStep:+0.0;-0.0} deg (rotator={activeRotator.Name}).");
            }
        }

        public static void OverrideCanRotatePiece(ref bool canRotate)
        {
            if (ModConfig.General.EnableMod.Value &&
                !BuildStateManager.IsTerrainOpGhostActive &&
                (BuildStateManager.EditState != PlacementEditState.Normal || BuildStateManager.IsPrecisionModeActive))
            {
                canRotate = true;
            }
        }

        public static Quaternion ResolveTargetRotation(Quaternion nativeRotation)
        {
            if (!ModConfig.General.EnableMod.Value)
            {
                return nativeRotation;
            }

            GameObject? targetObject = BuildStateManager.GetActiveTargetObject(Player.m_localPlayer);
            RefreshTargetMetadata(targetObject);

            if (BuildStateManager.IsTerrainOpTarget)
            {
                return nativeRotation;
            }

            if (BuildStateManager.IsPrecisionModeActive || BuildStateManager.EditState != PlacementEditState.Normal)
            {
                return ResolveSavedRotation();
            }

            return nativeRotation;
        }

        public static Vector3 GetSnappedEulerAngles() =>
            MathUtils.SnapEulerToSaveGrid(GetActiveRotator().GetEulerAngles());

        // only snap the output so local and roof rotator state stays exact across steps
        private static Quaternion ResolveSavedRotation()
        {
            Quaternion snappedRotation = Quaternion.Euler(GetSnappedEulerAngles());
            // SetRotation stores eulerAngles from the quaternion and unity can read back a different angle set so snap again
            return Quaternion.Euler(MathUtils.SnapEulerToSaveGrid(snappedRotation.eulerAngles));
        }

        public static void SyncFromNativeScrollIfDirty(Player player)
        {
            if (_dirtyFromNativeScroll)
            {
                SyncNativeYaw(player);
            }
        }

        public static void SyncNativeYaw(Player player)
        {
            _dirtyFromNativeScroll = false;
            Quaternion nativeRotation = Quaternion.Euler(0f, player.m_placeRotationDegrees * player.m_placeRotation, 0f);
            SyncRotatorsFromBasis(nativeRotation, isRoofBasis: false);
        }

        public static void SetAllRotators(Quaternion targetRotation)
        {
            _dirtyFromNativeScroll = false;
            SyncRotatorsFromBasis(targetRotation, BuildStateManager.IsRoofModeEffective);
        }

        public static void MatchPieceRotation(Piece targetPiece)
        {
            _dirtyFromNativeScroll = false;
            string prefabName = Utils.GetPrefabName(targetPiece.gameObject);
            Quaternion targetRotation = targetPiece.transform.rotation;
            // a corner roof only carries the extra 45 degrees while roof mode is on. with it off the piece sits on the
            // standard basis like everything else
            bool isRoofBasis = BuildStateManager.IsRoofModeActive && BuildFrameBasis.IsCornerRoofPrefab(prefabName);
            SyncRotatorsFromBasis(targetRotation, isRoofBasis);
            if (ModConfig.IsDebugLogging)
            {
                Plugin.LogDebug($"[PieceRotationEngine] Matched rotation from piece {prefabName}: {targetRotation.eulerAngles}");
            }
        }

        public static void ResetAllRotators()
        {
            _dirtyFromNativeScroll = false;
            EulerRotator.ResetRotation();
            LocalRotator.ResetRotation();
            RoofRotator.ResetRotation();

            if (Player.m_localPlayer)
            {
                Player.m_localPlayer.m_placeRotation = 0;
            }

            Plugin.LogDebug("[PieceRotationEngine] Reset all rotators to base orientation and reset base game m_placeRotation to 0.");
        }

        // euler and local always hold the standard basis rotation, roof gets rebuilt from it on restore
        public static void SaveRotation() => _savedRotation = EulerRotator.GetRotation();

        public static void RestoreSavedRotation()
        {
            _dirtyFromNativeScroll = false;
            SyncRotatorsFromBasis(_savedRotation, isRoofBasis: false);
        }

        public static void ResetAxis(RotationAxis axis)
        {
            IRotator activeRotator = GetActiveRotator();
            activeRotator.ResetAxis(axis);
            SyncInactiveRotators(activeRotator);

            if ((axis & RotationAxis.Yaw) != 0 && Player.m_localPlayer)
            {
                Player.m_localPlayer.m_placeRotation = 0;
            }

            if (ModConfig.IsDebugLogging)
            {
                Plugin.LogDebug($"[PieceRotationEngine] Reset axis {axis} on {activeRotator.Name} and synced rotators.");
            }
        }

        public static void FlipAxis(Player player, RotationAxis axis)
        {
            if (!TryGetRotatableTarget(player, out _))
            {
                return;
            }

            IRotator activeRotator = RotateActiveRotator(axis, 180f);

            if (ModConfig.IsDebugLogging)
            {
                Plugin.LogDebug($"[PieceRotationEngine] Flipped axis {axis} 180 deg (rotator={activeRotator.Name}).");
            }
        }

        // left to right mirror across a vertical plane. the plane runs along whichever grid line the camera looks down
        // so the angles stay on the grid. LocalGridRotation is identity while the grid frame is World
        public static void MirrorRotation(Player player)
        {
            UnityEngine.Camera? activeCamera = CameraUtils.GetActiveCamera();
            if (!activeCamera || !TryGetRotatableTarget(player, out GameObject targetObject))
            {
                return;
            }

            Quaternion gridBasis = BuildStateManager.LocalGridRotation;
            Vector3 gridRight = gridBasis * Vector3.right;
            Vector3 gridForward = gridBasis * Vector3.forward;
            Vector3 viewRight = activeCamera.transform.right;
            Vector3 planeNormal = Mathf.Abs(Vector3.Dot(viewRight, gridRight)) >= Mathf.Abs(Vector3.Dot(viewRight, gridForward))
                ? gridRight
                : gridForward;

            // forward and up get reflected and LookRotation builds the rotation back from them, so the piece keeps its
            // own left and right sides. a piece thats not the same on both sides doesnt come out as a true mirror image
            Quaternion rotation = targetObject.transform.rotation;
            // wood and darkwood corner roofs are the same on both sides of their diagonal and not left to right, so the
            // right axis goes in as the new forward and that swaps the two wings. a grausten corner is only half a
            // corner and its other half is a different piece, so the same math turns it into the other half of the
            // mirrored corner
            Vector3 forwardSourceAxis = BuildStateManager.IsCornerRoofTarget ? Vector3.right : Vector3.forward;
            Quaternion mirroredRotation = Quaternion.LookRotation(
                Vector3.Reflect(rotation * forwardSourceAxis, planeNormal),
                Vector3.Reflect(rotation * Vector3.up, planeNormal));
            SetAllRotators(mirroredRotation);

            if (ModConfig.IsDebugLogging)
            {
                Plugin.LogDebug($"[PieceRotationEngine] Mirrored rotation {rotation.eulerAngles} to {mirroredRotation.eulerAngles} across plane normal {planeNormal}.");
            }
        }

        // true when theres a target on screen the rotators can turn. refreshes the target classification first
        private static bool TryGetRotatableTarget(Player player, out GameObject targetObject)
        {
            targetObject = BuildStateManager.GetActiveTargetObject(player)!;
            RefreshTargetMetadata(targetObject);
            return targetObject && targetObject.activeInHierarchy && !BuildStateManager.IsTerrainOpTarget;
        }

        private static IRotator RotateActiveRotator(RotationAxis axes, float signedStep)
        {
            IRotator activeRotator = GetActiveRotator();
            if ((axes & RotationAxis.Pitch) != 0)
            {
                activeRotator.Rotate(Vector3.right, signedStep);
            }

            if ((axes & RotationAxis.Yaw) != 0)
            {
                activeRotator.Rotate(Vector3.up, signedStep);
            }

            if ((axes & RotationAxis.Roll) != 0)
            {
                activeRotator.Rotate(Vector3.forward, signedStep);
            }

            SyncInactiveRotators(activeRotator);
            return activeRotator;
        }

        private static void SyncRotatorsFromBasis(Quaternion rotation, bool isRoofBasis)
        {
            Quaternion normalized = Quaternion.Normalize(rotation);
            if (isRoofBasis)
            {
                Quaternion standardRotation = Quaternion.Normalize(normalized * BuildFrameBasis.InverseCornerRoofRotation);
                EulerRotator.SyncFromRotation(standardRotation);
                LocalRotator.SyncFromRotation(standardRotation);
                RoofRotator.SyncFromRotation(normalized);
                return;
            }

            EulerRotator.SyncFromRotation(normalized);
            LocalRotator.SyncFromRotation(normalized);
            RoofRotator.SyncFromRotation(Quaternion.Normalize(normalized * BuildFrameBasis.CornerRoofBaseRotation));
        }

        // each rotator keeps its own state, copy the result to the others or switching frames makes the piece jump
        private static void SyncInactiveRotators(IRotator activeRotator)
        {
            Quaternion syncedRotation = Quaternion.Normalize(activeRotator.GetRotation());
            if (ReferenceEquals(activeRotator, RoofRotator))
            {
                Quaternion standardRotation = Quaternion.Normalize(syncedRotation * BuildFrameBasis.InverseCornerRoofRotation);
                EulerRotator.SyncFromRotation(standardRotation);
                LocalRotator.SyncFromRotation(standardRotation);
                return;
            }

            if (!ReferenceEquals(activeRotator, EulerRotator))
            {
                EulerRotator.SyncFromRotation(syncedRotation);
            }

            if (!ReferenceEquals(activeRotator, LocalRotator))
            {
                LocalRotator.SyncFromRotation(syncedRotation);
            }

            RoofRotator.SyncFromRotation(Quaternion.Normalize(syncedRotation * BuildFrameBasis.CornerRoofBaseRotation));
        }

        public static void ClearTargetMetadata()
        {
            _currentTargetInstanceId = 0;
            BuildStateManager.SetTargetClassification(isTerrainOp: false, isCornerRoof: false);
        }

        public static void RefreshTargetMetadata(GameObject? targetObject)
        {
            if (!targetObject)
            {
                ClearTargetMetadata();
                return;
            }

            int instanceId = targetObject.GetInstanceID();
            if (instanceId == _currentTargetInstanceId)
            {
                return;
            }

            _currentTargetInstanceId = instanceId;
            string prefabName = Utils.GetPrefabName(targetObject);
            bool isTerrainOp = targetObject.TryGetComponent(out TerrainOp _);
            bool isCornerRoof = BuildFrameBasis.IsCornerRoofPrefab(prefabName);
            BuildStateManager.SetTargetClassification(isTerrainOp, isCornerRoof);
        }
    }
}
