using UnityEngine;

namespace UnifiedPlayerController
{
    /// <summary>
    /// Allows the camera to dynamically follow the player's head bone with an adjustable offset.
    /// Supports toggling which axes to follow, smoothing movement, and snapping to position when the mouse is inactive.
    /// Useful for immersive first-person or over-the-shoulder camera systems.
    /// </summary>
    /// <remarks>
    /// Attach this script to your camera GameObject and assign the player's head bone in the inspector.
    /// Works well with <see cref="UnifiedPlayerController"/> for immersive camera setups.
    /// </remarks>
    /// <seealso cref="UnifiedPlayerController"/>
    public class DynamicFollowHead : MonoBehaviour
    {
        [Header("Head and Offset Settings")]
        /// <summary>
        /// The transform of the player's head bone to follow.
        /// Assign this in the inspector. The camera will track this transform's position and orientation.
        /// </summary>
        public Transform headBone;

        /// <summary>
        /// The local offset from the head bone's position.
        /// This allows the camera to be positioned at a specific point relative to the head (e.g., slightly behind or above).
        /// </summary>
        public Vector3 offset;

        [Header("Axis Toggles")]
        /// <summary>
        /// If true, the camera will follow the X axis of the head bone.
        /// </summary>
        public bool followX = true;
        /// <summary>
        /// If true, the camera will follow the Y axis of the head bone.
        /// </summary>
        public bool followY = true;
        /// <summary>
        /// If true, the camera will follow the Z axis of the head bone.
        /// </summary>
        public bool followZ = true;

        [Header("Smoothing Settings")]
        /// <summary>
        /// The smoothing speed used when the character is walking or moving normally.
        /// Lower values result in slower, smoother camera movement.
        /// </summary>
        [Tooltip("Smooth speed when walking (lower value means slower update)")]
        public float normalSmoothSpeed = 10f;

        /// <summary>
        /// The smoothing speed used when the character is sprinting.
        /// Higher values result in faster camera updates to keep up with rapid movement.
        /// </summary>
        [Tooltip("Smooth speed when sprinting (higher value means faster update)")]
        public float sprintSmoothSpeed = 20f;

        /// <summary>
        /// The distance threshold at which the camera will snap directly to the target position,
        /// instead of smoothing, to prevent lagging behind during fast movement or teleportation.
        /// </summary>
        [Tooltip("Threshold distance; if exceeded, the camera will snap to position")]
        public float snapThreshold = 0.5f;

        /// <summary>
        /// The amount of time (in seconds) to wait after mouse input stops before the camera recenters or snaps.
        /// Prevents the camera from snapping while the player is actively looking around.
        /// </summary>
        [Tooltip("Time to wait after mouse input ceases before recentering")]
        public float recenterDelay = 0.5f;

        [Header("Movement State (if available)")]
        /// <summary>
        /// Indicates whether the character is currently sprinting.
        /// This should be set by your character movement script to adjust camera smoothing dynamically.
        /// </summary>
        [Tooltip("Set this flag from your character movement script")]
        public bool isSprinting = false;

        /// <summary>
        /// Internal velocity used by SmoothDamp for smooth camera movement.
        /// </summary>
        private Vector3 velocity = Vector3.zero;

        /// <summary>
        /// Tracks how long the mouse has been inactive, used to trigger camera recentering.
        /// </summary>
        private float mouseInactiveTime = 0f;

        /// <summary>
        /// Unity LateUpdate callback. Updates the camera's position based on the head bone, offset, axis toggles, and smoothing settings.
        /// </summary>
        /// <remarks>
        /// Called after all Update methods. Handles smoothing, snapping, and axis toggles for immersive camera following.
        /// </remarks>
        void LateUpdate()
        {
            // Get mouse input to detect if the player is actively looking around.
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            // If the mouse is moved, reset the inactivity timer.
            // Otherwise, increment the timer to track how long the mouse has been idle.
            if (Mathf.Abs(mouseX) > 0.01f || Mathf.Abs(mouseY) > 0.01f)
            {
                mouseInactiveTime = 0f;
            }
            else
            {
                mouseInactiveTime += Time.deltaTime;
            }

            // If no head bone is assigned, do nothing.
            if (!headBone)
                return;

            // Calculate the desired camera position using the head bone's local axes and the specified offset.
            // This allows the camera to follow the head's orientation and position.
            Vector3 desiredPosition = headBone.position +
                                     headBone.right * offset.x +
                                     headBone.up * offset.y +
                                     headBone.forward * offset.z;

            // Build the target position, only updating the axes that are enabled.
            Vector3 targetPosition = transform.position;
            if (followX) targetPosition.x = desiredPosition.x;
            if (followY) targetPosition.y = desiredPosition.y;
            if (followZ) targetPosition.z = desiredPosition.z;

            // Choose the smoothing speed based on whether the character is sprinting.
            float currentSmoothSpeed = isSprinting ? sprintSmoothSpeed : normalSmoothSpeed;

            // If the camera is too far from the target and the mouse has been inactive long enough, snap to the target.
            // Otherwise, smoothly move toward the target position.
            if (Vector3.Distance(transform.position, targetPosition) > snapThreshold && mouseInactiveTime >= recenterDelay)
            {
                transform.position = targetPosition;
            }
            else
            {
                transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, 1f / currentSmoothSpeed);
            }
        }
    }
}