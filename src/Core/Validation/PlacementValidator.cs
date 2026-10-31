using System;
using System.Collections.Generic;
using MasterBuilderHeeler.Configuration;
using MasterBuilderHeeler.Core.State;
using UnityEngine;

namespace MasterBuilderHeeler.Core.Validation
{
    public static class PlacementValidator
    {
        public const string OutsideActiveZoneMessage = "Outside active world zone";

        private static readonly int WaterLayer = LayerMask.NameToLayer("Water");
        private static readonly int PieceLayerMask = LayerMask.GetMask("piece", "piece_nonsolid");
        private static readonly Collider[] OverlapColliderBuffer = new Collider[128];
        private static readonly List<Collider> TargetColliderBuffer = new List<Collider>(16);
        private static readonly List<Character> NearbyCharacterBuffer = new List<Character>(16);
        private static readonly RaycastHit[] SurfaceHitBuffer = new RaycastHit[16];

        private static bool _isValidatingEditedPiece;
        private static Player.PlacementStatus _prePostfixEditStatus;

        public static bool IsValidatingEditedPiece => _isValidatingEditedPiece;

        public static bool IsInsideActiveLoadedZone(Vector3 position) =>
            !ZNetScene.instance.OutsideActiveArea(position) && ZoneSystem.instance.IsZoneLoaded(position);

        // only touches a Valid status so base game rejection messages stay intact... allegedly
        public static void RejectGhostOutsideActiveZone(Player player)
        {
            if (player.m_placementStatus == Player.PlacementStatus.Valid &&
                !IsInsideActiveLoadedZone(player.m_placementGhost.transform.position))
            {
                player.m_placementStatus = Player.PlacementStatus.Invalid;
                player.SetPlacementGhostValid(false);
            }
        }

        public static void OverrideStationRangePoint(ref Vector3 point)
        {
            if (!ModConfig.EnableMod.Value ||
                (!BuildStateManager.IsBuildCameraActive && !ModConfig.ValidateStationRangeAtPieceLocation.Value))
            {
                return;
            }

            Player localPlayer = Player.m_localPlayer;
            if (!localPlayer || (point - localPlayer.transform.position).sqrMagnitude > 0.01f)
            {
                return;
            }

            GameObject? targetObject = BuildStateManager.GetActiveTargetObject(localPlayer);
            if (targetObject && targetObject.activeInHierarchy)
            {
                point = targetObject.transform.position;
            }
        }

        public static Player.PlacementStatus ValidateEditedPiece(Player player, Piece editedPiece, bool flashGuardStone)
        {
            // swap editedPiece into m_placementGhost and call UpdatePlacementGhost. janky but the prefix skips the base body
            // and other mods postfixes still run, so they can read m_placementGhost and change the status
            GameObject? previousGhost = player.m_placementGhost;
            _isValidatingEditedPiece = true;
            _prePostfixEditStatus = Player.PlacementStatus.Valid;
            player.m_placementGhost = editedPiece.gameObject;

            try
            {
                player.UpdatePlacementGhost(flashGuardStone);
                Player.PlacementStatus finalStatus = player.m_placementStatus;
                // another mods postfix can change the status after validation, make the highlight match what came out
                if (_prePostfixEditStatus != Player.PlacementStatus.Valid && finalStatus == Player.PlacementStatus.Valid)
                {
                    editedPiece.SetInvalidPlacementHeightlight(false);
                }
                else if (finalStatus != Player.PlacementStatus.Valid)
                {
                    editedPiece.SetInvalidPlacementHeightlight(true);
                }

                return finalStatus;
            }
            finally
            {
                player.m_placementGhost = previousGhost;
                _isValidatingEditedPiece = false;
            }
        }

        public static void ValidateEditedGhostInPrefix(Player player, bool flashGuardStone) =>
            _prePostfixEditStatus = ValidateGhost(player, flashGuardStone);

        public static Player.PlacementStatus ValidateGhost(Player player, bool flashGuardStone)
        {
            GameObject ghost = player.m_placementGhost;
            if (ghost && ghost.TryGetComponent(out Piece piece))
            {
                return Validate(player, ghost, piece, flashGuardStone);
            }

            return player.m_placementStatus;
        }

        public static string GetStatusMessage(Player.PlacementStatus status) => status switch
        {
            Player.PlacementStatus.NoBuildZone => "$msg_nobuildzone",
            Player.PlacementStatus.BlockedbyPlayer => "$msg_blocked",
            Player.PlacementStatus.PrivateZone => "$msg_privatezone",
            Player.PlacementStatus.MoreSpace => "$msg_needspace",
            Player.PlacementStatus.NoTeleportArea => "$msg_noteleportarea",
            Player.PlacementStatus.Invalid => "$msg_invalidplacement",
            Player.PlacementStatus.NoRayHits => "$msg_invalidplacement",
            Player.PlacementStatus.ExtensionMissingStation => "$msg_extensionmissingstation",
            Player.PlacementStatus.WrongBiome => "$msg_wrongbiome",
            Player.PlacementStatus.NeedCultivated => "$msg_needcultivated",
            Player.PlacementStatus.NeedDirt => "$msg_needdirt",
            Player.PlacementStatus.NotInDungeon => "$msg_notindungeon",
            Player.PlacementStatus.DeepSnow => "$msg_snowtoodeep",
            Player.PlacementStatus.NoSnow => "$msg_nosnow",
            _ => "$msg_invalidplacement"
        };

        // base game status checks all run off its own raycast hit, which is useless once a ghost is nudged snapped or frozen
        // so this redoes most of them against where the piece actually is. the wood, floor, ceiling and connection
        // rules are left out on purpose, because thats kind of the point of a precision placement mod. m_supports is out too, 
        // because WearNTear still runs stability, and if you do something dumb, it will punish you anyways.
        private static Player.PlacementStatus Validate(Player player, GameObject ghost, Piece piece, bool flashGuardStone)
        {
            Vector3 position = ghost.transform.position;
            if (!IsInsideActiveLoadedZone(position))
            {
                return SetStatus(player, piece, Player.PlacementStatus.Invalid);
            }

            if (Location.IsInsideNoBuildLocation(position))
            {
                return SetStatus(player, piece, Player.PlacementStatus.NoBuildZone);
            }

            bool hasWard = piece.TryGetComponent(out PrivateArea ward);
            float wardRadius = hasWard ? ward.m_radius : 0f;
            if (!PrivateArea.CheckAccess(position, wardRadius, flashGuardStone, hasWard))
            {
                return SetStatus(player, piece, Player.PlacementStatus.PrivateZone);
            }

            if (IsBlockedByCharacter(ghost, position))
            {
                return SetStatus(player, piece, Player.PlacementStatus.BlockedbyPlayer);
            }

            Heightmap.Biome targetBiome = Heightmap.FindBiome(position);
            if (piece.m_onlyInBiome != Heightmap.Biome.None && (targetBiome & piece.m_onlyInBiome) == 0)
            {
                return SetStatus(player, piece, Player.PlacementStatus.WrongBiome);
            }

            if (!piece.m_allowedInDungeons &&
                Character.InInterior(position) &&
                (!EnvMan.instance || !EnvMan.instance.CheckInteriorBuildingOverride()) &&
                (!ZoneSystem.instance || !ZoneSystem.instance.GetGlobalKey(GlobalKeys.DungeonBuild)))
            {
                return SetStatus(player, piece, Player.PlacementStatus.NotInDungeon);
            }

            if (piece.m_onlyInTeleportArea && !EffectArea.IsPointInsideArea(position, EffectArea.Type.Teleport))
            {
                return SetStatus(player, piece, Player.PlacementStatus.NoTeleportArea);
            }

            if (piece.TryGetComponent(out StationExtension stationExtension))
            {
                CraftingStation closestStation = stationExtension.FindClosestStationInRange(position);
                if (closestStation)
                {
                    stationExtension.StartConnectionEffect(closestStation);
                }
                else
                {
                    stationExtension.StopConnectionEffect();
                    return SetStatus(player, piece, Player.PlacementStatus.ExtensionMissingStation);
                }

                if (stationExtension.OtherExtensionInRange(piece.m_spaceRequirement))
                {
                    return SetStatus(player, piece, Player.PlacementStatus.MoreSpace);
                }
            }

            if (piece.m_blockRadius > 0f && piece.m_blockingPieces.Count > 0)
            {
                int hitCount = Physics.OverlapSphereNonAlloc(position, piece.m_blockRadius, OverlapColliderBuffer, PieceLayerMask);
                bool blockedByPiece = false;
                for (int index = 0; index < hitCount && !blockedByPiece; index++)
                {
                    Piece hitPiece = OverlapColliderBuffer[index].GetComponentInParent<Piece>();
                    if (!hitPiece || hitPiece == piece)
                    {
                        continue;
                    }

                    for (int blockIndex = 0; blockIndex < piece.m_blockingPieces.Count; blockIndex++)
                    {
                        Piece blockingPrefab = piece.m_blockingPieces[blockIndex];
                        if (blockingPrefab && string.Equals(blockingPrefab.m_name, hitPiece.m_name, StringComparison.Ordinal))
                        {
                            blockedByPiece = true;
                            break;
                        }
                    }
                }

                Array.Clear(OverlapColliderBuffer, 0, hitCount);
                if (blockedByPiece)
                {
                    return SetStatus(player, piece, Player.PlacementStatus.MoreSpace);
                }
            }

            if (piece.m_noClipping && IsClippingBlocked(player, ghost, 0.2f))
            {
                return SetStatus(player, piece, Player.PlacementStatus.Invalid);
            }

            bool checkDeepSnowLimit = !piece.m_allowedInDeepSnow && targetBiome == Heightmap.Biome.DeepNorth;
            if (piece.m_groundOnly ||
                piece.m_groundPiece ||
                piece.m_waterPiece ||
                piece.m_noInWater ||
                piece.m_notOnTiltingSurface ||
                piece.m_cultivatedGroundOnly ||
                piece.m_vegetationGroundOnly ||
                checkDeepSnowLimit ||
                piece.m_requireDeepSnow)
            {
                int rayMask = (piece.m_waterPiece || piece.m_noInWater) ? player.m_placeWaterRayMask : player.m_placeRayMask;
                float rayDistance = piece.m_waterPiece ? 4.5f : 1.5f;
                bool hasSurfaceHit = TryRaycastSurface(position, rayMask, rayDistance, piece.transform, out RaycastHit surfaceHit);
                Heightmap? heightmap = null;
                if (hasSurfaceHit && surfaceHit.collider.TryGetComponent(out Heightmap resolvedHeightmap))
                {
                    heightmap = resolvedHeightmap;
                }

                bool hasWaterSurface = hasSurfaceHit && surfaceHit.collider.gameObject.layer == WaterLayer;
                bool isAlternatePlacement = ZInput.GetButton("AltPlace") || ZInput.GetButton("JoyAltPlace");
                if (piece.m_waterPiece && !hasWaterSurface && !isAlternatePlacement)
                {
                    return SetStatus(player, piece, Player.PlacementStatus.Invalid);
                }

                if (piece.m_noInWater && hasWaterSurface)
                {
                    return SetStatus(player, piece, Player.PlacementStatus.Invalid);
                }

                if ((piece.m_groundOnly || piece.m_groundPiece) && !heightmap)
                {
                    return SetStatus(player, piece, Player.PlacementStatus.Invalid);
                }

                if (piece.m_notOnTiltingSurface && hasSurfaceHit && surfaceHit.normal.y < 0.8f)
                {
                    return SetStatus(player, piece, Player.PlacementStatus.Invalid);
                }

                if (piece.m_cultivatedGroundOnly && (!heightmap || !heightmap.IsCultivated(surfaceHit.point)))
                {
                    return SetStatus(player, piece, Player.PlacementStatus.NeedCultivated);
                }

                if (piece.m_vegetationGroundOnly)
                {
                    if (!heightmap)
                    {
                        return SetStatus(player, piece, Player.PlacementStatus.NeedDirt);
                    }

                    Heightmap.Biome surfaceBiome = heightmap.GetBiome(surfaceHit.point);
                    float vegetationMask = heightmap.GetVegetationMask(surfaceHit.point);
                    bool missingDirt = surfaceBiome == Heightmap.Biome.AshLands ? vegetationMask > 0.1f : vegetationMask < 0.25f;
                    if (missingDirt)
                    {
                        return SetStatus(player, piece, Player.PlacementStatus.NeedDirt);
                    }
                }

                if (checkDeepSnowLimit && heightmap && heightmap.GetCultivationMask(surfaceHit.point) > player.m_deepSnowBuildHeight)
                {
                    return SetStatus(player, piece, Player.PlacementStatus.DeepSnow);
                }

                if (piece.m_requireDeepSnow && (targetBiome != Heightmap.Biome.DeepNorth || !heightmap || heightmap.GetCultivationMask(surfaceHit.point) <= 0f))
                {
                    return SetStatus(player, piece, Player.PlacementStatus.NoSnow);
                }
            }

            return SetStatus(player, piece, Player.PlacementStatus.Valid);
        }

        private static bool IsBlockedByCharacter(GameObject targetObject, Vector3 position)
        {
            TargetColliderBuffer.Clear();
            targetObject.GetComponentsInChildren(TargetColliderBuffer);
            if (TargetColliderBuffer.Count == 0)
            {
                return false;
            }

            NearbyCharacterBuffer.Clear();
            Character.GetCharactersInRange(position, 30f, NearbyCharacterBuffer);
            bool isBlocked = false;

            for (int targetIndex = 0; targetIndex < TargetColliderBuffer.Count && !isBlocked; targetIndex++)
            {
                Collider targetCollider = TargetColliderBuffer[targetIndex];
                if (!targetCollider || !targetCollider.enabled || targetCollider.isTrigger)
                {
                    continue;
                }

                if (targetCollider is MeshCollider targetMesh && !targetMesh.convex)
                {
                    continue;
                }

                for (int characterIndex = 0; characterIndex < NearbyCharacterBuffer.Count; characterIndex++)
                {
                    Character character = NearbyCharacterBuffer[characterIndex];
                    if (!character || character.IsDead())
                    {
                        continue;
                    }

                    Collider characterCollider = character.GetCollider();
                    if (!characterCollider || !characterCollider.enabled)
                    {
                        continue;
                    }

                    if (Physics.ComputePenetration(
                        targetCollider,
                        targetCollider.transform.position,
                        targetCollider.transform.rotation,
                        characterCollider,
                        characterCollider.transform.position,
                        characterCollider.transform.rotation,
                        out Vector3 _,
                        out float _))
                    {
                        isBlocked = true;
                        break;
                    }
                }
            }

            TargetColliderBuffer.Clear();
            NearbyCharacterBuffer.Clear();
            return isBlocked;
        }

        // TestGhostClipping filters nothing and only works on ghosts cause they sit on the ghost layer. a placed piece
        // finds its own colliders and clips into itself lol. HasInvalidClipping skips self, triggers and non convex
        // colliders, then TestGhostClipping still runs so any mods hooking TestGhostClipping still work.
        private static bool IsClippingBlocked(Player player, GameObject targetObject, float maxPenetration) =>
            (!_isValidatingEditedPiece || HasInvalidClipping(targetObject, player.m_placeRayMask, maxPenetration)) &&
            player.TestGhostClipping(targetObject, maxPenetration);

        private static bool HasInvalidClipping(GameObject targetObject, int placeRayMask, float maxPenetration)
        {
            Transform targetRoot = targetObject.transform;
            TargetColliderBuffer.Clear();
            targetObject.GetComponentsInChildren(TargetColliderBuffer);
            if (TargetColliderBuffer.Count == 0)
            {
                return false;
            }

            int hitCount = Physics.OverlapSphereNonAlloc(targetRoot.position, 10f, OverlapColliderBuffer, placeRayMask);
            bool isClipping = false;

            for (int targetIndex = 0; targetIndex < TargetColliderBuffer.Count && !isClipping; targetIndex++)
            {
                Collider targetCollider = TargetColliderBuffer[targetIndex];
                if (!targetCollider || !targetCollider.enabled || targetCollider.isTrigger)
                {
                    continue;
                }

                if (targetCollider is MeshCollider targetMesh && !targetMesh.convex)
                {
                    continue;
                }

                for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
                {
                    Collider otherCollider = OverlapColliderBuffer[hitIndex];
                    if (!otherCollider || otherCollider.transform.IsChildOf(targetRoot))
                    {
                        continue;
                    }

                    if (otherCollider is MeshCollider otherMesh && !otherMesh.convex)
                    {
                        continue;
                    }

                    if (Physics.ComputePenetration(
                            targetCollider,
                            targetCollider.transform.position,
                            targetCollider.transform.rotation,
                            otherCollider,
                            otherCollider.transform.position,
                            otherCollider.transform.rotation,
                            out Vector3 _,
                            out float penetrationDistance) &&
                        penetrationDistance > maxPenetration)
                    {
                        isClipping = true;
                        break;
                    }
                }
            }

            TargetColliderBuffer.Clear();
            Array.Clear(OverlapColliderBuffer, 0, hitCount);
            return isClipping;
        }

        private static bool TryRaycastSurface(Vector3 position, int rayMask, float maxDistance, Transform ignoredRoot, out RaycastHit closestHit)
        {
            int hitCount = Physics.RaycastNonAlloc(position + Vector3.up * 0.5f, Vector3.down, SurfaceHitBuffer, maxDistance, rayMask);
            closestHit = default;
            float closestDistance = float.MaxValue;
            bool foundHit = false;

            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit candidate = SurfaceHitBuffer[index];
                if (!candidate.collider || candidate.collider.transform.IsChildOf(ignoredRoot))
                {
                    continue;
                }

                if (candidate.distance < closestDistance)
                {
                    closestDistance = candidate.distance;
                    closestHit = candidate;
                    foundHit = true;
                }
            }

            Array.Clear(SurfaceHitBuffer, 0, hitCount);
            return foundHit;
        }

        private static Player.PlacementStatus SetStatus(Player player, Piece piece, Player.PlacementStatus status)
        {
            player.m_placementStatus = status;
            bool isValid = status == Player.PlacementStatus.Valid;

            if (!_isValidatingEditedPiece)
            {
                player.SetPlacementGhostValid(isValid);
            }
            else
            {
                piece.SetInvalidPlacementHeightlight(!isValid);
            }

            return status;
        }
    }
}
