using MasterBuilderHeeler.Configuration;
using MasterBuilderHeeler.Core.State;
using UnityEngine;

namespace MasterBuilderHeeler.Core.Visuals.Gizmo.Themes.Wireframe
{
    public sealed class WireframeGizmoRenderer : IGizmoRenderer
    {
        private const float ActiveRingAlpha = 0.95f;
        private const float InactiveRingAlpha = 0.45f;
        private const float ActiveRingScaleMultiplier = 1.12f;
        private const float ArrowLengthScale = 1.35f;

        private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");

        private readonly MaterialPropertyBlock _propertyBlock = new MaterialPropertyBlock();

        private Mesh? _xRingMesh;
        private Mesh? _yRingMesh;
        private Mesh? _zRingMesh;
        private Mesh? _arrowMesh;
        private Material? _material;

        public GizmoStyle Style => GizmoStyle.ProceduralWireframe;

        public void Render(in GizmoDrawContext context)
        {
            EnsureResources();
            if (!_material || !_xRingMesh || !_yRingMesh || !_zRingMesh || !_arrowMesh)
            {
                return;
            }

            DrawAxisRing(in context, _xRingMesh, _material, RotationAxis.Pitch, context.PitchRingColor);
            DrawAxisRing(in context, _yRingMesh, _material, RotationAxis.Yaw, context.YawRingColor);
            DrawAxisRing(in context, _zRingMesh, _material, RotationAxis.Roll, context.RollRingColor);

            float arrowScale = context.Radius * ArrowLengthScale;
            DrawArrow(_arrowMesh, _material, context.Center, context.RightAxis, context.RightArrowColor, arrowScale);
            DrawArrow(_arrowMesh, _material, context.Center, context.UpAxis, context.UpArrowColor, arrowScale);
            DrawArrow(_arrowMesh, _material, context.Center, context.ForwardAxis, context.ForwardArrowColor, arrowScale);
        }

        public void OnHide()
        {
        }

        public void Dispose()
        {
            DestroyUnityObject(ref _material);
            DestroyUnityObject(ref _xRingMesh);
            DestroyUnityObject(ref _yRingMesh);
            DestroyUnityObject(ref _zRingMesh);
            DestroyUnityObject(ref _arrowMesh);
        }

        private static void DestroyUnityObject<T>(ref T? unityObject) where T : Object
        {
            if (unityObject)
            {
                Object.Destroy(unityObject);
                unityObject = null;
            }
        }

        private void EnsureResources()
        {
            if (!_xRingMesh)
            {
                _xRingMesh = WireframeGizmoMeshBuilder.CreateUnitRingMesh(Vector3.right);
            }

            if (!_yRingMesh)
            {
                _yRingMesh = WireframeGizmoMeshBuilder.CreateUnitRingMesh(Vector3.up);
            }

            if (!_zRingMesh)
            {
                _zRingMesh = WireframeGizmoMeshBuilder.CreateUnitRingMesh(Vector3.forward);
            }

            if (!_arrowMesh)
            {
                _arrowMesh = WireframeGizmoMeshBuilder.CreateUnitArrowMesh();
            }

            if (!_material)
            {
                Shader? shader = OverlayShaderUtils.FindUnlitVertexColorShader();
                if (!shader)
                {
                    return;
                }

                _material = new Material(shader)
                {
                    // one above the grid overlays 3100 so the gizmo draws on top
                    // was hilarious tho seeing the grid over top the gizmo.
                    renderQueue = 3101,
                    hideFlags = HideFlags.HideAndDontSave
                };
            }
        }

        private void DrawAxisRing(
            in GizmoDrawContext context,
            Mesh ringMesh,
            Material material,
            RotationAxis ringAxis,
            Color baseColor)
        {
            bool isActive = (context.ActiveAxes & ringAxis) != 0;
            Color ringColor = baseColor;
            ringColor.a *= isActive ? ActiveRingAlpha : InactiveRingAlpha;

            float ringScale = context.Radius * (isActive ? ActiveRingScaleMultiplier : 1.0f);
            _propertyBlock.SetColor(ColorPropertyId, ringColor);
            Matrix4x4 matrix = Matrix4x4.TRS(context.Center, context.RingBasis, Vector3.one * ringScale);
            Graphics.DrawMesh(ringMesh, matrix, material, 0, null, 0, _propertyBlock);
        }

        private void DrawArrow(
            Mesh arrowMesh,
            Material material,
            Vector3 origin,
            Vector3 direction,
            Color color,
            float scale)
        {
            _propertyBlock.SetColor(ColorPropertyId, color);
            Quaternion rotation = direction.sqrMagnitude > 0.0001f
                ? Quaternion.FromToRotation(Vector3.forward, direction)
                : Quaternion.identity;
            Matrix4x4 matrix = Matrix4x4.TRS(origin, rotation, Vector3.one * scale);
            Graphics.DrawMesh(arrowMesh, matrix, material, 0, null, 0, _propertyBlock);
        }
    }
}
