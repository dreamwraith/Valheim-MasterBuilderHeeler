using System.Collections.Generic;
using MasterBuilderHeeler.Configuration;
using MasterBuilderHeeler.Core.Placement;
using MasterBuilderHeeler.Core.Rotation;
using MasterBuilderHeeler.Core.State;
using UnityEngine;

namespace MasterBuilderHeeler.Core.Visuals.Gizmo
{
    public sealed class GizmoController : MonoBehaviour
    {
        private const float DefaultRadius = 1.8f;
        private const float MinRadius = 1.325f;
        private const float MaxRadius = 6.0f;
        private const float RadiusPaddingFactor = 1.425f;
        private const int MaxRadiusRetries = 10;

        private static readonly List<Collider> ColliderBuffer = new List<Collider>(16);

        private IGizmoRenderer? _activeTheme;
        private int _trackedTargetInstanceId;
        private float _calculatedRadius = DefaultRadius;
        private int _radiusRetryCount;
        private bool _radiusCalculated;
        private bool _wasVisible;

        private void Awake()
        {
            GizmoRendering.RegisterController(this);
            SetStyle(ModConfig.Build.GizmoRendererStyle.Value);
            enabled = BuildStateManager.IsPrecisionModeActive;
        }

        private void OnDisable() => HideIfVisible();

        private void LateUpdate()
        {
            Player localPlayer = Player.m_localPlayer;
            GameObject? targetObject = BuildStateManager.GetActiveTargetObject(localPlayer);
            if (_activeTheme == null ||
                !ModConfig.General.EnableMod.Value ||
                !BuildStateManager.IsPrecisionModeActive ||
                !targetObject ||
                !targetObject.activeInHierarchy ||
                BuildStateManager.IsTerrainOpGhostActive)
            {
                HideIfVisible();
                if (!targetObject)
                {
                    _trackedTargetInstanceId = 0;
                }
                return;
            }

            UpdateTargetBounds(targetObject);

            Transform targetTransform = targetObject.transform;
            Quaternion pieceRotation = targetTransform.rotation;
            (Quaternion pitchRingBasis, Quaternion yawRingBasis, Quaternion rollRingBasis) =
                PieceRotationEngine.ResolveRingBases(pieceRotation);
            (Vector3 rightAxis, Vector3 upAxis, Vector3 forwardAxis) =
                BuildFrameBasis.ResolveTranslationAxes(pieceRotation);

            GizmoDrawContext context = new GizmoDrawContext(
                targetTransform.position,
                _calculatedRadius,
                ModConfig.Build.GizmoScale.Value,
                pitchRingBasis,
                yawRingBasis,
                rollRingBasis,
                BuildStateManager.ActiveAxis,
                rightAxis,
                upAxis,
                forwardAxis,
                ModConfig.Build.GizmoPitchRingColor.Value,
                ModConfig.Build.GizmoYawRingColor.Value,
                ModConfig.Build.GizmoRollRingColor.Value,
                ModConfig.Build.GizmoRightArrowColor.Value,
                ModConfig.Build.GizmoUpArrowColor.Value,
                ModConfig.Build.GizmoForwardArrowColor.Value,
                ModConfig.Build.EnableGizmoLighting.Value);

            _activeTheme.Render(in context);
            _wasVisible = true;
        }

        private void OnDestroy()
        {
            DisposeActiveTheme();
            GizmoRendering.UnregisterController(this);
        }

        public void SetStyle(GizmoStyle style)
        {
            if (_activeTheme != null && _activeTheme.Style == style)
            {
                return;
            }

            DisposeActiveTheme();
            _activeTheme = GizmoRendering.CreateTheme(style);
            _wasVisible = false;
        }

        private void HideIfVisible()
        {
            if (!_wasVisible)
            {
                return;
            }

            if (_activeTheme != null)
            {
                _activeTheme.OnHide();
            }

            _wasVisible = false;
        }

        private void DisposeActiveTheme()
        {
            if (_activeTheme == null)
            {
                return;
            }

            if (_wasVisible)
            {
                _activeTheme.OnHide();
                _wasVisible = false;
            }

            _activeTheme.Dispose();
            _activeTheme = null;
        }

        private void UpdateTargetBounds(GameObject targetObject)
        {
            int instanceId = targetObject.GetInstanceID();
            if (instanceId != _trackedTargetInstanceId)
            {
                _trackedTargetInstanceId = instanceId;
                _calculatedRadius = DefaultRadius;
                _radiusRetryCount = 0;
                _radiusCalculated = false;
            }

            if (!_radiusCalculated)
            {
                CalculateRadius(targetObject);
            }
        }

        private void CalculateRadius(GameObject targetObject)
        {
            targetObject.GetComponentsInChildren(ColliderBuffer);
            int colliderCount = ColliderBuffer.Count;
            int solidColliderCount = 0;
            Bounds bounds = default;

            for (int index = 0; index < colliderCount; index++)
            {
                Collider collider = ColliderBuffer[index];
                if (!collider || !collider.enabled || collider.isTrigger)
                {
                    continue;
                }

                if (solidColliderCount == 0)
                {
                    bounds = collider.bounds;
                }
                else
                {
                    bounds.Encapsulate(collider.bounds);
                }

                solidColliderCount++;
            }

            ColliderBuffer.Clear();

            if (solidColliderCount == 0)
            {
                _calculatedRadius = DefaultRadius;
                _radiusCalculated = true;
                return;
            }

            // collider bounds can still be zero on a brand new object, use the default and retry for a few frames
            if (bounds.size.sqrMagnitude < 0.0001f)
            {
                _calculatedRadius = DefaultRadius;
                _radiusRetryCount++;
                if (_radiusRetryCount >= MaxRadiusRetries)
                {
                    _radiusCalculated = true;
                    if (ModConfig.IsDebugLogging)
                    {
                        Plugin.LogDebug($"[GizmoController] Target '{targetObject.name}' collider bounds remained zero after {MaxRadiusRetries} frames. Locking default radius ({DefaultRadius:F2}m).");
                    }
                }
                else if (_radiusRetryCount == 1 && ModConfig.IsDebugLogging)
                {
                    Plugin.LogDebug($"[GizmoController] Target '{targetObject.name}' collider bounds uninitialized on frame 0. Deferring radius calculation.");
                }
                return;
            }

            float extent = Mathf.Max(bounds.extents.x, bounds.extents.z);
            _calculatedRadius = Mathf.Clamp(extent * RadiusPaddingFactor, MinRadius, MaxRadius);
            _radiusCalculated = true;
            if (ModConfig.IsDebugLogging)
            {
                Plugin.LogDebug($"[GizmoController] Resolved bounds for '{targetObject.name}': extent={extent:F2}m across {solidColliderCount} solid colliders -> radius={_calculatedRadius:F2}m.");
            }
        }
    }
}
