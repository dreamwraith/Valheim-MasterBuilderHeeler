using System;
using MasterBuilderHeeler.Configuration;
using MasterBuilderHeeler.Utilities;
using UnityEngine;

namespace MasterBuilderHeeler.Core.State
{
    public static class BuildStateManager
    {
        public static event Action<bool>? PrecisionModeChanged;

        public static bool IsHammerSessionActive { get; private set; }
        public static bool IsBuildCameraActive { get; private set; }
        public static PlacementEditState EditState { get; private set; } = PlacementEditState.Normal;
        public static RotationFrame ActiveRotationFrame { get; private set; } = RotationFrame.World;
        public static TranslationFrame ActiveTranslationFrame { get; private set; } = TranslationFrame.Local;
        public static RotationAxis ActiveAxis { get; private set; } = RotationAxis.Yaw;
        public static StepPreset ActiveStepPreset { get; private set; } = StepPreset.Fine;
        public static bool IsPrecisionModeActive { get; private set; } = true;
        public static bool IsRoofModeActive { get; private set; }
        public static bool IsTerrainOpTarget { get; private set; }
        public static bool IsCornerRoofTarget { get; private set; }
        public static bool IsGridSnapActive { get; private set; }
        public static bool IsGridOverlayVisible { get; private set; }
        public static GridFrame ActiveGridFrame { get; private set; } = GridFrame.World;
        public static GridSurface ActiveGridSurface { get; private set; } = GridSurface.Terrain;
        public static Vector3 LocalGridOrigin { get; private set; }
        public static Quaternion LocalGridRotation { get; private set; } = Quaternion.identity;
        public static float GridCellSize { get; private set; } = 1.0f;
        public static float StepMultiplier { get; private set; } = 1.0f;
        public static Vector3 FrozenGhostPosition { get; private set; }
        public static Vector3 FrozenGhostOriginalPosition { get; private set; }
        public static Piece? EditedPiece { get; private set; }
        public static Vector3 EditedPiecePosition { get; private set; }
        public static Vector3 EditedPieceOriginalPosition { get; private set; }
        public static Quaternion EditedPieceOriginalRotation { get; private set; } = Quaternion.identity;

        public static bool HasActiveSpecialMode =>
            IsBuildCameraActive || EditState != PlacementEditState.Normal;

        public static bool IsRoofModeEffective =>
            IsRoofModeActive && IsCornerRoofTarget && !IsTerrainOpTarget;

        public static bool IsTerrainOpGhostActive =>
            !EditedPiece && IsTerrainOpTarget;

        public static void Initialize()
        {
            SetPrecisionModeActive(ModConfig.Build.EnablePrecisionModeByDefault.Value);
            IsRoofModeActive = ModConfig.Build.EnableRoofModeByDefault.Value;
            GridCellSize = ModConfig.Build.DefaultGridSize.Value;
            ModConfig.Build.DefaultGridSize.SettingChanged += OnDefaultGridSizeChanged;
        }

        public static void Cleanup()
        {
            ModConfig.Build.DefaultGridSize.SettingChanged -= OnDefaultGridSizeChanged;
        }

        public static void EndHammerSession()
        {
            IsHammerSessionActive = false;
            IsBuildCameraActive = false;
            EditState = PlacementEditState.Normal;

            FrozenGhostPosition = Vector3.zero;
            FrozenGhostOriginalPosition = Vector3.zero;
            EditedPiece = null;
            EditedPiecePosition = Vector3.zero;
            EditedPieceOriginalPosition = Vector3.zero;
            EditedPieceOriginalRotation = Quaternion.identity;
        }

        public static void ResetBuildSettings()
        {
            ActiveRotationFrame = RotationFrame.World;
            ActiveTranslationFrame = TranslationFrame.Local;
            ResetLatches();

            IsRoofModeActive = ModConfig.Build.EnableRoofModeByDefault.Value;
            IsGridSnapActive = false;
            IsGridOverlayVisible = false;
            ActiveGridFrame = GridFrame.World;
            ActiveGridSurface = GridSurface.Terrain;
            LocalGridOrigin = Vector3.zero;
            LocalGridRotation = Quaternion.identity;
            GridCellSize = ModConfig.Build.DefaultGridSize.Value;
            StepMultiplier = 1.0f;
        }

        public static void SetHammerSessionActive(bool active) => IsHammerSessionActive = active;

        public static void SetTargetClassification(bool isTerrainOp, bool isCornerRoof)
        {
            IsTerrainOpTarget = isTerrainOp;
            IsCornerRoofTarget = isCornerRoof;
        }

        public static GameObject? GetActiveTargetObject(Player? player)
        {
            if (EditedPiece)
            {
                return EditedPiece.gameObject;
            }

            return player ? player.m_placementGhost : null;
        }

        public static void ToggleBuildCamera()
        {
            IsBuildCameraActive = !IsBuildCameraActive;
            if (ModConfig.IsDebugLogging)
            {
                Plugin.LogDebug($"[BuildStateManager] Build Camera toggled: {IsBuildCameraActive}");
            }
        }

        public static void ExitBuildCamera() => IsBuildCameraActive = false;

        public static void EnterGhostFreeze(Vector3 worldPosition, Vector3 originalPosition)
        {
            FrozenGhostOriginalPosition = originalPosition;
            FrozenGhostPosition = worldPosition;
            EditState = PlacementEditState.GhostFrozen;
        }

        public static void TranslateFrozenGhost(Vector3 delta) => FrozenGhostPosition += delta;

        public static void ResetFrozenGhostPosition() => FrozenGhostPosition = FrozenGhostOriginalPosition;

        public static bool ExitGhostFreeze()
        {
            if (EditState != PlacementEditState.GhostFrozen)
            {
                return false;
            }

            EditState = PlacementEditState.Normal;
            return true;
        }

        public static void EnterPieceEdit(Piece piece)
        {
            EditedPiece = piece;
            EditedPieceOriginalPosition = piece.transform.position;
            EditedPiecePosition = EditedPieceOriginalPosition;
            EditedPieceOriginalRotation = piece.transform.rotation;
            EditState = PlacementEditState.InPlaceEdit;
        }

        public static void TranslateEditedPiece(Vector3 delta) => EditedPiecePosition += delta;

        public static void ResetEditedPiecePosition() => EditedPiecePosition = EditedPieceOriginalPosition;

        public static void ExitPieceEdit(bool applyChanges)
        {
            if (EditedPiece)
            {
                if (!applyChanges)
                {
                    EditedPiece.transform.position = EditedPieceOriginalPosition;
                    EditedPiece.transform.rotation = EditedPieceOriginalRotation;
                }
                EditedPiece = null;
            }

            EditedPiecePosition = Vector3.zero;
            EditState = PlacementEditState.Normal;
        }

        public static void TogglePrecisionMode()
        {
            SetPrecisionModeActive(!IsPrecisionModeActive);
            if (ModConfig.IsDebugLogging)
            {
                Plugin.LogDebug($"[BuildStateManager] Precision Mode toggled: {IsPrecisionModeActive}");
            }
            HudMessageUtils.ShowTopLeft(IsPrecisionModeActive ? "Precision Mode: ON" : "Precision Mode: OFF");
        }

        // wraps at both ends because gamepad has one button for this and it has to reach every preset
        public static void ShiftStepPreset(int offset)
        {
            int firstPreset = (int)StepPreset.UltraFine;
            int presetCount = (int)StepPreset.Coarse - firstPreset + 1;
            ActiveStepPreset = (StepPreset)(firstPreset + (((int)ActiveStepPreset - firstPreset + offset + presetCount) % presetCount));
            if (ModConfig.IsDebugLogging)
            {
                Plugin.LogDebug($"[BuildStateManager] Step Preset shifted to: {ActiveStepPreset}");
            }
        }

        public static void DoubleStepMultiplier()
        {
            StepMultiplier = Mathf.Min(StepMultiplier * 2.0f, 8.0f);
            if (ModConfig.IsDebugLogging)
            {
                Plugin.LogDebug($"[BuildStateManager] Step multiplier scaled: {StepMultiplier}x");
            }
            HudMessageUtils.ShowTopLeft($"Step Scale: {StepMultiplier:0.###}x");
        }

        public static void HalveStepMultiplier()
        {
            StepMultiplier = Mathf.Max(StepMultiplier * 0.5f, 0.125f);
            if (ModConfig.IsDebugLogging)
            {
                Plugin.LogDebug($"[BuildStateManager] Step multiplier scaled: {StepMultiplier}x");
            }
            HudMessageUtils.ShowTopLeft($"Step Scale: {StepMultiplier:0.###}x");
        }

        public static void ResetStepMultiplier()
        {
            StepMultiplier = 1.0f;
            Plugin.LogDebug("[BuildStateManager] Step multiplier reset to 1x.");
            HudMessageUtils.ShowTopLeft("Step Scale: 1x");
        }

        public static float GetActiveRotationStep()
        {
            float baseStep = ActiveStepPreset switch
            {
                StepPreset.UltraFine => ModConfig.Build.RotationStepUltraFine.Value,
                StepPreset.ExtraFine => ModConfig.Build.RotationStepExtraFine.Value,
                StepPreset.Fine => ModConfig.Build.RotationStepFine.Value,
                StepPreset.Coarse => ModConfig.Build.RotationStepCoarse.Value,
                _ => ModConfig.Build.RotationStepStandard.Value
            };
            return baseStep * StepMultiplier;
        }

        public static float GetActiveTranslationStep()
        {
            float baseStep = ActiveStepPreset switch
            {
                StepPreset.UltraFine => ModConfig.Build.TranslationStepUltraFine.Value,
                StepPreset.ExtraFine => ModConfig.Build.TranslationStepExtraFine.Value,
                StepPreset.Fine => ModConfig.Build.TranslationStepFine.Value,
                StepPreset.Coarse => ModConfig.Build.TranslationStepCoarse.Value,
                _ => ModConfig.Build.TranslationStepStandard.Value
            };
            return baseStep * StepMultiplier;
        }

        public static void ToggleAxis(RotationAxis axis) =>
            ActiveAxis = ActiveAxis == axis ? RotationAxis.Yaw : axis;

        public static void SelectYawAxis()
        {
            ActiveAxis = RotationAxis.Yaw;
            Plugin.LogDebug("[BuildStateManager] Selected axis: Yaw.");
        }

        public static void TogglePitchAxis()
        {
            ToggleAxis(RotationAxis.Pitch);
            if (ModConfig.IsDebugLogging)
            {
                Plugin.LogDebug($"[BuildStateManager] ActiveAxis toggled to {ActiveAxis}.");
            }
        }

        public static void ToggleRollAxis()
        {
            ToggleAxis(RotationAxis.Roll);
            if (ModConfig.IsDebugLogging)
            {
                Plugin.LogDebug($"[BuildStateManager] ActiveAxis toggled to {ActiveAxis}.");
            }
        }

        public static void CycleAxis()
        {
            ActiveAxis = ActiveAxis switch
            {
                RotationAxis.Yaw => RotationAxis.Pitch,
                RotationAxis.Pitch => RotationAxis.Roll,
                _ => RotationAxis.Yaw
            };
            if (ModConfig.IsDebugLogging)
            {
                Plugin.LogDebug($"[BuildStateManager] ActiveAxis cycled to {ActiveAxis}.");
            }
        }

        public static void ResetLatches()
        {
            ActiveAxis = RotationAxis.Yaw;
            ActiveStepPreset = StepPreset.Fine;
        }

        public static void ToggleRoofMode()
        {
            IsRoofModeActive = !IsRoofModeActive;
            if (ModConfig.IsDebugLogging)
            {
                Plugin.LogDebug($"[BuildStateManager] Roof Mode toggled: {IsRoofModeActive}");
            }
            HudMessageUtils.ShowTopLeft(IsRoofModeActive ? "Roof Mode: ON" : "Roof Mode: OFF");
        }

        public static void CycleRotationFrame()
        {
            ActiveRotationFrame = ActiveRotationFrame == RotationFrame.World
                ? RotationFrame.Local
                : RotationFrame.World;
            if (ModConfig.IsDebugLogging)
            {
                Plugin.LogDebug($"[BuildStateManager] Rotation Frame cycled to: {ActiveRotationFrame}");
            }
            HudMessageUtils.ShowTopLeft($"Rotation Frame: {ActiveRotationFrame}");
        }

        public static void CycleTranslationFrame()
        {
            ActiveTranslationFrame = ActiveTranslationFrame == TranslationFrame.Local
                ? TranslationFrame.World
                : TranslationFrame.Local;
            if (ModConfig.IsDebugLogging)
            {
                Plugin.LogDebug($"[BuildStateManager] Translation Frame cycled to: {ActiveTranslationFrame}");
            }
            HudMessageUtils.ShowTopLeft($"Nudge Frame: {ActiveTranslationFrame}");
        }

        public static void ToggleGridSnap()
        {
            IsGridSnapActive = !IsGridSnapActive;
            if (ModConfig.IsDebugLogging)
            {
                Plugin.LogDebug($"[BuildStateManager] Grid Snap toggled: {IsGridSnapActive}");
            }
            HudMessageUtils.ShowTopLeft(IsGridSnapActive ? "Grid Snap: ON" : "Grid Snap: OFF");
        }

        public static void ToggleGridOverlayVisibility()
        {
            IsGridOverlayVisible = !IsGridOverlayVisible;
            if (ModConfig.IsDebugLogging)
            {
                Plugin.LogDebug($"[BuildStateManager] Grid Overlay toggled: {IsGridOverlayVisible}");
            }
            HudMessageUtils.ShowTopLeft(IsGridOverlayVisible ? "Grid Overlay: ON" : "Grid Overlay: OFF");
        }

        public static void ClearLocalGrid()
        {
            ActiveGridFrame = GridFrame.World;
            LocalGridOrigin = Vector3.zero;
            LocalGridRotation = Quaternion.identity;
            Plugin.LogDebug("[BuildStateManager] Local Grid cleared; reverted to World Grid.");
            HudMessageUtils.ShowTopLeft("Grid Frame: World");
        }

        public static void AnchorLocalGrid(Vector3 origin, float yawDegrees, string pieceDisplayName, string prefabName)
        {
            LocalGridOrigin = origin;
            LocalGridRotation = Quaternion.Euler(0f, yawDegrees, 0f);
            ActiveGridFrame = GridFrame.Local;

            if (ModConfig.IsDebugLogging)
            {
                Plugin.LogDebug($"[BuildStateManager] Local Grid anchored to '{prefabName}' at origin={LocalGridOrigin}, yaw={yawDegrees:F1}.");
            }
            HudMessageUtils.ShowTopLeft($"Local Grid anchored to {pieceDisplayName}");
        }

        public static void ToggleGridSurface()
        {
            ActiveGridSurface = ActiveGridSurface == GridSurface.Terrain
                ? GridSurface.Flat
                : GridSurface.Terrain;
            if (ModConfig.IsDebugLogging)
            {
                Plugin.LogDebug($"[BuildStateManager] Grid Surface toggled to: {ActiveGridSurface}");
            }
            HudMessageUtils.ShowTopLeft($"Grid Surface: {ActiveGridSurface}");
        }

        public static void CycleGridSize()
        {
            GridCellSize = GridCellSize switch
            {
                <= 0.5f => 1.0f,
                <= 1.0f => 2.0f,
                <= 2.0f => 4.0f,
                _ => 0.5f
            };
            if (ModConfig.IsDebugLogging)
            {
                Plugin.LogDebug($"[BuildStateManager] Grid Size cycled to: {GridCellSize}m");
            }
            HudMessageUtils.ShowTopLeft($"Grid Size: {GridCellSize:F1}m");
        }

        private static void OnDefaultGridSizeChanged(object sender, EventArgs eventArgs) =>
            GridCellSize = ModConfig.Build.DefaultGridSize.Value;

        public static void SetPrecisionModeActive(bool active)
        {
            bool resolvedActive = active && ModConfig.General.EnablePrecision.Value;
            if (IsPrecisionModeActive == resolvedActive)
            {
                return;
            }

            IsPrecisionModeActive = resolvedActive;
            if (PrecisionModeChanged != null)
            {
                PrecisionModeChanged(resolvedActive);
            }
        }
    }
}
