using MasterBuilderHeeler.Configuration;
using UnityEngine;

namespace MasterBuilderHeeler.Core.Visuals.Gizmo.Themes.Wireframe
{
    public sealed class WireframeGizmoRenderer : GizmoRendererBase
    {
        private const float ActiveRingAlpha = 0.95f;
        private const float InactiveRingAlpha = 0.45f;

        private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");

        private readonly MaterialPropertyBlock _propertyBlock = new MaterialPropertyBlock();

        private Mesh? _xRingMesh;
        private Mesh? _yRingMesh;
        private Mesh? _zRingMesh;
        private Mesh? _arrowMesh;
        private Material? _material;

        public override GizmoStyle Style => GizmoStyle.ProceduralWireframe;

        protected override float ActiveRingScaleMultiplier => 1.12f;

        protected override float ArrowLengthScale => 1.35f;

        public override void Dispose()
        {
            DestroyUnityObject(ref _material);
            DestroyUnityObject(ref _xRingMesh);
            DestroyUnityObject(ref _yRingMesh);
            DestroyUnityObject(ref _zRingMesh);
            DestroyUnityObject(ref _arrowMesh);
        }

        protected override bool EnsureResources()
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
                    return false;
                }

                _material = new Material(shader)
                {
                    // one above the grid overlays 3100 so the gizmo draws on top
                    // was hilarious tho seeing the grid over top the gizmo.
                    renderQueue = 3101,
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            return true;
        }

        protected override void DrawRing(GizmoAxis axis, Matrix4x4 matrix, float ringScale, Color color, bool isActive)
        {
            Mesh? ringMesh = axis switch
            {
                GizmoAxis.X => _xRingMesh,
                GizmoAxis.Y => _yRingMesh,
                _ => _zRingMesh
            };

            color.a *= isActive ? ActiveRingAlpha : InactiveRingAlpha;
            _propertyBlock.SetColor(ColorPropertyId, color);
            Graphics.DrawMesh(ringMesh!, matrix, _material!, 0, null, 0, _propertyBlock);
        }

        // every arrow is the same lines, so one mesh does for all three
        protected override void DrawArrow(GizmoAxis axis, Matrix4x4 matrix, float arrowScale, Color color)
        {
            _propertyBlock.SetColor(ColorPropertyId, color);
            Graphics.DrawMesh(_arrowMesh!, matrix, _material!, 0, null, 0, _propertyBlock);
        }
    }
}
