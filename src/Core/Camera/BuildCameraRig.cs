using System;
using MasterBuilderHeeler.Configuration;
using UnityEngine;

namespace MasterBuilderHeeler.Core.Camera
{
    public static class BuildCameraRig
    {
        private const float WisplightCheckInterval = 0.25f;

        private static readonly int EffectLayer = LayerMask.NameToLayer("effect");

        private static GameObject? _rigObject;
        private static Light? _light;
        private static GameObject? _demisterObject;
        private static ParticleSystemForceField? _forceField;
        private static bool _settingsDirty = true;
        private static float _nextWisplightCheckTime;
        private static bool _cachedHasWisplight;

        public static void Initialize()
        {
            Cleanup();
            ModConfig.EnableCameraLight.SettingChanged += OnRigSettingChanged;
            ModConfig.CameraLightColor.SettingChanged += OnRigSettingChanged;
            ModConfig.CameraLightIntensity.SettingChanged += OnRigSettingChanged;
            ModConfig.CameraLightRange.SettingChanged += OnRigSettingChanged;
            ModConfig.CameraLightSpotAngle.SettingChanged += OnRigSettingChanged;
            ModConfig.EnableCameraDemister.SettingChanged += OnRigSettingChanged;
            ModConfig.RequireWisplightForDemister.SettingChanged += OnRigSettingChanged;
            ModConfig.CameraDemisterRadius.SettingChanged += OnRigSettingChanged;
            _settingsDirty = true;
            _nextWisplightCheckTime = 0f;
        }

        public static void Cleanup()
        {
            ModConfig.EnableCameraLight.SettingChanged -= OnRigSettingChanged;
            ModConfig.CameraLightColor.SettingChanged -= OnRigSettingChanged;
            ModConfig.CameraLightIntensity.SettingChanged -= OnRigSettingChanged;
            ModConfig.CameraLightRange.SettingChanged -= OnRigSettingChanged;
            ModConfig.CameraLightSpotAngle.SettingChanged -= OnRigSettingChanged;
            ModConfig.EnableCameraDemister.SettingChanged -= OnRigSettingChanged;
            ModConfig.RequireWisplightForDemister.SettingChanged -= OnRigSettingChanged;
            ModConfig.CameraDemisterRadius.SettingChanged -= OnRigSettingChanged;
            Destroy();
        }

        public static void Destroy()
        {
            if (_rigObject)
            {
                UnityEngine.Object.Destroy(_rigObject);
            }

            _rigObject = null;
            _light = null;
            _demisterObject = null;
            _forceField = null;
        }

        public static void UpdateRig(Player player, Transform cameraTransform)
        {
            // rig lives under the camera so a scene unload kills it and Destroy never runs.
            // thats why all the rechecks - im too dumb to find a better way, maybe redseiko will yell at me
            if (!_rigObject || !_light || !_demisterObject || !_forceField)
            {
                Destroy();
                Create(cameraTransform);
            }

            if (_settingsDirty)
            {
                _light!.enabled = ModConfig.EnableCameraLight.Value;
                _light.color = ModConfig.CameraLightColor.Value;
                _light.intensity = ModConfig.CameraLightIntensity.Value;
                _light.range = ModConfig.CameraLightRange.Value;
                _light.spotAngle = ModConfig.CameraLightSpotAngle.Value;
                _forceField!.endRange = ModConfig.CameraDemisterRadius.Value;
                _settingsDirty = false;
            }

            bool demisterActive = false;
            if (ModConfig.EnableCameraDemister.Value)
            {
                if (!ModConfig.RequireWisplightForDemister.Value)
                {
                    demisterActive = true;
                }
                else
                {
                    if (Time.time >= _nextWisplightCheckTime)
                    {
                        _nextWisplightCheckTime = Time.time + WisplightCheckInterval;
                        _cachedHasWisplight = HasWisplightEffect(player);
                    }
                    demisterActive = _cachedHasWisplight;
                }
            }

            if (_demisterObject!.activeSelf != demisterActive)
            {
                _demisterObject.SetActive(demisterActive);
            }
        }

        private static void OnRigSettingChanged(object sender, EventArgs eventArgs)
        {
            _settingsDirty = true;
            _nextWisplightCheckTime = 0f;
        }

        private static void Create(Transform cameraTransform)
        {
            _rigObject = new GameObject("MBH_BuildCameraRig");
            _rigObject.transform.SetParent(cameraTransform, false);

            _light = _rigObject.AddComponent<Light>();
            _light.type = LightType.Spot;
            _light.shadows = LightShadows.None;

            // stay inactive until the force field is set up, Demister.Awake and OnEnable grab and register it immediately
            _demisterObject = new GameObject("MBH_BuildCameraDemister") { layer = EffectLayer };
            _demisterObject.SetActive(false);
            _demisterObject.transform.SetParent(_rigObject.transform, false);

            _forceField = _demisterObject.AddComponent<ParticleSystemForceField>();
            _forceField.shape = ParticleSystemForceFieldShape.Sphere;
            _forceField.startRange = 0f;
            _forceField.gravity = -0.15f;
            _forceField.gravityFocus = 0f;
            _forceField.drag = 0f;

            _demisterObject.AddComponent<Demister>();
            _settingsDirty = true;
            _nextWisplightCheckTime = 0f;
        }

        // SEMan has no static wisplight hash for some reason, and other mods subclass SE_Demister anyway. so just check the type
        private static bool HasWisplightEffect(Player player)
        {
            foreach (StatusEffect statusEffect in player.GetSEMan().GetStatusEffects())
            {
                if (statusEffect && statusEffect is SE_Demister)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
