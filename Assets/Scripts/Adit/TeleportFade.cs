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

    private void Update()
    {
        // Hanya tahan posisi jika misi berjalan, pergerakan dikunci, ADA target posisi, DAN menggunakan fitur teleport
        if (hasMissionStarted && lockMovementDuringMission && missionTargetPosition != null && useTeleport)
        {
            if (playerController != null)
            {
                // TETAP KUNCI POSISI (termasuk height/sumbu Y agar tidak jatuh/melayang)
                playerController.transform.position = missionTargetPosition.position;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasMissionStarted) return;
        BNGPlayerController player = other.GetComponentInParent<BNGPlayerController>();
        if (player != null && player == playerController) StartMissionSequence();
    }

    public void OnMissionStart()
    {
        if (hasMissionStarted) return;
        StartMissionSequence();
    }

    private void StartMissionSequence()
    {
        hasMissionStarted = true;
        if (triggerCollider != null) triggerCollider.enabled = false;

        // MODIFIKASI: Cek apakah missionTargetPosition tidak kosong
        if (useTeleport && missionTargetPosition != null)
        {
            // Mode Normal: Pindah tempat dan layar gelap
            StartCoroutine(ExecuteTeleport(missionTargetPosition, lockMovementDuringMission, isDialogueMission));
        }
        else
        {
            // Mode Tanpa Teleport Awal: Kunci pergerakan dan langsung putar dialog (tanpa fade)
            SetMovementEnabled(!lockMovementDuringMission);
            if (isDialogueMission && dialogueSystem != null)
            {
                dialogueSystem.StartDialogueSequence();
            }
        }
    }

    public void OnMissionComplete()
    {
        // JANGAN set hasMissionStarted = false di sini dulu! Biarkan terkunci.

        // Cek apakah postMissionTargetPosition tidak kosong
        if (useTeleport && postMissionTargetPosition != null)
        {
            // Mode Normal: Teleport setelah misi selesai dengan fade
            StartCoroutine(ExecutePostMissionTeleport());
        }
        else
        {
            // Mode Tanpa Post-Teleport: Buka pergerakan dan langsung tembak event estafet
            hasMissionStarted = false;
            SetMovementEnabled(true);
            onPostTeleportFinished?.Invoke();
        }
    }

    // --- COROUTINE UNTUK MODE TELEPORT ---
    private IEnumerator ExecuteTeleport(Transform targetPos, bool lockMovement, bool startDialogue)
    {
        if (screenFader != null) { screenFader.DoFadeIn(); yield return new WaitForSeconds(fadeDuration); }
        MovePlayer(targetPos, lockMovement);
        if (screenFader != null) { screenFader.DoFadeOut(); yield return new WaitForSeconds(fadeDuration); }
        if (startDialogue && dialogueSystem != null) dialogueSystem.StartDialogueSequence();
    }

    private IEnumerator ExecutePostMissionTeleport()
    {
        if (screenFader != null) { screenFader.DoFadeIn(); yield return new WaitForSeconds(fadeDuration); }

        MovePlayer(postMissionTargetPosition, false); // Teleport terjadi di sini

        if (screenFader != null) { screenFader.DoFadeOut(); yield return new WaitForSeconds(fadeDuration); }

        // BARU DILEPAS DI SINI: Setelah layar kembali terang dan teleport selesai
        hasMissionStarted = false;
        onPostTeleportFinished?.Invoke();
    }

    // --- FUNGSI PEMBANTU ---
    private void MovePlayer(Transform targetPos, bool lockMovement)
    {
        if (playerController != null && targetPos != null)
        {
            CharacterController cc = playerController.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            playerController.transform.position = targetPos.position;
            playerController.transform.rotation = targetPos.rotation;
        }
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