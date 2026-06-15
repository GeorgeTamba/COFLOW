using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class NPCMovementTrigger : MonoBehaviour
{
    [Header("NPC Components")]
    [Tooltip("The NavMeshAgent component on the NPC")]
    public NavMeshAgent npcAgent;
    [Tooltip("The Animator component on the NPC")]
    public Animator npcAnimator;

    [Header("Animation States")]
    [Tooltip("The state name for the walking animation")]
    public string walkAnimationState = "Walk";
    [Tooltip("The state name for the idle animation when arriving")]
    public string idleAnimationState = "Idle";

    [Header("Movement Target")]
    [Tooltip("Create an empty GameObject at the target location and drag it here")]
    public Transform destinationTarget;

    [Header("Player Detection")]
    [Tooltip("The tag assigned to your VR Player or Controller Collider")]
    public string playerTag = "Player";

    [Header("Events (Optional)")]
    [Tooltip("Actions to trigger exactly when the NPC reaches the destination")]
    public UnityEvent onNPCArrived;

    private bool hasTriggered = false;
    private bool isWalking = false;

    private void OnTriggerEnter(Collider other)
    {
        // Trigger only once when the player enters the collider zone
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
            // Tell NavMeshAgent to calculate path and move to target
            npcAgent.SetDestination(destinationTarget.position);
            isWalking = true;

            // Transition smoothly to walking animation
            if (npcAnimator != null && !string.IsNullOrEmpty(walkAnimationState))
            {
                npcAnimator.CrossFade(walkAnimationState, 0.2f);
            }
        }
    }

    private void Update()
    {
        // Monitor if the NPC has reached its destination
        if (isWalking && npcAgent != null)
        {
            // Check if path is calculated and remaining distance is less than stopping threshold
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

        // Transition smoothly back to idle animation
        if (npcAnimator != null && !string.IsNullOrEmpty(idleAnimationState))
        {
            npcAnimator.CrossFade(idleAnimationState, 0.3f);
        }

        // Trigger any event assigned in the Inspector (e.g., open next dialogue or mission step)
        onNPCArrived?.Invoke();

        // Disable this script so it doesn't run Update anymore
        this.enabled = false;
    }
}