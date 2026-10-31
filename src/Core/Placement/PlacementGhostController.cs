using System;
using MasterBuilderHeeler.Configuration;
using MasterBuilderHeeler.Core.Camera;
using MasterBuilderHeeler.Core.Rotation;
using MasterBuilderHeeler.Core.State;
using MasterBuilderHeeler.Core.Translation;
using MasterBuilderHeeler.Core.Validation;
using MasterBuilderHeeler.Core.Visuals.Gizmo;
using MasterBuilderHeeler.Core.Visuals.Grid;
using MasterBuilderHeeler.Utilities;
using UnityEngine;

namespace MasterBuilderHeeler.Core.Placement
{
    public static class PlacementGhostController
    {
        private static int _activePieceTableInstanceId;
        private static int _lastSelectedPrefabInstanceId;

        public static void Initialize()
        {
            Cleanup();
            ModConfig.EnableMod.SettingChanged += OnFeatureAvailabilityChanged;
            ModConfig.EnablePrecision.SettingChanged += OnFeatureAvailabilityChanged;
            ModConfig.EnableBuildCamera.SettingChanged += OnFeatureAvailabilityChanged;
            ModConfig.EnableGrid.SettingChanged += OnFeatureAvailabilityChanged;
        }

        public static void Cleanup()
        {
            _activePieceTableInstanceId = 0;
            _lastSelectedPrefabInstanceId = 0;
            ModConfig.EnableMod.SettingChanged -= OnFeatureAvailabilityChanged;
            ModConfig.EnablePrecision.SettingChanged -= OnFeatureAvailabilityChanged;
            ModConfig.EnableBuildCamera.SettingChanged -= OnFeatureAvailabilityChanged;
            ModConfig.EnableGrid.SettingChanged -= OnFeatureAvailabilityChanged;
        }

        public static void ResetBuildSession()
        {
            if (BuildStateManager.EditState == PlacementEditState.InPlaceEdit)
            {
                PieceEditController.CancelEdit();
            }

            _activePieceTableInstanceId = 0;
            _lastSelectedPrefabInstanceId = 0;
            BuildStateManager.ResetState();
            BuildCameraController.Deactivate();
            DetachPlacementMarker(Player.m_localPlayer);
            PieceRotationEngine.ResetAllRotators();
            PieceTranslationEngine.ResetOffset();
        }

        public static void OnLocalPlayerReady(Player player)
        {
            ResetBuildSession();
            GizmoRendering.EnsureController(player);
        }

        public static void OnPlayerDestroyed(Player player)
        {
            if (ReferenceEquals(player, Player.m_localPlayer))
            {
                ResetBuildSession();
            }
        }

        public static void UpdateSessionLifecycle(Player player)
        {
            if (!ModConfig.EnableMod.Value || player != Player.m_localPlayer)
            {
                return;
            }

            if (player.IsDead() || player.IsTeleporting() || !player.InPlaceMode())
            {
                if (BuildStateManager.IsHammerSessionActive)
                {
                    ResetBuildSession();
                    Plugin.LogDebug("[PlacementGhostController] Hammer exited: build session state reset.");
                }
                return;
            }

            int currentPieceTableId = 0;
            if (player.m_buildPieces)
            {
                currentPieceTableId = player.m_buildPieces.GetInstanceID();
            }

            if (BuildStateManager.EditState == PlacementEditState.InPlaceEdit &&
                _activePieceTableInstanceId != 0 &&
                currentPieceTableId != _activePieceTableInstanceId)
            {
                PieceEditController.CancelEdit();
            }

            _activePieceTableInstanceId = currentPieceTableId;
            BuildStateManager.SetHammerSessionActive(true);
        }

        public static void TogglePrecisionMode(Player player)
        {
            if (BuildStateManager.IsPrecisionModeActive && BuildStateManager.EditState == PlacementEditState.GhostFrozen)
            {
                UnfreezeGhost();
            }

            BuildStateManager.TogglePrecisionMode();
            if (BuildStateManager.IsPrecisionModeActive)
            {
                PieceRotationEngine.SyncFromNativeScrollIfDirty(player);
            }
            else
            {
                PieceTranslationEngine.ResetOffset();
            }
        }

        public static void ToggleGhostFreeze(Player player)
        {
            if (BuildStateManager.EditState == PlacementEditState.GhostFrozen)
            {
                UnfreezeGhost();
                return;
            }

            if (BuildStateManager.EditState == PlacementEditState.InPlaceEdit || !player.m_placementGhost)
            {
                Plugin.LogDebug("[PlacementGhostController] ToggleGhostFreeze rejected: in edit mode or no active placement ghost.");
                return;
            }

            GameObject ghost = player.m_placementGhost;
            Vector3 frozenPosition = ghost.transform.position;
            // still not happy with this, but allowing nudge while not frozen adds some fuckery, so this hopefully
            // deals with it. original position is the un-nudged spot, thats where ResetNudge goes back to
            BuildStateManager.EnterGhostFreeze(frozenPosition, frozenPosition - PieceTranslationEngine.ActiveOffset);
            PieceRotationEngine.SetAllRotators(ghost.transform.rotation);
            PieceTranslationEngine.ResetOffset();
            AttachPlacementMarker(player, ghost.transform);

            if (ModConfig.IsDebugLogging)
            {
                Plugin.LogDebug($"[PlacementGhostController] Ghost frozen at position {BuildStateManager.FrozenGhostPosition}.");
            }
            HudMessageUtils.ShowTopLeft("Freeze Ghost: ON");
        }

        public static void UnfreezeGhost()
        {
            if (!BuildStateManager.ExitGhostFreeze())
            {
                return;
            }

            PieceTranslationEngine.ResetOffset();
            DetachPlacementMarker(Player.m_localPlayer);

            Plugin.LogDebug("[PlacementGhostController] Ghost unfrozen.");
            HudMessageUtils.ShowTopLeft("Freeze Ghost: OFF");
        }

        public static void ToggleGridOverlay(Player player)
        {
            GridOverlayRenderer.EnsureRenderer(player);
            BuildStateManager.ToggleGridOverlayVisibility();
        }

        public static void ToggleLocalGrid(Player player)
        {
            if (BuildStateManager.ActiveGridFrame == GridFrame.Local)
            {
                BuildStateManager.ClearLocalGrid();
                return;
            }

            Piece? targetPiece = BuildReachService.GetAimedPiece(player);
            if (!targetPiece)
            {
                Plugin.LogDebug("[PlacementGhostController] ToggleLocalGrid rejected: no piece targeted in aim raycast.");
                HudMessageUtils.ShowTopLeft("Aim at a placed piece to anchor Local Grid.");
                return;
            }

            string displayName = PieceUtils.GetDisplayName(targetPiece);
            string prefabName = Utils.GetPrefabName(targetPiece.gameObject);
            BuildStateManager.AnchorLocalGrid(
                targetPiece.transform.position,
                targetPiece.transform.eulerAngles.y,
                displayName,
                prefabName);
        }

        public static void SuppressPlacementInputIfEditing(Player player)
        {
            if (!ModConfig.EnableMod.Value || BuildStateManager.EditState != PlacementEditState.InPlaceEdit)
            {
                return;
            }

            // UpdatePlacement fires buffered clicks if Time.time - pressedTime < 0.2f. -9999f is what the game itself uses to clear them
            // so we copy it i guess. dogshit solution. redseiko will probably also yell at me here and say theres a better way.
            player.m_blockRemove = true;
            player.m_removePressedTime = -9999f;
            player.m_placePressedTime = -9999f;
        }

        public static void OnSelectedPieceChanging(Player player, Vector2Int pieceIndex)
        {
            if (ModConfig.EnableMod.Value &&
                player.m_buildPieces &&
                player.m_buildPieces.GetSelectedIndex() != pieceIndex)
            {
                PieceTranslationEngine.ResetOffset();
            }
        }

        public static void OnPiecePlaced()
        {
            if (ModConfig.EnableMod.Value)
            {
                UnfreezeGhost();
            }
        }

        public static void OnTryPlacePieceCompleted(Player player, bool placed)
        {
            if (!placed && !PlacementValidator.IsInsideActiveLoadedZone(player.m_placementGhost.transform.position))
            {
                HudMessageUtils.ShowCenter(PlacementValidator.OutsideActiveZoneMessage);
            }
        }

        public static void BeforeSetupPlacementGhost(Player player)
        {
            if (!ModConfig.EnableMod.Value)
            {
                return;
            }

            // SetupPlacementGhost is about to destroy the old ghost, unparent the shared marker or it dies with it
            DetachPlacementMarker(player);

            int selectedPrefabId = 0;
            if (player.m_buildPieces)
            {
                GameObject selectedPrefab = player.m_buildPieces.GetSelectedPrefab();
                if (selectedPrefab)
                {
                    selectedPrefabId = selectedPrefab.GetInstanceID();
                }
            }

            if (_lastSelectedPrefabInstanceId != selectedPrefabId)
            {
                bool hadPreviousPrefab = _lastSelectedPrefabInstanceId != 0;
                _lastSelectedPrefabInstanceId = selectedPrefabId;
                if (hadPreviousPrefab)
                {
                    PieceTranslationEngine.ResetOffset();
                }
            }
        }

        public static void AfterSetupPlacementGhost(Player player)
        {
            if (!ModConfig.EnableMod.Value)
            {
                return;
            }

            GameObject? ghost = player.m_placementGhost;
            if (BuildStateManager.EditState != PlacementEditState.InPlaceEdit)
            {
                PieceRotationEngine.RefreshTargetMetadata(ghost);
            }

            if (BuildStateManager.EditState == PlacementEditState.GhostFrozen && ghost)
            {
                ApplyFrozenGhostTransform(ghost);
                bool hideMarker = ghost.TryGetComponent(out Piece ghostPiece) &&
                    (ghostPiece.m_groundOnly || ghostPiece.m_groundPiece || ghostPiece.m_cultivatedGroundOnly);
                if (!hideMarker)
                {
                    AttachPlacementMarker(player, ghost.transform);
                }
            }
        }

        public static bool TryOverridePlacementGhost(Player player, bool flashGuardStone)
        {
            if (!ModConfig.EnableMod.Value)
            {
                return true;
            }

            // this is the fake UpdatePlacementGhost call from ValidateEditedPiece, m_placementGhost is the real edited piece
            // right now. validate it and skip the body or game drags the actual piece around
            if (PlacementValidator.IsValidatingEditedPiece)
            {
                PlacementValidator.ValidateEditedGhostInPrefix(player, flashGuardStone);
                return false;
            }

            if (BuildStateManager.EditState == PlacementEditState.InPlaceEdit)
            {
                if (player.m_placementGhost)
                {
                    player.m_placementGhost.SetActive(false);
                }
                if (player.m_placementMarkerInstance)
                {
                    player.m_placementMarkerInstance.SetActive(false);
                }
                return false;
            }

            if (BuildStateManager.EditState == PlacementEditState.GhostFrozen)
            {
                GameObject ghost = player.m_placementGhost;
                if (ghost)
                {
                    ghost.SetActive(true);
                    ApplyFrozenGhostTransform(ghost);
                    PlacementValidator.ValidateGhost(player, flashGuardStone);
                }
                return false;
            }

            return true;
        }

        public static void ApplyPrecisionGhostOffset(Player player, bool flashGuardStone)
        {
            if (!ModConfig.EnableMod.Value ||
                BuildStateManager.EditState != PlacementEditState.Normal ||
                !player.m_placementGhost ||
                !player.m_placementGhost.activeSelf)
            {
                return;
            }

            // no snap and no nudge means the ghost is where game left it, so game status is still good and only the
            // zone check is missing. anything else moved it so it gets revalidated
            if (!BuildStateManager.IsGridSnapActive && PieceTranslationEngine.ActiveOffset == Vector3.zero)
            {
                PlacementValidator.RejectGhostOutsideActiveZone(player);
                return;
            }

            PieceTranslationEngine.AdjustPlacementGhost(player.m_placementGhost);
            PlacementValidator.ValidateGhost(player, flashGuardStone);
        }

        private static void ApplyFrozenGhostTransform(GameObject ghost)
        {
            Quaternion resolvedRotation = PieceRotationEngine.ResolveTargetRotation(ghost.transform.rotation);
            Vector3 resolvedPosition = ghost.TryGetComponent(out Piece ghostPiece)
                ? PieceTranslationEngine.ResolveTargetPosition(ghostPiece, ghost.transform.position, resolvedRotation)
                : BuildStateManager.FrozenGhostPosition;

            ghost.transform.position = resolvedPosition;
            ghost.transform.rotation = resolvedRotation;
        }

        private static void OnFeatureAvailabilityChanged(object sender, EventArgs eventArgs)
        {
            ResetBuildSession();
            if (!ModConfig.EnableGrid.Value)
            {
                GridOverlayRenderer.Cleanup();
            }
        }

        private static void AttachPlacementMarker(Player player, Transform targetTransform)
        {
            if (player.m_placementMarkerInstance)
            {
                player.m_placementMarkerInstance.transform.SetParent(targetTransform, worldPositionStays: true);
            }
        }

        private static void DetachPlacementMarker(Player? player)
        {
            if (player && player.m_placementMarkerInstance && player.m_placementMarkerInstance.transform.parent)
            {
                player.m_placementMarkerInstance.transform.SetParent(null, worldPositionStays: true);
            }
        }
    }
}
