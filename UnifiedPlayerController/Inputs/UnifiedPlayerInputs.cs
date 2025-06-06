using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace UnifiedPlayerController
{
    /// <summary>
    /// Handles player input for movement, looking, jumping, sprinting, and cursor state.
    /// Supports both Unity's legacy and new Input System.
    /// </summary>
    /// <remarks>
    /// This component should be attached to the player GameObject and is used by <see cref="UnifiedPlayerController"/>.
    /// It abstracts input handling so the controller can remain agnostic to the input system.
    /// </remarks>
    /// <seealso cref="UnifiedPlayerController"/>
    public class UnifiedPlayerInputs : MonoBehaviour
    {
        [Header("Character Input Values")]
        /// <summary>
        /// Stores the current movement input as a Vector2 (x = horizontal, y = vertical).
        /// </summary>
        /// <seealso cref="UnifiedPlayerController"/>
        public Vector2 move;

        /// <summary>
        /// Stores the current look input as a Vector2 (x = mouse X, y = mouse Y).
        /// </summary>
        /// <seealso cref="UnifiedPlayerController"/>
        public Vector2 look;

        /// <summary>
        /// True if the jump input is pressed.
        /// </summary>
        /// <seealso cref="UnifiedPlayerController"/>
        public bool jump;

        /// <summary>
        /// True if the sprint input is pressed.
        /// </summary>
        /// <seealso cref="UnifiedPlayerController"/>
        public bool sprint;

        [Header("Movement Settings")]
        /// <summary>
        /// If true, enables analog movement (for gamepads).
        /// </summary>
        public bool analogMovement;

        [Header("Mouse Cursor Settings")]
        /// <summary>
        /// If true, the mouse cursor will be locked to the center of the screen.
        /// </summary>
        public bool cursorLocked = true;

        /// <summary>
        /// If true, mouse input will be used for looking around.
        /// </summary>
        public bool cursorInputForLook = true;

#if ENABLE_INPUT_SYSTEM
        /// <summary>
        /// Called by Unity's Input System when movement input is received.
        /// </summary>
        /// <param name="value">The movement input value.</param>
        public void OnMove(InputValue value)
        {
            MoveInput(value.Get<Vector2>());
        }

        /// <summary>
        /// Called by Unity's Input System when look input is received.
        /// </summary>
        /// <param name="value">The look input value.</param>
        public void OnLook(InputValue value)
        {
            if(cursorInputForLook)
            {
                LookInput(value.Get<Vector2>());
            }
        }

        /// <summary>
        /// Called by Unity's Input System when jump input is received.
        /// </summary>
        /// <param name="value">The jump input value.</param>
        public void OnJump(InputValue value)
        {
            JumpInput(value.isPressed);
        }

        /// <summary>
        /// Called by Unity's Input System when sprint input is received.
        /// </summary>
        /// <param name="value">The sprint input value.</param>
        public void OnSprint(InputValue value)
        {
            SprintInput(value.isPressed);
        }
#endif

        /// <summary>
        /// Sets the movement input value.
        /// </summary>
        /// <param name="newMoveDirection">The new movement direction.</param>
        /// <seealso cref="move"/>
        public void MoveInput(Vector2 newMoveDirection)
        {
            move = newMoveDirection;
        } 

        /// <summary>
        /// Sets the look input value.
        /// </summary>
        /// <param name="newLookDirection">The new look direction.</param>
        /// <seealso cref="look"/>
        public void LookInput(Vector2 newLookDirection)
        {
            look = newLookDirection;
        }

        /// <summary>
        /// Sets the jump input value.
        /// </summary>
        /// <param name="newJumpState">True if jump is pressed.</param>
        /// <seealso cref="jump"/>
        public void JumpInput(bool newJumpState)
        {
            jump = newJumpState;
        }

        /// <summary>
        /// Sets the sprint input value.
        /// </summary>
        /// <param name="newSprintState">True if sprint is pressed.</param>
        /// <seealso cref="sprint"/>
        public void SprintInput(bool newSprintState)
        {
            sprint = newSprintState;
        }
        
        /// <summary>
        /// Called when the application gains or loses focus.
        /// Locks or unlocks the cursor based on the cursorLocked setting.
        /// </summary>
        /// <param name="hasFocus">True if the application has focus.</param>
        private void OnApplicationFocus(bool hasFocus)
        {
            SetCursorState(cursorLocked);
        }

        /// <summary>
        /// Sets the cursor lock state.
        /// </summary>
        /// <param name="newState">True to lock the cursor, false to unlock.</param>
        private void SetCursorState(bool newState)
        {
            Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
        }
    }
}