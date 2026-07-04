using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class VRPanelController : MonoBehaviour
{
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        // Hide panel automatically when the simulation starts
        HidePanel();
    }

    // Call this from a UnityEvent when the panel needs to appear
    public void ShowPanel()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    // Call this when the "Done" button is pressed
    public void HidePanel()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}