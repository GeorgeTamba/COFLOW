using UnityEngine;
using UnityEngine.EventSystems;

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
        GameObject currentButton = EventSystem.current?.currentSelectedGameObject;
            
        if (manager != null && destinationTarget != null)
        {
            manager.StartGuidingPlayer(destinationTarget);
            Debug.Log("<color=cyan>GPS Activated! Leading patient to: " + destinationTarget.name + "</color>");
            if (currentButton != null && currentButton.transform.parent != null)
            {
                currentButton.transform.parent.gameObject.SetActive(false);
            }
        }
        else
        {
            Debug.LogError("Whoops! Missing the VRPathfinderManager in the scene, or you forgot to assign the Target.");
        }
    }
}