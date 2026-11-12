using MasterBuilderHeeler.Configuration;
using MasterBuilderHeeler.Core.State;
using MasterBuilderHeeler.Utilities;
using UnityEngine;
using UnityEngine.Rendering;

namespace MasterBuilderHeeler.Core.Visuals.Grid
{
    public sealed class GridOverlayRenderer : MonoBehaviour
    {
        private static GridOverlayRenderer? _instance;

        private GridMeshBuilder _meshBuilder = GridMeshBuilder.Create(GridStyle.Wireframe);
        private Mesh? _gridMesh;
        private Material? _material;
        private GridMeshBuilder.GridBuildSpec _builtSpec;
        private bool _hasValidMesh;

        private void Awake()
        {
            _instance = this;
        }

        private void LateUpdate()
        {
            if (!ModConfig.General.EnableMod.Value || !BuildStateManager.IsGridOverlayVisible)
            {
                _hasValidMesh = false;
                return;
            }

            Player player = Player.m_localPlayer;
            if (!player)
            {
                _hasValidMesh = false;
                return;
            }

            bool hasBuildContext = player.InPlaceMode() || BuildStateManager.HasActiveSpecialMode;
            if (!hasBuildContext)
            {
                _hasValidMesh = false;
                return;
            }

            GridFrame activeFrame = BuildStateManager.ActiveGridFrame;
            GridSurface activeSurface = BuildStateManager.ActiveGridSurface;
            float step = BuildStateManager.GridCellSize;
            float radius = Mathf.Clamp(ModConfig.Build.GridOverlayRadius.Value, 4.0f, 64.0f);
            float heightOffset = ResolveHeightOffset(activeFrame, activeSurface);
            // the game renders in linear and vertex colors reach the shader as is, so a picked srgb color shows
            // lighter without this
            Color baseColor = (activeFrame == GridFrame.Local
                ? ModConfig.Build.LocalGridColor.Value
                : ModConfig.Build.WorldGridColor.Value).linear;
            GridMeshBuilder.GridLineWidths lineWidths = new GridMeshBuilder.GridLineWidths(
                ModConfig.Build.RibbonGridMinorWidth.Value,
                ModConfig.Build.RibbonGridMajorWidth.Value,
                ModConfig.Build.RibbonGridOriginWidth.Value);

            Vector3 origin = activeFrame == GridFrame.Local
                ? BuildStateManager.LocalGridOrigin
                : new Vector3(0f, player.transform.position.y, 0f);
            Quaternion rotation = activeFrame == GridFrame.Local
                ? BuildStateManager.LocalGridRotation
                : Quaternion.identity;

            Vector3 focusPosition = ResolveFocusPosition(player);
            Vector3 localFocus = Quaternion.Inverse(rotation) * (focusPosition - origin);
            int focusCellX = Mathf.RoundToInt(localFocus.x / step);
            int focusCellZ = Mathf.RoundToInt(localFocus.z / step);

            GridMeshBuilder.GridBuildSpec buildSpec = new GridMeshBuilder.GridBuildSpec(
                activeFrame,
                activeSurface,
                ModConfig.Build.GridOverlayStyle.Value,
                step,
                radius,
                heightOffset,
                baseColor,
                lineWidths,
                origin,
                rotation,
                focusCellX,
                focusCellZ);

            if (NeedsRebuild(in buildSpec))
            {
                RebuildMesh(in buildSpec);
            }

            if (_gridMesh && _meshBuilder.HasGeometry)
            {
                Material? material = GetOrCreateMaterial();
                if (!material)
                {
                    return;
                }

                Matrix4x4 drawMatrix = buildSpec.FollowTerrain
                    ? Matrix4x4.identity
                    : Matrix4x4.TRS(origin, rotation, Vector3.one);
                Graphics.DrawMesh(_gridMesh, drawMatrix, material, 0);
            }
        }

        private void OnDestroy()
        {
            ReleaseResources();

            if (ReferenceEquals(_instance, this))
            {
                _instance = null;
            }
        }

        public static void EnsureRenderer(Player player)
        {
            if (!player.TryGetComponent(out GridOverlayRenderer _))
            {
                player.gameObject.AddComponent<GridOverlayRenderer>();
            }
        }

        public static void Cleanup()
        {
            if (_instance)
            {
                _instance.ReleaseResources();
                Destroy(_instance);
                _instance = null;
            }
        }

        // zone loads, height edits and terrain data from other players all end in a collision mesh rebuild, so a
        // terrain grid picks up new ground without polling. distant lod tiles arent sampled so they change nothing here
        public static void MarkTerrainDirty(Heightmap heightmap)
        {
            if (!_instance || !_instance._hasValidMesh || !_instance._builtSpec.FollowTerrain || heightmap.IsDistantLod)
            {
                return;
            }

            GridMeshBuilder.GridBuildSpec builtSpec = _instance._builtSpec;
            if (GridTerrainSampler.IsWithinReach(heightmap.transform.position, builtSpec.FocusWorldPosition, builtSpec.Radius))
            {
                _instance._hasValidMesh = false;
            }
        }

        private static Vector3 ResolveFocusPosition(Player player)
        {
            if (BuildStateManager.IsBuildCameraActive)
            {
                UnityEngine.Camera? activeCamera = CameraUtils.GetActiveCamera();
                if (activeCamera)
                {
                    return activeCamera.transform.position;
                }
            }

            return player.transform.position;
        }

        private static float ResolveHeightOffset(GridFrame frame, GridSurface surface)
        {
            if (surface == GridSurface.Terrain)
            {
                return ModConfig.Build.TerrainGridHeight.Value;
            }

            return frame == GridFrame.Local
                ? ModConfig.Build.LocalFlatGridHeight.Value
                : ModConfig.Build.WorldFlatGridHeight.Value;
        }

        private bool NeedsRebuild(in GridMeshBuilder.GridBuildSpec buildSpec)
        {
            if (!_hasValidMesh ||
                !_gridMesh ||
                buildSpec.Frame != _builtSpec.Frame ||
                buildSpec.Surface != _builtSpec.Surface ||
                buildSpec.Style != _builtSpec.Style ||
                buildSpec.LineWidths != _builtSpec.LineWidths ||
                !Mathf.Approximately(buildSpec.Step, _builtSpec.Step) ||
                !Mathf.Approximately(buildSpec.Radius, _builtSpec.Radius) ||
                !Mathf.Approximately(buildSpec.HeightOffset, _builtSpec.HeightOffset) ||
                buildSpec.BaseColor != _builtSpec.BaseColor ||
                buildSpec.FocusCellX != _builtSpec.FocusCellX ||
                buildSpec.FocusCellZ != _builtSpec.FocusCellZ)
            {
                return true;
            }

            // world grids follow player y every frame but heightmap y overwrites it anyway, so only local terrain grids
            // check Origin. terrain edits dont show up in the spec so MarkTerrainDirty catches those
            return buildSpec.FollowTerrain &&
                buildSpec.Frame == GridFrame.Local &&
                ((buildSpec.Origin - _builtSpec.Origin).sqrMagnitude > 0.0001f ||
                 Quaternion.Angle(buildSpec.Rotation, _builtSpec.Rotation) > 0.01f);
        }

        private void RebuildMesh(in GridMeshBuilder.GridBuildSpec buildSpec)
        {
            if (!_gridMesh)
            {
                _gridMesh = new Mesh
                {
                    name = "MBH_GridOverlayMesh",
                    indexFormat = IndexFormat.UInt32,
                    hideFlags = HideFlags.HideAndDontSave
                };
                _gridMesh.MarkDynamic();
            }

            if (_meshBuilder.Style != buildSpec.Style)
            {
                _meshBuilder = GridMeshBuilder.Create(buildSpec.Style);
            }

            _meshBuilder.RebuildPlaneMesh(_gridMesh, in buildSpec);
            _builtSpec = buildSpec;
            _hasValidMesh = true;
        }

        private Material? GetOrCreateMaterial()
        {
            if (!_material)
            {
                Shader? shader = OverlayShaderUtils.FindUnlitVertexColorShader();
                if (!shader)
                {
                    return null;
                }

                _material = new Material(shader)
                {
                    color = Color.white,
                    renderQueue = 3100,
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            return _material;
        }

        private void ReleaseResources()
        {
            if (_gridMesh)
            {
                Destroy(_gridMesh);
                _gridMesh = null;
            }

            if (_material)
            {
                Destroy(_material);
                _material = null;
            }

            _hasValidMesh = false;
        }
    }
}
