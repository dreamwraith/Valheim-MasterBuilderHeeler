using MasterBuilderHeeler.Configuration;
using MasterBuilderHeeler.Core.State;
using UnityEngine;

namespace MasterBuilderHeeler.Core.Visuals.Gizmo
{
    // lays the gizmo out as three rings around the piece and three arrows along its axes. a style decides what a ring
    // and an arrow are made of and how big they are, so there are no sizes in here
    public abstract class GizmoRendererBase : IGizmoRenderer
    {
        public abstract GizmoStyle Style { get; }

        protected abstract float ActiveRingScaleMultiplier { get; }

        // arrow length in ring radii
        protected abstract float ArrowLengthScale { get; }

        // the lighting setting of the frame being drawn. a style reads it while it draws in places the draw context
        // doesnt reach, so a copy is kept here
        protected bool UseLighting { get; private set; }

        public void Render(in GizmoDrawContext context)
        {
            UseLighting = context.UseLighting;
            if (!EnsureResources())
            {
                return;
            }

            DrawAxisRing(in context, GizmoAxis.X, RotationAxis.Pitch, context.PitchRingBasis, context.PitchRingColor);
            DrawAxisRing(in context, GizmoAxis.Y, RotationAxis.Yaw, context.YawRingBasis, context.YawRingColor);
            DrawAxisRing(in context, GizmoAxis.Z, RotationAxis.Roll, context.RollRingBasis, context.RollRingColor);

            float arrowScale = context.Radius * context.Scale * ArrowLengthScale;
            DrawAxisArrow(GizmoAxis.X, context.Center, context.RightAxis, context.RightArrowColor, arrowScale);
            DrawAxisArrow(GizmoAxis.Y, context.Center, context.UpAxis, context.UpArrowColor, arrowScale);
            DrawAxisArrow(GizmoAxis.Z, context.Center, context.ForwardAxis, context.ForwardArrowColor, arrowScale);
        }

        public virtual void OnHide()
        {
        }

        public abstract void Dispose();

        protected static void DestroyUnityObject<T>(ref T? unityObject) where T : Object
        {
            if (unityObject)
            {
                Object.Destroy(unityObject);
                unityObject = null;
            }
        }

        protected abstract bool EnsureResources();

        // the scale is already in the matrix. it is passed as well for a style that sizes something in meters
        protected abstract void DrawRing(GizmoAxis axis, Matrix4x4 matrix, float ringScale, Color color, bool isActive);

        protected abstract void DrawArrow(GizmoAxis axis, Matrix4x4 matrix, float arrowScale, Color color);

        private void DrawAxisRing(
            in GizmoDrawContext context,
            GizmoAxis axis,
            RotationAxis rotationAxis,
            Quaternion ringBasis,
            Color color)
        {
            bool isActive = (context.ActiveAxes & rotationAxis) != 0;
            float ringScale = context.Radius * context.Scale * (isActive ? ActiveRingScaleMultiplier : 1.0f);
            Matrix4x4 matrix = Matrix4x4.TRS(context.Center, ringBasis, Vector3.one * ringScale);
            DrawRing(axis, matrix, ringScale, color, isActive);
        }

        private void DrawAxisArrow(GizmoAxis axis, Vector3 origin, Vector3 direction, Color color, float arrowScale)
        {
            Quaternion rotation = direction.sqrMagnitude > 0.0001f
                ? Quaternion.FromToRotation(Vector3.forward, direction)
                : Quaternion.identity;
            Matrix4x4 matrix = Matrix4x4.TRS(origin, rotation, Vector3.one * arrowScale);
            DrawArrow(axis, matrix, arrowScale, color);
        }
    }
}
