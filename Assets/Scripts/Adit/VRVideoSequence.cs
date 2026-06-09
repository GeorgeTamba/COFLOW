using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class VRVideoSequence : MonoBehaviour
{
    private VideoPlayer videoPlayer;

    [Header("Teleport Settings")]
    [Tooltip("Masukkan objek TeleportTrigger Doctor ke sini")]
    public TeleportFade teleportFadeScript;

    private void Start()
    {
        videoPlayer = GetComponent<VideoPlayer>();

        // Daftarkan event: saat video mencapai detik terakhir, jalankan fungsi OnVideoEnd
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached += OnVideoEnd;
        }
    }

    // Fungsi ini akan otomatis terpanggil oleh sistem Unity saat video selesai berputar
    private void OnVideoEnd(VideoPlayer vp)
    {
        Debug.Log("Video Dokter Selesai! Memulai teleport...");
        if (teleportFadeScript != null)
        {
            teleportFadeScript.OnMissionComplete();
        }
    }
}