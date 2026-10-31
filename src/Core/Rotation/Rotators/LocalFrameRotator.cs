using MasterBuilderHeeler.Core.Placement;
using MasterBuilderHeeler.Core.State;
using UnityEngine;

namespace MasterBuilderHeeler.Core.Rotation.Rotators
{
    public sealed class LocalFrameRotator : IRotator
    {
        private Quaternion _currentRotation = Quaternion.identity;

        public string Name => nameof(LocalFrameRotator);

        public void Rotate(Vector3 axis, float angle) =>
            _currentRotation = Quaternion.Normalize(_currentRotation * Quaternion.AngleAxis(angle, axis));

        public void ResetRotation() => _currentRotation = Quaternion.identity;

        public void ResetAxis(RotationAxis axis) =>
            _currentRotation = Quaternion.Euler(BuildFrameBasis.ZeroAxis(_currentRotation.eulerAngles, axis));

        public void SyncFromRotation(Quaternion rotation) =>
            _currentRotation = rotation;

        public Quaternion GetRotation() => _currentRotation;

        public Vector3 GetEulerAngles() => _currentRotation.eulerAngles;
    }
}
