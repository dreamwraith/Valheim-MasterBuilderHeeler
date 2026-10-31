namespace MasterBuilderHeeler.Core.UI
{
    public static class HudGlyphs
    {
        public const string ToggleActive = "\u25c8";
        public const string ToggleInactive = "\u25c7";
        public const string Heart = "\u2661";
        public const string House = "\u2616";
        public const string CircledBullet = "\u29bf";
        public const string Command = "\u2318";
        public const string SquareCrosshatch = "\u25a5";
        public const string GridOrthogonal = "\u25a6";
        public const string GridDiagonal = "\u25a9";
        public const string SurfaceTerrain = "\u2248";
        public const string SurfaceFlat = "\uff1d";
        public const string ArrowRight = "\u27a1";
        public const string ThinSpace = "\u2009";
        public const string KeycapBracketLeft = "\u3014";
        public const string KeycapBracketRight = "\u3015";
        public const string MeasureBracketLeft = "\u3010";
        public const string MeasureBracketRight = "\u3011";
        public const string RawPipe = "|";
        public const string RawBrokenBar = "\u00a6";
        public const string Degree = "\u00b0";

        public const string TriangleUpSolid = "\u25b2";
        public const string TriangleDownSolid = "\u25bc";
        public const string TriangleLeftSolid = "\u25c0";
        public const string TriangleRightSolid = "\u25b6";

        public const string TriangleUpHollow = "\u25b3";
        public const string TriangleDownHollow = "\u25bd";
        public const string TriangleLeftHollow = "\u25c1";
        public const string TriangleRightHollow = "\u25b7";

        public const string CircleBullseye = "\u25c9";
        public const string CircleHollow = "\u25cb";

        public const string RawKeycapOpen = KeycapBracketLeft + ThinSpace;
        public const string RawKeycapClose = ThinSpace + KeycapBracketRight;
        public const string RawMeasureOpen = MeasureBracketLeft + ThinSpace;
        public const string RawMeasureClose = ThinSpace + MeasureBracketRight;

        public const string YawActive = TriangleLeftSolid + TriangleRightSolid;
        public const string YawInactive = TriangleLeftHollow + TriangleRightHollow;
        public const string PitchActive = TriangleUpSolid + TriangleDownSolid;
        public const string PitchInactive = TriangleUpHollow + TriangleDownHollow;
    }
}
