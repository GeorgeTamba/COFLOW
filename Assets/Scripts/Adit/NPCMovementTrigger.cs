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

    [Header("Arrival Detection")]
    public float arrivalTolerance = 0.35f;
    public float settledSpeedThreshold = 0.08f;
    public float minStoppingDistance = 0.25f;
    [Tooltip("Safety net in case arrival is never detected.")]
    public float maxWalkDuration = 30f;

    [Header("Rotation")]
    public float rotationDuration = 0.5f;

    [Header("Events (Optional)")]
    public UnityEvent onNPCArrived;

    private bool hasTriggered = false;
    private bool isWalking = false;
    private bool hasArrived = false;
    private float walkStartTime = 0f;
    private Vector3 finalDestination;

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
        if (npcAgent == null || destinationTarget == null || !npcAgent.isOnNavMesh)
        {
            Debug.LogWarning("[NPCMovementTrigger] Agent/target not ready, or agent is not on the NavMesh.", this);
            return;
        }

        // Root motion and the agent both write to the transform every frame.
        if (npcAnimator != null) npcAnimator.applyRootMotion = false;

        // Snap the target onto the NavMesh, otherwise the path can come back partial.
        finalDestination = destinationTarget.position;
        if (NavMesh.SamplePosition(finalDestination, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            finalDestination = hit.position;
        else
            Debug.LogWarning("[NPCMovementTrigger] Target could not be sampled onto the NavMesh within 3m. Check its Y position.", this);

        // Without braking and a stopping distance the agent overshoots and orbits the target forever.
        npcAgent.autoBraking = true;
        if (npcAgent.stoppingDistance < minStoppingDistance) npcAgent.stoppingDistance = minStoppingDistance;

        npcAgent.SetDestination(finalDestination);
        isWalking = true;
        walkStartTime = Time.time;

        if (npcAnimator != null && !string.IsNullOrEmpty(walkAnimationState))
            npcAnimator.CrossFade(walkAnimationState, 0.2f);
    }

    private void Update()
    {
        if (!isWalking || hasArrived || npcAgent == null || npcAgent.pathPending) return;

        Vector3 npcFlat = npcAgent.transform.position;
        Vector3 destFlat = finalDestination;
        npcFlat.y = 0f;
        destFlat.y = 0f;
        float flatDistance = Vector3.Distance(npcFlat, destFlat);

        bool isNear = flatDistance <= npcAgent.stoppingDistance + arrivalTolerance;
        bool pathFinished = !npcAgent.hasPath;
        bool settled = npcAgent.velocity.sqrMagnitude <= settledSpeedThreshold * settledSpeedThreshold;
        bool pathBroken = npcAgent.pathStatus != NavMeshPathStatus.PathComplete;
        bool timedOut = Time.time - walkStartTime > maxWalkDuration;

        // OR on purpose: velocity is practically never exactly 0, so a single condition can hang forever.
        if (isNear || pathFinished || (pathBroken && settled) || timedOut)
        {
            if (timedOut) Debug.LogWarning($"[NPCMovementTrigger] Timeout at {flatDistance:F2}m.", this);
            StopNPCMovement();
        }
    }

    private void StopNPCMovement()
    {
        if (hasArrived) return;
        hasArrived = true;
        isWalking = false;

        // isStopped and ResetPath do not release rotation control; updateRotation must be turned off.
        if (npcAgent != null && npcAgent.isOnNavMesh)
        {
            npcAgent.updateRotation = false;
            npcAgent.updatePosition = false;
            npcAgent.ResetPath();
            npcAgent.velocity = Vector3.zero;
            npcAgent.isStopped = true;
            npcAgent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        }

        if (npcAnimator != null && !string.IsNullOrEmpty(idleAnimationState))
            npcAnimator.CrossFade(idleAnimationState, 0.3f);

        if (destinationTarget != null)
            StartCoroutine(RotateToFaceTarget(Quaternion.Euler(0f, destinationTarget.eulerAngles.y, 0f)));
        else
            FinishArrival();
    }

    private IEnumerator RotateToFaceTarget(Quaternion targetRotation)
    {
        Transform npcTransform = npcAgent != null ? npcAgent.transform : transform;
        Quaternion startRotation = npcTransform.rotation;
        float duration = Mathf.Max(0.01f, rotationDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            npcTransform.rotation = Quaternion.Slerp(startRotation, targetRotation, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        npcTransform.rotation = targetRotation;
        FinishArrival();
    }

    private void FinishArrival()
    {
        // A disabled agent can no longer touch the transform.
        if (npcAgent != null) npcAgent.enabled = false;

        onNPCArrived?.Invoke();
        this.enabled = false;
    }
}