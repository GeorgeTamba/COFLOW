using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class NPCMovementTrigger : MonoBehaviour
{
    [Header("NPC Components")]
    public NavMeshAgent npcAgent;
    public Animator npcAnimator;

    [Header("Animation States")]
    public string walkAnimationState = "Walk";
    public string idleAnimationState = "Idle";

    [Header("Movement Target")]
    public Transform destinationTarget;

    [Header("Player Detection")]
    public string playerTag = "Player";

    [Header("Events (Optional)")]
    public UnityEvent onNPCArrived;

    private bool hasTriggered = false;
    private bool isWalking = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!hasTriggered && other.CompareTag(playerTag))
        {
            hasTriggered = true;
            StartNPCMovement();
        }
    }

    private void StartNPCMovement()
    {
        if (npcAgent != null && destinationTarget != null)
        {
            npcAgent.SetDestination(destinationTarget.position);
            isWalking = true;

            if (npcAnimator != null && !string.IsNullOrEmpty(walkAnimationState))
            {
                npcAnimator.CrossFade(walkAnimationState, 0.2f);
            }
        }
    }

    private void Update()
    {
        if (isWalking && npcAgent != null)
        {
            if (!npcAgent.pathPending && npcAgent.remainingDistance <= npcAgent.stoppingDistance)
            {
                if (!npcAgent.hasPath || npcAgent.velocity.sqrMagnitude == 0f)
                {
                    StopNPCMovement();
                }
            }
        }
    }

    private void StopNPCMovement()
    {
        isWalking = false;

        // 1. Completely disable the NavMesh agent
        if (npcAgent != null)
        {
            npcAgent.isStopped = true;
            npcAgent.ResetPath();
        }

        // 2. Play Idle animation
        if (npcAnimator != null && !string.IsNullOrEmpty(idleAnimationState))
        {
            npcAnimator.CrossFade(idleAnimationState, 0.3f);
        }

        // 3. Rotate to face target, then execute Event
        if (destinationTarget != null)
        {
            StartCoroutine(RotateToFaceTarget(destinationTarget.rotation));
        }
        else
        {
            onNPCArrived?.Invoke();
            this.enabled = false;
        }
    }

    // Coroutine to smoothly rotate the NPC
    private IEnumerator RotateToFaceTarget(Quaternion targetRotation)
    {
        float duration = 0.5f; // Rotation duration
        float elapsed = 0f;
        Quaternion startRotation = npcAgent.transform.rotation;

        while (elapsed < duration)
        {
            // Smoothly interpolate rotation
            npcAgent.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure precise final rotation
        npcAgent.transform.rotation = targetRotation;

        // Execute event after rotation is complete
        onNPCArrived?.Invoke();

        // Disable script
        this.enabled = false;
    }
}