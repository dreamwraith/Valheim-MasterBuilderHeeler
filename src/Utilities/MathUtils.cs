using UnityEngine;

namespace MasterBuilderHeeler.Utilities
{
    public static class MathUtils
    {
        public static float SnapScalar(float value, float step) =>
            Mathf.Round(value / step) * step;

        public static string FormatEulerPlain(Vector3 euler) =>
            $"({euler.x:0.#} deg, {euler.y:0.#} deg, {euler.z:0.#} deg)";
    }
}
