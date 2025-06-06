using UnityEngine;
using Unity.Cinemachine;

namespace UnifiedPlayerController
{
    /// <summary>
    /// Allows zooming in and out of a third-person camera using the mouse scroll wheel.
    /// Uses the CinemachineThirdPersonFollow component to adjust the camera distance.
    /// </summary>
    /// <remarks>
    /// Attach this script to a GameObject with a CinemachineCamera. 
    /// Assign the virtual camera in the inspector. 
    /// Used in conjunction with <see cref="UnifiedPlayerController"/> for third-person gameplay.
    /// </remarks>
    /// <seealso cref="UnifiedPlayerController"/>
    /// <seealso cref="CinemachineThirdPersonFollow"/>
    [RequireComponent(typeof(CinemachineCamera))]
    public class ThirdPersonCameraZoom : MonoBehaviour
    {
        /// <summary>
        /// Reference to the CinemachineCamera (or VirtualCamera) that this script will control.
        /// Assign this in the inspector.
        /// </summary>
        [SerializeField]
        private CinemachineCamera virtualCamera;

        /// <summary>
        /// Speed at which the camera zooms in and out based on mouse scroll input.
        /// Higher values make the zoom more sensitive.
        /// </summary>
        [SerializeField]
        private float zoomSpeed = 10f;

        /// <summary>
        /// Minimum distance the camera can zoom in.
        /// </summary>
        [SerializeField]
        private float minDistance = 1f;

        /// <summary>
        /// Maximum distance the camera can zoom out.
        /// </summary>
        [SerializeField]
        private float maxDistance = 10f;

        /// <summary>
        /// Reference to the CinemachineThirdPersonFollow component, used to control camera distance.
        /// </summary>
        private CinemachineThirdPersonFollow thirdPersonFollow;

        /// <summary>
        /// Default camera distance for resetting the zoom.
        /// </summary>
        private float defaultDistance;

        /// <summary>
        /// Initializes the CinemachineThirdPersonFollow component and saves the default camera distance.
        /// </summary>
        /// <remarks>
        /// Unity Start callback. Called before the first frame update.
        /// </remarks>
        void Start()
        {
            // Retrieve the Third Person Follow component from the virtual camera.
            thirdPersonFollow = virtualCamera.GetCinemachineComponent(CinemachineCore.Stage.Body) as CinemachineThirdPersonFollow;
            if (thirdPersonFollow == null)
            {
                Debug.LogError("CinemachineThirdPersonFollow component not found. Ensure your camera has the Third Person Follow component attached.");
                return;
            }
            // Save the default camera distance for reset functionality.
            defaultDistance = thirdPersonFollow.CameraDistance;
        }

        /// <summary>
        /// Checks for mouse scroll input to adjust the camera distance and resets the camera distance when the middle mouse button is pressed.
        /// Called once per frame.
        /// </summary>
        /// <remarks>
        /// Unity Update callback. Called every frame.
        /// </remarks>
        void Update()
        {
            // Get scroll wheel input.
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scrollInput) > 0.01f && thirdPersonFollow != null)
            {
                // Adjust the CameraDistance value based on scroll input.
                // Scrolling up (positive value) decreases the distance (zooms in),
                // while scrolling down increases the distance (zooms out).
                thirdPersonFollow.CameraDistance = Mathf.Clamp(
                    thirdPersonFollow.CameraDistance - scrollInput * zoomSpeed,
                    minDistance,
                    maxDistance
                );
                Debug.Log("Updated Camera Distance: " + thirdPersonFollow.CameraDistance);
            }

            // Reset the camera distance if the middle mouse button is pressed.
            if (Input.GetMouseButtonDown(2))
            {
                thirdPersonFollow.CameraDistance = defaultDistance;
                Debug.Log("Reset Camera Distance to Default: " + defaultDistance);
            }
        }
    }
}