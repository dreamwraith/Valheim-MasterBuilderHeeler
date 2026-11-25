using UnityEngine;

namespace MasterBuilderHeeler.Utilities
{
    public static class PieceUtils
    {
        public static string GetDisplayName(Piece piece) =>
            string.IsNullOrEmpty(piece.m_name)
                ? Utils.GetPrefabName(piece.gameObject)
                : Localization.instance.Localize(piece.m_name);

        public static string FormatPanelName(Piece piece, bool showName, bool showPrefab)
        {
            string prefabName = Utils.GetPrefabName(piece.gameObject);
            if (!showName)
            {
                return prefabName;
            }

            string displayName = GetDisplayName(piece);
            return showPrefab ? $"{displayName} ({prefabName})" : displayName;
        }

        // writes only what changed. Quaternion != skips rotations under ~0.16 deg so the rotation check is Equals
        // unity also normalizes a rotation on every set, so writing back one that was just read can shift its last bits
        public static void SetPoseIfChanged(Transform targetTransform, Vector3 position, Quaternion rotation)
        {
            if (targetTransform.position != position)
            {
                targetTransform.position = position;
            }

            if (!targetTransform.rotation.Equals(rotation))
            {
                targetTransform.rotation = rotation;
            }
        }
    }
}
