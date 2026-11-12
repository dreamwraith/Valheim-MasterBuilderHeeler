using System;
using MasterBuilderHeeler.Configuration;
using MasterBuilderHeeler.Core.Input;
using MasterBuilderHeeler.Core.Sampling;
using MasterBuilderHeeler.Core.State;
using UnityEngine;

namespace MasterBuilderHeeler.Core.UI.Panels
{
    public sealed class GlobalInfoPanel : HudKeymapPanel
    {
        private static readonly Func<bool> PrecisionEnabled = () => ModConfig.General.EnablePrecision.Value;
        private static readonly Func<bool> CameraEnabled = () => ModConfig.General.EnableBuildCamera.Value;
        private static readonly Func<bool> GridEnabled = () => ModConfig.General.EnableGrid.Value;

        private static readonly Func<bool> PrecisionOn = () => BuildStateManager.IsPrecisionModeActive;
        private static readonly Func<bool> CameraOn = () => BuildStateManager.IsBuildCameraActive;
        private static readonly Func<bool> GhostFrozen = () => BuildStateManager.EditState == PlacementEditState.GhostFrozen;
        private static readonly Func<bool> InEdit = () => BuildStateManager.EditState == PlacementEditState.InPlaceEdit;
        private static readonly Func<bool> SamplingAvailable = () => PieceEyedropper.IsCustomSamplingAvailable();
        private static readonly Func<bool> GridSnapOn = () => BuildStateManager.IsGridSnapActive;
        private static readonly Func<bool> OverlayOn = () => BuildStateManager.IsGridOverlayVisible;

        private PanelStateSnapshot _lastSnapshot;
        private bool _hasValidSnapshot;
        private int _preseededKeybindVersion = -1;

        public GlobalInfoPanel(Transform parentTransform)
            : base(parentTransform, "MBH_GlobalInfoPanel", ModConfig.Hud.GlobalPanelLayout)
        {
            Panel.SetFloatingHeader("Global");

            Row(GlobalKeymapRow.Precision, BuildActions.TogglePrecisionMode, PrecisionEnabled,
                indicator: HudIndicator.OnOff(PrecisionOn));
            Row(GlobalKeymapRow.BuildCamera, BuildActions.ToggleBuildCamera, CameraEnabled,
                indicator: HudIndicator.Choice(CameraOn, () => HudTheme.StatusBuildCameraOn, () => HudTheme.StatusOff));
            Row(GlobalKeymapRow.FreezeGhost, BuildActions.ToggleGhostFreeze, PrecisionEnabled, PrecisionOn,
                HudIndicator.Choice(GhostFrozen, () => HudTheme.StatusGhostFreezeOn, () => HudTheme.StatusOff));
            Row(GlobalKeymapRow.SamplePiece, BuildActions.SamplePiece, PrecisionEnabled, SamplingAvailable);
            Row(GlobalKeymapRow.SampleRotation, BuildActions.MatchPieceRotation, PrecisionEnabled, SamplingAvailable);
            Row(GlobalKeymapRow.SampleBoth, BuildActions.SamplePieceAndRotation, PrecisionEnabled, SamplingAvailable);
            Row(GlobalKeymapRow.EditPiece, BuildActions.TogglePieceEdit, PrecisionEnabled, PrecisionOn,
                HudIndicator.OnOff(InEdit));
            Row(GlobalKeymapRow.CommitEdit, BuildActions.CommitPieceEdit, BuildActions.TogglePieceEdit, PrecisionEnabled, InEdit);
            Row(GlobalKeymapRow.CancelEdit, BuildActions.CancelPieceEdit, PrecisionEnabled, InEdit);
            Row(GlobalKeymapRow.GridSnap, BuildActions.ToggleGridSnap, GridEnabled,
                indicator: HudIndicator.OnOff(GridSnapOn));
            Row(GlobalKeymapRow.GridOverlay, BuildActions.ToggleGridOverlay, GridEnabled,
                indicator: HudIndicator.OnOff(OverlayOn));
            Row(GlobalKeymapRow.GridFrame, BuildActions.ToggleLocalGrid, GridEnabled, OverlayOn,
                HudIndicator.Choice(
                    () => BuildStateManager.ActiveGridFrame == GridFrame.Local,
                    () => HudTheme.StatusGridLocal,
                    () => HudTheme.StatusGridWorld));
            Row(GlobalKeymapRow.GridSurface, BuildActions.ToggleGridSurface, GridEnabled, OverlayOn,
                HudIndicator.Choice(
                    () => BuildStateManager.ActiveGridSurface == GridSurface.Flat,
                    () => HudTheme.StatusGridFlat,
                    () => HudTheme.StatusGridTerrain));
            Row(GlobalKeymapRow.GridSize, BuildActions.CycleGridSize, GridEnabled, OverlayOn,
                new HudIndicator(() => FormatGridSize(BuildStateManager.GridCellSize), () => FormatGridSize(4.0f)));

            Panel.SetAnchors(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f));

            PreseedConstantWidth();
            ModConfig.General.EnablePrecision.SettingChanged += OnFeatureSettingChanged;
            ModConfig.General.EnableBuildCamera.SettingChanged += OnFeatureSettingChanged;
            ModConfig.General.EnableGrid.SettingChanged += OnFeatureSettingChanged;
            Panel.SetActive(false);
        }

        public void Refresh(Player localPlayer)
        {
            bool hasEnabledFeature = ModConfig.General.EnablePrecision.Value ||
                ModConfig.General.EnableBuildCamera.Value ||
                ModConfig.General.EnableGrid.Value;
            if (!ModConfig.Hud.GlobalPanelLayout.Show.Value || !hasEnabledFeature)
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

            // the rows read the build state themselves. this is only the check that skips the work when nothing changed
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

            RefreshKeymapRows();
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
            ModConfig.General.EnablePrecision.SettingChanged -= OnFeatureSettingChanged;
            ModConfig.General.EnableBuildCamera.SettingChanged -= OnFeatureSettingChanged;
            ModConfig.General.EnableGrid.SettingChanged -= OnFeatureSettingChanged;
            base.Destroy();
        }

        protected override void OnFontSizeChanged()
        {
            _hasValidSnapshot = false;
            PreseedConstantWidth();
        }

        private static string FormatGridSize(float cellSize) =>
            $"<color={HudTheme.SecondaryValueHex}>{HudTheme.Measure($"{cellSize:F1}m")}</color>";

        private void Row(
            GlobalKeymapRow row,
            BuildAction action,
            Func<bool> isEnabled,
            Func<bool>? isVisible = null,
            HudIndicator? indicator = null) =>
            AddKeymapRow(ModConfig.Hud.Get(row), () => HudKeybindFormatter.Format(action), isEnabled, isVisible, indicator);

        private void Row(
            GlobalKeymapRow row,
            BuildAction firstAction,
            BuildAction secondAction,
            Func<bool> isEnabled,
            Func<bool> isVisible) =>
            AddKeymapRow(ModConfig.Hud.Get(row), () => HudKeybindFormatter.FormatPair(firstAction, secondAction), isEnabled, isVisible);

        // a feature going on or off changes which rows count for the width
        private void OnFeatureSettingChanged(object sender, EventArgs eventArgs)
        {
            Panel.ResetClampedWidth();
            PreseedConstantWidth();
            _hasValidSnapshot = false;
        }

        // no fucking jitter please damnit ffs. how many times i gotta teach the same lesson.
        private void PreseedConstantWidth()
        {
            _preseededKeybindVersion = HudKeybindFormatter.KeybindVersion;
            ObserveKeymapRowWidths();
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
