using System.Collections.Generic;
using UnityEngine;
using static MasterBuilderHeeler.Core.Visuals.Gizmo.GizmoRingMath;

namespace MasterBuilderHeeler.Core.Visuals.Gizmo.Themes.Solid
{
    // every vertex needs a tangent. the base game piece shader normalizes it and a mesh without one lights wrong
    internal static class SolidGizmoMeshBuilder
    {
        private const float UnitRadius = 1.0f;
        private const int ArcSegments = 72;
        private const int KnobAngleDegrees = 45;
        private const int TickIntervalDegrees = 45;
        private const float KnobArcHalfLength = 0.0975f;
        private const float KnobHalfWidth = KnobArcHalfLength * 0.85f;
        private const float TickHalfLength = 0.0915f;
        private const float ArrowShaftLength = 0.912f;
        private const float ArrowTipLength = 1.0f;
        private const float ArrowHeadHalfWidth = 0.044f;
        private const int TubeSides = 8;
        private const int RingCrossingIntervalDegrees = 90;
        private const float ShadedBrightness = 0.35f;

        private static readonly List<Vector3> Vertices = new List<Vector3>(1024);
        private static readonly List<Vector3> Normals = new List<Vector3>(1024);
        private static readonly List<Vector4> Tangents = new List<Vector4>(1024);
        private static readonly List<Color> Colors = new List<Color>(1024);
        private static readonly List<int> Indices = new List<int>(4096);

        private static Vector3 _lightDirection;

        public static Mesh CreateMesh(string name) => new Mesh
        {
            name = name,
            hideFlags = HideFlags.HideAndDontSave
        };

        public static void RebuildRingMesh(Mesh mesh, Vector3 normalAxis, float tubeRadius, Vector3 lightDirection)
        {
            BeginMesh(lightDirection);
            (Vector3 axisU, Vector3 axisV) = ResolveRingBasis(normalAxis);

            int firstVertex = Vertices.Count;
            for (int step = 0; step <= ArcSegments; step++)
            {
                float angle = Mathf.PI * 2f * step / ArcSegments;
                Vector3 outward = PointOnRing(axisU, axisV, angle, 1f);
                Vector3 along = PointOnRing(axisU, axisV, angle + (Mathf.PI * 0.5f), 1f);
                AddTubeRing(outward * UnitRadius, outward, normalAxis, along, tubeRadius);
            }

            AddTubeQuads(firstVertex, ArcSegments);
            AddKnob(axisU, axisV, normalAxis);

            // no tick where the rings cross. an arrow shaft and the tick of the other ring are the same thickness in
            // the same spot and two surfaces in one place flicker
            for (int degrees = 0; degrees < 360; degrees += TickIntervalDegrees)
            {
                if (degrees != KnobAngleDegrees && degrees % RingCrossingIntervalDegrees != 0)
                {
                    AddTick(axisU, axisV, normalAxis, degrees * Mathf.Deg2Rad, tubeRadius);
                }
            }

            ApplyTo(mesh);
        }

        public static void RebuildArrowMesh(Mesh mesh, float rodRadius, Vector3 lightDirection)
        {
            BeginMesh(lightDirection);

            int firstVertex = Vertices.Count;
            Vector3 shaftEnd = Vector3.forward * ArrowShaftLength;
            AddTubeRing(Vector3.zero, Vector3.right, Vector3.up, Vector3.forward, rodRadius);
            AddTubeRing(shaftEnd, Vector3.right, Vector3.up, Vector3.forward, rodRadius);
            AddTubeQuads(firstVertex, 1);

            Vector3 tip = Vector3.forward * ArrowTipLength;
            Vector3 headCenter = Vector3.Lerp(shaftEnd, tip, 0.25f);
            for (int side = 0; side < TubeSides; side++)
            {
                Vector3 corner = shaftEnd + PointOnRing(Vector3.right, Vector3.up, Mathf.PI * 2f * side / TubeSides, ArrowHeadHalfWidth);
                Vector3 nextCorner = shaftEnd + PointOnRing(Vector3.right, Vector3.up, Mathf.PI * 2f * (side + 1) / TubeSides, ArrowHeadHalfWidth);
                AddFlatTriangle(tip, corner, nextCorner, headCenter);
                AddFlatTriangle(shaftEnd, corner, nextCorner, headCenter);
            }

            ApplyTo(mesh);
        }

        private static void BeginMesh(Vector3 lightDirection)
        {
            _lightDirection = lightDirection;
            Vertices.Clear();
            Normals.Clear();
            Tangents.Clear();
            Colors.Clear();
            Indices.Clear();
        }

        private static void ApplyTo(Mesh mesh)
        {
            mesh.Clear();
            mesh.SetVertices(Vertices);
            mesh.SetNormals(Normals);
            mesh.SetTangents(Tangents);
            mesh.SetColors(Colors);
            mesh.SetTriangles(Indices, 0);
        }

        // one loop of vertices around the tube, along is the way the tube runs
        private static void AddTubeRing(Vector3 center, Vector3 sideA, Vector3 sideB, Vector3 along, float radius)
        {
            for (int side = 0; side <= TubeSides; side++)
            {
                float angle = Mathf.PI * 2f * side / TubeSides;
                Vector3 normal = (sideA * Mathf.Cos(angle)) + (sideB * Mathf.Sin(angle));
                AddVertex(center + (normal * radius), normal, along);
            }
        }

        private static void AddTubeQuads(int firstVertex, int segmentCount)
        {
            int loopSize = TubeSides + 1;
            for (int segment = 0; segment < segmentCount; segment++)
            {
                for (int side = 0; side < TubeSides; side++)
                {
                    int corner = firstVertex + (segment * loopSize) + side;
                    AddQuad(corner, corner + 1, corner + loopSize + 1, corner + loopSize);
                }
            }
        }

        // a diamond with its two tips on the ring
        private static void AddKnob(Vector3 axisU, Vector3 axisV, Vector3 normalAxis)
        {
            float knobRadians = KnobAngleDegrees * Mathf.Deg2Rad;
            float tipRadians = KnobArcHalfLength / UnitRadius;
            Vector3 center = PointOnRing(axisU, axisV, knobRadians, UnitRadius);
            Vector3 radialOut = PointOnRing(axisU, axisV, knobRadians, UnitRadius + KnobHalfWidth);
            Vector3 radialIn = PointOnRing(axisU, axisV, knobRadians, UnitRadius - KnobHalfWidth);
            Vector3 normalPlus = center + (normalAxis * KnobHalfWidth);
            Vector3 normalMinus = center - (normalAxis * KnobHalfWidth);

            AddKnobHalf(center, PointOnRing(axisU, axisV, knobRadians + tipRadians, UnitRadius), radialOut, normalPlus, radialIn, normalMinus);
            AddKnobHalf(center, PointOnRing(axisU, axisV, knobRadians - tipRadians, UnitRadius), radialOut, normalPlus, radialIn, normalMinus);
        }

        private static void AddKnobHalf(Vector3 center, Vector3 tip, Vector3 cornerA, Vector3 cornerB, Vector3 cornerC, Vector3 cornerD)
        {
            AddFlatTriangle(tip, cornerA, cornerB, center);
            AddFlatTriangle(tip, cornerB, cornerC, center);
            AddFlatTriangle(tip, cornerC, cornerD, center);
            AddFlatTriangle(tip, cornerD, cornerA, center);
        }

        private static void AddTick(Vector3 axisU, Vector3 axisV, Vector3 normalAxis, float angle, float halfThickness)
        {
            Vector3 center = PointOnRing(axisU, axisV, angle, UnitRadius);
            Vector3 start = PointOnRing(axisU, axisV, angle, UnitRadius - TickHalfLength);
            Vector3 end = PointOnRing(axisU, axisV, angle, UnitRadius + TickHalfLength);
            Vector3 side = PointOnRing(axisU, axisV, angle + (Mathf.PI * 0.5f), halfThickness);
            Vector3 up = normalAxis * halfThickness;

            AddFlatQuad(start - side - up, start + side - up, end + side - up, end - side - up, center);
            AddFlatQuad(start - side + up, start + side + up, end + side + up, end - side + up, center);
            AddFlatQuad(start - side - up, start - side + up, end - side + up, end - side - up, center);
            AddFlatQuad(start + side - up, start + side + up, end + side + up, end + side - up, center);
            AddFlatQuad(start - side - up, start + side - up, start + side + up, start - side + up, center);
            AddFlatQuad(end - side - up, end + side - up, end + side + up, end - side + up, center);
        }

        // inside is any point inside the shape, the face normal gets pointed away from it
        private static void AddFlatTriangle(Vector3 cornerA, Vector3 cornerB, Vector3 cornerC, Vector3 inside)
        {
            Vector3 normal = ResolveFaceNormal(cornerA, cornerB, cornerC, inside);
            Vector3 along = (cornerB - cornerA).normalized;
            int firstVertex = Vertices.Count;
            AddVertex(cornerA, normal, along);
            AddVertex(cornerB, normal, along);
            AddVertex(cornerC, normal, along);
            AddTriangle(firstVertex, firstVertex + 1, firstVertex + 2);
        }

        private static void AddFlatQuad(Vector3 cornerA, Vector3 cornerB, Vector3 cornerC, Vector3 cornerD, Vector3 inside)
        {
            Vector3 normal = ResolveFaceNormal(cornerA, cornerB, cornerC, inside);
            Vector3 along = (cornerB - cornerA).normalized;
            int firstVertex = Vertices.Count;
            AddVertex(cornerA, normal, along);
            AddVertex(cornerB, normal, along);
            AddVertex(cornerC, normal, along);
            AddVertex(cornerD, normal, along);
            AddQuad(firstVertex, firstVertex + 1, firstVertex + 2, firstVertex + 3);
        }

        private static Vector3 ResolveFaceNormal(Vector3 cornerA, Vector3 cornerB, Vector3 cornerC, Vector3 inside)
        {
            Vector3 normal = Vector3.Cross(cornerB - cornerA, cornerC - cornerA).normalized;
            Vector3 outward = ((cornerA + cornerB + cornerC) / 3f) - inside;
            return Vector3.Dot(normal, outward) < 0f ? -normal : normal;
        }

        private static void AddVertex(Vector3 position, Vector3 normal, Vector3 along)
        {
            Vertices.Add(position);
            Normals.Add(normal);
            Tangents.Add(new Vector4(along.x, along.y, along.z, 1f));

            // the unlit shader multiplies ring color by vertex color so the unlit shading gets baked in here as a gray
            float facing = 0.5f + (0.5f * Vector3.Dot(normal, _lightDirection));
            float shade = Mathf.Lerp(ShadedBrightness, 1f, facing);
            Colors.Add(new Color(shade, shade, shade, 1f));
        }

        private static void AddQuad(int cornerA, int cornerB, int cornerC, int cornerD)
        {
            AddTriangle(cornerA, cornerB, cornerC);
            AddTriangle(cornerA, cornerC, cornerD);
        }

        // the piece shader culls back faces, so each triangle is wound to face the way its normals point
        private static void AddTriangle(int cornerA, int cornerB, int cornerC)
        {
            Vector3 facing = Vector3.Cross(Vertices[cornerB] - Vertices[cornerA], Vertices[cornerC] - Vertices[cornerA]);
            Vector3 normal = Normals[cornerA] + Normals[cornerB] + Normals[cornerC];
            if (Vector3.Dot(facing, normal) < 0f)
            {
                (cornerB, cornerC) = (cornerC, cornerB);
            }

            Indices.Add(cornerA);
            Indices.Add(cornerB);
            Indices.Add(cornerC);
        }
    }
}
