using System;
using MasterBuilderHeeler.Core.State;
using UnityEngine;

namespace MasterBuilderHeeler.Core.Placement
{
    public static class BuildFrameBasis
    {
        // why the fuck iron gate made roofs work like this i dont understand. thanks to comfy gizmo for the hints
        // roof mode rotations carry an extra 45 degrees of yaw, standard frame stuff multiplies by the inverse first
        public static readonly Quaternion CornerRoofBaseRotation = Quaternion.Euler(0f, 45f, 0f);
        public static readonly Quaternion InverseCornerRoofRotation = Quaternion.Euler(0f, -45f, 0f);

        public static bool IsCornerRoofPrefab(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName))
            {
                return false;
            }

            return prefabName.IndexOf("roof", StringComparison.OrdinalIgnoreCase) >= 0 &&
                   prefabName.IndexOf("corner", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static Quaternion ResolveGizmoBasis(Quaternion pieceRotation) =>
            BuildStateManager.IsRoofModeEffective
                ? pieceRotation * InverseCornerRoofRotation
                : pieceRotation;

        public static (Vector3 RightAxis, Vector3 UpAxis, Vector3 ForwardAxis) ResolveTranslationAxes(Quaternion pieceRotation)
        {
            if (BuildStateManager.ActiveTranslationFrame == TranslationFrame.Local)
            {
                Quaternion localBasis = ResolveGizmoBasis(pieceRotation);
                return (localBasis * Vector3.right, localBasis * Vector3.up, localBasis * Vector3.forward);
            }

            return (Vector3.right, Vector3.up, Vector3.forward);
        }

        public static Vector3 ZeroAxis(Vector3 angles, RotationAxis axis)
        {
            if ((axis & RotationAxis.Pitch) != 0)
            {
                angles.x = 0f;
            }

            if ((axis & RotationAxis.Yaw) != 0)
            {
                angles.y = 0f;
            }

            if ((axis & RotationAxis.Roll) != 0)
            {
                angles.z = 0f;
            }

            return angles;
        }
    }
}
