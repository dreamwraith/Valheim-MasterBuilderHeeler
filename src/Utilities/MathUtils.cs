using UnityEngine;

namespace MasterBuilderHeeler.Utilities
{
    public static class MathUtils
    {
        // WriteSmallRotation and ReadSmallRotation pack euler angles in 0.5 degree steps
        public const float SavedRotationStep = 0.5f;

        public static float SnapScalar(float value, float step) =>
            Mathf.Round(value / step) * step;

        public static Vector3 SnapEulerToSaveGrid(Vector3 eulerAngles) =>
            new Vector3(
                Utils.QuantizeHalfDegrees(eulerAngles.x) * SavedRotationStep,
                Utils.QuantizeHalfDegrees(eulerAngles.y) * SavedRotationStep,
                Utils.QuantizeHalfDegrees(eulerAngles.z) * SavedRotationStep);

        public static string FormatEulerPlain(Vector3 euler) =>
            $"({euler.x:0.#} deg, {euler.y:0.#} deg, {euler.z:0.#} deg)";
    }
}
