using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI; // Wajib dipanggil untuk mengakses GraphicRaycaster

[System.Serializable]
public class DialogueLine
{
    [TextArea(3, 5)]
    public string sentence;
    public bool waitForButtonPress = false;
    public bool hideMainPanelDuringWait = true;
    [Space(5)]
    public UnityEvent onLineFinished;
}

public class VRIFDialogueSystem : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI dialogueTextDisplay;
    public GameObject dialogueUIPanel; // Biarkan tetap terisi objek 'Background'
    public TeleportFade teleportFadeScript;

    [Header("Typewriter Settings")]
    public float typeSpeed = 0.05f;
    public float delayBetweenSentences = 1.0f;

    [Header("Modular Dialogue Content")]
    public DialogueLine[] dialogueLines;

    private bool isWaitingForInput = false;

    // --- VARIABEL BARU UNTUK TEMBOK INVISIBLE ---
    private Canvas parentCanvas;
    private GraphicRaycaster parentRaycaster;

    private void Start()
    {
        // Secara otomatis mencari komponen pemblokir laser di objek induk
        parentCanvas = GetComponent<Canvas>();
        parentRaycaster = GetComponent<GraphicRaycaster>();

        ToggleUI(false);
    }

    public void StartDialogueSequence()
    {
        ToggleUI(true);
        StartCoroutine(PlayDialogueSequence());
    }

    private IEnumerator PlayDialogueSequence()
    {
        foreach (DialogueLine line in dialogueLines)
        {
            if (dialogueUIPanel != null && !dialogueUIPanel.activeSelf)
            {
                ToggleUI(true);
            }

            yield return StartCoroutine(TypeSentence(line.sentence));

            line.onLineFinished?.Invoke();

            if (line.waitForButtonPress)
            {
                if (line.hideMainPanelDuringWait)
                {
                    ToggleUI(false); // Panggil fungsi pintar untuk mematikan semuanya
                }

                isWaitingForInput = true;
                yield return new WaitWhile(() => isWaitingForInput);
            }
            else
            {
                yield return new WaitForSeconds(delayBetweenSentences);
            }
        }

        EndDialogueSequence();
    }

    private IEnumerator TypeSentence(string textToType)
    {
        dialogueTextDisplay.text = "";
        foreach (char character in textToType.ToCharArray())
        {
            dialogueTextDisplay.text += character;
            yield return new WaitForSeconds(typeSpeed);
        }
    }

    public void ResumeDialogue()
    {
        isWaitingForInput = false;
    }

    private void EndDialogueSequence()
    {
        ToggleUI(false);
        if (teleportFadeScript != null) teleportFadeScript.OnMissionComplete();
    }

    // --- FUNGSI PINTAR BARU ---
    // Fungsi ini akan mematikan visual sekaligus melumpuhkan penghalang lasernya
    private void ToggleUI(bool isActive)
    {
        // 1. Matikan/nyalakan objek visualnya (Background)
        if (dialogueUIPanel != null) dialogueUIPanel.SetActive(isActive);

        // 2. Matikan/nyalakan penangkap laser (Graphic Raycaster & Canvas)
        if (parentCanvas != null) parentCanvas.enabled = isActive;
        if (parentRaycaster != null) parentRaycaster.enabled = isActive;
    }
}