using UnityEngine;

public class VRMovementManager : MonoBehaviour
{
    [Header("Automatic Trigger?")]
    [Tooltip("Check this if you want the player completely frozen as soon as the scene loads.")]
    public bool freezeOnStart = false;

    [Header("Locomotion Scripts")]
    [Tooltip("Drag your specific movement scripts here from your PlayerController and Locomotion objects.")]
    public MonoBehaviour[] movementScripts;

    void Start()
    {
        if (freezeOnStart)
        {
            FreezePlayer();
        }
    }

    public void FreezePlayer()
    {
        foreach (MonoBehaviour script in movementScripts)
        {
            if (script != null)
            {
                script.enabled = false;
            }
        }
        Debug.Log("<color=yellow>VR Movement Disabled - Player is Frozen.</color>");
    }

    public void UnfreezePlayer()
    {
        foreach (MonoBehaviour script in movementScripts)
        {
            if (script != null)
            {
                script.enabled = true;
            }
        }
        Debug.Log("<color=green>VR Movement Enabled - Player can walk.</color>");
    }
}