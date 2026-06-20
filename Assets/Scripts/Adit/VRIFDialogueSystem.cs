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
    public UnityEvent onLineStarted; // Useful for triggering external effects right when the line starts
    public UnityEvent onLineFinished; // Useful for triggering external effects after the text finishes typing
}

public class VRIFDialogueSystem : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI dialogueTextDisplay;
    public GameObject dialogueUIPanel;
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
                // Smoothly blend into the next line's animation state to avoid snapping
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

            // Wait for the typewriter effect to complete
            yield return StartCoroutine(TypeSentence(line.sentence));

            // --- INPUT & DELAY LOGIC ---
            if (line.waitForButtonPress)
            {
                // If a button press is required, execute events first so interactive elements can appear
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

    // Ganti fungsi atau Coroutine efek ketik kamu yang lama dengan ini HANYA di bagian dalamnya saja:
    public IEnumerator TypeSentence(string sentence)
    {
        // 1. Masukkan seluruh teks dari awal
        dialogueTextDisplay.text = sentence;

        // 2. Sembunyikan semua karakter di awal
        dialogueTextDisplay.maxVisibleCharacters = 0;

        // 3. Paksa TextMeshPro untuk mengkalkulasi layout/baris seketika itu juga
        dialogueTextDisplay.ForceMeshUpdate();

        // Dapatkan total huruf yang akan ditampilkan
        int totalVisibleCharacters = dialogueTextDisplay.textInfo.characterCount;
        int counter = 0;

        // 4. Munculkan karakter satu per satu menggunakan maxVisibleCharacters
        while (counter <= totalVisibleCharacters)
        {
            dialogueTextDisplay.maxVisibleCharacters = counter;
            counter++;

            // Gunakan variabel 'typeSpeed' LAMA milikmu agar tidak merusak scene lain
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

        // --- PANGGIL EVENT BARU KITA DI SINI ---
        onDialogueSequenceEnded?.Invoke();

        // (Biarkan kode lama tetap ada di bawahnya)
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