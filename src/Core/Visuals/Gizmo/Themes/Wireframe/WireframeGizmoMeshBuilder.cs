using System.Collections.Generic;
using UnityEngine;

namespace MasterBuilderHeeler.Core.Visuals.Gizmo.Themes.Wireframe
{
    internal static class WireframeGizmoMeshBuilder
    {
        private const float UnitRadius = 1.0f;
        private const int ArcSegments = 72;
        private const int KnobAngleDegrees = 45;
        private const int TickIntervalDegrees = 45;
        private const float KnobArcHalfLength = 0.065f;
        private const float KnobHalfWidth = KnobArcHalfLength * 0.85f;
        private const float TickHalfLength = 0.061f;
        private const float ArrowShaftLength = 0.9f;
        private const float ArrowTipLength = 1.0f;
        private const float ArrowHeadHalfWidth = 0.05f;

        public static Mesh CreateUnitRingMesh(Vector3 normalAxis)
        {
            Mesh mesh = new Mesh
            {
                name = $"MBH_WireframeRing_{normalAxis}",
                hideFlags = HideFlags.HideAndDontSave
            };

            ResolveRingBasis(normalAxis, out Vector3 axisU, out Vector3 axisV);

            // leave a gap at 45 degrees so the ring line and tick dont run through the hollow knob
            // this prevents the penetration. giggity.
            float gapRadians = KnobArcHalfLength / UnitRadius;
            float knobRadians = KnobAngleDegrees * Mathf.Deg2Rad;
            float startAngle = knobRadians + gapRadians;
            float arcSpan = (Mathf.PI * 2f) - (2f * gapRadians);

            List<Vector3> vertices = new List<Vector3>(91);
            List<int> indices = new List<int>(182);

            for (int step = 0; step <= ArcSegments; step++)
            {
                float angle = startAngle + (arcSpan * step / ArcSegments);
                vertices.Add(PointOnRing(axisU, axisV, angle, UnitRadius));

                if (step < ArcSegments)
                {
                    AppendLine(indices, step, step + 1);
                }
            }

            int frontTipIndex = 0;
            int backTipIndex = ArcSegments;
            Vector3 knobCenter = PointOnRing(axisU, axisV, knobRadians, UnitRadius);

            int radialOutIndex = vertices.Count;
            vertices.Add(PointOnRing(axisU, axisV, knobRadians, UnitRadius + KnobHalfWidth));

            int normalPlusIndex = vertices.Count;
            vertices.Add(knobCenter + (normalAxis * KnobHalfWidth));

            int radialInIndex = vertices.Count;
            vertices.Add(PointOnRing(axisU, axisV, knobRadians, UnitRadius - KnobHalfWidth));

            int normalMinusIndex = vertices.Count;
            vertices.Add(knobCenter - (normalAxis * KnobHalfWidth));

            AppendLine(indices, frontTipIndex, radialOutIndex);
            AppendLine(indices, frontTipIndex, normalPlusIndex);
            AppendLine(indices, frontTipIndex, radialInIndex);
            AppendLine(indices, frontTipIndex, normalMinusIndex);

            AppendLine(indices, backTipIndex, radialOutIndex);
            AppendLine(indices, backTipIndex, normalPlusIndex);
            AppendLine(indices, backTipIndex, radialInIndex);
            AppendLine(indices, backTipIndex, normalMinusIndex);

            AppendLine(indices, radialOutIndex, normalPlusIndex);
            AppendLine(indices, normalPlusIndex, radialInIndex);
            AppendLine(indices, radialInIndex, normalMinusIndex);
            AppendLine(indices, normalMinusIndex, radialOutIndex);

            for (int degrees = 0; degrees < 360; degrees += TickIntervalDegrees)
            {
                if (degrees == KnobAngleDegrees)
                {
                    continue;
                }

                float tickRadians = degrees * Mathf.Deg2Rad;
                int innerTickIndex = vertices.Count;
                vertices.Add(PointOnRing(axisU, axisV, tickRadians, UnitRadius - TickHalfLength));

                int outerTickIndex = vertices.Count;
                vertices.Add(PointOnRing(axisU, axisV, tickRadians, UnitRadius + TickHalfLength));

                AppendLine(indices, innerTickIndex, outerTickIndex);
            }

            mesh.SetVertices(vertices);
            mesh.SetIndices(indices, MeshTopology.Lines, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh CreateUnitArrowMesh()
        {
            Mesh mesh = new Mesh
            {
                name = "MBH_WireframeArrow",
                hideFlags = HideFlags.HideAndDontSave
            };

            Vector3 shaftEnd = Vector3.forward * ArrowShaftLength;
            Vector3 tip = Vector3.forward * ArrowTipLength;

            Vector3[] vertices = new Vector3[]
            {
                Vector3.zero,
                shaftEnd,
                tip,
                shaftEnd + (Vector3.left * ArrowHeadHalfWidth),
                shaftEnd + (Vector3.right * ArrowHeadHalfWidth),
                shaftEnd + (Vector3.up * ArrowHeadHalfWidth),
                shaftEnd + (Vector3.down * ArrowHeadHalfWidth)
            };

            int[] indices = new int[]
            {
                0, 1,
                2, 3,
                2, 4,
                2, 5,
                2, 6,
                3, 5,
                5, 4,
                4, 6,
                6, 3
            };

            mesh.vertices = vertices;
            mesh.SetIndices(indices, MeshTopology.Lines, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void ResolveRingBasis(Vector3 normalAxis, out Vector3 axisU, out Vector3 axisV)
        {
            if (normalAxis == Vector3.right)
            {
                axisU = Vector3.up;
                axisV = Vector3.forward;
                return;
            }

            if (normalAxis == Vector3.up)
            {
                axisU = Vector3.right;
                axisV = Vector3.forward;
                return;
            }

            axisU = Vector3.right;
            axisV = Vector3.up;
        }

        private static Vector3 PointOnRing(Vector3 axisU, Vector3 axisV, float angleRadians, float ringRadius) =>
            (axisU * (Mathf.Cos(angleRadians) * ringRadius)) + (axisV * (Mathf.Sin(angleRadians) * ringRadius));

        private static void AppendLine(List<int> indices, int startIndex, int endIndex)
        {
            indices.Add(startIndex);
            indices.Add(endIndex);
        }
    }
}
