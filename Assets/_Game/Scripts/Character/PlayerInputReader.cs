using UnityEngine;
using UnityEngine.InputSystem;

namespace AICompanionRoguelike.Character
{
    /// <summary>
    /// Reads player input and exposes stable values for movement components.
    /// </summary>
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [Header("Movement Keys")]
        [SerializeField] private Key leftKey = Key.A;
        [SerializeField] private Key alternateLeftKey = Key.LeftArrow;
        [SerializeField] private Key rightKey = Key.D;
        [SerializeField] private Key alternateRightKey = Key.RightArrow;

        [Header("Action Keys")]
        [SerializeField] private Key jumpKey = Key.Space;
        [SerializeField] private Key dashKey = Key.LeftShift;
        [SerializeField] private Key alternateDashKey = Key.RightShift;

        [Header("Gamepad")]
        [SerializeField] private bool readGamepad = true;
        [SerializeField, Range(0f, 1f)] private float gamepadMoveDeadZone = 0.2f;

        private bool jumpQueued;
        private bool dashQueued;

        public float MoveInput { get; private set; }
        public bool JumpHeld { get; private set; }

        private void OnDisable()
        {
            MoveInput = 0f;
            JumpHeld = false;
            jumpQueued = false;
            dashQueued = false;
        }

        private void Update()
        {
            ReadKeyboard();

            if (readGamepad)
            {
                ReadGamepad();
            }
        }

        public bool ConsumeJumpPressed()
        {
            if (!jumpQueued)
            {
                return false;
            }

            jumpQueued = false;
            return true;
        }

        public bool ConsumeDashPressed()
        {
            if (!dashQueued)
            {
                return false;
            }

            dashQueued = false;
            return true;
        }

        private void ReadKeyboard()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                MoveInput = 0f;
                JumpHeld = false;
                return;
            }

            bool leftPressed = IsPressed(keyboard, leftKey) || IsPressed(keyboard, alternateLeftKey);
            bool rightPressed = IsPressed(keyboard, rightKey) || IsPressed(keyboard, alternateRightKey);

            if (leftPressed == rightPressed)
            {
                MoveInput = 0f;
            }
            else
            {
                MoveInput = leftPressed ? -1f : 1f;
            }

            JumpHeld = IsPressed(keyboard, jumpKey);

            if (WasPressedThisFrame(keyboard, jumpKey))
            {
                jumpQueued = true;
            }

            if (WasPressedThisFrame(keyboard, dashKey) || WasPressedThisFrame(keyboard, alternateDashKey))
            {
                dashQueued = true;
            }
        }

        private void ReadGamepad()
        {
            Gamepad gamepad = Gamepad.current;
            if (gamepad == null)
            {
                return;
            }

            float stickX = gamepad.leftStick.x.ReadValue();
            if (Mathf.Abs(stickX) >= gamepadMoveDeadZone)
            {
                MoveInput = Mathf.Sign(stickX);
            }

            JumpHeld = JumpHeld || gamepad.buttonSouth.isPressed;

            if (gamepad.buttonSouth.wasPressedThisFrame)
            {
                jumpQueued = true;
            }

            if (gamepad.buttonEast.wasPressedThisFrame || gamepad.rightShoulder.wasPressedThisFrame)
            {
                dashQueued = true;
            }
        }

        private static bool IsPressed(Keyboard keyboard, Key key)
        {
            return key != Key.None && keyboard[key].isPressed;
        }

        private static bool WasPressedThisFrame(Keyboard keyboard, Key key)
        {
            return key != Key.None && keyboard[key].wasPressedThisFrame;
        }
    }
}
