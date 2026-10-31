using MasterBuilderHeeler.Core.Placement;
using MasterBuilderHeeler.Core.State;
using UnityEngine;

namespace MasterBuilderHeeler.Core.Rotation.Rotators
{
    public sealed class WorldEulerRotator : IRotator
    {
        private Vector3 _eulerAngles;

        public string Name => nameof(WorldEulerRotator);

        public void Rotate(Vector3 axis, float angle)
        {
            _eulerAngles.x = Mathf.Repeat(_eulerAngles.x + (axis.x * angle), 360f);
            _eulerAngles.y = Mathf.Repeat(_eulerAngles.y + (axis.y * angle), 360f);
            _eulerAngles.z = Mathf.Repeat(_eulerAngles.z + (axis.z * angle), 360f);
        }

        public void ResetRotation() => _eulerAngles = Vector3.zero;

        public void ResetAxis(RotationAxis axis) =>
            _eulerAngles = BuildFrameBasis.ZeroAxis(_eulerAngles, axis);

        public void SyncFromRotation(Quaternion rotation) =>
            _eulerAngles = rotation.eulerAngles;

        public Quaternion GetRotation() => Quaternion.Euler(_eulerAngles);

        public Vector3 GetEulerAngles() => _eulerAngles;
    }
}
