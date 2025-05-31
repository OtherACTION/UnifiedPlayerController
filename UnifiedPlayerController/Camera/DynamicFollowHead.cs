using UnityEngine;

public class DynamicFollowHead : MonoBehaviour
{
    [Header("Head and Offset Settings")]
    public Transform headBone;
    public Vector3 offset;

    [Header("Axis Toggles")]
    public bool followX = true;
    public bool followY = true;
    public bool followZ = true;

    [Header("Smoothing Settings")]
    [Tooltip("Smooth speed when walking (lower value means slower update)")]
    public float normalSmoothSpeed = 10f;
    [Tooltip("Smooth speed when sprinting (higher value means faster update)")]
    public float sprintSmoothSpeed = 20f;
    [Tooltip("Threshold distance; if exceeded, the camera will snap to position")]
    public float snapThreshold = 0.5f;
    [Tooltip("Time to wait after mouse input ceases before recentering")]
    public float recenterDelay = 0.5f;

    [Header("Movement State (if available)")]
    [Tooltip("Set this flag from your character movement script")]
    public bool isSprinting = false;
    
    // Internal variables to smooth movement.
    private Vector3 velocity = Vector3.zero;
    private float mouseInactiveTime = 0f;

    void LateUpdate()
    {
        // Detect mouse input that might affect the offset (or any other relevant input)
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        // Reset inactivity timer if there's active input
        if (Mathf.Abs(mouseX) > 0.01f || Mathf.Abs(mouseY) > 0.01f)
        {
            mouseInactiveTime = 0f;
        }
        else
        {
            mouseInactiveTime += Time.deltaTime;
        }

        if (!headBone)
            return;
        
        // Calculate the desired position using the headBone's local axes.
        Vector3 desiredPosition = headBone.position +
                                  headBone.right * offset.x +
                                  headBone.up * offset.y +
                                  headBone.forward * offset.z;
        
        // Build target position, adjusting only the axes we want to follow.
        Vector3 targetPosition = transform.position;
        if (followX) targetPosition.x = desiredPosition.x;
        if (followY) targetPosition.y = desiredPosition.y;
        if (followZ) targetPosition.z = desiredPosition.z;
        
        // Choose a smoothing speed based on whether the character is sprinting.
        float currentSmoothSpeed = isSprinting ? sprintSmoothSpeed : normalSmoothSpeed;
        
        // If the target is too far off—and if the mouse is inactive—snap to the target.
        if (Vector3.Distance(transform.position, targetPosition) > snapThreshold && mouseInactiveTime >= recenterDelay)
        {
            transform.position = targetPosition;
        }
        else
        {
            // Smoothly move toward the target position.
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, 1f / currentSmoothSpeed);
        }
    }
}
