using System;
using MasterBuilderHeeler.Configuration;
using UnityEngine;

namespace MasterBuilderHeeler.Core.Visuals.Grid
{
    // one vertex per point with hardware lines between them, so a line is the same hair width at any distance
    internal sealed class WireframeGridMeshBuilder : GridMeshBuilder
    {
        private float[] _sampleHeights = Array.Empty<float>();
        private int[] _sampleStamps = Array.Empty<int>();
        private int _currentSampleStamp;

        public override GridStyle Style => GridStyle.Wireframe;

        protected override MeshTopology Topology => MeshTopology.Lines;

        protected override void OnBuildStarted(in GridMeshContext context)
        {
            if (context.BuildSpec.FollowTerrain)
            {
                PrepareTerrainSampleCache(context.GridSide * context.GridSide);
            }
        }

        protected override void AddFlatPoint(in GridMeshContext context, in GridLine line, float deltaParallel, Color32 color) =>
            AddVertex(LocalPoint(in line, deltaParallel, 0f, context.BuildSpec.HeightOffset), color);

        protected override void AddTerrainPoint(in GridMeshContext context, in GridLine line, int sampleOffset, Color32 color)
        {
            ref readonly GridBuildSpec buildSpec = ref context.BuildSpec;
            Vector3 worldPoint = WorldPoint(in buildSpec, in line, sampleOffset * context.SampleStep, 0f);

            int lineSampleOffset = line.LineOffset * context.SubdivisionsPerCell;
            int sampleX = (line.AlongZ ? lineSampleOffset : sampleOffset) + context.MaxHalfSamples;
            int sampleZ = (line.AlongZ ? sampleOffset : lineSampleOffset) + context.MaxHalfSamples;
            worldPoint.y = GetOrSampleTerrainY((sampleX * context.GridSide) + sampleZ, in buildSpec, worldPoint);

            AddVertex(worldPoint, color);
        }

        protected override void AddSegments(int firstVertex, int segmentCount)
        {
            for (int segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
            {
                int current = firstVertex + segmentIndex;
                Indices.Add(current);
                Indices.Add(current + 1);
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

        private float GetOrSampleTerrainY(int cacheIndex, in GridBuildSpec buildSpec, Vector3 worldPoint)
        {
            if (_sampleStamps[cacheIndex] == _currentSampleStamp)
            {
                return _sampleHeights[cacheIndex];
            }

            float terrainY = SampleTerrainY(in buildSpec, worldPoint);
            _sampleStamps[cacheIndex] = _currentSampleStamp;
            _sampleHeights[cacheIndex] = terrainY;
            return terrainY;
        }
    }
}
