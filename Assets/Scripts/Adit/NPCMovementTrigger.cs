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

        // 1. Matikan mesin NavMesh secara total
        if (npcAgent != null)
        {
            npcAgent.isStopped = true;
            npcAgent.ResetPath();
        }

        // 2. Putar animasi ke Idle
        if (npcAnimator != null && !string.IsNullOrEmpty(idleAnimationState))
        {
            npcAnimator.CrossFade(idleAnimationState, 0.3f);
        }

        // 3. Putar badan menghadap target, baru eksekusi Event
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

    // Coroutine untuk memutar NPC dengan halus
    private IEnumerator RotateToFaceTarget(Quaternion targetRotation)
    {
        float duration = 0.5f; // Durasi waktu berputar (setengah detik)
        float elapsed = 0f;
        Quaternion startRotation = npcAgent.transform.rotation;

        while (elapsed < duration)
        {
            // Slerp membuat putaran menjadi sangat mulus
            npcAgent.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Pastikan rotasi benar-benar presisi di akhir
        npcAgent.transform.rotation = targetRotation;

        // Eksekusi event/dialog SETELAH NPC selesai menghadap depan
        onNPCArrived?.Invoke();

        // Matikan skrip
        this.enabled = false;
    }
}