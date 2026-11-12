using HarmonyLib;
using MasterBuilderHeeler.Core.Visuals.Grid;

namespace MasterBuilderHeeler.Patches
{
    [HarmonyPatch(typeof(Heightmap), nameof(Heightmap.RebuildCollisionMesh))]
    public static class HeightmapPatches
    {
        [HarmonyPostfix]
        private static void RebuildCollisionMeshPostfix(Heightmap __instance) =>
            GridOverlayRenderer.MarkTerrainDirty(__instance);
    }
}
