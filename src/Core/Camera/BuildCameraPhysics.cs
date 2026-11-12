using MasterBuilderHeeler.Configuration;
using UnityEngine;

namespace MasterBuilderHeeler.Core.Camera
{
    public static class BuildCameraPhysics
    {
        private const float ClearanceRadius = 0.35f;
        private const float SkinWidth = 0.04f;
        private const float MovementEpsilon = 0.0001f;

        private static readonly int FullCollisionMask = LayerMask.GetMask("terrain", "piece", "static_solid", "Default");
        private static readonly int WorldOnlyCollisionMask = LayerMask.GetMask("terrain", "static_solid");

        private static Vector3 _currentVelocity;
        private static Vector3 _velocityAcceleration;
        private static Vector3 _lastSafePosition;
        private static float _currentPitch;
        private static float _currentYaw;
        private static float _targetPitch;
        private static float _targetYaw;
        private static float _currentPitchVelocity;
        private static float _currentYawVelocity;

        public static void Initialize(Vector3 startPosition, Quaternion startRotation)
        {
            _currentVelocity = Vector3.zero;
            _velocityAcceleration = Vector3.zero;
            // eulerAngles is 0-360 but pitch clamps to -89..89, gotta wrap it or the view snaps when you enter looking up
            _currentPitch = startRotation.eulerAngles.x;
            if (_currentPitch > 180f)
            {
                _currentPitch -= 360f;
            }
            _targetPitch = _currentPitch;

            _currentYaw = startRotation.eulerAngles.y;
            _targetYaw = _currentYaw;

            _currentPitchVelocity = 0f;
            _currentYawVelocity = 0f;
            _lastSafePosition = startPosition;
        }

        public static Quaternion UpdateFlightRotation(Vector2 lookDelta, float deltaTime)
        {
            _targetYaw += lookDelta.x;
            _targetPitch -= lookDelta.y;
            _targetPitch = Mathf.Clamp(_targetPitch, -89f, 89f);

            float smoothTime = Mathf.Max(0.01f, ModConfig.Build.BuildCameraLookSmoothTime.Value);
            _currentYaw = Mathf.SmoothDampAngle(_currentYaw, _targetYaw, ref _currentYawVelocity, smoothTime, float.PositiveInfinity, deltaTime);
            _currentPitch = Mathf.SmoothDampAngle(_currentPitch, _targetPitch, ref _currentPitchVelocity, smoothTime, float.PositiveInfinity, deltaTime);

            return Quaternion.Euler(_currentPitch, _currentYaw, 0f);
        }

        public static Vector3 StepFlightVelocity(Vector3 currentPosition, Vector3 inputVector, bool isSprinting, float deltaTime)
        {
            float targetSpeed = isSprinting
                ? ModConfig.Build.BuildCameraFastMoveSpeed.Value
                : ModConfig.Build.BuildCameraMoveSpeed.Value;

            Vector3 targetVelocity = inputVector * targetSpeed;
            float smoothTime = Mathf.Max(0.02f, ModConfig.Build.BuildCameraSmoothTime.Value);

            _currentVelocity = Vector3.SmoothDamp(
                _currentVelocity,
                targetVelocity,
                ref _velocityAcceleration,
                smoothTime,
                float.PositiveInfinity,
                deltaTime);

            return currentPosition + (_currentVelocity * deltaTime);
        }

        public static Vector3 ResolveWorldCollision(Vector3 fromPosition, Vector3 toPosition)
        {
            Vector3 movement = toPosition - fromPosition;
            float distance = movement.magnitude;

            if (distance <= MovementEpsilon)
            {
                return fromPosition;
            }

            int collisionMask = ModConfig.Build.BuildCameraIgnoreCollision.Value
                ? WorldOnlyCollisionMask
                : FullCollisionMask;

            Vector3 direction = movement / distance;
            if (Physics.SphereCast(
                fromPosition,
                ClearanceRadius,
                direction,
                out RaycastHit sphereCastHit,
                distance,
                collisionMask,
                QueryTriggerInteraction.Ignore))
            {
                float allowedDistance = Mathf.Max(0f, sphereCastHit.distance - SkinWidth);
                Vector3 deflectedPoint = fromPosition + (direction * allowedDistance);
                Vector3 remainingMove = toPosition - deflectedPoint;
                Vector3 slideMove = Vector3.ProjectOnPlane(remainingMove, sphereCastHit.normal);
                toPosition = deflectedPoint + slideMove;
            }

            if (Physics.CheckSphere(toPosition, ClearanceRadius, collisionMask, QueryTriggerInteraction.Ignore))
            {
                if (!Physics.CheckSphere(_lastSafePosition, ClearanceRadius, collisionMask, QueryTriggerInteraction.Ignore))
                {
                    return _lastSafePosition;
                }
                return fromPosition;
            }

            _lastSafePosition = toPosition;
            return toPosition;
        }
    }
}
