using UnityEngine;

namespace MasterBuilderHeeler.Utilities
{
    public static class CameraUtils
    {
        public static Camera? GetActiveCamera() =>
            GameCamera.instance && GameCamera.instance.m_camera
                ? GameCamera.instance.m_camera
                : Camera.main;
    }
}
