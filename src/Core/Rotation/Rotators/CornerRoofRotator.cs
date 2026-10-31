using MasterBuilderHeeler.Core.Placement;
using MasterBuilderHeeler.Core.State;
using UnityEngine;

namespace MasterBuilderHeeler.Core.Rotation.Rotators
{
    public sealed class CornerRoofRotator : IRotator
    {
        private Quaternion _roofRotation = BuildFrameBasis.CornerRoofBaseRotation;

        public string Name => nameof(CornerRoofRotator);

        public void Rotate(Vector3 axis, float angle)
        {
            // _roofRotation ends in the 45 degree base so pull the axis through the inverse. same result as a local rotate
            // on the standard basis with the base stuck on the end. still hate all this code just for roofs. fml
            Vector3 tiltedAxis = BuildFrameBasis.InverseCornerRoofRotation * axis;
            _roofRotation = Quaternion.Normalize(_roofRotation * Quaternion.AngleAxis(angle, tiltedAxis));
        }

        public void ResetRotation() =>
            _roofRotation = BuildFrameBasis.CornerRoofBaseRotation;

        public void ResetAxis(RotationAxis axis)
        {
            Vector3 tiltedEuler = BuildFrameBasis.ZeroAxis(
                (_roofRotation * BuildFrameBasis.InverseCornerRoofRotation).eulerAngles,
                axis);
            _roofRotation = Quaternion.Normalize(
                Quaternion.Euler(tiltedEuler) * BuildFrameBasis.CornerRoofBaseRotation);
        }

        public void SyncFromRotation(Quaternion rotation) =>
            _roofRotation = rotation;

        public Quaternion GetRotation() => _roofRotation;

        public Vector3 GetEulerAngles() => _roofRotation.eulerAngles;
    }
}
