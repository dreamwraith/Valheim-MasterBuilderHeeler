using System;
using MasterBuilderHeeler.Configuration;
using MasterBuilderHeeler.Core.State;
using MasterBuilderHeeler.Core.Visuals.Gizmo.Themes.Solid;
using MasterBuilderHeeler.Core.Visuals.Gizmo.Themes.Wireframe;

namespace MasterBuilderHeeler.Core.Visuals.Gizmo
{
    public static class GizmoRendering
    {
        private static GizmoController? _controller;

        public static void Initialize()
        {
            Cleanup();
            ModConfig.Build.GizmoRendererStyle.SettingChanged += OnRendererSettingChanged;
            ModConfig.General.EnablePrecision.SettingChanged += OnRendererSettingChanged;
            BuildStateManager.PrecisionModeChanged += OnPrecisionModeChanged;
        }

        public static void Cleanup()
        {
            ModConfig.Build.GizmoRendererStyle.SettingChanged -= OnRendererSettingChanged;
            ModConfig.General.EnablePrecision.SettingChanged -= OnRendererSettingChanged;
            BuildStateManager.PrecisionModeChanged -= OnPrecisionModeChanged;
            DestroyController();
        }

        public static void EnsureController(Player player)
        {
            if (ModConfig.Build.GizmoRendererStyle.Value == GizmoStyle.Disabled || !ModConfig.General.EnablePrecision.Value)
            {
                return;
            }

            if (!player.TryGetComponent(out GizmoController controller))
            {
                controller = player.gameObject.AddComponent<GizmoController>();
            }

            controller.enabled = BuildStateManager.IsPrecisionModeActive;
        }

        public static IGizmoRenderer? CreateTheme(GizmoStyle style) => style switch
        {
            GizmoStyle.ProceduralWireframe => new WireframeGizmoRenderer(),
            GizmoStyle.Solid => new SolidGizmoRenderer(),
            _ => null
        };

        internal static void RegisterController(GizmoController controller) =>
            _controller = controller;

        internal static void UnregisterController(GizmoController controller)
        {
            if (_controller == controller)
            {
                _controller = null;
            }
        }

        private static void DestroyController()
        {
            if (_controller)
            {
                UnityEngine.Object.Destroy(_controller);
                _controller = null;
            }
        }

        private static void OnRendererSettingChanged(object sender, EventArgs eventArgs)
        {
            GizmoStyle style = ModConfig.Build.GizmoRendererStyle.Value;
            if (style == GizmoStyle.Disabled || !ModConfig.General.EnablePrecision.Value)
            {
                DestroyController();
                return;
            }

            if (_controller)
            {
                _controller.SetStyle(style);
                _controller.enabled = BuildStateManager.IsPrecisionModeActive;
                return;
            }

            Player localPlayer = Player.m_localPlayer;
            if (localPlayer)
            {
                EnsureController(localPlayer);
            }
        }

        private static void OnPrecisionModeChanged(bool active)
        {
            if (_controller)
            {
                _controller.enabled = active;
            }
        }
    }
}
