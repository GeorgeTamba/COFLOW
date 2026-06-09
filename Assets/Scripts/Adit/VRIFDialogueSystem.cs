using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

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
    public GameObject dialogueUIPanel;
    public TeleportFade teleportFadeScript;

    [Header("End Sequence Actions")]
    [Tooltip("Hapus centang ini jika ingin teleport dikendalikan oleh hal lain (misal: Video Selesai)")]
    public bool autoTeleportOnEnd = true; // --- TAMBAHAN BARU ---

    [Header("Typewriter Settings")]
    public float typeSpeed = 0.05f;
    public float delayBetweenSentences = 1.0f;

    [Header("Modular Dialogue Content")]
    public DialogueLine[] dialogueLines;

    private bool isWaitingForInput = false;

    private Canvas parentCanvas;
    private GraphicRaycaster parentRaycaster;
    private CanvasGroup canvasGroup;

    private void Start()
    {
        parentCanvas = GetComponent<Canvas>();
        parentRaycaster = GetComponent<GraphicRaycaster>();
        canvasGroup = GetComponent<CanvasGroup>();

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
            ToggleUI(true);

            yield return StartCoroutine(TypeSentence(line.sentence));

            line.onLineFinished?.Invoke();

            if (line.waitForButtonPress)
            {
                if (line.hideMainPanelDuringWait)
                {
                    ToggleUI(false);
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
        Debug.Log("Dialog resumed!");
    }

    private void EndDialogueSequence()
    {
        ToggleUI(false);
        // --- MODIFIKASI: Hanya teleport jika autoTeleportOnEnd dicentang ---
        if (autoTeleportOnEnd && teleportFadeScript != null)
        {
            teleportFadeScript.OnMissionComplete();
        }
    }

    private void ToggleUI(bool isActive)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = isActive ? 1f : 0f;
            canvasGroup.interactable = isActive;
            canvasGroup.blocksRaycasts = isActive;
        }
        else if (dialogueUIPanel != null)
        {
            dialogueUIPanel.SetActive(isActive);
        }

        if (parentCanvas != null) parentCanvas.enabled = isActive;
        if (parentRaycaster != null) parentRaycaster.enabled = isActive;
    }
}