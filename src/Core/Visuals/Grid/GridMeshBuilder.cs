using System.Collections.Generic;
using MasterBuilderHeeler.Configuration;
using MasterBuilderHeeler.Core.State;
using UnityEngine;

namespace MasterBuilderHeeler.Core.Visuals.Grid
{
    // walks the grid lines and hands each point to the style builder. the style only decides what a point and a
    // segment turn into and everything else about the grid lives here
    internal abstract class GridMeshBuilder
    {
        private const float InnerFadeFraction = 0.65f;
        private const float MaxTerrainSampleSpacing = 1.0f;
        private const int MajorLineInterval = 4;
        private const int FlatFadeSegments = 6;
        private const int FlatOuterChordSegments = 8;

        private readonly List<Vector3> _vertices = new List<Vector3>(2048);
        private readonly List<Color32> _colors = new List<Color32>(2048);
        private readonly GridTerrainSampler _terrainSampler = new GridTerrainSampler();

        public abstract GridStyle Style { get; }

        public bool HasGeometry => Indices.Count > 0;

        protected abstract MeshTopology Topology { get; }

        protected List<int> Indices { get; } = new List<int>(4096);

        public static GridMeshBuilder Create(GridStyle style) => style switch
        {
            GridStyle.Ribbon => new RibbonGridMeshBuilder(),
            _ => new WireframeGridMeshBuilder()
        };

        public void RebuildPlaneMesh(Mesh mesh, in GridBuildSpec buildSpec)
        {
            _vertices.Clear();
            _colors.Clear();
            Indices.Clear();

            EmitPlaneLines(in buildSpec);
            _terrainSampler.Clear();

            mesh.Clear();
            if (_vertices.Count > 0)
            {
                mesh.SetVertices(_vertices);
                mesh.SetColors(_colors);
                mesh.SetIndices(Indices, Topology, 0);
                mesh.RecalculateBounds();
            }
        }

        protected static Vector3 LocalPoint(in GridLine line, float deltaParallel, float sideOffset, float height)
        {
            float localPerpendicular = line.LocalPerpendicular + sideOffset;
            float localParallel = line.CenterParallel + deltaParallel;
            return line.AlongZ
                ? new Vector3(localPerpendicular, height, localParallel)
                : new Vector3(localParallel, height, localPerpendicular);
        }

        // the same point in the world. y still has to come from the terrain
        protected static Vector3 WorldPoint(in GridBuildSpec buildSpec, in GridLine line, float deltaParallel, float sideOffset) =>
            buildSpec.Origin + (buildSpec.Rotation * LocalPoint(in line, deltaParallel, sideOffset, 0f));

        // one point of a flat line in the grids local space
        protected abstract void AddFlatPoint(in GridMeshContext context, in GridLine line, float deltaParallel, Color32 color);

        // one point of a terrain line in world space
        protected abstract void AddTerrainPoint(in GridMeshContext context, in GridLine line, int sampleOffset, Color32 color);

        protected abstract void AddSegments(int firstVertex, int segmentCount);

        protected virtual void OnBuildStarted(in GridMeshContext context)
        {
        }

        protected void AddVertex(Vector3 position, Color32 color)
        {
            _vertices.Add(position);
            _colors.Add(color);
        }

        protected float SampleTerrainY(in GridBuildSpec buildSpec, Vector3 worldPoint) =>
            _terrainSampler.SampleGroundHeight(worldPoint.x, worldPoint.z, buildSpec.Origin.y) + buildSpec.HeightOffset;

        private static GridLineClass ResolveLineClass(int gridCell)
        {
            if (gridCell == 0)
            {
                return GridLineClass.Origin;
            }

            return gridCell % MajorLineInterval == 0 ? GridLineClass.Major : GridLineClass.Minor;
        }

        private static Color ResolveLineColor(Color baseColor, GridLineClass lineClass)
        {
            if (lineClass == GridLineClass.Origin)
            {
                Color originColor = Color.Lerp(baseColor, Color.white, 0.35f);
                originColor.a = Mathf.Clamp01(baseColor.a * 1.15f);
                return originColor;
            }

            if (lineClass == GridLineClass.Major)
            {
                Color majorColor = Color.Lerp(baseColor, Color.white, 0.18f);
                majorColor.a = baseColor.a;
                return majorColor;
            }

            Color minorColor = baseColor;
            minorColor.a = baseColor.a * 0.55f;
            return minorColor;
        }

        private static float ResolveLineWidth(GridLineWidths lineWidths, GridLineClass lineClass) => lineClass switch
        {
            GridLineClass.Origin => lineWidths.Origin,
            GridLineClass.Major => lineWidths.Major,
            _ => lineWidths.Minor
        };

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
                _terrainSampler.Prepare(buildSpec.FocusWorldPosition, context.SampleReach);
            }

            OnBuildStarted(in context);

            // minor lines go in first and origin lines last, so where two lines cross the bigger one is drawn on top
            EmitLineClass(in context, GridLineClass.Minor);
            EmitLineClass(in context, GridLineClass.Major);
            EmitLineClass(in context, GridLineClass.Origin);
        }

        private void EmitLineClass(in GridMeshContext context, GridLineClass lineClass)
        {
            for (int lineOffset = -context.HalfLineCount; lineOffset <= context.HalfLineCount; lineOffset++)
            {
                float deltaPerpendicular = lineOffset * context.BuildSpec.Step;
                if (deltaPerpendicular * deltaPerpendicular >= context.RadiusSquared)
                {
                    continue;
                }

                EmitGridLine(in context, lineClass, alongZ: true, lineOffset, deltaPerpendicular);
                EmitGridLine(in context, lineClass, alongZ: false, lineOffset, deltaPerpendicular);
            }
        }

        private void EmitGridLine(
            in GridMeshContext context,
            GridLineClass lineClass,
            bool alongZ,
            int lineOffset,
            float deltaPerpendicular)
        {
            ref readonly GridBuildSpec buildSpec = ref context.BuildSpec;
            int gridCell = (alongZ ? buildSpec.FocusCellX : buildSpec.FocusCellZ) + lineOffset;
            if (ResolveLineClass(gridCell) != lineClass)
            {
                return;
            }

            GridLine line = new GridLine(
                alongZ,
                lineOffset,
                deltaPerpendicular,
                (alongZ ? context.CenterLocalX : context.CenterLocalZ) + deltaPerpendicular,
                alongZ ? context.CenterLocalZ : context.CenterLocalX,
                ResolveLineColor(buildSpec.BaseColor, lineClass),
                0.5f * ResolveLineWidth(buildSpec.LineWidths, lineClass));

            if (buildSpec.FollowTerrain)
            {
                EmitTerrainLine(in context, in line);
            }
            else
            {
                EmitFlatLine(in context, in line);
            }
        }

        private void EmitTerrainLine(in GridMeshContext context, in GridLine line)
        {
            float perpendicularSquared = line.DeltaPerpendicular * line.DeltaPerpendicular;
            int halfSamples = Mathf.Min(
                context.MaxHalfSamples,
                Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(context.RadiusSquared - perpendicularSquared) / context.SampleStep)));
            int firstVertex = _vertices.Count;

            for (int sampleOffset = -halfSamples; sampleOffset <= halfSamples; sampleOffset++)
            {
                Color32 color = ApplyRadialFade(in context, line.Color, line.DeltaPerpendicular, sampleOffset * context.SampleStep);
                AddTerrainPoint(in context, in line, sampleOffset, color);
            }

            AddSegments(firstVertex, 2 * halfSamples);
        }

        private void EmitFlatLine(in GridMeshContext context, in GridLine line)
        {
            float perpendicularSquared = line.DeltaPerpendicular * line.DeltaPerpendicular;
            float outerExtent = Mathf.Sqrt(context.RadiusSquared - perpendicularSquared);
            int firstVertex = _vertices.Count;

            if (perpendicularSquared < context.InnerRadiusSquared)
            {
                // grid fades out toward the edge and alpha only exists on vertices, the gpu blends between them
                // this line crosses the solid middle so only the two faded ends need extra vertices. the middle is one
                // long segment, thats the + 1. fancy grid rendering math go zooooooom
                float innerExtent = Mathf.Sqrt(context.InnerRadiusSquared - perpendicularSquared);
                AddFlatPoints(in context, in line, -outerExtent, -innerExtent, FlatFadeSegments);
                AddFlatPoints(in context, in line, innerExtent, outerExtent, FlatFadeSegments);
                AddSegments(firstVertex, (2 * FlatFadeSegments) + 1);
                return;
            }

            // this line never reaches the solid middle, both ends are on the edge at zero alpha
            // two vertices would blend 0 to 0 and it would never show up, so add some in between to give it alpha. or dont, idc, im not your mom.
            AddFlatPoints(in context, in line, -outerExtent, outerExtent, FlatOuterChordSegments);
            AddSegments(firstVertex, FlatOuterChordSegments);
        }

        private void AddFlatPoints(
            in GridMeshContext context,
            in GridLine line,
            float startDelta,
            float endDelta,
            int segmentCount)
        {
            for (int segmentIndex = 0; segmentIndex <= segmentCount; segmentIndex++)
            {
                float deltaParallel = Mathf.Lerp(startDelta, endDelta, segmentIndex / (float)segmentCount);
                Color32 color = ApplyRadialFade(in context, line.Color, line.DeltaPerpendicular, deltaParallel);
                AddFlatPoint(in context, in line, deltaParallel, color);
            }
        }

        protected enum GridLineClass
        {
            Minor,
            Major,
            Origin
        }

        // line widths in meters. only the ribbon style draws them
        internal record struct GridLineWidths(float Minor, float Major, float Origin)
        {
            public readonly float Widest => Mathf.Max(Minor, Mathf.Max(Major, Origin));
        }

        internal readonly struct GridBuildSpec
        {
            public readonly GridFrame Frame;
            public readonly GridSurface Surface;
            public readonly GridStyle Style;
            public readonly float Step;
            public readonly float Radius;
            public readonly float HeightOffset;
            public readonly Color BaseColor;
            public readonly GridLineWidths LineWidths;
            public readonly Vector3 Origin;
            public readonly Quaternion Rotation;
            public readonly int FocusCellX;
            public readonly int FocusCellZ;

            public GridBuildSpec(
                GridFrame frame,
                GridSurface surface,
                GridStyle style,
                float step,
                float radius,
                float heightOffset,
                Color baseColor,
                GridLineWidths lineWidths,
                Vector3 origin,
                Quaternion rotation,
                int focusCellX,
                int focusCellZ)
            {
                Frame = frame;
                Surface = surface;
                Style = style;
                Step = step;
                Radius = radius;
                HeightOffset = heightOffset;
                BaseColor = baseColor;
                LineWidths = lineWidths;
                Origin = origin;
                Rotation = rotation;
                FocusCellX = focusCellX;
                FocusCellZ = focusCellZ;
            }

            public bool FollowTerrain => Surface == GridSurface.Terrain;

            public Vector3 FocusWorldPosition => Origin + (Rotation * new Vector3(FocusCellX * Step, 0f, FocusCellZ * Step));
        }

        // one grid line. the perpendicular values place the line and the parallel one is where its middle sits
        protected record struct GridLine(
            bool AlongZ,
            int LineOffset,
            float DeltaPerpendicular,
            float LocalPerpendicular,
            float CenterParallel,
            Color Color,
            float HalfWidth);

        protected readonly struct GridMeshContext
        {
            public readonly GridBuildSpec BuildSpec;
            public readonly int HalfLineCount;
            public readonly int SubdivisionsPerCell;
            public readonly float SampleStep;
            public readonly float SampleReach;
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
                // a line can run one sample past the radius and a ribbon edge sits half a width further out
                SampleReach = buildSpec.Radius + SampleStep + (0.5f * buildSpec.LineWidths.Widest);
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
