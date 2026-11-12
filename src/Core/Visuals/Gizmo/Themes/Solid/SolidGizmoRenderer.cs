using Jotunn.Managers;
using MasterBuilderHeeler.Configuration;
using UnityEngine;
using UnityEngine.Rendering;

namespace MasterBuilderHeeler.Core.Visuals.Gizmo.Themes.Solid
{
    // lit puts the tubes on the base game piece shader so they get light, shadow and fog like a build piece
    // unlit is a unity shader the world doesnt touch, shaded by a light baked into the meshes from straight overhead
    public sealed class SolidGizmoRenderer : GizmoRendererBase
    {
        // jotunn looks game assets up by file name. Custom/Piece is Assets/Shaders/Piece.shader
        private const string PieceShaderAssetName = "Piece";
        private const string UnlitShaderName = "Hidden/Internal-Colored";
        private const float RingTubeRadius = 0.03f;
        // has to stay under 0.0277. a ring tube has 8 flat sides and the middle of a flat is only 92% of the radius
        // out, so anything thicker pokes its corners through where a shaft passes through a ring
        private const float ArrowRodRadius = 0.026f;
        private const float InactiveBrightness = 0.55f;
        // full strength unlit color goes past what the games bloom and tonemapping can show and washes out to white
        private const float UnlitBrightness = 0.75f;
        // emission as a share of the color. fills in the shaded side so it reads at night, but its the same flat
        // color all over a tube so any more and it stops looking round
        private const float Glow = 0.2f;
        // the piece shader multiplies this by one plus the rgb of the texture. no texture reads as white so it lights
        // with four times this. low because the games anti aliasing flickers on the thin highlight a shiny tube gets
        private const float ActiveGlossiness = 0.1125f;
        private const float InactiveGlossiness = 0.05f;
        // one above the grid overlay like the wireframe gizmo. the game fogs opaque stuff right before this queue and
        // its anti aliasing only does solid geometry, so whats drawn here gets no fog and hard edges that dont flicker
        private const int LateRenderQueue = 3101;
        private const float BakedLightTolerance = 0.000001f;

        private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorPropertyId = Shader.PropertyToID("_EmissionColor");
        private static readonly int GlossinessPropertyId = Shader.PropertyToID("_Glossiness");
        private static readonly Vector3[] RingNormals = { Vector3.right, Vector3.up, Vector3.forward };

        private readonly MaterialPropertyBlock _propertyBlock = new MaterialPropertyBlock();
        private readonly Part[] _rings = { new Part(), new Part(), new Part() };
        private readonly Part[] _arrows = { new Part(), new Part(), new Part() };

        private Material? _litMaterial;
        private Material? _unlitMaterial;
        private bool _litShaderMissing;
        private bool _unlitShaderMissing;

        public override GizmoStyle Style => GizmoStyle.Solid;

        protected override float ActiveRingScaleMultiplier => 1.12f;

        protected override float ArrowLengthScale => 1.5f;

        public override void Dispose()
        {
            DestroyUnityObject(ref _litMaterial);
            DestroyUnityObject(ref _unlitMaterial);
            for (int axisIndex = 0; axisIndex < _rings.Length; axisIndex++)
            {
                DestroyUnityObject(ref _rings[axisIndex].Mesh);
                DestroyUnityObject(ref _arrows[axisIndex].Mesh);
            }
        }

        protected override bool EnsureResources()
        {
            if (!EnsureMaterial())
            {
                return false;
            }

            EnsureMeshes(_rings, "MBH_SolidRing");
            EnsureMeshes(_arrows, "MBH_SolidArrow");
            return true;
        }

        protected override void DrawRing(GizmoAxis axis, Matrix4x4 matrix, float ringScale, Color color, bool isActive)
        {
            // the tube is a fixed thickness in meters so its mesh radius is that over the draw scale
            Part ring = _rings[(int)axis];
            if (UpdateBuildValues(ring, RingTubeRadius / ringScale, ResolveBakedLight(ring, matrix)))
            {
                SolidGizmoMeshBuilder.RebuildRingMesh(ring.Mesh!, RingNormals[(int)axis], ring.TubeRadius, ring.Light);
            }

            if (isActive)
            {
                SetSurface(color, ActiveGlossiness);
            }
            else
            {
                SetSurface(color * InactiveBrightness, InactiveGlossiness);
            }

            Draw(ring.Mesh!, matrix);
        }

        // three arrow meshes and not one because the baked overhead light hits each direction differently
        protected override void DrawArrow(GizmoAxis axis, Matrix4x4 matrix, float arrowScale, Color color)
        {
            Part arrow = _arrows[(int)axis];
            if (UpdateBuildValues(arrow, ArrowRodRadius / arrowScale, ResolveBakedLight(arrow, matrix)))
            {
                SolidGizmoMeshBuilder.RebuildArrowMesh(arrow.Mesh!, arrow.TubeRadius, arrow.Light);
            }

            SetSurface(color, InactiveGlossiness);
            Draw(arrow.Mesh!, matrix);
        }

        private static void EnsureMeshes(Part[] parts, string namePrefix)
        {
            for (int axisIndex = 0; axisIndex < parts.Length; axisIndex++)
            {
                Part part = parts[axisIndex];
                if (!part.Mesh)
                {
                    part.Mesh = SolidGizmoMeshBuilder.CreateMesh($"{namePrefix}_{(GizmoAxis)axisIndex}");
                    // zero never matches a real radius so the new empty mesh gets built
                    part.TubeRadius = 0f;
                }
            }
        }

        // true means the caller has to rebuild. the new values are already stored as built
        private static bool UpdateBuildValues(Part part, float tubeRadius, Vector3 light)
        {
            if (Mathf.Approximately(tubeRadius, part.TubeRadius) && (light - part.Light).sqrMagnitude < BakedLightTolerance)
            {
                return false;
            }

            part.TubeRadius = tubeRadius;
            part.Light = light;
            return true;
        }

        private static void LogMissingShader(string shaderName) =>
            Plugin.Log.LogWarning($"[SolidGizmoRenderer] The shader '{shaderName}' did not load. The Solid gizmo style cannot draw with the current EnableGizmoLighting value.");

        // the baked light sits straight up in the world. in mesh space thats the second row of the matrix rotation
        // a mesh lit by the world doesnt need the bake so it stays as is and turning the gizmo rebuilds nothing
        private Vector3 ResolveBakedLight(Part part, Matrix4x4 matrix) =>
            UseLighting ? part.Light : new Vector3(matrix.m10, matrix.m11, matrix.m12).normalized;

        private bool EnsureMaterial()
        {
            if (UseLighting)
            {
                return _litMaterial || TryCreateLitMaterial();
            }

            return _unlitMaterial || TryCreateUnlitMaterial();
        }

        private bool TryCreateLitMaterial()
        {
            if (_litShaderMissing)
            {
                return false;
            }

            Shader shader = PrefabManager.Cache.GetPrefab<Shader>(PieceShaderAssetName);
            if (!shader)
            {
                _litShaderMissing = true;
                LogMissingShader("Custom/Piece");
                return false;
            }

            // stays in the shaders own queue. the games anti aliasing skips the late one and the edges go hard
            _litMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            // no rain wetness, snow or random brightness on a gizmo
            _litMaterial.SetFloat("_AddRain", 0f);
            _litMaterial.SetInteger("_AddSnow", 0);
            _litMaterial.SetFloat("_ValueNoise", 0f);
            return true;
        }

        private bool TryCreateUnlitMaterial()
        {
            if (_unlitShaderMissing)
            {
                return false;
            }

            Shader shader = Shader.Find(UnlitShaderName);
            if (!shader)
            {
                _unlitShaderMissing = true;
                LogMissingShader(UnlitShaderName);
                return false;
            }

            _unlitMaterial = new Material(shader)
            {
                renderQueue = LateRenderQueue,
                hideFlags = HideFlags.HideAndDontSave
            };
            // this shader writes depth by default so with back faces culled the tubes come out solid
            _unlitMaterial.SetFloat("_Cull", (float)CullMode.Back);
            return true;
        }

        // opaque in this style so the alpha of the configured color isnt used
        private void SetSurface(Color color, float glossiness)
        {
            // unlit has no emission to set. the color is its own light and the baked shade darkens it from there
            Color surfaceColor = UseLighting ? color : color * UnlitBrightness;
            surfaceColor.a = 1f;
            _propertyBlock.SetColor(ColorPropertyId, surfaceColor);
            _propertyBlock.SetColor(EmissionColorPropertyId, color * Glow);
            _propertyBlock.SetFloat(GlossinessPropertyId, glossiness);
        }

        private void Draw(Mesh mesh, Matrix4x4 matrix)
        {
            Material material = UseLighting ? _litMaterial! : _unlitMaterial!;
            Graphics.DrawMesh(mesh, matrix, material, 0, null, 0, _propertyBlock, castShadows: false, receiveShadows: false);
        }

        private sealed class Part
        {
            public Mesh? Mesh;
            public float TubeRadius;
            public Vector3 Light;
        }
    }
}
