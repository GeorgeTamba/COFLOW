using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

// Struktur data modular untuk setiap "Halte" atau titik pemberhentian
[System.Serializable]
public class BedWaypoint
{
    [Tooltip("Nama halte (misal: Ruang Pra-Operasi, ICU)")]
    public string waypointName = "Halte Baru";

    [Tooltip("Titik tujuan kasur (Bisa lokasi jalan, atau titik spawn teleport)")]
    public Transform destination;

    [Tooltip("Centang jika ke titik ini menggunakan Teleport (layar gelap), BUKAN jalan biasa")]
    public bool useTeleport = false;

    [Tooltip("Event yang menyala saat kasur sampai (Misal: menyalakan VRIFDialogueSystem)")]
    public UnityEvent onArrived;

    [Tooltip("Centang ini jika kasur harus BERHENTI MENUNGGU sampai fungsi ResumeSequence() dipanggil (Misal: nunggu dialog selesai)")]
    public bool waitForExternalTrigger = false;

    [Tooltip("Jeda waktu sebelum mengeksekusi Event, atau jeda sebelum lanjut jalan")]
    public float delayAfterArrive = 1.0f;
}

[RequireComponent(typeof(Collider))]
public class BedTransitionSequence : MonoBehaviour
{
    [Header("Mode Memulai")]
    [Tooltip("Centang jika ingin kasur langsung jalan saat scene dimuat (Tanpa perlu nabrak trigger)")]
    public bool autoStartOnAwake = false;

    [Header("Referensi Transisi & UI")]
    public BNG.ScreenFader screenFader;
    public CanvasGroup nextDayTextCanvas;

    [Header("Durasi (Detik)")]
    public float fadeDuration = 1.0f;
    public float textDisplayDuration = 3.0f;

    [Header("Referensi Player VR")]
    public Transform playerRig;
    public MonoBehaviour[] movementScripts;

    [Header("Pergerakan Kasur Modular")]
    public NavMeshAgent bedAgent;
    public Transform bedMountPoint;

    [Tooltip("Posisi awal kasur saat sequence dimulai (mis. tepat di DEPAN PINTU). " +
             "Kasur dipindah ke sini selagi layar MASIH GELAP, jadi player langsung muncul di depan pintu, " +
             "bukan di posisi parkir kasur (di dalam kamar). Kosongkan jika kasur sudah diletakkan di posisi awal yang benar di editor.")]
    public Transform bedStartPosition;

    [Tooltip("Daftar titik halte kasur secara berurutan")]
    public BedWaypoint[] bedWaypoints; // ARRAY MODULAR KITA

    [Header("Scene Berikutnya")]
    public string nextSceneName;

    // === TAMBAHAN BARU: EVENT OPSIONAL ===
    [Header("Optional Events")]
    [Tooltip("Dipanggil tepat setelah layar kembali terang dan pemain sudah berada di atas kasur awal")]
    public UnityEvent onInitialTeleportComplete;
    // =====================================

    private bool sequenceStarted = false;
    private bool isWaitingForResume = false; // Kunci penahan kasur

    private void Start()
    {
        if (nextDayTextCanvas != null) nextDayTextCanvas.alpha = 0f;
        if (bedAgent != null) bedAgent.enabled = false;

        // MODE SCENE BARU: Langsung eksekusi tanpa trigger
        if (autoStartOnAwake)
        {
            sequenceStarted = true;
            StartCoroutine(PlayModularSequence(true));
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // MODE SCENE LAMA: Tunggu player nabrak kubus trigger
        if (!autoStartOnAwake && !sequenceStarted && other.CompareTag("Player"))
        {
            sequenceStarted = true;
            StartCoroutine(PlayModularSequence(false));
        }
    }

    // FUNGSI PENTING: Dipanggil dari panel dialog untuk melepas rem kasur
    public void ResumeSequence()
    {
        Debug.Log("<color=green>Perintah diterima: Kasur melanjutkan perjalanan!</color>");
        isWaitingForResume = false;
    }

    private IEnumerator PlayModularSequence(bool isAutoStart)
    {
        LockPlayerMovement(true);

        if (!isAutoStart)
        {
            // Flow Scene Lama (Layar gelap -> Teks Keesokan harinya)
            if (screenFader != null) screenFader.DoFadeIn();
            yield return new WaitForSeconds(fadeDuration);

            if (nextDayTextCanvas != null)
            {
                nextDayTextCanvas.alpha = 1f;
                yield return new WaitForSeconds(textDisplayDuration);
                nextDayTextCanvas.alpha = 0f;
            }
        }
        else
        {
            // Flow Scene Baru (Gelap kilat untuk persiapan posisi awal)
            if (screenFader != null) screenFader.DoFadeIn();
            yield return new WaitForSeconds(0.5f);
        }

        // ============================================================
        // PENEMPATAN AWAL (LAYAR MASIH GELAP)
        // ------------------------------------------------------------
        // Pindahkan kasur ke posisi awal (mis. depan pintu) SEBELUM fade out.
        // Inilah yang mencegah "mampir ke dalam kamar": reveal hanya terjadi
        // SEKALI, di tempat kasur yang sudah benar.
        // ============================================================
        if (bedAgent != null)
        {
            bedAgent.enabled = false; // matikan NavMesh agar bisa dipindah manual
            if (bedStartPosition != null)
                bedAgent.transform.SetPositionAndRotation(bedStartPosition.position, bedStartPosition.rotation);
        }

        // Dudukkan player di kasur YANG SUDAH di posisi awal.
        // CATATAN PENTING: bedMountPoint harus jadi CHILD dari kasur (bedAgent),
        // supaya posisinya ikut berpindah saat kasur dipindah ke bedStartPosition.
        if (bedMountPoint != null && playerRig != null)
        {
            playerRig.SetParent(bedAgent != null ? bedAgent.transform : null);
            playerRig.SetPositionAndRotation(bedMountPoint.position, bedMountPoint.rotation);
        }

        // Reveal SEKALI: layar terang, player sudah di depan pintu di atas kasur.
        if (screenFader != null) screenFader.DoFadeOut();
        yield return new WaitForSeconds(fadeDuration);

        // === TAMBAHAN BARU: PANGGIL EVENT SETELAH REVEAL ===
        onInitialTeleportComplete?.Invoke();
        // ===================================================

        // ============================================
        // MESIN MODULAR: Eksekusi setiap Halte
        // ============================================
        foreach (BedWaypoint waypoint in bedWaypoints)
        {
            if (waypoint.destination == null) continue;

            if (waypoint.useTeleport)
            {
                // MODE TELEPORT (Contoh: Masuk kamar ICU tembus tembok)
                if (screenFader != null) screenFader.DoFadeIn();
                yield return new WaitForSeconds(fadeDuration);

                if (bedAgent != null) bedAgent.enabled = false;

                // Pindah posisi kasur (player ikut karena di-parent ke kasur)
                bedAgent.transform.SetPositionAndRotation(waypoint.destination.position, waypoint.destination.rotation);

                // Samakan kepala player ke titik duduk kasur
                if (bedMountPoint != null && playerRig != null)
                    playerRig.SetPositionAndRotation(bedMountPoint.position, bedMountPoint.rotation);

                if (screenFader != null) screenFader.DoFadeOut();
                yield return new WaitForSeconds(fadeDuration);
            }
            else
            {
                // MODE JALAN (NavMesh biasa)
                if (bedAgent != null)
                {
                    bedAgent.enabled = true;
                    // Warp agar agent ter-snap rapi ke NavMesh setelah dipindah manual
                    bedAgent.Warp(bedAgent.transform.position);
                    bedAgent.isStopped = false;
                    bedAgent.SetDestination(waypoint.destination.position);

                    // Tunggu sampai jarak ke halte tersisa sangat dekat
                    while (bedAgent.pathPending || bedAgent.remainingDistance > 0.1f)
                    {
                        yield return null;
                    }

                    // Tarik Rem!
                    bedAgent.isStopped = true;
                    bedAgent.ResetPath();
                    bedAgent.enabled = false;
                }
            }

            // Eksekusi Event di halte tersebut (Misal: StartDialogueSequence)
            waypoint.onArrived?.Invoke();

            // Beri jeda sesuai inputan di Inspector
            yield return new WaitForSeconds(waypoint.delayAfterArrive);

            // Tahan kasur di sini jika dicentang, sampai ResumeSequence() dipanggil!
            if (waypoint.waitForExternalTrigger)
            {
                isWaitingForResume = true;
                while (isWaitingForResume)
                {
                    yield return null; // Looping diam di tempat
                }
            }
        }

        // ============================================
        // AKHIR SEQUENCE (Pindah Scene Akhir)
        // ============================================
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            if (screenFader != null) screenFader.DoFadeIn();
            yield return new WaitForSeconds(fadeDuration);
            SceneManager.LoadScene(nextSceneName);
        }
    }

    private void LockPlayerMovement(bool lockMovement)
    {
        foreach (var script in movementScripts)
        {
            if (script != null) script.enabled = !lockMovement;
        }

        CharacterController cc = playerRig.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = !lockMovement;
    }
}