using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

// Modular data structure for each waypoint
[System.Serializable]
public class BedWaypoint
{
    [Tooltip("Waypoint name (e.g., Pre-Op Room, ICU)")]
    public string waypointName = "New Waypoint";

    [Tooltip("Target destination (Walking target or teleport spawn)")]
    public Transform destination;

    [Tooltip("Check to use Teleport (fade screen) instead of normal walking")]
    public bool useTeleport = false;

    [Tooltip("Event triggered upon arrival")]
    public UnityEvent onArrived;

    [Tooltip("Check to pause movement until ResumeSequence() is called")]
    public bool waitForExternalTrigger = false;

    [Tooltip("Delay before executing the arrival event or moving to the next waypoint")]
    public float delayAfterArrive = 1.0f;

    [Tooltip("Movement speed towards this waypoint")]
    public float moveSpeed = 3.5f;
}

[RequireComponent(typeof(Collider))]
public class FlowManager : MonoBehaviour
{
    [Header("Start Mode")]
    [Tooltip("Start sequence automatically on Awake (No trigger collision required)")]
    public bool autoStartOnAwake = false;

    [Header("Transition & UI References")]
    public BNG.ScreenFader screenFader;
    public CanvasGroup nextDayTextCanvas;

    [Header("Durations (Seconds)")]
    public float fadeDuration = 1.0f;
    public float textDisplayDuration = 3.0f;

    [Header("VR Player References")]
    public Transform playerRig;
    public MonoBehaviour[] movementScripts;

    [Header("Modular Bed Movement")]
    public NavMeshAgent bedAgent;
    public Transform bedMountPoint;

    [Tooltip("Initial bed position during the black screen. Leave empty if already placed correctly in the scene.")]
    public Transform bedStartPosition;

    [Tooltip("Sequential list of waypoints")]
    public BedWaypoint[] bedWaypoints;

    [Header("Bed Audio")]
    [Tooltip("AudioSource for the bed wheel sounds.")]
    public AudioSource bedMovementAudio;

    [Header("Next Scene")]
    public string nextSceneName;

    [Header("Optional Events")]
    [Tooltip("Called immediately after the screen fades in at the initial position")]
    public UnityEvent onInitialTeleportComplete;

    private bool sequenceStarted = false;
    private bool isWaitingForResume = false;

    private void Start()
    {
        if (nextDayTextCanvas != null) nextDayTextCanvas.alpha = 0f;
        if (bedAgent != null) bedAgent.enabled = false;

        // Auto-configure audio
        if (bedMovementAudio != null)
        {
            bedMovementAudio.loop = true;
            bedMovementAudio.playOnAwake = false;
        }

        // Auto-start mode
        if (autoStartOnAwake)
        {
            sequenceStarted = true;
            StartCoroutine(PlayModularSequence(true));
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Trigger mode: Wait for player collision
        if (!autoStartOnAwake && !sequenceStarted && other.CompareTag("Player"))
        {
            sequenceStarted = true;
            StartCoroutine(PlayModularSequence(false));
        }
    }

    // Called externally (e.g., from a dialogue panel) to resume movement
    public void ResumeSequence()
    {
        Debug.Log("<color=green>Command received: Resuming bed movement!</color>");
        isWaitingForResume = false;
    }

    private IEnumerator PlayModularSequence(bool isAutoStart)
    {
        LockPlayerMovement(true);

        if (!isAutoStart)
        {
            // Legacy flow: Fade out -> Next day text -> Fade in
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
            // Auto-start flow: Quick fade for initial positioning
            if (screenFader != null) screenFader.DoFadeIn();
            yield return new WaitForSeconds(0.5f);
        }

        // --- INITIAL POSITIONING (DURING BLACK SCREEN) ---
        if (bedAgent != null)
        {
            bedAgent.enabled = false; // Disable NavMesh for manual repositioning
            if (bedStartPosition != null)
                bedAgent.transform.SetPositionAndRotation(bedStartPosition.position, bedStartPosition.rotation);
        }

        // Parent player to the positioned bed
        if (bedMountPoint != null && playerRig != null)
        {
            playerRig.SetParent(bedAgent != null ? bedAgent.transform : null);
            playerRig.SetPositionAndRotation(bedMountPoint.position, bedMountPoint.rotation);
        }

        // Reveal Scene
        if (screenFader != null) screenFader.DoFadeOut();
        yield return new WaitForSeconds(fadeDuration);

        onInitialTeleportComplete?.Invoke();

        // --- MODULAR WAYPOINT EXECUTION ---
        foreach (BedWaypoint waypoint in bedWaypoints)
        {
            if (waypoint.destination == null) continue;

            if (waypoint.useTeleport)
            {
                // Teleport mode
                if (screenFader != null) screenFader.DoFadeIn();
                yield return new WaitForSeconds(fadeDuration);

                if (bedAgent != null) bedAgent.enabled = false;

                bedAgent.transform.SetPositionAndRotation(waypoint.destination.position, waypoint.destination.rotation);

                // Snap player to mount point
                if (bedMountPoint != null && playerRig != null)
                    playerRig.SetPositionAndRotation(bedMountPoint.position, bedMountPoint.rotation);

                if (screenFader != null) screenFader.DoFadeOut();
                yield return new WaitForSeconds(fadeDuration);
            }
            else
            {
                // Walking mode (NavMesh)
                if (bedAgent != null)
                {
                    bedAgent.enabled = true;
                    bedAgent.Warp(bedAgent.transform.position); // Snap agent to NavMesh

                    bedAgent.speed = waypoint.moveSpeed;
                    bedAgent.isStopped = false;
                    bedAgent.SetDestination(waypoint.destination.position);

                    if (bedMovementAudio != null && !bedMovementAudio.isPlaying)
                    {
                        bedMovementAudio.Play();
                    }

                    // Wait until close to destination
                    while (bedAgent.pathPending || bedAgent.remainingDistance > 0.1f)
                    {
                        yield return null;
                    }

                    // Stop movement
                    bedAgent.isStopped = true;
                    bedAgent.ResetPath();
                    bedAgent.enabled = false;

                    if (bedMovementAudio != null && bedMovementAudio.isPlaying)
                    {
                        bedMovementAudio.Stop();
                    }
                }
            }

            // Execute arrival event
            waypoint.onArrived?.Invoke();

            yield return new WaitForSeconds(waypoint.delayAfterArrive);

            // Wait for external trigger to resume
            if (waypoint.waitForExternalTrigger)
            {
                isWaitingForResume = true;
                while (isWaitingForResume)
                {
                    yield return null;
                }
            }
        }

        // --- SEQUENCE END (Load Next Scene) ---
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