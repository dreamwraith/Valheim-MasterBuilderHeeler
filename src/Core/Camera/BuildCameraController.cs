using MasterBuilderHeeler.Configuration;
using MasterBuilderHeeler.Core.State;
using MasterBuilderHeeler.Utilities;
using UnityEngine;

namespace MasterBuilderHeeler.Core.Camera
{
    public static class BuildCameraController
    {
        private static bool _isInitialized;
        private static bool _wasBoundaryClamped;

        public static void Deactivate()
        {
            if (_isInitialized)
            {
                Plugin.LogInfo("[BuildCameraController] Build Camera deactivated. Camera returned to player.");
            }

            _isInitialized = false;
            _wasBoundaryClamped = false;
            BuildCameraRig.Destroy();
        }

        public static bool InterceptCameraUpdate(float deltaTime, GameCamera camera, out float previousZoomSensitivity)
        {
            previousZoomSensitivity = camera.m_zoomSens;
            if (!ModConfig.EnableMod.Value)
            {
                if (_isInitialized)
                {
                    Deactivate();
                }
                return true;
            }

            if (BuildStateManager.IsBuildCameraActive)
            {
                UpdateBuildCamera(deltaTime, camera);
                return false;
            }

            if (_isInitialized)
            {
                Deactivate();
            }

            // the game still scroll zooms when status is NoRayHits, even if the piece can rotate. zero m_zoomSens for now,
            // then RestoreZoomSensitivity puts it back after. this feels like a hacky ass workaround, but whatever.
            if (BuildStateManager.EditState != PlacementEditState.Normal && !BuildStateManager.IsTerrainOpTarget)
            {
                camera.m_zoomSens = 0f;
            }

            return true;
        }

        public static void RestoreZoomSensitivity(GameCamera camera, float previousZoomSensitivity) =>
            camera.m_zoomSens = previousZoomSensitivity;

        public static void OverrideTakeInput(ref bool canTakeInput)
        {
            if (ModConfig.EnableMod.Value && BuildStateManager.IsBuildCameraActive)
            {
                canTakeInput = false;
            }
        }

        private static void InitializeFlight(Player player, GameCamera camera)
        {
            Vector3 anchorPosition = player.transform.position;
            Vector3 startPosition = camera.transform.position;
            // camera can be sitting at the origin or way off somewhere, just start from the eye in that case
            if (startPosition == Vector3.zero || Vector3.Distance(startPosition, anchorPosition) > 25.0f)
            {
                startPosition = player.m_eye.position;
            }

            camera.transform.position = startPosition;
            BuildCameraPhysics.Initialize(startPosition, camera.transform.rotation);
            _isInitialized = true;
            _wasBoundaryClamped = false;

            if (ModConfig.IsInfoLogging)
            {
                Plugin.LogInfo($"[BuildCameraController] Build Camera activated at position={startPosition}, anchored to player at {anchorPosition} (tether radius: {ModConfig.BuildCameraTetherRadius.Value:F0}m).");
            }
        }

        private static void UpdateBuildCamera(float deltaTime, GameCamera camera)
        {
            Player player = Player.m_localPlayer;
            if (!player || player.IsDead() || player.IsTeleporting() || !player.InPlaceMode())
            {
                BuildStateManager.ExitBuildCamera();
                Deactivate();
                return;
            }

            if (!_isInitialized)
            {
                InitializeFlight(player, camera);
            }

            Transform cameraTransform = camera.transform;
            bool isMenuOpen = InputUtils.IsTextInputOrMenuOpen();
            Vector2 lookDelta = isMenuOpen ? Vector2.zero : ZInput.GetMouseDelta();
            Vector3 moveDirection = isMenuOpen ? Vector3.zero : InputUtils.GetBuildCameraMoveDirection(cameraTransform);
            bool isSprinting = !isMenuOpen && ZInput.GetButton("Run");

            Quaternion targetRotation = BuildCameraPhysics.UpdateFlightRotation(lookDelta, deltaTime);
            Vector3 currentPosition = cameraTransform.position;
            Vector3 intendedPosition = BuildCameraPhysics.StepFlightVelocity(currentPosition, moveDirection, isSprinting, deltaTime);
            Vector3 tetheredPosition = ClampToAvatarTether(intendedPosition, player.transform.position);
            Vector3 resolvedPosition = BuildCameraPhysics.ResolveWorldCollision(currentPosition, tetheredPosition);

            cameraTransform.position = resolvedPosition;
            cameraTransform.rotation = targetRotation;

            BuildCameraRig.UpdateRig(player, cameraTransform);
            BuildCameraAutoPickup.ScanAndCollect(player, resolvedPosition);
        }

        private static Vector3 ClampToAvatarTether(Vector3 position, Vector3 anchorPosition)
        {
            float maxRadius = ModConfig.BuildCameraTetherRadius.Value;
            Vector3 offset = position - anchorPosition;

            if (offset.sqrMagnitude > maxRadius * maxRadius)
            {
                if (!_wasBoundaryClamped)
                {
                    _wasBoundaryClamped = true;
                    if (ModConfig.IsDebugLogging)
                    {
                        Plugin.LogDebug($"[BuildCameraController] Build Camera reached tether boundary ({maxRadius:F1}m from player).");
                    }
                }
                return anchorPosition + (offset.normalized * maxRadius);
            }

            _wasBoundaryClamped = false;
            return position;
        }
    }
}
