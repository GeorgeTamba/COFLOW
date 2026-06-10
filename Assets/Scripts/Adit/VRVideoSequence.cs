using UnityEngine;
using UnityEngine.Video;
using UnityEngine.Events; // Wajib dipanggil untuk memunculkan kolom Event di Inspector

[RequireComponent(typeof(VideoPlayer))]
public class VRVideoSequence : MonoBehaviour
{
    private VideoPlayer videoPlayer;

    [Header("Video Events")]
    [Tooltip("Semua yang ada di list ini akan tereksekusi otomatis saat video selesai, fungsinya persis seperti OnClick pada Button")]
    public UnityEvent onVideoFinished;

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
        Debug.Log("<color=orange>Video Selesai! Mengeksekusi semua event di daftar...</color>");

        // Memanggil/menjalankan semua fungsi yang kamu daftarkan di Inspector
        onVideoFinished?.Invoke();
    }
}