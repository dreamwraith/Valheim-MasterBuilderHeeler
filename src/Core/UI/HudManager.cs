using System;
using System.Collections;
using MasterBuilderHeeler.Configuration;
using MasterBuilderHeeler.Core.UI.Panels;
using UnityEngine;

namespace MasterBuilderHeeler.Core.UI
{
    public sealed class HudManager : MonoBehaviour
    {
        private static HudManager? _instance;

        private GlobalInfoPanel _globalInfoPanel = null!;
        private PrecisionInfoPanel _precisionInfoPanel = null!;
        private PlacementInfoPanel? _placementInfoPanel;
        private HoverInfoPanel? _hoverInfoPanel;
        private Coroutine? _inspectionCoroutine;
        private WaitForSeconds _cachedWait = null!;
        private bool _loggedMissingCrosshair;

        private void Awake()
        {
            _instance = this;
            RebuildWaitInstruction();
            _globalInfoPanel = new GlobalInfoPanel(transform);
            _precisionInfoPanel = new PrecisionInfoPanel(transform);
            EnsureInspectionPanels();
            ModConfig.InfoUpdateInterval.SettingChanged += OnUpdateIntervalChanged;
            _inspectionCoroutine = StartCoroutine(InspectionPollLoop());
        }

        private void Update()
        {
            if (!ModConfig.EnableMod.Value)
            {
                HideControlPanels();
                HideInspectionPanels();
                return;
            }

            Player localPlayer = Player.m_localPlayer;
            if (!localPlayer)
            {
                HideControlPanels();
                HideInspectionPanels();
                return;
            }

            _globalInfoPanel.Refresh(localPlayer);
            _precisionInfoPanel.Refresh(localPlayer);
        }

        private void OnDestroy()
        {
            ReleaseManagedResources();

            if (ReferenceEquals(_instance, this))
            {
                _instance = null;
            }
        }

        public static void Initialize() =>
            ModConfig.EnableInfoPanels.SettingChanged += OnEnableInfoPanelsChanged;

        public static void AttachToHud(Hud hud)
        {
            if (ModConfig.EnableInfoPanels.Value && hud && !hud.TryGetComponent(out HudManager _))
            {
                hud.gameObject.AddComponent<HudManager>();
            }
        }

        public static void Cleanup()
        {
            ModConfig.EnableInfoPanels.SettingChanged -= OnEnableInfoPanelsChanged;
            DestroyInstance();
            HudPanelView.Cleanup();
        }

        public static void UpdateNativePieceHealthBar(Hud hud)
        {
            if (!ModConfig.EnableMod.Value ||
                !ModConfig.EnableInfoPanels.Value ||
                !ModConfig.HideNativeCrosshairHealthBar.Value ||
                !ModConfig.HoverPanelLayout.Show.Value ||
                !ModConfig.ShowHoverHealth.Value ||
                !hud ||
                !hud.m_pieceHealthRoot)
            {
                return;
            }

            if (hud.m_pieceHealthRoot.gameObject.activeSelf)
            {
                hud.m_pieceHealthRoot.gameObject.SetActive(false);
            }
        }

        private static void DestroyInstance()
        {
            if (_instance)
            {
                // sure sure, good things come to those who wait, but fuck all that. not gonna wait to Destroy().
                // this is mainly to keep it clean when people toggle the whole feature off because they are weird.
                _instance.ReleaseManagedResources();
                Destroy(_instance);
                _instance = null;
            }
        }

        private static void OnEnableInfoPanelsChanged(object sender, EventArgs eventArgs)
        {
            if (ModConfig.EnableInfoPanels.Value)
            {
                AttachToHud(Hud.instance);
            }
            else
            {
                DestroyInstance();
            }
        }

        private void ReleaseManagedResources()
        {
            enabled = false;
            ModConfig.InfoUpdateInterval.SettingChanged -= OnUpdateIntervalChanged;

            if (_inspectionCoroutine != null)
            {
                StopCoroutine(_inspectionCoroutine);
                _inspectionCoroutine = null;
            }

            _globalInfoPanel.Destroy();
            _precisionInfoPanel.Destroy();

            if (_placementInfoPanel != null)
            {
                _placementInfoPanel.Destroy();
                _placementInfoPanel = null;
            }

            if (_hoverInfoPanel != null)
            {
                _hoverInfoPanel.Destroy();
                _hoverInfoPanel = null;
            }
        }

        private void EnsureInspectionPanels()
        {
            if (_placementInfoPanel != null && _hoverInfoPanel != null)
            {
                return;
            }

            if (!TryGetComponent(out Hud hud) || !hud.m_crosshair)
            {
                if (!_loggedMissingCrosshair && ModConfig.IsDebugLogging)
                {
                    _loggedMissingCrosshair = true;
                    Plugin.LogDebug("[HudManager] Could not locate Hud.m_crosshair. Deferring inspection panel creation.");
                }
                return;
            }

            _loggedMissingCrosshair = false;
            Transform crosshairTransform = hud.m_crosshair.transform;

            if (_placementInfoPanel == null)
            {
                _placementInfoPanel = new PlacementInfoPanel(crosshairTransform);
            }

            if (_hoverInfoPanel == null)
            {
                _hoverInfoPanel = new HoverInfoPanel(crosshairTransform);
            }
        }

        private IEnumerator InspectionPollLoop()
        {
            while (true)
            {
                yield return _cachedWait;

                if (!ModConfig.EnableMod.Value)
                {
                    HideInspectionPanels();
                    continue;
                }

                Player localPlayer = Player.m_localPlayer;
                if (!localPlayer)
                {
                    HideInspectionPanels();
                    continue;
                }

                if (_placementInfoPanel == null || _hoverInfoPanel == null)
                {
                    EnsureInspectionPanels();
                }

                if (_placementInfoPanel != null)
                {
                    _placementInfoPanel.Refresh(localPlayer);
                }

                if (_hoverInfoPanel != null)
                {
                    _hoverInfoPanel.Refresh(localPlayer);
                }
            }
        }

        private void HideControlPanels()
        {
            _globalInfoPanel.SetActive(false);
            _precisionInfoPanel.SetActive(false);
        }

        private void HideInspectionPanels()
        {
            if (_placementInfoPanel != null)
            {
                _placementInfoPanel.SetActive(false);
            }

            if (_hoverInfoPanel != null)
            {
                _hoverInfoPanel.SetActive(false);
            }
        }

        private void OnUpdateIntervalChanged(object sender, EventArgs eventArgs) =>
            RebuildWaitInstruction();

        private void RebuildWaitInstruction()
        {
            float interval = Mathf.Max(0.05f, ModConfig.InfoUpdateInterval.Value);
            _cachedWait = new WaitForSeconds(interval);
        }
    }
}
