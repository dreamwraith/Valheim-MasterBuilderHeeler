using MasterBuilderHeeler.Configuration;
using MasterBuilderHeeler.Core.Placement;
using MasterBuilderHeeler.Core.Rotation.Rotators;
using MasterBuilderHeeler.Core.State;
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

        public static void UpdatePlacementInput(Player player, int rotationStepSign)
        {
            GameObject? targetObject = BuildStateManager.GetActiveTargetObject(player);
            RefreshTargetMetadata(targetObject);

            if (!targetObject ||
                !targetObject.activeInHierarchy ||
                BuildStateManager.IsTerrainOpTarget ||
                rotationStepSign == 0)
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
            IRotator activeRotator = GetActiveRotator();
            ApplyAxisSteps(activeRotator, currentAxis, signedStep);
            SyncInactiveRotators(activeRotator);

            if (ModConfig.IsDebugLogging)
            {
                Plugin.LogDebug($"[PieceRotationEngine] Applied rotation: axis={currentAxis} step={signedStep:+0.0;-0.0} deg (rotator={activeRotator.Name}).");
            }
        }

        public static void OverrideCanRotatePiece(ref bool canRotate)
        {
            if (ModConfig.EnableMod.Value &&
                !BuildStateManager.IsTerrainOpGhostActive &&
                (BuildStateManager.EditState != PlacementEditState.Normal || BuildStateManager.IsPrecisionModeActive))
            {
                canRotate = true;
            }
        }

        public static Quaternion ResolveTargetRotation(Quaternion nativeRotation)
        {
            if (!ModConfig.EnableMod.Value)
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
                return GetActiveRotator().GetRotation();
            }

            return nativeRotation;
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
            SyncRotatorsFromBasis(targetRotation, BuildFrameBasis.IsCornerRoofPrefab(prefabName));
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

            if (!BuildStateManager.IsHammerSessionActive)
            {
                ClearTargetMetadata();
            }

            if (Player.m_localPlayer)
            {
                Player.m_localPlayer.m_placeRotation = 0;
            }

            Plugin.LogDebug("[PieceRotationEngine] Reset all rotators to base orientation and reset native m_placeRotation to 0.");
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

        private static void ApplyAxisSteps(IRotator rotator, RotationAxis axes, float signedStep)
        {
            if ((axes & RotationAxis.Pitch) != 0)
            {
                rotator.Rotate(Vector3.right, signedStep);
            }

            if ((axes & RotationAxis.Yaw) != 0)
            {
                rotator.Rotate(Vector3.up, signedStep);
            }

            if ((axes & RotationAxis.Roll) != 0)
            {
                rotator.Rotate(Vector3.forward, signedStep);
            }
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

        private static void ClearTargetMetadata()
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
