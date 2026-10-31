using MasterBuilderHeeler.Core.State;
using UnityEngine;

namespace MasterBuilderHeeler.Core.Rotation.Rotators
{
    public interface IRotator
    {
        string Name { get; }

        void Rotate(Vector3 axis, float angle);

        void ResetRotation();

        void ResetAxis(RotationAxis axis);

        void SyncFromRotation(Quaternion rotation);

        Quaternion GetRotation();

        Vector3 GetEulerAngles();
    }
}
