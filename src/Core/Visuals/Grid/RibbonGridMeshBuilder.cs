using MasterBuilderHeeler.Configuration;
using UnityEngine;

namespace MasterBuilderHeeler.Core.Visuals.Grid
{
    // two vertices per point for the two edges of the line and quads between them. the width is in meters so a
    // line gets thinner with distance like anything else lying on the ground
    internal sealed class RibbonGridMeshBuilder : GridMeshBuilder
    {
        public override GridStyle Style => GridStyle.Ribbon;

        protected override MeshTopology Topology => MeshTopology.Triangles;

        protected override void AddFlatPoint(in GridMeshContext context, in GridLine line, float deltaParallel, Color32 color)
        {
            float height = context.BuildSpec.HeightOffset;
            AddVertex(LocalPoint(in line, deltaParallel, -line.HalfWidth, height), color);
            AddVertex(LocalPoint(in line, deltaParallel, line.HalfWidth, height), color);
        }

        protected override void AddTerrainPoint(in GridMeshContext context, in GridLine line, int sampleOffset, Color32 color)
        {
            float deltaParallel = sampleOffset * context.SampleStep;
            AddTerrainEdgeVertex(in context.BuildSpec, in line, deltaParallel, -line.HalfWidth, color);
            AddTerrainEdgeVertex(in context.BuildSpec, in line, deltaParallel, line.HalfWidth, color);
        }

        protected override void AddSegments(int firstVertex, int segmentCount)
        {
            for (int segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
            {
                int nearLeft = firstVertex + (2 * segmentIndex);
                int nearRight = nearLeft + 1;
                int farLeft = nearLeft + 2;
                int farRight = nearLeft + 3;

                Indices.Add(nearLeft);
                Indices.Add(nearRight);
                Indices.Add(farRight);
                Indices.Add(nearLeft);
                Indices.Add(farRight);
                Indices.Add(farLeft);
            }
        }

        // each edge takes its own ground height, so a line on a side slope lies on the slope across its whole width
        private void AddTerrainEdgeVertex(
            in GridBuildSpec buildSpec,
            in GridLine line,
            float deltaParallel,
            float sideOffset,
            Color32 color)
        {
            Vector3 worldPoint = WorldPoint(in buildSpec, in line, deltaParallel, sideOffset);
            worldPoint.y = SampleTerrainY(in buildSpec, worldPoint);
            AddVertex(worldPoint, color);
        }
    }
}
