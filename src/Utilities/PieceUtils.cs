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
    }
}
