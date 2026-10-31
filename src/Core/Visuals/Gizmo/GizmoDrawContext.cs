using MasterBuilderHeeler.Core.State;
using UnityEngine;

namespace MasterBuilderHeeler.Core.Visuals.Gizmo
{
    public readonly struct GizmoDrawContext
    {
        public readonly Vector3 Center;
        public readonly float Radius;
        public readonly Quaternion RingBasis;
        public readonly RotationAxis ActiveAxes;
        public readonly Vector3 RightAxis;
        public readonly Vector3 UpAxis;
        public readonly Vector3 ForwardAxis;
        public readonly Color PitchRingColor;
        public readonly Color YawRingColor;
        public readonly Color RollRingColor;
        public readonly Color RightArrowColor;
        public readonly Color UpArrowColor;
        public readonly Color ForwardArrowColor;

        public GizmoDrawContext(
            Vector3 center,
            float radius,
            Quaternion ringBasis,
            RotationAxis activeAxes,
            Vector3 rightAxis,
            Vector3 upAxis,
            Vector3 forwardAxis,
            Color pitchRingColor,
            Color yawRingColor,
            Color rollRingColor,
            Color rightArrowColor,
            Color upArrowColor,
            Color forwardArrowColor)
        {
            Center = center;
            Radius = radius;
            RingBasis = ringBasis;
            ActiveAxes = activeAxes;
            RightAxis = rightAxis;
            UpAxis = upAxis;
            ForwardAxis = forwardAxis;
            PitchRingColor = pitchRingColor;
            YawRingColor = yawRingColor;
            RollRingColor = rollRingColor;
            RightArrowColor = rightArrowColor;
            UpArrowColor = upArrowColor;
            ForwardArrowColor = forwardArrowColor;
        }
    }
}
