using UnityEngine;

public class PathfinderTrigger : MonoBehaviour
{
    [Header("Where should the chevrons lead?")]
    public Transform destinationTarget;

    [Header("Automatic Trigger?")]
    [Tooltip("Check this box if you want the GPS to start as soon as the scene loads!")]
    public bool activateOnStart = false;

    void Start()
    {
        if (activateOnStart)
        {
            ActivateGuidance();
        }
    }

    public void ActivateGuidance()
    {
        VRPathfinderManager manager = FindAnyObjectByType<VRPathfinderManager>();

        if (manager != null && destinationTarget != null)
        {
            manager.StartGuidingPlayer(destinationTarget);
            Debug.Log("<color=cyan>GPS Activated! Leading patient to: " + destinationTarget.name + "</color>");
        }
        else
        {
            Debug.LogError("Whoops! Missing the VRPathfinderManager in the scene, or you forgot to assign the Target.");
        }
    }
}