using System.Collections;
using UnityEngine;

public class VRCameraAutofocus : MonoBehaviour
{
    [Header("VR Rig References")]
    [Tooltip("Drag the VRIF PlayerController object here")]
    public Transform playerController;

    [Tooltip("Drag the CenterEyeAnchor (VR Camera) object here")]
    public Transform centerEyeAnchor;

    [Header("Settings")]
    [Tooltip("Camera rotation speed towards the panel. Higher means faster.")]
    public float rotationSpeed = 3f;

    // This function is called from a UnityEvent (e.g., from a BedTransitionSequence waypoint)
    public void FocusOnPanel(Transform targetPanel)
    {
        if (targetPanel == null || playerController == null || centerEyeAnchor == null) return;

        StopAllCoroutines();
        StartCoroutine(FocusRoutine(targetPanel));
    }

    private IEnumerator FocusRoutine(Transform target)
    {
        bool isFocusing = true;

        while (isFocusing)
        {
            // 1. Find the direction from the player's eye position to the target panel
            // Zero out the Y axis so the player isn't forced to look up/down
            Vector3 dirToTarget = target.position - centerEyeAnchor.position;
            dirToTarget.y = 0;

            if (dirToTarget.sqrMagnitude > 0.001f)
            {
                // 2. Calculate the ideal target rotation
                Quaternion desiredCameraRot = Quaternion.LookRotation(dirToTarget);

                // 3. Calculate the angle difference between current view direction and panel direction
                float angleDifference = Mathf.DeltaAngle(centerEyeAnchor.eulerAngles.y, desiredCameraRot.eulerAngles.y);

                // 4. If the angle difference is very small (< 2 degrees), RELEASE THE LOCK!
                if (Mathf.Abs(angleDifference) < 2f)
                {
                    isFocusing = false;
                    break;
                }

                // 5. Smoothly rotate the player's body (PlayerController) towards the panel
                float step = angleDifference * Time.deltaTime * rotationSpeed;
                playerController.Rotate(0, step, 0, Space.World);
            }

            yield return null;
        }
    }
}