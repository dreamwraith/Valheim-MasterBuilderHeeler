using MasterBuilderHeeler.Core.State;
using UnityEngine;

namespace MasterBuilderHeeler.Core.Visuals.Gizmo
{
    public readonly struct GizmoDrawContext
    {
        public readonly Vector3 Center;
        public readonly float Radius;
        // the gizmo scale setting. Radius doesnt include it so a style multiplies the two
        public readonly float Scale;
        public readonly Quaternion PitchRingBasis;
        public readonly Quaternion YawRingBasis;
        public readonly Quaternion RollRingBasis;
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
        // off draws the gizmo with its own shading and no light, shadow or fog from the world. a style that has
        // nothing to light ignores it
        public readonly bool UseLighting;

        public GizmoDrawContext(
            Vector3 center,
            float radius,
            float scale,
            Quaternion pitchRingBasis,
            Quaternion yawRingBasis,
            Quaternion rollRingBasis,
            RotationAxis activeAxes,
            Vector3 rightAxis,
            Vector3 upAxis,
            Vector3 forwardAxis,
            Color pitchRingColor,
            Color yawRingColor,
            Color rollRingColor,
            Color rightArrowColor,
            Color upArrowColor,
            Color forwardArrowColor,
            bool useLighting)
        {
            Center = center;
            Radius = radius;
            Scale = scale;
            PitchRingBasis = pitchRingBasis;
            YawRingBasis = yawRingBasis;
            RollRingBasis = rollRingBasis;
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
            UseLighting = useLighting;
        }
    }
}
