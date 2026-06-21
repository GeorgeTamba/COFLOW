using UnityEngine;

public class PathfinderDestination : MonoBehaviour
{
    [Header("Optional: Turn off trigger after arrival?")]
    [Tooltip("If true, the arrows won't accidentally turn off again if the player walks back through this area.")]
    public bool disableAfterArrival = true;

    [Header("Optional: Turn on target panel after arrival?")]
    public GameObject dialogPanel;

    private void OnTriggerEnter(Collider other)
    {
        // Remember: Ensure your VR Player's tag is set to "Player" in the Inspector!
        if (other.CompareTag("Player"))
        {
            Debug.Log("<color=yellow>Player reached the destination! Turning off GPS.</color>");

            VRPathfinderManager manager = FindAnyObjectByType<VRPathfinderManager>();
            if (manager != null)
            {
                manager.StopGuiding();
            }

            if (disableAfterArrival)
            {
                GetComponent<Collider>().enabled = false;
            }

            if (dialogPanel != null)
            {
                dialogPanel.SetActive(true);
            }
        }
    }
}