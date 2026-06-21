using System.Collections;
using UnityEngine;
using BNG;
using UnityEngine.Events;

public class TeleportFade : MonoBehaviour
{
    [Header("VRIF References")]
    public BNGPlayerController playerController;
    public ScreenFader screenFader;

    [Header("Mission & Dialogue")]
    public bool isDialogueMission = true;
    public VRIFDialogueSystem dialogueSystem;

    [Header("Teleport Settings")]
    [Tooltip("Hapus centang ini jika area ini HANYA memicu dialog TANPA teleportasi & layar gelap (Contoh: Poli)")]
    public bool useTeleport = true;
    public float fadeDuration = 1.0f;

    [Header("Movement Control")]
    public bool lockMovementDuringMission = true;

    [Space(10)]
    public Transform missionTargetPosition;
    public Transform postMissionTargetPosition;

    [Header("Sequence Events (Sistem Estafet)")]
    public UnityEvent onPostTeleportFinished;

    private SmoothLocomotion smoothMove;
    private PlayerTeleport teleportMove;
    private Collider triggerCollider;
    private bool hasMissionStarted = false;

    private void Start()
    {
        if (screenFader == null) screenFader = FindObjectOfType<ScreenFader>();
        if (playerController == null) playerController = FindObjectOfType<BNGPlayerController>();

        if (playerController != null)
        {
            smoothMove = playerController.GetComponentInChildren<SmoothLocomotion>();
            teleportMove = playerController.GetComponentInChildren<PlayerTeleport>();
        }
        triggerCollider = GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasMissionStarted) return; // Cegah trigger ganda

        if (other.CompareTag("Player"))
        {
            hasMissionStarted = true;
            if (triggerCollider != null) triggerCollider.enabled = false;

            StartCoroutine(ExecuteMissionSequence());
        }
    }

    private IEnumerator ExecuteMissionSequence()
    {
        // 1. Kunci pergerakan pemain jika diaktifkan
        if (lockMovementDuringMission) SetMovementEnabled(false);

        // 2. Jika ini adalah area yang butuh teleport awal (seperti masuk ruangan), jalankan teleport
        if (useTeleport && missionTargetPosition != null)
        {
            if (screenFader != null) { screenFader.DoFadeIn(); yield return new WaitForSeconds(fadeDuration); }

            MovePlayer(missionTargetPosition, lockMovementDuringMission);

            // [PERBAIKAN]: Beri waktu tunggu 1 frame agar posisi & rotasi Headset VR terkalibrasi dengan benar!
            yield return null;

            if (screenFader != null) { screenFader.DoFadeOut(); yield return new WaitForSeconds(fadeDuration); }
        }

        // 3. Jalankan dialog (jika ada)
        if (isDialogueMission && dialogueSystem != null)
        {
            dialogueSystem.StartDialogueSequence();
        }
        else
        {
            // Jika tidak ada dialog, langsung anggap misi selesai
            OnMissionComplete();
        }
    }

    // Fungsi ini dipanggil dari luar (misal oleh VRIFDialogueSystem saat dialog usai atau dari video player)
    public void OnMissionComplete()
    {
        if (postMissionTargetPosition != null)
        {
            StartCoroutine(ExecutePostMissionTeleport());
        }
        else
        {
            // Buka kunci pergerakan jika tidak ada teleportasi lanjutan
            SetMovementEnabled(true);
            hasMissionStarted = false;
        }
    }

    private IEnumerator ExecutePostMissionTeleport()
    {
        if (screenFader != null) { screenFader.DoFadeIn(); yield return new WaitForSeconds(fadeDuration); }

        // MATIKAN KUNCI POSISI DI SINI, SEBELUM TELEPORT
        hasMissionStarted = false;

        MovePlayer(postMissionTargetPosition, false); // Teleport terjadi di sini

        // [PERBAIKAN]: Beri waktu tunggu 1 frame agar posisi & rotasi Headset VR terkalibrasi dengan benar!
        yield return null;

        if (screenFader != null) { screenFader.DoFadeOut(); yield return new WaitForSeconds(fadeDuration); }

        onPostTeleportFinished?.Invoke();
    }

    // --- FUNGSI PEMBANTU ---
    private void MovePlayer(Transform targetPos, bool lockMovement)
    {
        if (playerController != null && targetPos != null)
        {
            CharacterController cc = playerController.GetComponent<CharacterController>();
            // Matikan CharacterController SESAAT SEBELUM PINDAH agar tidak bertabrakan (bug mental)
            if (cc != null) cc.enabled = false;

            playerController.transform.position = targetPos.position;
            playerController.transform.rotation = targetPos.rotation;
        }

        // Nyalakan kembali berdasarkan penguncian yang diinginkan
        SetMovementEnabled(!lockMovement);
    }

    private void SetMovementEnabled(bool canMove)
    {
        if (playerController != null)
        {
            CharacterController cc = playerController.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = canMove;
        }
        if (smoothMove != null) smoothMove.enabled = canMove;
        if (teleportMove != null) teleportMove.enabled = canMove;
    }
}