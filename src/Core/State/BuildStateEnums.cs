using System;

namespace MasterBuilderHeeler.Core.State
{
    public enum PlacementEditState
    {
        Normal,
        GhostFrozen,
        InPlaceEdit
    }

    public enum StepPreset
    {
        Fine,
        Standard,
        Coarse
    }

    public enum TranslationFrame
    {
        Local,
        World
    }

    public enum RotationFrame
    {
        World,
        Local
    }

    [Flags]
    public enum RotationAxis
    {
        None = 0,
        Yaw = 1 << 0,
        Pitch = 1 << 1,
        Roll = 1 << 2
    }

    public enum GridFrame
    {
        World,
        Local
    }

    public enum GridSurface
    {
        Terrain,
        Flat
    }
}
