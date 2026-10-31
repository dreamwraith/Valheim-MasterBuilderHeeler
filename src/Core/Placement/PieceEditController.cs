using System;
using System.Collections.Generic;
using MasterBuilderHeeler.Configuration;
using MasterBuilderHeeler.Core.Rotation;
using MasterBuilderHeeler.Core.State;
using MasterBuilderHeeler.Core.Translation;
using MasterBuilderHeeler.Core.Validation;
using MasterBuilderHeeler.Utilities;
using UnityEngine;

namespace MasterBuilderHeeler.Core.Placement
{
    public static class PieceEditController
    {
        private static readonly int PieceLayerMask = LayerMask.GetMask("piece");
        private static readonly Collider[] SupportColliderBuffer = new Collider[128];
        private static readonly List<Container> ContainerBuffer = new List<Container>(4);

        private static bool _editedPieceHasContainers;

        public static void DeferWearNTearIfEditing(WearNTearUpdater updater)
        {
            // hold the WearNTear sweeps during an edit or unsupported pieces just break mid move lol
            // alternatively, delete these lines and laugh when people constantly collapse massive builds
            if (ModConfig.EnableMod.Value && BuildStateManager.EditState == PlacementEditState.InPlaceEdit)
            {
                updater.m_sleepUntil = updater.m_sleepUntilNext = Time.time + 0.5f;
                updater.m_index = 0;
            }
        }

        public static void TryTogglePieceEdit(Player player)
        {
            if (BuildStateManager.EditState == PlacementEditState.InPlaceEdit)
            {
                CommitEdit();
                return;
            }

            Piece? targetPiece = BuildReachService.GetAimedPiece(player, enforceBuildReach: true);
            if (!targetPiece)
            {
                Plugin.LogDebug("[PieceEditController] Edit rejected: no piece targeted in aim raycast.");
                HudMessageUtils.ShowTopLeft("Aim at a placed piece to edit.");
                return;
            }

            string targetPrefabName = Utils.GetPrefabName(targetPiece.gameObject);
            if (!targetPiece.m_canBeRemoved)
            {
                if (ModConfig.IsDebugLogging)
                {
                    Plugin.LogDebug($"[PieceEditController] Edit rejected: '{targetPrefabName}' has m_canBeRemoved=false.");
                }
                HudMessageUtils.ShowTopLeft("This piece cannot be edited.");
                return;
            }

            if (!IsPieceInEquippedTool(player.m_buildPieces, targetPrefabName))
            {
                string pieceName = PieceUtils.GetDisplayName(targetPiece);
                if (ModConfig.IsDebugLogging)
                {
                    Plugin.LogDebug($"[PieceEditController] Edit rejected: '{pieceName}' not available in active build table.");
                }
                HudMessageUtils.ShowTopLeft($"'{pieceName}' is not in the equipped tool's build menu.");
                return;
            }

            Vector3 piecePosition = targetPiece.transform.position;
            if (Location.IsInsideNoBuildLocation(piecePosition))
            {
                HudMessageUtils.ShowCenter("$msg_nobuildzone");
                return;
            }

            if (!PrivateArea.CheckAccess(piecePosition))
            {
                HudMessageUtils.ShowCenter("$msg_privatezone");
                return;
            }

            if (!player.CheckCanRemovePiece(targetPiece))
            {
                return;
            }

            if (!CanEditPiece(targetPiece, player))
            {
                HudMessageUtils.ShowCenter("$msg_cantremovenow");
                return;
            }

            ZNetView netView = targetPiece.m_nview;
            if (!netView || !netView.IsValid())
            {
                if (ModConfig.IsDebugLogging)
                {
                    Plugin.LogDebug($"[PieceEditController] Edit rejected: '{targetPrefabName}' has invalid ZNetView.");
                }
                HudMessageUtils.ShowTopLeft("This piece cannot be edited.");
                return;
            }

            if (!TryLockPieceContainers(targetPiece, netView, out bool hasContainers))
            {
                HudMessageUtils.ShowCenter("$msg_inuse");
                return;
            }

            _editedPieceHasContainers = hasContainers;

            BuildStateManager.EnterPieceEdit(targetPiece);
            PieceRotationEngine.RefreshTargetMetadata(targetPiece.gameObject);
            PieceTranslationEngine.ResetOffset();
            PieceRotationEngine.MatchPieceRotation(targetPiece);

            string displayName = PieceUtils.GetDisplayName(targetPiece);
            string commitKey = KeyLabelFormatter.Format(KeyboardMouseConfig.CommitPieceEditKey);
            string cancelKey = KeyLabelFormatter.Format(KeyboardMouseConfig.CancelPieceEditKey);
            HudMessageUtils.ShowTopLeft($"Editing {displayName} ([{commitKey}] Commit | [{cancelKey}] Cancel)");
            if (ModConfig.IsInfoLogging)
            {
                Plugin.LogInfo($"[PieceEditController] Started editing piece '{targetPrefabName}' at position {piecePosition}.");
            }
        }

        public static void UpdateEditMode(Player player)
        {
            Piece? piece = BuildStateManager.EditedPiece;
            if (!piece)
            {
                BuildStateManager.ExitPieceEdit(false);
                ResetEditTransform();
                return;
            }

            Quaternion resolvedRotation = PieceRotationEngine.ResolveTargetRotation(piece.transform.rotation);
            if (piece.transform.rotation != resolvedRotation)
            {
                piece.transform.rotation = resolvedRotation;
            }

            Vector3 resolvedPosition = PieceTranslationEngine.ResolveTargetPosition(piece, piece.transform.position, resolvedRotation);
            if (piece.transform.position != resolvedPosition)
            {
                piece.transform.position = resolvedPosition;
            }

            PlacementValidator.ValidateEditedPiece(player, piece, false);
        }

        public static void CommitEdit()
        {
            Piece? piece = BuildStateManager.EditedPiece;
            if (!piece)
            {
                BuildStateManager.ExitPieceEdit(false);
                ResetEditTransform();
                return;
            }

            Vector3 newPosition = piece.transform.position;
            Quaternion newRotation = piece.transform.rotation;
            string prefabName = Utils.GetPrefabName(piece.gameObject);
            Player player = Player.m_localPlayer;

            if (!player.CheckCanRemovePiece(piece))
            {
                return;
            }

            Player.PlacementStatus status = PlacementValidator.ValidateEditedPiece(player, piece, true);
            if (status != Player.PlacementStatus.Valid)
            {
                string statusMessage = PlacementValidator.IsInsideActiveLoadedZone(newPosition)
                    ? PlacementValidator.GetStatusMessage(status)
                    : PlacementValidator.OutsideActiveZoneMessage;
                HudMessageUtils.ShowCenter(statusMessage);
                if (ModConfig.IsInfoLogging)
                {
                    Plugin.LogInfo($"[PieceEditController] Commit rejected on '{prefabName}': status is {status}.");
                }
                return;
            }

            ZNetView netView = piece.m_nview;
            if (!netView || !netView.IsValid() || !ZNetScene.instance)
            {
                BuildStateManager.ExitPieceEdit(false);
                ResetEditTransform();
                return;
            }

            ZDO oldZdo = netView.GetZDO();
            int prefabHash = oldZdo.GetPrefab();
            if (prefabHash == 0)
            {
                HudMessageUtils.ShowTopLeft("This piece cannot be edited.");
                return;
            }

            Vector3 previousPosition = BuildStateManager.EditedPieceOriginalPosition;
            GameObject oldGameObject = piece.gameObject;
            bool hadContainers = _editedPieceHasContainers;

            if (!netView.IsOwner())
            {
                netView.ClaimOwnership();
            }

            // most pieces have no ZSyncTransform so remote clients only get zdo position and rotation in ZNetScene.CreateObject
            // moving the object does nothing for them, so copy the zdo into a new one and let peers spawn it. yes really
            // i know its some crazy shit, but StinkyPoopyFase's mod was way worse, and made signs break. we ride
            ZDO? newZdo = null;
            GameObject? newGameObject = null;
            try
            {
                ZPackage package = new ZPackage();
                oldZdo.Serialize(package);

                newZdo = ZDOMan.instance.CreateNewZDO(newPosition, prefabHash);
                package.SetPos(0);
                newZdo.Deserialize(package);
                newZdo.SetPosition(newPosition);
                newZdo.SetRotation(newRotation);
                // copy inherited the TryLockPieceContainers lock, clear it or the moved chest shows as in use
                if (hadContainers)
                {
                    newZdo.Set(ZDOVars.s_inUse, 0);
                }

                if (newZdo.DataRevision == 0)
                {
                    newZdo.IncreaseDataRevision();
                }

                newGameObject = ZNetScene.instance.CreateObject(newZdo);
            }
            catch (Exception exception)
            {
                // CreateObject leaves m_useInitZDO true if Instantiate throws, then every prefab spawn after is broken. nice
                ZNetView.m_initZDO = null;
                ZNetView.m_useInitZDO = false;
                Plugin.Log.LogError($"[PieceEditController] Failed to copy ZDO for '{prefabName}': {exception}");
            }

            if (!newGameObject)
            {
                if (newZdo != null)
                {
                    ZNetView partialInstance = ZNetScene.instance.FindInstance(newZdo);
                    if (partialInstance)
                    {
                        ZNetScene.instance.Destroy(partialInstance.gameObject);
                    }
                    else
                    {
                        ZDOMan.instance.DestroyZDO(newZdo);
                    }
                }

                HudMessageUtils.ShowTopLeft("Could not save piece edit; kept original piece.");
                return;
            }

            ZNetScene.instance.Destroy(oldGameObject);
            // Destroy waits for end of frame, turn the old colliders off now or support checks still count the old piece
            oldGameObject.SetActive(false);

            ZDOID creatorZdoId = player.GetZDOID();
            if (newGameObject.TryGetComponent(out Piece newPiece))
            {
                RecalculatePieceSupport(newPiece, previousPosition);
                newPiece.m_placeEffect.Create(newPosition, newRotation, newGameObject.transform, 1f, -1, creatorZdoId);
            }

            ItemDrop.ItemData rightItem = player.GetRightItem();
            if (rightItem != null)
            {
                rightItem.m_shared.m_buildEffect.Create(player.transform.position, Quaternion.identity, null, 1f, -1, creatorZdoId);
            }

            BuildStateManager.ExitPieceEdit(true);
            ResetEditTransform();
            HudMessageUtils.ShowTopLeft("Piece edit saved.");
            if (ModConfig.IsInfoLogging)
            {
                Plugin.LogInfo($"[PieceEditController] Committed edit on piece '{prefabName}' at position {newPosition}.");
            }
        }

        public static void CancelEdit()
        {
            Piece? piece = BuildStateManager.EditedPiece;
            if (piece)
            {
                if (_editedPieceHasContainers)
                {
                    UnlockPieceContainers(piece);
                }

                piece.SetInvalidPlacementHeightlight(false);
            }

            BuildStateManager.ExitPieceEdit(false);
            ResetEditTransform();
            Physics.SyncTransforms();

            HudMessageUtils.ShowTopLeft("Piece edit canceled.");
            Plugin.LogInfo("[PieceEditController] Canceled piece edit and restored original transform.");
        }

        private static void ResetEditTransform()
        {
            _editedPieceHasContainers = false;
            PieceRotationEngine.ResetAllRotators();
            PieceTranslationEngine.ResetOffset();

            Player localPlayer = Player.m_localPlayer;
            if (localPlayer && localPlayer.InPlaceMode())
            {
                PieceRotationEngine.RefreshTargetMetadata(localPlayer.m_placementGhost);
            }
        }

        private static bool IsPieceInEquippedTool(PieceTable buildPieces, string targetPrefabName)
        {
            List<GameObject> availablePrefabs = buildPieces.m_pieces;
            for (int index = 0; index < availablePrefabs.Count; index++)
            {
                GameObject candidatePrefab = availablePrefabs[index];
                if (candidatePrefab && string.Equals(candidatePrefab.name, targetPrefabName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool CanEditPiece(Piece piece, Player player)
        {
            if (piece.CanBeRemoved())
            {
                return true;
            }

            // only owners are allowed to edit their chests with items. empty privates still can be fucked by anyone.
            Container container = piece.GetComponentInChildren<Container>();
            return container && container.m_privacy == Container.PrivacySetting.Private && container.CheckAccess(player.GetPlayerID());
        }

        // lol, before i added this, i was nuking chest contents. hilarious, maybe i should make it a toggle :D
        private static bool TryLockPieceContainers(Piece piece, ZNetView netView, out bool hasContainers)
        {
            ContainerBuffer.Clear();
            piece.GetComponentsInChildren(true, ContainerBuffer);
            hasContainers = ContainerBuffer.Count > 0;
            if (!hasContainers)
            {
                return true;
            }

            ZDO targetZdo = netView.GetZDO();
            if (targetZdo.GetInt(ZDOVars.s_inUse) == 1)
            {
                ContainerBuffer.Clear();
                return false;
            }

            for (int index = 0; index < ContainerBuffer.Count; index++)
            {
                if (ContainerBuffer[index].IsInUse())
                {
                    ContainerBuffer.Clear();
                    return false;
                }
            }

            if (!netView.IsOwner())
            {
                netView.ClaimOwnership();
            }

            for (int index = 0; index < ContainerBuffer.Count; index++)
            {
                ContainerBuffer[index].m_inUse = true;
            }

            ContainerBuffer.Clear();
            targetZdo.Set(ZDOVars.s_inUse, 1);
            return true;
        }

        private static void UnlockPieceContainers(Piece piece)
        {
            ContainerBuffer.Clear();
            piece.GetComponentsInChildren(true, ContainerBuffer);
            for (int index = 0; index < ContainerBuffer.Count; index++)
            {
                ContainerBuffer[index].m_inUse = false;
            }

            ContainerBuffer.Clear();

            ZNetView netView = piece.m_nview;
            if (netView && netView.IsValid())
            {
                netView.GetZDO().Set(ZDOVars.s_inUse, 0);
            }
        }

        private static void RecalculatePieceSupport(Piece piece, Vector3 previousPosition)
        {
            if (!piece.TryGetComponent(out WearNTear wearNTear))
            {
                return;
            }

            // push same frame transform moves to physx, then clear cached neighbor support at the old spot
            Physics.SyncTransforms();
            wearNTear.OnPlaced();
            wearNTear.UpdateSupport();

            if (previousPosition != piece.transform.position)
            {
                int neighborCount = Physics.OverlapSphereNonAlloc(previousPosition, 4f, SupportColliderBuffer, PieceLayerMask);
                for (int index = 0; index < neighborCount; index++)
                {
                    WearNTear neighbor = SupportColliderBuffer[index].GetComponentInParent<WearNTear>();
                    if (neighbor && neighbor != wearNTear)
                    {
                        neighbor.ClearCachedSupport();
                    }
                }

                Array.Clear(SupportColliderBuffer, 0, neighborCount);
            }

            // no support means it takes a wear hit on the next sweep anyway, run it now so shiz will just break already
            if (wearNTear.m_nview && wearNTear.m_nview.IsValid() && wearNTear.m_nview.IsOwner() && !wearNTear.HaveSupport())
            {
                wearNTear.UpdateWear(Time.time);
            }
        }
    }
}
