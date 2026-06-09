using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class VRPanelController : MonoBehaviour
{
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        // Sembunyikan panel secara otomatis saat simulasi dimulai
        HidePanel();
    }

    // Panggil ini dari UnityEvent saat panel perlu muncul
    public void ShowPanel()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    // Panggil ini saat tombol "Selesai" ditekan
    public void HidePanel()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}