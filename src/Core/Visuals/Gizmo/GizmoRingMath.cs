using UnityEngine;

namespace MasterBuilderHeeler.Core.Visuals.Gizmo
{
    // ring math that every style uses. sizes stay out of here because every style keeps its own and gets tuned alone
    internal static class GizmoRingMath
    {
        // the two axes a ring is drawn in when it turns around normalAxis
        public static (Vector3 AxisU, Vector3 AxisV) ResolveRingBasis(Vector3 normalAxis)
        {
            if (normalAxis == Vector3.right)
            {
                return (Vector3.up, Vector3.forward);
            }

            if (normalAxis == Vector3.up)
            {
                return (Vector3.right, Vector3.forward);
            }

            return (Vector3.right, Vector3.up);
        }

        public static Vector3 PointOnRing(Vector3 axisU, Vector3 axisV, float angleRadians, float ringRadius) =>
            (axisU * (Mathf.Cos(angleRadians) * ringRadius)) + (axisV * (Mathf.Sin(angleRadians) * ringRadius));
    }
}
