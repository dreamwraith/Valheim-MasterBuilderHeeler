using System;
using MasterBuilderHeeler.Configuration;
using MasterBuilderHeeler.Core.Rotation;
using MasterBuilderHeeler.Core.State;
using MasterBuilderHeeler.Utilities;
using UnityEngine;

namespace MasterBuilderHeeler.Core.UI.Panels
{
    public sealed class PlacementInfoPanel : HudPanelBase
    {
        private readonly HudLabelRow _nameRow;
        private readonly HudLabelRow _rotationRow;
        private readonly HudLabelRow _quaternionRow;
        private readonly HudLabelRow _distanceRow;

        private PanelStateSnapshot _lastSnapshot;
        private string _cachedPrefabName = string.Empty;
        private int _lastTargetInstanceId;
        private bool _hasValidSnapshot;

        public PlacementInfoPanel(Transform crosshairTransform)
            : base(crosshairTransform, "MBH_PlacementInfoPanel", ModConfig.PlacementPanelLayout)
        {
            _nameRow = Panel.AddRow($"Placing {HudGlyphs.SquareCrosshatch}:", isSecondaryValue: true);
            _rotationRow = Panel.AddRow($"Rotation {HudGlyphs.CircledBullet}:", isSecondaryValue: true);
            _quaternionRow = Panel.AddRow($"Quaternion {HudGlyphs.Command}:", isSecondaryValue: true);
            _distanceRow = Panel.AddRow($"Distance {HudGlyphs.ArrowRight}:", isSecondaryValue: true);

            Panel.SetAnchors(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));

            SubscribeFieldEvents();
            Panel.SetActive(false);
        }

        public void Refresh(Player localPlayer)
        {
            bool isTerrainOp = BuildStateManager.IsTerrainOpGhostActive;
            bool showName = ModConfig.ShowGhostName.Value;
            bool showPrefab = ModConfig.ShowGhostPrefab.Value;
            bool showRotation = ModConfig.ShowGhostEuler.Value && !isTerrainOp;
            bool showQuaternion = ModConfig.ShowGhostQuaternion.Value && !isTerrainOp;
            bool showDistance = ModConfig.ShowGhostDistance.Value;
            bool showPosition = ModConfig.ShowGhostWorldPosition.Value;
            bool hasAnyRow = showName || showPrefab || showRotation || showQuaternion || showDistance || showPosition;

            if (!ModConfig.PlacementPanelLayout.Show.Value || !hasAnyRow)
            {
                SetActive(false);
                return;
            }

            GameObject? targetObject = BuildStateManager.GetActiveTargetObject(localPlayer);
            if (!targetObject || !targetObject.activeInHierarchy)
            {
                SetActive(false);
                return;
            }

            int instanceId = targetObject.GetInstanceID();
            if (instanceId != _lastTargetInstanceId)
            {
                _lastTargetInstanceId = instanceId;
                _cachedPrefabName = Utils.GetPrefabName(targetObject);
                _hasValidSnapshot = false;
                Panel.ResetClampedWidth();
            }

            Panel.SetActive(true);

            bool isEditing = BuildStateManager.EditState == PlacementEditState.InPlaceEdit;
            int stateFlags =
                (isEditing ? 1 : 0) |
                (showName ? 2 : 0) |
                (showPrefab ? 4 : 0) |
                (showRotation ? 8 : 0) |
                (showQuaternion ? 16 : 0) |
                (showDistance ? 32 : 0) |
                (showPosition ? 64 : 0);

            Transform targetTransform = targetObject.transform;
            Vector3 position = (showDistance || showPosition) ? targetTransform.position : Vector3.zero;
            float distance = showDistance ? Vector3.Distance(localPlayer.transform.position, position) : 0f;
            Quaternion rotation = (showRotation || showQuaternion) ? targetTransform.rotation : Quaternion.identity;
            Vector3 euler = Vector3.zero;
            if (showRotation)
            {
                euler = (BuildStateManager.IsPrecisionModeActive || BuildStateManager.EditState != PlacementEditState.Normal)
                    ? PieceRotationEngine.GetActiveRotator().GetEulerAngles()
                    : rotation.eulerAngles;
            }

            PanelStateSnapshot snapshot = new PanelStateSnapshot(
                instanceId,
                stateFlags,
                showDistance ? Mathf.RoundToInt(distance * 10f) : 0,
                showPosition ? HudTheme.QuantizeCoordinates(position) : Vector3Int.zero,
                showRotation ? HudTheme.QuantizeEuler(euler) : Vector3Int.zero,
                showQuaternion ? HudTheme.QuantizeQuaternion(rotation) : default,
                HudTheme.ThemeVersion);

            if (_hasValidSnapshot && snapshot.Equals(_lastSnapshot))
            {
                return;
            }

            _lastSnapshot = snapshot;
            _hasValidSnapshot = true;

            _nameRow.SetActive(showName || showPrefab);
            if (showName || showPrefab)
            {
                string rowLabel = isEditing
                    ? $"Editing {HudGlyphs.SquareCrosshatch}:"
                    : $"Placing {HudGlyphs.SquareCrosshatch}:";
                string displayName = targetObject.TryGetComponent(out Piece targetPiece)
                    ? PieceUtils.FormatPanelName(targetPiece, showName, showPrefab)
                    : _cachedPrefabName;
                _nameRow.SetText(rowLabel, displayName);
            }

            _rotationRow.SetActive(showRotation);
            if (showRotation)
            {
                _rotationRow.SetValue(HudTheme.FormatEuler(euler));
            }

            _quaternionRow.SetActive(showQuaternion);
            if (showQuaternion)
            {
                _quaternionRow.SetValue(HudTheme.FormatQuaternion(rotation));
            }

            _distanceRow.SetActive(showDistance || showPosition);
            if (showDistance || showPosition)
            {
                string rowLabel = !showDistance && showPosition
                    ? $"Position {HudGlyphs.ArrowRight}:"
                    : $"Distance {HudGlyphs.ArrowRight}:";

                string rowValue;
                if (showDistance && showPosition)
                {
                    rowValue = $"{distance:0.#}m{HudTheme.BrokenBarSeparator}{HudTheme.FormatCoordinates(position)}";
                }
                else if (showDistance)
                {
                    rowValue = $"{distance:0.#}m";
                }
                else
                {
                    rowValue = HudTheme.FormatCoordinates(position);
                }

                _distanceRow.SetText(rowLabel, rowValue);
            }

            Panel.UpdateClampedWidth();
        }

        public override void SetActive(bool active)
        {
            if (!active)
            {
                _lastTargetInstanceId = 0;
                _cachedPrefabName = string.Empty;
                _hasValidSnapshot = false;
                Panel.ResetClampedWidth();
            }

            base.SetActive(active);
        }

        public override void Destroy()
        {
            UnsubscribeFieldEvents();
            base.Destroy();
        }

        protected override void OnFontSizeChanged() => _hasValidSnapshot = false;

        private void SubscribeFieldEvents()
        {
            ModConfig.ShowGhostName.SettingChanged += OnFieldVisibilityChanged;
            ModConfig.ShowGhostPrefab.SettingChanged += OnFieldVisibilityChanged;
            ModConfig.ShowGhostEuler.SettingChanged += OnFieldVisibilityChanged;
            ModConfig.ShowGhostQuaternion.SettingChanged += OnFieldVisibilityChanged;
            ModConfig.ShowGhostDistance.SettingChanged += OnFieldVisibilityChanged;
            ModConfig.ShowGhostWorldPosition.SettingChanged += OnFieldVisibilityChanged;
        }

        private void UnsubscribeFieldEvents()
        {
            ModConfig.ShowGhostName.SettingChanged -= OnFieldVisibilityChanged;
            ModConfig.ShowGhostPrefab.SettingChanged -= OnFieldVisibilityChanged;
            ModConfig.ShowGhostEuler.SettingChanged -= OnFieldVisibilityChanged;
            ModConfig.ShowGhostQuaternion.SettingChanged -= OnFieldVisibilityChanged;
            ModConfig.ShowGhostDistance.SettingChanged -= OnFieldVisibilityChanged;
            ModConfig.ShowGhostWorldPosition.SettingChanged -= OnFieldVisibilityChanged;
        }

        private void OnFieldVisibilityChanged(object sender, EventArgs eventArgs)
        {
            _hasValidSnapshot = false;
            Panel.ResetClampedWidth();
        }

        private readonly struct PanelStateSnapshot : IEquatable<PanelStateSnapshot>
        {
            public readonly int InstanceId;
            public readonly int StateFlags;
            public readonly int DistanceTenths;
            public readonly Vector3Int QuantizedPosition;
            public readonly Vector3Int QuantizedEuler;
            public readonly HudTheme.QuantizedQuaternion QuantizedRotation;
            public readonly int ThemeVersion;

            public PanelStateSnapshot(
                int instanceId,
                int stateFlags,
                int distanceTenths,
                Vector3Int quantizedPosition,
                Vector3Int quantizedEuler,
                HudTheme.QuantizedQuaternion quantizedRotation,
                int themeVersion)
            {
                InstanceId = instanceId;
                StateFlags = stateFlags;
                DistanceTenths = distanceTenths;
                QuantizedPosition = quantizedPosition;
                QuantizedEuler = quantizedEuler;
                QuantizedRotation = quantizedRotation;
                ThemeVersion = themeVersion;
            }

            public bool Equals(PanelStateSnapshot other) =>
                InstanceId == other.InstanceId &&
                StateFlags == other.StateFlags &&
                DistanceTenths == other.DistanceTenths &&
                QuantizedPosition.Equals(other.QuantizedPosition) &&
                QuantizedEuler.Equals(other.QuantizedEuler) &&
                QuantizedRotation.Equals(other.QuantizedRotation) &&
                ThemeVersion == other.ThemeVersion;
        }
    }
}
