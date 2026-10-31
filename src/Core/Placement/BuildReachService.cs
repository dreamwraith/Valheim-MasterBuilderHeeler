using MasterBuilderHeeler.Configuration;
using MasterBuilderHeeler.Core.State;
using MasterBuilderHeeler.Utilities;
using UnityEngine;

namespace MasterBuilderHeeler.Core.Placement
{
    public static class BuildReachService
    {
        public static Piece? GetAimedPiece(Player player, bool enforceBuildReach = false)
        {
            Piece? hoveringPiece = player.m_hoveringPiece;
            if (hoveringPiece)
            {
                return hoveringPiece;
            }

            UnityEngine.Camera? activeCamera = CameraUtils.GetActiveCamera();
            if (!activeCamera)
            {
                return null;
            }

            float maxRayDistance = ModConfig.MaxBuildReachDistance.Value;
            if (!Physics.Raycast(activeCamera.transform.position, activeCamera.transform.forward, out RaycastHit raycastHit, maxRayDistance, player.m_removeRayMask))
            {
                return null;
            }

            bool capToAvatarReach = enforceBuildReach &&
                !BuildStateManager.IsBuildCameraActive &&
                !ModConfig.EnableExtendedAvatarReach.Value;
            if (capToAvatarReach && Vector3.Distance(player.m_eye.position, raycastHit.point) > player.m_maxPlaceDistance)
            {
                return null;
            }

            return raycastHit.collider.GetComponentInParent<Piece>();
        }

        public static void ApplyReachExpansion(Player player, out float originalDistance)
        {
            originalDistance = player.m_maxPlaceDistance;
            if (ModConfig.EnableMod.Value)
            {
                if (BuildStateManager.IsBuildCameraActive)
                {
                    // raycasts are capped from the build camera already, Player measures reach from the body, so crank it
                    // basically this allows you to raycast further away for placement and interacts.
                    player.m_maxPlaceDistance = 1000f;
                }
                else if (ModConfig.EnableExtendedAvatarReach.Value)
                {
                    player.m_maxPlaceDistance = Mathf.Max(originalDistance, ModConfig.MaxBuildReachDistance.Value);
                }
            }
        }

        public static void RestoreReach(Player player, float originalDistance) =>
            player.m_maxPlaceDistance = originalDistance;
    }
}
