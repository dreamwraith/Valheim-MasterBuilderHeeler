using System.Collections.Generic;
using UnityEngine;

namespace MasterBuilderHeeler.Core.Visuals.Grid
{
    internal sealed class GridTerrainSampler
    {
        private const float ZoneHalfDiagonal = 45.26f;

        private readonly Dictionary<Vector2s, Heightmap> _heightmapsByZone = new Dictionary<Vector2s, Heightmap>(16);

        // a zone heightmap can touch the grid when its center is within the radius plus half a zone diagonal
        public static bool IsWithinReach(Vector3 heightmapCenter, Vector3 focus, float radius)
        {
            float reach = radius + ZoneHalfDiagonal;
            float offsetX = heightmapCenter.x - focus.x;
            float offsetZ = heightmapCenter.z - focus.z;
            return (offsetX * offsetX) + (offsetZ * offsetZ) <= reach * reach;
        }

        // one pass over the games heightmap list per rebuild so every sample after that is just a zone lookup
        public void Prepare(Vector3 focus, float radius)
        {
            _heightmapsByZone.Clear();

            List<Heightmap> heightmaps = Heightmap.GetAllHeightmaps();
            for (int index = 0; index < heightmaps.Count; index++)
            {
                Heightmap heightmap = heightmaps[index];
                Vector3 center = heightmap.transform.position;
                if (IsWithinReach(center, focus, radius))
                {
                    _heightmapsByZone[ZoneSystem.GetZone(center)] = heightmap;
                }
            }
        }

        // drop the heightmap refs after each rebuild so unloaded zones dont stay referenced
        public void Clear() => _heightmapsByZone.Clear();

        public float SampleGroundHeight(float worldX, float worldZ, float fallbackHeight)
        {
            Vector3 worldPosition = new Vector3(worldX, fallbackHeight, worldZ);
            if (TrySampleHeightmapSurface(worldPosition, out float surfaceHeight))
            {
                return surfaceHeight;
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
        //
        // and then the bilinear part turned out to be wrong too. it blends all four corners of a terrain square, but
        // the game builds each square as two flat triangles, so it only matched the ground along the square edges
        // anything sampled inside a square (half meter grids, local grids) could sink into the ground or float over it
        // so now it finds the triangle the point is on and reads the height off the same surface the game draws
        private bool TrySampleHeightmapSurface(Vector3 worldPosition, out float groundHeight)
        {
            if (!_heightmapsByZone.TryGetValue(ZoneSystem.GetZone(worldPosition), out Heightmap heightmap))
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

            // the cut runs south east to north west, so x + z <= 1 is the south west triangle
            float surfaceHeight;
            if (interpolationX + interpolationZ <= 1f)
            {
                surfaceHeight = southWestHeight
                    + ((southEastHeight - southWestHeight) * interpolationX)
                    + ((northWestHeight - southWestHeight) * interpolationZ);
            }
            else
            {
                surfaceHeight = northEastHeight
                    + ((northWestHeight - northEastHeight) * (1f - interpolationX))
                    + ((southEastHeight - northEastHeight) * (1f - interpolationZ));
            }

            groundHeight = heightmapOrigin.y + surfaceHeight;
            return true;
        }
    }
}
