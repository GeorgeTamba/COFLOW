using UnityEngine;

public class TimeController : MonoBehaviour
{
    [Header("Pengaturan Waktu (Hanya Jalan di Editor)")]
    [Tooltip("Kecepatan waktu. 1 = Normal, 2 = 2x lebih cepat, 0.5 = Slow motion")]
    [Range(0.1f, 30f)]
    public float timeMultiplier = 3.0f;

    [Header("Mode Tombol")]
    [Tooltip("Jika dicentang, waktu hanya cepat saat kamu MENAHAN tombol di keyboard. Jika tidak, waktu akan otomatis cepat terus.")]
    public bool holdToFastForward = true;
    [Tooltip("Tombol keyboard untuk mempercepat waktu")]
    public KeyCode fastForwardKey = KeyCode.F;

    private void Update()
    {
#if UNITY_EDITOR
        if (holdToFastForward)
        {
            if (Input.GetKeyDown(fastForwardKey))
            {
                Time.timeScale = timeMultiplier;
            }
            else if (Input.GetKeyUp(fastForwardKey))
            {
                Time.timeScale = 1f;
            }
        }
        else
        {
            Time.timeScale = timeMultiplier;
        }
#endif
    }

    private void OnDisable()
    {
        Time.timeScale = 1f;
    }
}