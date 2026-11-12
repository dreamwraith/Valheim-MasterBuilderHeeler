using MasterBuilderHeeler.Configuration;
using MasterBuilderHeeler.Core.Input;
using UnityEngine;

namespace MasterBuilderHeeler.Utilities
{
    public static class InputUtils
    {
        private const float BuildCameraMouseLookScale = 1.5f;
        private const float GamepadLookDegreesPerSecond = 110f;

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
            bool isGamepadActive = GamepadChordLayer.IsBuildInputActive;
            Vector3 moveDirection = Vector3.zero;

            if (ZInput.GetButton("Forward")) moveDirection += cameraTransform.forward;
            if (ZInput.GetButton("Backward")) moveDirection -= cameraTransform.forward;
            if (ZInput.GetButton("Right")) moveDirection += cameraTransform.right;
            if (ZInput.GetButton("Left")) moveDirection -= cameraTransform.right;
            if (ZInput.GetButton("Jump")) moveDirection += Vector3.up;
            if (ZInput.GetButton("Crouch")) moveDirection += Vector3.down;

            if (isGamepadActive)
            {
                if (ZInput.GetButton("JoyJump")) moveDirection += Vector3.up;
                // snap point cycling is a short tap on the same stick click as crouch, so going down is a hold
                if (ZInput.GetButtonPressedTimer("JoyCrouch") > GamepadChordLayer.TapThreshold) moveDirection += Vector3.down;
            }

            moveDirection.Normalize();

            if (isGamepadActive)
            {
                Vector2 leftStick = ZInput.GetJoyLeftStick();
                moveDirection += (cameraTransform.right * leftStick.x) + (cameraTransform.forward * leftStick.y);
            }

            return Vector3.ClampMagnitude(moveDirection, 1f);
        }

        // degrees to turn this frame. the invert settings, the stick math and no look while the rotate button is held
        // are all the same as PlayerController.LateUpdate
        public static Vector2 GetBuildCameraLookDelta(float deltaTime)
        {
            float mouseSensitivity = ModConfig.Build.BuildCameraLookSensitivity.Value * PlayerController.m_mouseSens * BuildCameraMouseLookScale;
            Vector2 lookDelta = ZInput.GetMouseDelta() * mouseSensitivity;
            if (PlayerController.m_invertMouse)
            {
                lookDelta.y = -lookDelta.y;
            }

            if (!GamepadChordLayer.IsBuildInputActive || ZInput.GetButton("JoyRotate"))
            {
                return lookDelta;
            }

            Vector2 stickLook = ZInput.GetJoyRightStick() * (GamepadLookDegreesPerSecond * deltaTime * PlayerController.m_gamepadSens);
            if (PlayerController.m_invertCameraX)
            {
                stickLook.x = -stickLook.x;
            }

            if (PlayerController.m_invertCameraY)
            {
                stickLook.y = -stickLook.y;
            }

            return lookDelta + stickLook;
        }

        public static bool IsBuildCameraSprintHeld() =>
            ZInput.GetButton("Run") || (GamepadChordLayer.IsBuildInputActive && ZInput.GetButton("JoyRun"));
    }
}
