using UnityEngine;
using Unity.Cinemachine;

public class ThirdPersonCameraZoom : MonoBehaviour
{
    // Reference to your CinemachineCamera (or VirtualCamera if that's what you're using)
    [SerializeField]
    private CinemachineCamera virtualCamera;

    // How much to change the camera distance per scroll unit.
    [SerializeField]
    private float zoomSpeed = 10f;

    // Minimum and maximum distance allowed.
    [SerializeField]
    private float minDistance = 1f;
    [SerializeField]
    private float maxDistance = 10f;

    // This will hold the Third Person Follow component (updated type).
    private CinemachineThirdPersonFollow thirdPersonFollow;

    // Store the default camera distance for resetting.
    private float defaultDistance;

    void Start()
    {
        // Retrieve the Third Person Follow component.
        // Note: We're using GetCinemachineComponent with CinemachineCore.Stage.Body and casting it to CinemachineThirdPersonFollow.
        thirdPersonFollow = virtualCamera.GetCinemachineComponent(CinemachineCore.Stage.Body) as CinemachineThirdPersonFollow;
        if (thirdPersonFollow == null)
        {
            Debug.LogError("CinemachineThirdPersonFollow component not found. Ensure your camera has the Third Person Follow component attached.");
            return;
        }
        // Save the default camera distance.
        defaultDistance = thirdPersonFollow.CameraDistance;
    }

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
