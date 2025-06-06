using UnityEngine;

namespace UnifiedPlayerController
{
    /// <summary>
    /// Allows the player to push rigid bodies when colliding with them using a CharacterController.
    /// Applies a force to the rigid body based on the character's movement direction.
    /// </summary>
    /// <remarks>
    /// Attach this component to a GameObject with a CharacterController to enable pushing of rigidbodies on specified layers.
    /// Used by <see cref="UnifiedPlayerController"/> for interactive physics.
    /// </remarks>
    /// <seealso cref="UnifiedPlayerController"/>
    public class BasicRigidBodyPush : MonoBehaviour
    {
        /// <summary>
        /// Layers that can be pushed by the player.
        /// Only objects on these layers will be affected by the push.
        /// </summary>
        public LayerMask pushLayers;

        /// <summary>
        /// If true, enables pushing of rigid bodies.
        /// </summary>
        public bool canPush;

        /// <summary>
        /// The strength of the push force applied to rigid bodies.
        /// </summary>
        [Range(0.5f, 5f)]
        public float strength = 1.1f;

        /// <summary>
        /// Unity callback called when the CharacterController hits another collider.
        /// Triggers the push logic if pushing is enabled.
        /// </summary>
        /// <param name="hit">Information about the collision.</param>
        /// <seealso cref="PushRigidBodies"/>
        /// <seealso cref="canPush"/>
        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (canPush) PushRigidBodies(hit);
        }

        /// <summary>
        /// Applies a force to a rigid body if it meets the criteria for being pushed.
        /// </summary>
        /// <param name="hit">Information about the collision.</param>
        /// <seealso cref="pushLayers"/>
        /// <seealso cref="strength"/>
        /// <seealso cref="OnControllerColliderHit"/>
        private void PushRigidBodies(ControllerColliderHit hit)
        {
            // Get the rigidbody attached to the collider.
            Rigidbody body = hit.collider.attachedRigidbody;
            // Only push non-kinematic rigidbodies.
            if (body == null || body.isKinematic) return;

            // Only push objects on the specified layers.
            var bodyLayerMask = 1 << body.gameObject.layer;
            if ((bodyLayerMask & pushLayers.value) == 0) return;

            // Do not push objects below the player.
            if (hit.moveDirection.y < -0.3f) return;

            // Calculate push direction (horizontal only).
            Vector3 pushDir = new Vector3(hit.moveDirection.x, 0.0f, hit.moveDirection.z);

            // Apply the push force.
            body.AddForce(pushDir * strength, ForceMode.Impulse);
        }
    }
}