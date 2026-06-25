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

    [Tooltip("Audio clip for this dialogue line (optional)")]
    public AudioClip dialogueAudio;

    [Tooltip("The state name in the Animator to play at the start of this line (e.g., 'NPCDialogue', 'Idle')")]
    public string animationStateName;

    public bool waitForButtonPress = false;
    public bool hideMainPanelDuringWait = true;

    [Space(5)]
    public UnityEvent onLineStarted; 
    public UnityEvent onLineFinished; 
}

public class VRIFDialogueSystem : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI dialogueTextDisplay;
    public GameObject dialogueUIPanel;

    [Header("Panel Animation")]
    public float unfoldDuration = 0.3f; 
    public TeleportFade teleportFadeScript;

    [Tooltip("AudioSource component used to play dialogue audio clips")]
    public AudioSource audioSource;

    [Header("Animation Settings")]
    [Tooltip("Animator component attached to the NPC character")]
    public Animator npcAnimator;

    [Tooltip("Smooth transition duration between dialogue lines (in seconds)")]
    public float dialogueTransitionDuration = 0.4f;

    [Tooltip("Animator state name to play when the entire dialogue sequence finishes (e.g., 'Idle')")]
    public string endAnimationStateName = "Idle";

    [Tooltip("Smooth transition duration when returning to the ending animation (in seconds)")]
    public float endTransitionDuration = 0.3f;

    [Header("End Sequence Actions")]
    public UnityEvent onDialogueSequenceEnded;
    [Tooltip("Uncheck this if teleportation is handled by an external event (e.g., a video ending)")]
    public bool autoTeleportOnEnd = true;

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
        StartCoroutine(HandleDialogueStartDelay());
    }

    private IEnumerator HandleDialogueStartDelay()
    {
        yield return StartCoroutine(UnfoldPanel());

        yield return new WaitForSeconds(1f);

        StartCoroutine(PlayDialogueSequence());
    }

    private IEnumerator PlayDialogueSequence()
    {
        foreach (DialogueLine line in dialogueLines)
        {
            ToggleUI(true);

            // --- ANIMATION & INITIAL EVENTS EXECUTION ---
            if (npcAnimator != null && !string.IsNullOrEmpty(line.animationStateName))
            {
                npcAnimator.CrossFade(line.animationStateName, dialogueTransitionDuration);
            }
            line.onLineStarted?.Invoke();

            // --- AUDIO PLAYBACK ---
            if (audioSource != null && line.dialogueAudio != null)
            {
                audioSource.Stop();
                audioSource.clip = line.dialogueAudio;
                audioSource.Play();
            }

            yield return StartCoroutine(TypeSentence(line.sentence));

            // --- INPUT & DELAY LOGIC ---
            if (line.waitForButtonPress)
            {
                line.onLineFinished?.Invoke();

                if (line.hideMainPanelDuringWait)
                {
                    ToggleUI(false);
                }

                isWaitingForInput = true;
                yield return new WaitWhile(() => isWaitingForInput);
            }
            else
            {
                // If automated, wait for the designated delay before triggering completion events
                yield return new WaitForSeconds(delayBetweenSentences);
                line.onLineFinished?.Invoke();
            }
        }

        EndDialogueSequence();
    }

    public IEnumerator UnfoldPanel()
    {
        if (dialogueUIPanel != null)
        {
            // Set initial Y scale to 0 (Flattened panel in the middle)
            Vector3 initialScale = dialogueUIPanel.transform.localScale;
            dialogueUIPanel.transform.localScale = new Vector3(initialScale.x, 0f, initialScale.z);

            float elapsedTime = 0f;

            while (elapsedTime < unfoldDuration)
            {
                elapsedTime += Time.deltaTime;

                float currentY = Mathf.Lerp(0f, 1f, elapsedTime / unfoldDuration);
                dialogueUIPanel.transform.localScale = new Vector3(initialScale.x, currentY, initialScale.z);

                yield return null;
            }

            dialogueUIPanel.transform.localScale = new Vector3(initialScale.x, 1f, initialScale.z);
        }
    }

    public IEnumerator TypeSentence(string sentence)
    {
        dialogueTextDisplay.text = sentence;

        dialogueTextDisplay.maxVisibleCharacters = 0;

        dialogueTextDisplay.ForceMeshUpdate();

        int totalVisibleCharacters = dialogueTextDisplay.textInfo.characterCount;
        int counter = 0;

        while (counter <= totalVisibleCharacters)
        {
            dialogueTextDisplay.maxVisibleCharacters = counter;
            counter++;

            yield return new WaitForSeconds(typeSpeed);
        }
    }

    public void ResumeDialogue()
    {
        isWaitingForInput = false;
        Debug.Log("Dialogue resumed!");
    }

    private void EndDialogueSequence()
    {
        ToggleUI(false);

        // --- TRANSITION BACK TO THE ENDING ANIMATION STATE ---
        if (npcAnimator != null && !string.IsNullOrEmpty(endAnimationStateName))
        {
            npcAnimator.CrossFade(endAnimationStateName, endTransitionDuration);
        }

        // --- TRIGGER OUR NEW EVENT HERE ---
        onDialogueSequenceEnded?.Invoke();

        // (Keep the old code below)
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