using UnityEngine;

namespace MasterBuilderHeeler.Core.Input
{
    public static class GamepadRotationGesture
    {
        private const float StickThreshold = 0.5f;
        private const float FirstRepeatDelay = 0.25f;
        private const float RepeatTimerRewind = 0.17f;

        private static float _rotateTimer;

        // same timing as the gamepad rotate in Player.UpdatePlacement: one step right away, the next after 0.25s,
        // then one every 0.08s. i read the gesture myself because the game skips its rotate code during in-place edit
        public static int ReadStepSign(float deltaTime)
        {
            int direction = GetDirection();
            if (direction == 0)
            {
                _rotateTimer = 0f;
                return 0;
            }

            int stepSign = 0;
            if (_rotateTimer == 0f)
            {
                stepSign = direction;
            }
            else if (_rotateTimer > FirstRepeatDelay)
            {
                stepSign = direction;
                _rotateTimer = RepeatTimerRewind;
            }

            _rotateTimer += deltaTime;
            return stepSign;
        }

        // the game does m_placeRotation++ on scroll up and +1 here is that same direction
        private static int GetDirection()
        {
            if (!GamepadChordLayer.IsBuildInputActive || GamepadChordLayer.IsHeld)
            {
                return 0;
            }

            if (ZInput.InputLayout == InputLayout.Default)
            {
                float stickX = ZInput.GetJoyRightStickX();
                if (!ZInput.GetButton("JoyRotate") || Mathf.Abs(stickX) <= StickThreshold)
                {
                    return 0;
                }

                return stickX < 0f ? 1 : -1;
            }

            if (ZInput.GetButton("JoyRotate"))
            {
                return -1;
            }

            return ZInput.GetButton("JoyRotateRight") ? 1 : 0;
        }
    }
}
