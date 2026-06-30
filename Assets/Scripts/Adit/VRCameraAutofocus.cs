using System.Collections;
using UnityEngine;

public class VRCameraAutofocus : MonoBehaviour
{
    [Header("VR Rig References")]
    [Tooltip("Tarik objek PlayerController milik VRIF ke sini")]
    public Transform playerController;

    [Tooltip("Tarik objek CenterEyeAnchor (kamera VR) ke sini")]
    public Transform centerEyeAnchor;

    [Header("Settings")]
    [Tooltip("Kecepatan putaran kamera menuju panel. Semakin besar makin cepat.")]
    public float rotationSpeed = 3f;

    // Fungsi ini dipanggil dari UnityEvent (misal dari halte BedTransitionSequence)
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
            // 1. Cari arah dari posisi mata pemain ke posisi target panel
            // Kita nol-kan sumbu Y agar pemain tidak dipaksa menunduk/mendangak
            Vector3 dirToTarget = target.position - centerEyeAnchor.position;
            dirToTarget.y = 0;

            if (dirToTarget.sqrMagnitude > 0.001f)
            {
                // 2. Hitung rotasi ideal yang seharusnya
                Quaternion desiredCameraRot = Quaternion.LookRotation(dirToTarget);

                // 3. Hitung selisih derajat antara arah lihat pemain saat ini dengan arah panel
                float angleDifference = Mathf.DeltaAngle(centerEyeAnchor.eulerAngles.y, desiredCameraRot.eulerAngles.y);

                // 4. Jika selisih sudut sudah sangat kecil (< 2 derajat), LEPASKAN KUNCIAN!
                if (Mathf.Abs(angleDifference) < 2f)
                {
                    isFocusing = false;
                    break;
                }

                // 5. Putar badan pemain (PlayerController) secara halus menuju panel
                float step = angleDifference * Time.deltaTime * rotationSpeed;
                playerController.Rotate(0, step, 0, Space.World);
            }

            yield return null;
        }
    }
}
