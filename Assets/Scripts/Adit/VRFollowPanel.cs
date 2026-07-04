using UnityEngine;

public class VRFollowPanel : MonoBehaviour
{
    [Header("Target References")]
    [Tooltip("Drag your PlayerController here")]
    public Transform playerRoot;

    [Tooltip("Drag the VR Camera (CenterEyeAnchor / Main Camera) here. If left empty, it will find it automatically.")]
    public Transform playerCamera;

    [Header("Spawn Offset Settings")]
    [Tooltip("X: Left/Right, Y: Height from floor, Z: Distance FORWARD from player's face")]
    public Vector3 spawnOffset = new Vector3(0f, 1.3f, 1.5f);

    [Tooltip("CHECK ONLY DURING EDITOR TESTING to adjust offset live. DISABLE in release/build for better performance!")]
    public bool liveEditOffset = false;

    [Tooltip("How smoothly the panel follows the player (Higher = more responsive)")]
    public float followSpeed = 5f;

    private Quaternion sceneRotation;
    private Vector3 lockedForward;
    private Vector3 lockedRight;
    private Vector3 fixedWorldOffset;
    private bool isInitialized = false;

    void Awake()
    {
        // Save the initial scene rotation/tilt
        sceneRotation = transform.rotation;
    }

    void OnEnable()
    {
        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main.transform;
        }

        if (playerRoot != null && playerCamera != null)
        {
            // Lock the forward and right directions when the panel first activates
            lockedForward = playerCamera.forward;
            lockedForward.y = 0;
            lockedForward.Normalize();

            lockedRight = playerCamera.right;
            lockedRight.y = 0;
            lockedRight.Normalize();

            // Calculate initial distance
            RecalculateOffset();

            // Move immediately to target position on the first frame (without Lerp)
            transform.position = playerRoot.position + fixedWorldOffset;
            transform.rotation = sceneRotation;

            isInitialized = true;
        }
    }

    void LateUpdate()
    {
        if (!isInitialized || playerRoot == null) return;

        // If edit mode is on, recalculate offset every frame (Real-time update)
        if (liveEditOffset)
        {
            RecalculateOffset();
        }

        // Smoothly move panel to follow the target position based on the locked distance
        Vector3 targetPosition = playerRoot.position + fixedWorldOffset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * followSpeed);

        // Keep the rotation locked to the scene's tilt
        transform.rotation = sceneRotation;
    }

    // Helper function to calculate the offset
    private void RecalculateOffset()
    {
        Vector3 targetSpawnPos = playerRoot.position
                                 + (lockedRight * spawnOffset.x)
                                 + (Vector3.up * spawnOffset.y)
                                 + (lockedForward * spawnOffset.z);

        // Lock the absolute distance between the player's body and the panel
        fixedWorldOffset = targetSpawnPos - playerRoot.position;
    }
}