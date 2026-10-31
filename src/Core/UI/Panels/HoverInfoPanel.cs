using System;
using MasterBuilderHeeler.Configuration;
using MasterBuilderHeeler.Core.Placement;
using MasterBuilderHeeler.Utilities;
using UnityEngine;

namespace MasterBuilderHeeler.Core.UI.Panels
{
    public sealed class HoverInfoPanel : HudPanelBase
    {
        private readonly HudLabelRow _nameRow;
        private readonly HudLabelRow _healthRow;
        private readonly HudLabelRow _stabilityRow;
        private readonly HudLabelRow _rotationRow;
        private readonly HudLabelRow _quaternionRow;

        private PanelStateSnapshot _lastSnapshot;
        private int _lastPieceInstanceId;
        private bool _hasValidSnapshot;

        public HoverInfoPanel(Transform crosshairTransform)
            : base(crosshairTransform, "MBH_HoverInfoPanel", ModConfig.HoverPanelLayout)
        {
            _nameRow = Panel.AddRow($"Piece {HudGlyphs.ToggleActive}:", isSecondaryValue: true);
            _healthRow = Panel.AddRow($"Health {HudGlyphs.Heart}:");
            _stabilityRow = Panel.AddRow($"Stability {HudGlyphs.House}:");
            _rotationRow = Panel.AddRow($"Rotation {HudGlyphs.CircledBullet}:", isSecondaryValue: true);
            _quaternionRow = Panel.AddRow($"Quaternion {HudGlyphs.Command}:", isSecondaryValue: true);

            Panel.SetAnchors(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));

            SubscribeFieldEvents();
            Panel.SetActive(false);
        }

        public void Refresh(Player localPlayer)
        {
            bool showName = ModConfig.ShowHoverName.Value;
            bool showPrefab = ModConfig.ShowHoverPrefab.Value;
            bool showHealth = ModConfig.ShowHoverHealth.Value;
            bool showSupport = ModConfig.ShowHoverSupport.Value;
            bool showRotation = ModConfig.ShowHoverEuler.Value;
            bool showQuaternion = ModConfig.ShowHoverQuaternion.Value;
            bool hasAnyRow = showName || showPrefab || showHealth || showSupport || showRotation || showQuaternion;

            if (!ModConfig.HoverPanelLayout.Show.Value ||
                !hasAnyRow ||
                (ModConfig.RequireHammerForHoverInspection.Value && !localPlayer.InPlaceMode()))
            {
                SetActive(false);
                return;
            }

            Piece? hoveredPiece = BuildReachService.GetAimedPiece(localPlayer);
            if (!hoveredPiece || !hoveredPiece.TryGetComponent(out WearNTear wearNTear))
            {
                SetActive(false);
                return;
            }

            int instanceId = hoveredPiece.GetInstanceID();
            if (instanceId != _lastPieceInstanceId)
            {
                _lastPieceInstanceId = instanceId;
                _hasValidSnapshot = false;
                Panel.ResetClampedWidth();
            }

            Panel.SetActive(true);

            int visibleRowsMask =
                (showName ? 1 : 0) |
                (showPrefab ? 2 : 0) |
                (showHealth ? 4 : 0) |
                (showSupport ? 8 : 0) |
                (showRotation ? 16 : 0) |
                (showQuaternion ? 32 : 0);

            float maxHealth = 0f;
            float currentHealth = 0f;
            float healthPercent = 1f;
            if (showHealth)
            {
                maxHealth = wearNTear.m_health;
                currentHealth = wearNTear.m_nview && wearNTear.m_nview.IsValid()
                    ? wearNTear.m_nview.GetZDO().GetFloat(ZDOVars.s_health, maxHealth)
                    : maxHealth;
                healthPercent = maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 1f;
            }

            float supportPercent = 1f;
            if (showSupport)
            {
                float maxSupport = wearNTear.GetMaxSupport();
                supportPercent = maxSupport > 0f ? Mathf.Clamp01(wearNTear.GetSupport() / maxSupport) : 1f;
            }

            Quaternion pieceRotation = (showRotation || showQuaternion) ? wearNTear.transform.rotation : Quaternion.identity;
            Vector3 euler = showRotation ? pieceRotation.eulerAngles : Vector3.zero;

            PanelStateSnapshot snapshot = new PanelStateSnapshot(
                instanceId,
                visibleRowsMask,
                showHealth ? QuantizeHealth(currentHealth) : 0,
                showHealth ? QuantizeHealth(maxHealth) : 0,
                showSupport ? Mathf.RoundToInt(supportPercent * 100f) : 0,
                showRotation ? HudTheme.QuantizeEuler(euler) : Vector3Int.zero,
                showQuaternion ? HudTheme.QuantizeQuaternion(pieceRotation) : default,
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
                _nameRow.SetValue(PieceUtils.FormatPanelName(hoveredPiece, showName, showPrefab));
            }

            _healthRow.SetActive(showHealth);
            if (showHealth)
            {
                Color healthColor = HudTheme.HealthPercentGradient.Evaluate(healthPercent);
                string healthColorHex = ColorUtility.ToHtmlStringRGB(healthColor);
                _healthRow.SetValue($"<color=#{healthColorHex}>{FormatHealth(currentHealth)}</color>{HudTheme.SlashSeparator}<color={HudTheme.MaxHealthHex}>{FormatHealth(maxHealth)}</color> {HudTheme.Measure($"<color=#{healthColorHex}>{healthPercent:0%}</color>")}");
            }

            _stabilityRow.SetActive(showSupport);
            if (showSupport)
            {
                Color supportColor = HudTheme.StabilityPercentGradient.Evaluate(supportPercent);
                string supportColorHex = ColorUtility.ToHtmlStringRGB(supportColor);
                _stabilityRow.SetValue($"<color=#{supportColorHex}>{supportPercent:0%}</color>");
            }

            _rotationRow.SetActive(showRotation);
            if (showRotation)
            {
                _rotationRow.SetValue(HudTheme.FormatEuler(euler));
            }

            _quaternionRow.SetActive(showQuaternion);
            if (showQuaternion)
            {
                _quaternionRow.SetValue(HudTheme.FormatQuaternion(pieceRotation));
            }

            Panel.UpdateClampedWidth();
        }

        public override void SetActive(bool active)
        {
            if (!active)
            {
                _lastPieceInstanceId = 0;
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

        // some indestructible pieces, probably just modded? pieces store float.MaxValue, which overflows RoundToInt
        private static int QuantizeHealth(float health) =>
            Mathf.RoundToInt(Mathf.Clamp(health, -2E9f, 2E9f));

        // and N0 turns that same value into a 39 digit number lol good times very fun stuff
        // having the whole screen be covered by a health number was funny, but ultimately lame i guess.
        private static string FormatHealth(float health) =>
            health.ToString(Mathf.Abs(health) > 1E9f ? "G5" : "N0");

        private void SubscribeFieldEvents()
        {
            ModConfig.ShowHoverName.SettingChanged += OnFieldVisibilityChanged;
            ModConfig.ShowHoverPrefab.SettingChanged += OnFieldVisibilityChanged;
            ModConfig.ShowHoverHealth.SettingChanged += OnFieldVisibilityChanged;
            ModConfig.ShowHoverSupport.SettingChanged += OnFieldVisibilityChanged;
            ModConfig.ShowHoverEuler.SettingChanged += OnFieldVisibilityChanged;
            ModConfig.ShowHoverQuaternion.SettingChanged += OnFieldVisibilityChanged;
        }

        private void UnsubscribeFieldEvents()
        {
            ModConfig.ShowHoverName.SettingChanged -= OnFieldVisibilityChanged;
            ModConfig.ShowHoverPrefab.SettingChanged -= OnFieldVisibilityChanged;
            ModConfig.ShowHoverHealth.SettingChanged -= OnFieldVisibilityChanged;
            ModConfig.ShowHoverSupport.SettingChanged -= OnFieldVisibilityChanged;
            ModConfig.ShowHoverEuler.SettingChanged -= OnFieldVisibilityChanged;
            ModConfig.ShowHoverQuaternion.SettingChanged -= OnFieldVisibilityChanged;
        }

        private void OnFieldVisibilityChanged(object sender, EventArgs eventArgs)
        {
            _hasValidSnapshot = false;
            Panel.ResetClampedWidth();
        }

        private readonly struct PanelStateSnapshot : IEquatable<PanelStateSnapshot>
        {
            public readonly int InstanceId;
            public readonly int VisibleRowsMask;
            public readonly int CurrentHealthRounded;
            public readonly int MaxHealthRounded;
            public readonly int SupportPercentRounded;
            public readonly Vector3Int QuantizedEuler;
            public readonly HudTheme.QuantizedQuaternion QuantizedRotation;
            public readonly int ThemeVersion;

            public PanelStateSnapshot(
                int instanceId,
                int visibleRowsMask,
                int currentHealthRounded,
                int maxHealthRounded,
                int supportPercentRounded,
                Vector3Int quantizedEuler,
                HudTheme.QuantizedQuaternion quantizedRotation,
                int themeVersion)
            {
                InstanceId = instanceId;
                VisibleRowsMask = visibleRowsMask;
                CurrentHealthRounded = currentHealthRounded;
                MaxHealthRounded = maxHealthRounded;
                SupportPercentRounded = supportPercentRounded;
                QuantizedEuler = quantizedEuler;
                QuantizedRotation = quantizedRotation;
                ThemeVersion = themeVersion;
            }

            public bool Equals(PanelStateSnapshot other) =>
                InstanceId == other.InstanceId &&
                VisibleRowsMask == other.VisibleRowsMask &&
                CurrentHealthRounded == other.CurrentHealthRounded &&
                MaxHealthRounded == other.MaxHealthRounded &&
                SupportPercentRounded == other.SupportPercentRounded &&
                QuantizedEuler.Equals(other.QuantizedEuler) &&
                QuantizedRotation.Equals(other.QuantizedRotation) &&
                ThemeVersion == other.ThemeVersion;
        }
    }
}
