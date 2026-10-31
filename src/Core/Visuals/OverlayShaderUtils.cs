using UnityEngine;

namespace MasterBuilderHeeler.Core.Visuals
{
    internal static class OverlayShaderUtils
    {
        // valheim builds strip the standard unlit shaders. these built ins stick around and multiply material color
        // by vertex color with alpha blending, which is all the overlays need
        public static Shader? FindUnlitVertexColorShader()
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader)
            {
                return shader;
            }

            shader = Shader.Find("Hidden/Internal-Colored");
            if (shader)
            {
                return shader;
            }

            shader = Shader.Find("UI/Default");
            if (shader)
            {
                return shader;
            }

            return null;
        }
    }
}
