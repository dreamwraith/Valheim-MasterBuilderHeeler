using System;
using MasterBuilderHeeler.Configuration;
using MasterBuilderHeeler.Core.Sampling;
using MasterBuilderHeeler.Core.State;
using MasterBuilderHeeler.Utilities;
using UnityEngine;

namespace MasterBuilderHeeler.Core.UI.Panels
{
    public sealed class GlobalInfoPanel : HudPanelBase
    {
        private readonly HudLabelRow _precisionRow;
        private readonly HudLabelRow _cameraRow;
        private readonly HudLabelRow _freezeRow;
        private readonly HudLabelRow _sampleRow;
        private readonly HudLabelRow _sampleRotationRow;
        private readonly HudLabelRow _sampleBothRow;
        private readonly HudLabelRow _editPieceRow;
        private readonly HudLabelRow _commitEditRow;
        private readonly HudLabelRow _cancelEditRow;
        private readonly HudLabelRow _gridSnapRow;
        private readonly HudLabelRow _gridOverlayRow;
        private readonly HudLabelRow _gridFrameRow;
        private readonly HudLabelRow _gridSurfaceRow;
        private readonly HudLabelRow _gridSizeRow;
        private readonly HudLabelRow[] _precisionRows;
        private readonly HudLabelRow[] _gridRows;

        private PanelStateSnapshot _lastSnapshot;
        private bool _hasValidSnapshot;
        private int _preseededKeybindVersion = -1;

        public GlobalInfoPanel(Transform parentTransform)
            : base(parentTransform, "MBH_GlobalInfoPanel", ModConfig.GlobalPanelLayout)
        {
            Panel.SetFloatingHeader("Global");

            _precisionRow = Panel.AddRow("Precision:");
            _cameraRow = Panel.AddRow("Build Camera:");
            _freezeRow = Panel.AddRow("Freeze Ghost:");
            _sampleRow = Panel.AddRow("Sample Piece:");
            _sampleRotationRow = Panel.AddRow("Sample Rotation:");
            _sampleBothRow = Panel.AddRow("Sample Both:");
            _editPieceRow = Panel.AddRow("Edit Piece:");
            _commitEditRow = Panel.AddRow("Commit Edit:");
            _cancelEditRow = Panel.AddRow("Cancel Edit:");
            _gridSnapRow = Panel.AddRow("Grid Snap:");
            _gridOverlayRow = Panel.AddRow("Grid Overlay:");
            _gridFrameRow = Panel.AddRow("Grid Frame:");
            _gridSurfaceRow = Panel.AddRow("Grid Surface:");
            _gridSizeRow = Panel.AddRow("Grid Size:");

            _precisionRows = new[]
            {
                _precisionRow,
                _freezeRow,
                _sampleRow,
                _sampleRotationRow,
                _sampleBothRow,
                _editPieceRow,
                _commitEditRow,
                _cancelEditRow
            };
            _gridRows = new[] { _gridSnapRow, _gridOverlayRow, _gridFrameRow, _gridSurfaceRow, _gridSizeRow };

            Panel.SetAnchors(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f));

            ApplyFeatureLayout();
            ModConfig.EnablePrecision.SettingChanged += OnFeatureSettingChanged;
            ModConfig.EnableBuildCamera.SettingChanged += OnFeatureSettingChanged;
            ModConfig.EnableGrid.SettingChanged += OnFeatureSettingChanged;
            Panel.SetActive(false);
        }

        public void Refresh(Player localPlayer)
        {
            bool hasEnabledFeature = ModConfig.EnablePrecision.Value ||
                ModConfig.EnableBuildCamera.Value ||
                ModConfig.EnableGrid.Value;
            if (!ModConfig.GlobalPanelLayout.Show.Value || !hasEnabledFeature)
            {
                SetActive(false);
                return;
            }

            bool inSpecialMode = BuildStateManager.HasActiveSpecialMode;
            bool hasBuildTool = localPlayer.InPlaceMode();
            if (!inSpecialMode && !hasBuildTool)
            {
                SetActive(false);
                return;
            }

            Panel.SetActive(true);

            int keybindVersion = HudKeybindFormatter.KeybindVersion;
            if (_preseededKeybindVersion != keybindVersion)
            {
                Panel.ResetClampedWidth();
                PreseedConstantWidth();
            }

            PanelStateSnapshot snapshot = new PanelStateSnapshot(
                BuildStateManager.IsBuildCameraActive,
                BuildStateManager.EditState,
                BuildStateManager.IsPrecisionModeActive,
                BuildStateManager.IsGridSnapActive,
                BuildStateManager.IsGridOverlayVisible,
                BuildStateManager.ActiveGridFrame,
                BuildStateManager.ActiveGridSurface,
                BuildStateManager.GridCellSize,
                PieceEyedropper.IsCustomSamplingAvailable(),
                keybindVersion,
                HudTheme.ThemeVersion);

            if (_hasValidSnapshot && snapshot.Equals(_lastSnapshot))
            {
                return;
            }

            _lastSnapshot = snapshot;
            _hasValidSnapshot = true;

            string precisionKey = KeyLabelFormatter.Format(KeyboardMouseConfig.TogglePrecisionModeKey);
            string precisionStatus = snapshot.PrecisionOn ? HudTheme.StatusOn : HudTheme.StatusOff;
            _precisionRow.SetValue($"{HudTheme.Keycap(precisionKey)} {precisionStatus}");

            string cameraKey = KeyLabelFormatter.Format(KeyboardMouseConfig.ToggleBuildCameraKey);
            string cameraStatus = snapshot.IsBuildCameraActive ? HudTheme.StatusBuildCameraOn : HudTheme.StatusOff;
            _cameraRow.SetValue($"{HudTheme.Keycap(cameraKey)} {cameraStatus}");

            string freezeKey = KeyLabelFormatter.Format(KeyboardMouseConfig.ToggleGhostFreezeKey);
            string freezeStatus = snapshot.EditState == PlacementEditState.GhostFrozen ? HudTheme.StatusGhostFreezeOn : HudTheme.StatusOff;
            _freezeRow.SetValue($"{HudTheme.Keycap(freezeKey)} {freezeStatus}");
            _freezeRow.SetActive(snapshot.PrecisionOn);

            string sampleKey = KeyLabelFormatter.Format(KeyboardMouseConfig.SamplePieceKey);
            _sampleRow.SetValue(HudTheme.Keycap(sampleKey));

            string sampleRotationKey = KeyLabelFormatter.Format(KeyboardMouseConfig.MatchPieceRotationKey);
            _sampleRotationRow.SetValue(HudTheme.Keycap(sampleRotationKey));

            string sampleBothKey = KeyLabelFormatter.Format(KeyboardMouseConfig.SamplePieceAndRotationKey);
            _sampleBothRow.SetValue(HudTheme.Keycap(sampleBothKey));

            _sampleRow.SetActive(snapshot.CustomSamplingAvailable);
            _sampleRotationRow.SetActive(snapshot.CustomSamplingAvailable);
            _sampleBothRow.SetActive(snapshot.CustomSamplingAvailable);

            bool inPlaceEdit = snapshot.EditState == PlacementEditState.InPlaceEdit;
            string pieceEditKey = KeyLabelFormatter.Format(KeyboardMouseConfig.TogglePieceEditKey);
            string editPieceStatus = inPlaceEdit ? HudTheme.StatusOn : HudTheme.StatusOff;
            _editPieceRow.SetValue($"{HudTheme.Keycap(pieceEditKey)} {editPieceStatus}");
            _editPieceRow.SetActive(snapshot.PrecisionOn);

            _commitEditRow.SetActive(inPlaceEdit);
            _cancelEditRow.SetActive(inPlaceEdit);

            if (inPlaceEdit)
            {
                string commitKey = KeyLabelFormatter.Format(KeyboardMouseConfig.CommitPieceEditKey);
                string cancelKey = KeyLabelFormatter.Format(KeyboardMouseConfig.CancelPieceEditKey);

                _commitEditRow.SetValue(HudTheme.Keycap($"{commitKey}{HudTheme.PipeSeparator}{pieceEditKey}"));
                _cancelEditRow.SetValue(HudTheme.Keycap(cancelKey));
            }

            string gridSnapKey = KeyLabelFormatter.Format(KeyboardMouseConfig.ToggleGridSnapKey);
            string gridSnapStatus = snapshot.GridSnapOn ? HudTheme.StatusOn : HudTheme.StatusOff;
            _gridSnapRow.SetValue($"{HudTheme.Keycap(gridSnapKey)} {gridSnapStatus}");

            string gridOverlayKey = KeyLabelFormatter.Format(KeyboardMouseConfig.ToggleGridOverlayKey);
            string gridOverlayStatus = snapshot.GridOverlayOn ? HudTheme.StatusOn : HudTheme.StatusOff;
            _gridOverlayRow.SetValue($"{HudTheme.Keycap(gridOverlayKey)} {gridOverlayStatus}");

            string localGridKey = KeyLabelFormatter.Format(KeyboardMouseConfig.ToggleLocalGridKey);
            string gridFrameStatus = snapshot.ActiveGridFrame == GridFrame.Local
                ? HudTheme.StatusGridLocal
                : HudTheme.StatusGridWorld;
            _gridFrameRow.SetValue($"{HudTheme.Keycap(localGridKey)} {gridFrameStatus}");

            string gridSurfaceKey = KeyLabelFormatter.Format(KeyboardMouseConfig.ToggleGridSurfaceKey);
            string gridSurfaceStatus = snapshot.ActiveGridSurface == GridSurface.Flat
                ? HudTheme.StatusGridFlat
                : HudTheme.StatusGridTerrain;
            _gridSurfaceRow.SetValue($"{HudTheme.Keycap(gridSurfaceKey)} {gridSurfaceStatus}");

            string cycleGridKey = KeyLabelFormatter.Format(KeyboardMouseConfig.CycleGridSizeKey);
            _gridSizeRow.SetValue($"{HudTheme.Keycap(cycleGridKey)} <color={HudTheme.SecondaryValueHex}>{HudTheme.Measure($"{snapshot.GridCellSize:F1}m")}</color>");

            _gridFrameRow.SetActive(snapshot.GridOverlayOn);
            _gridSurfaceRow.SetActive(snapshot.GridOverlayOn);
            _gridSizeRow.SetActive(snapshot.GridOverlayOn);

            Panel.UpdateClampedWidth();
        }

        public override void SetActive(bool active)
        {
            if (!active)
            {
                _hasValidSnapshot = false;
            }

            base.SetActive(active);
        }

        public override void Destroy()
        {
            ModConfig.EnablePrecision.SettingChanged -= OnFeatureSettingChanged;
            ModConfig.EnableBuildCamera.SettingChanged -= OnFeatureSettingChanged;
            ModConfig.EnableGrid.SettingChanged -= OnFeatureSettingChanged;
            base.Destroy();
        }

        protected override void OnFontSizeChanged()
        {
            _hasValidSnapshot = false;
            PreseedConstantWidth();
        }

        private static void SetRowsActive(HudLabelRow[] rows, bool active)
        {
            for (int index = 0; index < rows.Length; index++)
            {
                rows[index].SetActive(active);
            }
        }

        private void OnFeatureSettingChanged(object sender, EventArgs eventArgs) =>
            ApplyFeatureLayout();

        private void ApplyFeatureLayout()
        {
            SetRowsActive(_precisionRows, ModConfig.EnablePrecision.Value);
            _cameraRow.SetActive(ModConfig.EnableBuildCamera.Value);
            SetRowsActive(_gridRows, ModConfig.EnableGrid.Value);

            Panel.ResetClampedWidth();
            PreseedConstantWidth();
            _hasValidSnapshot = false;
        }

        private void ObserveRowLabels(HudLabelRow[] rows)
        {
            for (int index = 0; index < rows.Length; index++)
            {
                Panel.ObserveSampleWidths(rows[index].Label.text, string.Empty);
            }
        }

        // measures the widest text each row can show so the panel width stays put when values change
        // no fucking jitter please damnit ffs. how many times i gotta teach the same lesson.
        private void PreseedConstantWidth()
        {
            _preseededKeybindVersion = HudKeybindFormatter.KeybindVersion;

            if (ModConfig.EnablePrecision.Value)
            {
                ObserveRowLabels(_precisionRows);

                string precisionKey = KeyLabelFormatter.Format(KeyboardMouseConfig.TogglePrecisionModeKey);
                string freezeKey = KeyLabelFormatter.Format(KeyboardMouseConfig.ToggleGhostFreezeKey);
                string sampleKey = KeyLabelFormatter.Format(KeyboardMouseConfig.SamplePieceKey);
                string sampleRotationKey = KeyLabelFormatter.Format(KeyboardMouseConfig.MatchPieceRotationKey);
                string sampleBothKey = KeyLabelFormatter.Format(KeyboardMouseConfig.SamplePieceAndRotationKey);
                string pieceEditKey = KeyLabelFormatter.Format(KeyboardMouseConfig.TogglePieceEditKey);
                string commitKey = KeyLabelFormatter.Format(KeyboardMouseConfig.CommitPieceEditKey);
                string cancelKey = KeyLabelFormatter.Format(KeyboardMouseConfig.CancelPieceEditKey);

                Panel.ObserveSampleWidths(string.Empty, $"{HudTheme.Keycap(precisionKey)} {HudTheme.StatusOff}");
                Panel.ObserveSampleWidths(string.Empty, $"{HudTheme.Keycap(freezeKey)} {HudTheme.StatusOff}");
                Panel.ObserveSampleWidths(string.Empty, HudTheme.Keycap(sampleKey));
                Panel.ObserveSampleWidths(string.Empty, HudTheme.Keycap(sampleRotationKey));
                Panel.ObserveSampleWidths(string.Empty, HudTheme.Keycap(sampleBothKey));
                Panel.ObserveSampleWidths(string.Empty, $"{HudTheme.Keycap(pieceEditKey)} {HudTheme.StatusOff}");
                Panel.ObserveSampleWidths(string.Empty, HudTheme.Keycap($"{commitKey}{HudTheme.PipeSeparator}{pieceEditKey}"));
                Panel.ObserveSampleWidths(string.Empty, HudTheme.Keycap(cancelKey));
            }

            if (ModConfig.EnableBuildCamera.Value)
            {
                string cameraKey = KeyLabelFormatter.Format(KeyboardMouseConfig.ToggleBuildCameraKey);
                Panel.ObserveSampleWidths(_cameraRow.Label.text, $"{HudTheme.Keycap(cameraKey)} {HudTheme.StatusOff}");
            }

            if (ModConfig.EnableGrid.Value)
            {
                ObserveRowLabels(_gridRows);

                string gridSnapKey = KeyLabelFormatter.Format(KeyboardMouseConfig.ToggleGridSnapKey);
                string gridOverlayKey = KeyLabelFormatter.Format(KeyboardMouseConfig.ToggleGridOverlayKey);
                string localGridKey = KeyLabelFormatter.Format(KeyboardMouseConfig.ToggleLocalGridKey);
                string gridSurfaceKey = KeyLabelFormatter.Format(KeyboardMouseConfig.ToggleGridSurfaceKey);
                string cycleGridKey = KeyLabelFormatter.Format(KeyboardMouseConfig.CycleGridSizeKey);

                Panel.ObserveSampleWidths(string.Empty, $"{HudTheme.Keycap(gridSnapKey)} {HudTheme.StatusOff}");
                Panel.ObserveSampleWidths(string.Empty, $"{HudTheme.Keycap(gridOverlayKey)} {HudTheme.StatusOff}");
                Panel.ObserveSampleWidths(string.Empty, $"{HudTheme.Keycap(localGridKey)} {HudTheme.StatusGridWorld}");
                Panel.ObserveSampleWidths(string.Empty, $"{HudTheme.Keycap(localGridKey)} {HudTheme.StatusGridLocal}");
                Panel.ObserveSampleWidths(string.Empty, $"{HudTheme.Keycap(gridSurfaceKey)} {HudTheme.StatusGridTerrain}");
                Panel.ObserveSampleWidths(string.Empty, $"{HudTheme.Keycap(gridSurfaceKey)} {HudTheme.StatusGridFlat}");
                Panel.ObserveSampleWidths(string.Empty, $"{HudTheme.Keycap(cycleGridKey)} <color={HudTheme.SecondaryValueHex}>{HudTheme.Measure("4.0m")}</color>");
            }
        }

        private readonly struct PanelStateSnapshot : IEquatable<PanelStateSnapshot>
        {
            public readonly bool IsBuildCameraActive;
            public readonly PlacementEditState EditState;
            public readonly bool PrecisionOn;
            public readonly bool GridSnapOn;
            public readonly bool GridOverlayOn;
            public readonly GridFrame ActiveGridFrame;
            public readonly GridSurface ActiveGridSurface;
            public readonly float GridCellSize;
            public readonly bool CustomSamplingAvailable;
            public readonly int KeybindVersion;
            public readonly int ThemeVersion;

            public PanelStateSnapshot(
                bool isBuildCameraActive,
                PlacementEditState editState,
                bool precisionOn,
                bool gridSnapOn,
                bool gridOverlayOn,
                GridFrame activeGridFrame,
                GridSurface activeGridSurface,
                float gridCellSize,
                bool customSamplingAvailable,
                int keybindVersion,
                int themeVersion)
            {
                IsBuildCameraActive = isBuildCameraActive;
                EditState = editState;
                PrecisionOn = precisionOn;
                GridSnapOn = gridSnapOn;
                GridOverlayOn = gridOverlayOn;
                ActiveGridFrame = activeGridFrame;
                ActiveGridSurface = activeGridSurface;
                GridCellSize = gridCellSize;
                CustomSamplingAvailable = customSamplingAvailable;
                KeybindVersion = keybindVersion;
                ThemeVersion = themeVersion;
            }

            public bool Equals(PanelStateSnapshot other) =>
                IsBuildCameraActive == other.IsBuildCameraActive &&
                EditState == other.EditState &&
                PrecisionOn == other.PrecisionOn &&
                GridSnapOn == other.GridSnapOn &&
                GridOverlayOn == other.GridOverlayOn &&
                ActiveGridFrame == other.ActiveGridFrame &&
                ActiveGridSurface == other.ActiveGridSurface &&
                Mathf.Approximately(GridCellSize, other.GridCellSize) &&
                CustomSamplingAvailable == other.CustomSamplingAvailable &&
                KeybindVersion == other.KeybindVersion &&
                ThemeVersion == other.ThemeVersion;
        }
    }
}
