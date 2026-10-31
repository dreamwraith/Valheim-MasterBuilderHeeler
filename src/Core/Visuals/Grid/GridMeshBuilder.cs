using System;
using System.Collections.Generic;
using MasterBuilderHeeler.Core.State;
using UnityEngine;

namespace MasterBuilderHeeler.Core.Visuals.Grid
{
    internal sealed class GridMeshBuilder
    {
        private const float InnerFadeFraction = 0.65f;
        private const float MaxTerrainSampleSpacing = 1.0f;
        private const int MajorLineInterval = 4;
        private const int FlatFadeSegments = 6;
        private const int FlatOuterChordSegments = 8;

        private readonly List<Vector3> _vertices = new List<Vector3>(2048);
        private readonly List<Color32> _colors = new List<Color32>(2048);
        private readonly List<int> _indices = new List<int>(4096);

        private Heightmap? _cachedHeightmap;
        private float[] _sampleHeights = Array.Empty<float>();
        private int[] _sampleStamps = Array.Empty<int>();
        private int _currentSampleStamp;

        public bool HasGeometry => _indices.Count > 0;

        public void RebuildPlaneMesh(Mesh mesh, in GridBuildSpec buildSpec)
        {
            _vertices.Clear();
            _colors.Clear();
            _indices.Clear();

            EmitPlaneLines(in buildSpec);
            // clear after every rebuild so unloaded zones dont stay referenced
            _cachedHeightmap = null;

            mesh.Clear();
            if (_vertices.Count > 0)
            {
                mesh.SetVertices(_vertices);
                mesh.SetColors(_colors);
                mesh.SetIndices(_indices, MeshTopology.Lines, 0);
                mesh.RecalculateBounds();
            }
        }

        private static Color ResolveLineHierarchyColor(Color baseColor, bool isOriginAxis, bool isMajorLine)
        {
            if (isOriginAxis)
            {
                Color originColor = Color.Lerp(baseColor, Color.white, 0.35f);
                originColor.a = Mathf.Clamp01(baseColor.a * 1.15f);
                return originColor;
            }

            if (isMajorLine)
            {
                Color majorColor = Color.Lerp(baseColor, Color.white, 0.18f);
                majorColor.a = baseColor.a;
                return majorColor;
            }

            Color minorColor = baseColor;
            minorColor.a = baseColor.a * 0.55f;
            return minorColor;
        }

        private static Color32 ApplyRadialFade(
            in GridMeshContext context,
            Color lineColor,
            float offsetA,
            float offsetB)
        {
            float distanceSquared = (offsetA * offsetA) + (offsetB * offsetB);
            if (distanceSquared <= context.InnerRadiusSquared)
            {
                return lineColor;
            }

            if (distanceSquared >= context.RadiusSquared)
            {
                lineColor.a = 0f;
                return lineColor;
            }

            float distance = Mathf.Sqrt(distanceSquared);
            float normalizedFade = (context.BuildSpec.Radius - distance) / (context.BuildSpec.Radius - context.InnerRadius);
            float smoothFade = normalizedFade * normalizedFade * (3f - (2f * normalizedFade));
            lineColor.a *= smoothFade;
            return lineColor;
        }

        private void EmitPlaneLines(in GridBuildSpec buildSpec)
        {
            GridMeshContext context = new GridMeshContext(in buildSpec);
            if (buildSpec.FollowTerrain)
            {
                PrepareTerrainSampleCache(context.GridSide * context.GridSide);
            }

            for (int lineOffset = -context.HalfLineCount; lineOffset <= context.HalfLineCount; lineOffset++)
            {
                float deltaPerpendicular = lineOffset * buildSpec.Step;
                float perpendicularSquared = deltaPerpendicular * deltaPerpendicular;
                if (perpendicularSquared >= context.RadiusSquared)
                {
                    continue;
                }

                EmitGridLine(in context, alongZ: true, lineOffset, deltaPerpendicular, perpendicularSquared);
                EmitGridLine(in context, alongZ: false, lineOffset, deltaPerpendicular, perpendicularSquared);
            }
        }

        private void EmitGridLine(
            in GridMeshContext context,
            bool alongZ,
            int lineOffset,
            float deltaPerpendicular,
            float perpendicularSquared)
        {
            ref readonly GridBuildSpec buildSpec = ref context.BuildSpec;
            int gridCell = (alongZ ? buildSpec.FocusCellX : buildSpec.FocusCellZ) + lineOffset;
            float localPerpendicular = (alongZ ? context.CenterLocalX : context.CenterLocalZ) + deltaPerpendicular;
            float centerParallel = alongZ ? context.CenterLocalZ : context.CenterLocalX;

            Color lineColor = ResolveLineHierarchyColor(buildSpec.BaseColor, gridCell == 0, gridCell % MajorLineInterval == 0);
            int firstVertex = _vertices.Count;

            if (buildSpec.FollowTerrain)
            {
                int halfSamples = Mathf.Min(
                    context.MaxHalfSamples,
                    Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(context.RadiusSquared - perpendicularSquared) / context.SampleStep)));
                int lineSampleOffset = lineOffset * context.SubdivisionsPerCell;

                for (int sampleOffset = -halfSamples; sampleOffset <= halfSamples; sampleOffset++)
                {
                    float deltaParallel = sampleOffset * context.SampleStep;
                    float localParallel = centerParallel + deltaParallel;
                    float localX = alongZ ? localPerpendicular : localParallel;
                    float localZ = alongZ ? localParallel : localPerpendicular;

                    Vector3 worldPoint = buildSpec.Origin + (buildSpec.Rotation * new Vector3(localX, 0f, localZ));
                    int sampleX = (alongZ ? lineSampleOffset : sampleOffset) + context.MaxHalfSamples;
                    int sampleZ = (alongZ ? sampleOffset : lineSampleOffset) + context.MaxHalfSamples;
                    worldPoint.y = GetOrSampleTerrainY((sampleX * context.GridSide) + sampleZ, worldPoint.x, worldPoint.z, buildSpec.Origin.y, buildSpec.HeightOffset);

                    _vertices.Add(worldPoint);
                    _colors.Add(ApplyRadialFade(in context, lineColor, deltaPerpendicular, deltaParallel));
                }

                AppendPolylineIndices(firstVertex, 2 * halfSamples);
                return;
            }

            float outerExtent = Mathf.Sqrt(context.RadiusSquared - perpendicularSquared);
            if (perpendicularSquared < context.InnerRadiusSquared)
            {
                // grid fades out toward the edge and alpha only exists on vertices, the gpu blends between them
                // this line crosses the solid middle so only the two faded ends need extra vertices. the middle is one
                // long segment, thats the + 1. fancy grid rendering math go zooooooom
                float innerExtent = Mathf.Sqrt(context.InnerRadiusSquared - perpendicularSquared);
                AppendSegment(in context, alongZ, localPerpendicular, centerParallel, deltaPerpendicular, -outerExtent, -innerExtent, FlatFadeSegments, lineColor);
                AppendSegment(in context, alongZ, localPerpendicular, centerParallel, deltaPerpendicular, innerExtent, outerExtent, FlatFadeSegments, lineColor);
                AppendPolylineIndices(firstVertex, (2 * FlatFadeSegments) + 1);
                return;
            }

            // this line never reaches the solid middle, both ends are on the edge at zero alpha
            // two vertices would blend 0 to 0 and it would never show up, so add some in between to give it alpha. or dont, idc, im not your mom.
            AppendSegment(in context, alongZ, localPerpendicular, centerParallel, deltaPerpendicular, -outerExtent, outerExtent, FlatOuterChordSegments, lineColor);
            AppendPolylineIndices(firstVertex, FlatOuterChordSegments);
        }

        private void AppendSegment(
            in GridMeshContext context,
            bool alongZ,
            float localPerpendicular,
            float centerParallel,
            float deltaPerpendicular,
            float startDelta,
            float endDelta,
            int segmentCount,
            Color lineColor)
        {
            for (int segmentIndex = 0; segmentIndex <= segmentCount; segmentIndex++)
            {
                float deltaParallel = Mathf.Lerp(startDelta, endDelta, segmentIndex / (float)segmentCount);
                float localParallel = centerParallel + deltaParallel;
                float localX = alongZ ? localPerpendicular : localParallel;
                float localZ = alongZ ? localParallel : localPerpendicular;
                _vertices.Add(new Vector3(localX, context.BuildSpec.HeightOffset, localZ));
                _colors.Add(ApplyRadialFade(in context, lineColor, deltaPerpendicular, deltaParallel));
            }
        }

        // bump the stamp each rebuild so shared x/z intersections only sample the heightmap once, no clearing the
        // whole cache array every time the grid moves. when i didnt do this the cpu went zoomies.
        private void PrepareTerrainSampleCache(int requiredCount)
        {
            if (_sampleHeights.Length < requiredCount)
            {
                _sampleHeights = new float[requiredCount];
                _sampleStamps = new int[requiredCount];
            }

            if (_currentSampleStamp == int.MaxValue)
            {
                Array.Clear(_sampleStamps, 0, _sampleStamps.Length);
                _currentSampleStamp = 0;
            }

            _currentSampleStamp++;
        }

        private float GetOrSampleTerrainY(int cacheIndex, float worldX, float worldZ, float fallbackHeight, float heightOffset)
        {
            if (_sampleStamps[cacheIndex] == _currentSampleStamp)
            {
                return _sampleHeights[cacheIndex];
            }

            float terrainY = SampleGroundHeight(worldX, worldZ, fallbackHeight) + heightOffset;
            _sampleStamps[cacheIndex] = _currentSampleStamp;
            _sampleHeights[cacheIndex] = terrainY;
            return terrainY;
        }

        private float SampleGroundHeight(float worldX, float worldZ, float fallbackHeight)
        {
            Vector3 worldPosition = new Vector3(worldX, fallbackHeight, worldZ);
            if (TrySampleHeightmapBilinear(worldPosition, out float interpolatedHeight))
            {
                return interpolatedHeight;
            }

            ZoneSystem zoneSystem = ZoneSystem.instance;
            if (zoneSystem && zoneSystem.GetGroundHeight(worldPosition, out float groundHeight))
            {
                return groundHeight;
            }

            return fallbackHeight;
        }

        // this used to be a simple physx terrain lookup. dumb idea, it cost 10-15 fps even on my 4090+9800x3d lmao
        // bilinear heightmap sampling gets rid of almost all of that and let the max radius double
        // all at the low low cost of a bunch more fucking code. sigh.
        private bool TrySampleHeightmapBilinear(Vector3 worldPosition, out float groundHeight)
        {
            Heightmap? heightmap = _cachedHeightmap;
            if (!heightmap || !heightmap.IsPointInside(worldPosition))
            {
                heightmap = Heightmap.FindHeightmap(worldPosition);
                _cachedHeightmap = heightmap;
            }

            if (!heightmap)
            {
                groundHeight = 0f;
                return false;
            }

            int width = heightmap.m_width;
            int stride = width + 1;
            List<float> heights = heightmap.m_heights;
            if (width <= 0 || heights.Count < stride * stride)
            {
                groundHeight = 0f;
                return false;
            }

            Vector3 heightmapOrigin = heightmap.transform.position;
            float scale = heightmap.m_scale;
            float halfSize = width * scale * 0.5f;
            float clampedX = Mathf.Clamp(((worldPosition.x - heightmapOrigin.x) + halfSize) / scale, 0f, width);
            float clampedZ = Mathf.Clamp(((worldPosition.z - heightmapOrigin.z) + halfSize) / scale, 0f, width);

            int cellColumn = Mathf.Min(Mathf.FloorToInt(clampedX), width - 1);
            int cellRow = Mathf.Min(Mathf.FloorToInt(clampedZ), width - 1);
            float interpolationX = clampedX - cellColumn;
            float interpolationZ = clampedZ - cellRow;

            int southRowOffset = cellRow * stride;
            int northRowOffset = southRowOffset + stride;
            float southWestHeight = heights[southRowOffset + cellColumn];
            float southEastHeight = heights[southRowOffset + cellColumn + 1];
            float northWestHeight = heights[northRowOffset + cellColumn];
            float northEastHeight = heights[northRowOffset + cellColumn + 1];

            float southEdgeHeight = southWestHeight + ((southEastHeight - southWestHeight) * interpolationX);
            float northEdgeHeight = northWestHeight + ((northEastHeight - northWestHeight) * interpolationX);
            groundHeight = heightmapOrigin.y + southEdgeHeight + ((northEdgeHeight - southEdgeHeight) * interpolationZ);
            return true;
        }

        private void AppendPolylineIndices(int startVertexIndex, int segmentCount)
        {
            for (int segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
            {
                int current = startVertexIndex + segmentIndex;
                _indices.Add(current);
                _indices.Add(current + 1);
            }
        }

        internal readonly struct GridBuildSpec
        {
            public readonly GridFrame Frame;
            public readonly GridSurface Surface;
            public readonly float Step;
            public readonly float Radius;
            public readonly float HeightOffset;
            public readonly Color BaseColor;
            public readonly Vector3 Origin;
            public readonly Quaternion Rotation;
            public readonly int FocusCellX;
            public readonly int FocusCellZ;

            public GridBuildSpec(
                GridFrame frame,
                GridSurface surface,
                float step,
                float radius,
                float heightOffset,
                Color baseColor,
                Vector3 origin,
                Quaternion rotation,
                int focusCellX,
                int focusCellZ)
            {
                Frame = frame;
                Surface = surface;
                Step = step;
                Radius = radius;
                HeightOffset = heightOffset;
                BaseColor = baseColor;
                Origin = origin;
                Rotation = rotation;
                FocusCellX = focusCellX;
                FocusCellZ = focusCellZ;
            }

            public bool FollowTerrain => Surface == GridSurface.Terrain;
        }

        private readonly struct GridMeshContext
        {
            public readonly GridBuildSpec BuildSpec;
            public readonly int HalfLineCount;
            public readonly int SubdivisionsPerCell;
            public readonly float SampleStep;
            public readonly float CenterLocalX;
            public readonly float CenterLocalZ;
            public readonly float RadiusSquared;
            public readonly float InnerRadius;
            public readonly float InnerRadiusSquared;
            public readonly int MaxHalfSamples;
            public readonly int GridSide;

            public GridMeshContext(in GridBuildSpec buildSpec)
            {
                BuildSpec = buildSpec;
                HalfLineCount = Mathf.Max(1, Mathf.CeilToInt(buildSpec.Radius / buildSpec.Step));
                SubdivisionsPerCell = Mathf.Max(1, Mathf.RoundToInt(buildSpec.Step / MaxTerrainSampleSpacing));
                SampleStep = buildSpec.Step / SubdivisionsPerCell;
                CenterLocalX = buildSpec.FocusCellX * buildSpec.Step;
                CenterLocalZ = buildSpec.FocusCellZ * buildSpec.Step;
                RadiusSquared = buildSpec.Radius * buildSpec.Radius;
                InnerRadius = buildSpec.Radius * InnerFadeFraction;
                InnerRadiusSquared = InnerRadius * InnerRadius;

                if (buildSpec.FollowTerrain)
                {
                    MaxHalfSamples = Mathf.Max(HalfLineCount * SubdivisionsPerCell, Mathf.CeilToInt(buildSpec.Radius / SampleStep));
                    GridSide = (2 * MaxHalfSamples) + 1;
                }
                else
                {
                    MaxHalfSamples = 0;
                    GridSide = 0;
                }
            }
        }
    }
}
