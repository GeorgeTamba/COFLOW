using System.Collections;
using UnityEngine;
using BNG; // VRIF Namespace

public class TeleportFade : MonoBehaviour
{
    [Header("VRIF References")]
    [Tooltip("Main player controller (XR Rig)")]
    public BNGPlayerController playerController;
    public ScreenFader screenFader;

    [Header("Mission & Dialogue")]
    [Tooltip("Check this if this teleport mission should trigger a dialogue sequence first.")]
    public bool isDialogueMission = true;
    [Tooltip("Reference to the dialogue system. Leave empty if isDialogueMission is false.")]
    public VRIFDialogueSystem dialogueSystem;

    [Header("Teleport Settings")]
    public float fadeDuration = 1.0f;

    [Space(10)]
    [Tooltip("Target position during the mission (Player movement will be locked here)")]
    public Transform missionTargetPosition;

    [Tooltip("Target position after the mission is complete (Player movement will be unlocked)")]
    public Transform postMissionTargetPosition;

    // Store references to movement scripts
    private SmoothLocomotion smoothMove;
    private PlayerTeleport teleportMove;
    private Collider triggerCollider;

    // Safety check to prevent double-triggering
    private bool hasMissionStarted = false;

    private void Start()
    {
        // Automatically find components if they are not assigned in the inspector
        if (screenFader == null) screenFader = FindObjectOfType<ScreenFader>();
        if (playerController == null) playerController = FindObjectOfType<BNGPlayerController>();

        // Get locomotion references from the player's body
        if (playerController != null)
        {
            smoothMove = playerController.GetComponentInChildren<SmoothLocomotion>();
            teleportMove = playerController.GetComponentInChildren<PlayerTeleport>();
        }

        // Store the trigger collider so we can disable it after activation
        triggerCollider = GetComponent<Collider>();
    }

    // --- START MISSION VIA TRIGGER ZONE OR BUTTON PRESS ---
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
        // Call the teleport function to the mission position and LOCK movement (true)
        StartCoroutine(ExecuteTeleport(missionTargetPosition, true, isDialogueMission));
    }

    // --- PHASE 2: CALLER METHOD ---
    public void OnMissionComplete()
    {
        StartCoroutine(ExecuteTeleport(postMissionTargetPosition, false, false));
        hasMissionStarted = false;
    }

    // --- MAIN COROUTINE: HANDLES FADE, TELEPORT, & MOVEMENT LOCK ---
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
            if (cc != null) cc.enabled = false;
            playerController.transform.position = targetPos.position;
            playerController.transform.rotation = targetPos.rotation;
            if (cc != null) cc.enabled = true;
        }

        if (smoothMove != null) smoothMove.enabled = !lockMovement;
        if (teleportMove != null) teleportMove.enabled = !lockMovement;

        if (screenFader != null)
        {
            screenFader.DoFadeOut();
            yield return new WaitForSeconds(fadeDuration);
        }

        // --- NEW LOGIC: START DIALOGUE AFTER FADE ---
        if (startDialogue && dialogueSystem != null)
        {
            dialogueSystem.StartDialogueSequence();
        }
    }
}