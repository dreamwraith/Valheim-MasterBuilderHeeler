using HarmonyLib;
using MasterBuilderHeeler.Core.Input;

namespace MasterBuilderHeeler.Patches
{
    [HarmonyPatch(typeof(ZInput))]
    public static class ZInputPatches
    {
        [HarmonyPatch(nameof(ZInput.InternalUpdate))]
        [HarmonyPostfix]
        private static void InternalUpdatePostfix() =>
            GamepadChordLayer.OnInputUpdated();

        // buttons tick again for the fixed step and PlayerController reads them there, so the reset has to follow
        [HarmonyPatch(nameof(ZInput.InternalUpdateFixed))]
        [HarmonyPostfix]
        private static void InternalUpdateFixedPostfix() =>
            GamepadChordLayer.SuppressBaseButtons();
    }
}
