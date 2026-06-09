using System.Collections;
using UnityEngine;
using BNG; // VRIF Namespace

public class TeleportFade : MonoBehaviour
{
    [Header("VRIF References")]
    public BNGPlayerController playerController;
    public ScreenFader screenFader;

    [Header("Mission & Dialogue")]
    public bool isDialogueMission = true;
    public VRIFDialogueSystem dialogueSystem;

    [Header("Teleport Settings")]
    public float fadeDuration = 1.0f;

    [Header("Movement Control")]
    [Tooltip("Jika dicentang, pemain tidak bisa bergerak (WASD/Analog) selama misi aktif.")]
    public bool lockMovementDuringMission = true;

    [Space(10)]
    public Transform missionTargetPosition;
    public Transform postMissionTargetPosition;

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
            // Mencari komponen pergerakan di object atau child-nya
            smoothMove = playerController.GetComponentInChildren<SmoothLocomotion>();
            teleportMove = playerController.GetComponentInChildren<PlayerTeleport>();
        }
        triggerCollider = GetComponent<Collider>();
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

        // Panggil teleport & kunci pergerakan
        StartCoroutine(ExecuteTeleport(missionTargetPosition, lockMovementDuringMission, isDialogueMission));
    }

    public void OnMissionComplete()
    {
        // Buka kembali pergerakan (lockMovement = false)
        StartCoroutine(ExecuteTeleport(postMissionTargetPosition, false, false));
        hasMissionStarted = false;
    }

    private IEnumerator ExecuteTeleport(Transform targetPos, bool lockMovement, bool startDialogue)
    {
        if (screenFader != null)
        {
            screenFader.DoFadeIn();
            yield return new WaitForSeconds(fadeDuration);
        }

        if (playerController != null && targetPos != null)
        {
            CharacterController cc = playerController.GetComponent<CharacterController>();

            // 1. Matikan sementara untuk teleport
            if (cc != null) cc.enabled = false;

            playerController.transform.position = targetPos.position;
            playerController.transform.rotation = targetPos.rotation;

            // 2. KUNCI UTAMA: Hanya nyalakan kembali CharacterController jika lockMovement adalah false
            if (cc != null) cc.enabled = !lockMovement;
        }

        // Kunci komponen VRIF tambahan
        if (smoothMove != null) smoothMove.enabled = !lockMovement;
        if (teleportMove != null) teleportMove.enabled = !lockMovement;

        if (screenFader != null)
        {
            screenFader.DoFadeOut();
            yield return new WaitForSeconds(fadeDuration);
        }

        if (startDialogue && dialogueSystem != null)
        {
            dialogueSystem.StartDialogueSequence();
        }
    }
}