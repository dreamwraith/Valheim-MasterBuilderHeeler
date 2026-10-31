using UnityEngine;

namespace MasterBuilderHeeler.Utilities
{
    public static class InputUtils
    {
        public static bool IsTextInputOrMenuOpen()
        {
            if (Chat.instance && Chat.instance.HasFocus())
            {
                return true;
            }

            if (Console.IsVisible() || TextInput.IsVisible() || Menu.IsVisible() || InventoryGui.IsVisible() || StoreGui.IsVisible() || Hud.IsPieceSelectionVisible() || Hud.InRadial())
            {
                return true;
            }

            if (Minimap.InTextInput())
            {
                return true;
            }

            if (Hud.instance && Hud.instance.m_buildUi && Hud.instance.m_buildUi.SearchFieldFocused)
            {
                return true;
            }

            if (PlayerCustomizaton.IsBarberGuiVisible())
            {
                return true;
            }

            return false;
        }

        public static Vector3 GetBuildCameraMoveDirection(Transform cameraTransform)
        {
            Vector3 moveDirection = Vector3.zero;

            if (ZInput.GetButton("Forward")) moveDirection += cameraTransform.forward;
            if (ZInput.GetButton("Backward")) moveDirection -= cameraTransform.forward;
            if (ZInput.GetButton("Right")) moveDirection += cameraTransform.right;
            if (ZInput.GetButton("Left")) moveDirection -= cameraTransform.right;
            if (ZInput.GetButton("Jump")) moveDirection += Vector3.up;
            if (ZInput.GetButton("Crouch")) moveDirection += Vector3.down;

            return moveDirection;
        }
    }
}
