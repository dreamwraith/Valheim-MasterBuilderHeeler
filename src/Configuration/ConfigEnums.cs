namespace MasterBuilderHeeler.Configuration
{
    public enum ModLogLevel
    {
        None,
        Info,
        Debug
    }

    public enum PrecisionCopyMode
    {
        SideBySide,
        OverrideNative,
        DisableNative,
        DisableCustom
    }

    public enum GizmoStyle
    {
        ProceduralWireframe,
        Solid,
        Disabled
    }

    public enum GridStyle
    {
        Wireframe,
        Ribbon
    }

    public enum KeymapDisplayMode
    {
        CurrentInput,
        KeyboardOnly,
        GamepadOnly,
        BothAlways
    }

    // the Global panel rows that show a key. a name is used as the setting name and with spaces added as the row label
    public enum GlobalKeymapRow
    {
        Precision,
        BuildCamera,
        FreezeGhost,
        SamplePiece,
        SampleRotation,
        SampleBoth,
        EditPiece,
        CommitEdit,
        CancelEdit,
        GridSnap,
        GridOverlay,
        GridFrame,
        GridSurface,
        GridSize
    }

    // same for the Precision panel. Nudge covers all three nudge rows
    public enum PrecisionKeymapRow
    {
        StepPreset,
        AdjustStep,
        ResetStep,
        RotationFrame,
        RoofMode,
        Rotate,
        YawAxis,
        PitchAxis,
        RollAxis,
        ResetAxis,
        ResetAllAxes,
        NudgeFrame,
        Nudge,
        ResetNudge,
        ResetSettings
    }
}
