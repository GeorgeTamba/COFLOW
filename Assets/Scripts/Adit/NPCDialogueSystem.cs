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

public class NPCDialogueSystem : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI dialogueTextDisplay;
    public GameObject dialogueUIPanel;

    [Header("Panel Animation")]
    public float unfoldDuration = 0.3f;

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

    [Header("Typewriter Settings")]
    public float typeSpeed = 0.05f;
    public float delayBetweenSentences = 1.0f;

    [Header("Modular Dialogue Content")]
    public DialogueLine[] dialogueLines;

    private bool isWaitingForInput = false;
    private Canvas parentCanvas;
    private GraphicRaycaster parentRaycaster;
    private CanvasGroup canvasGroup;

    // Guards against StartDialogueSequence() being called again (e.g. double trigger,
    // double button press) while a sequence is already running. Without this, two
    // PlayDialogueSequence coroutines can run in parallel, both writing to the same
    // dialogueTextDisplay and fighting over the same AudioSource — causing overlapping
    // text and stuttering/overlapping audio.
    private bool isSequenceRunning = false;

    private void Start()
    {
        parentCanvas = GetComponent<Canvas>();
        parentRaycaster = GetComponent<GraphicRaycaster>();
        canvasGroup = GetComponent<CanvasGroup>();
        ToggleUI(false);
    }

    public void StartDialogueSequence()
    {
        if (isSequenceRunning) return; // already running — ignore duplicate calls
        isSequenceRunning = true;

        ToggleUI(true);
        StartCoroutine(HandleDialogueStartDelay());
    }

    private IEnumerator HandleDialogueStartDelay()
    {
        // 1. Run the unfold animation and WAIT until it is completely finished
        yield return StartCoroutine(UnfoldPanel());

        // 2. Delay for 1 second before starting the text (adjust this value if needed)
        yield return new WaitForSeconds(1f);

        // 3. Run the dialogue sequence
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

                // Defensive: always match whatever fast-forward speed is currently active.
                // TimeController only re-applies pitch to AudioSources it can find via
                // FindObjectsOfType() at the moment the FF button is pressed/released, so an
                // AudioSource that wasn't active/found at that exact moment could be left at
                // the wrong pitch. Setting it here guarantees this clip is always correct.
                audioSource.pitch = Time.timeScale > 0f ? Time.timeScale : 1f;

                audioSource.Play();
            }

            // Wait for the typewriter effect to complete
            yield return StartCoroutine(TypeSentence(line.sentence, line.dialogueAudio));

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

    public IEnumerator UnfoldPanel()
    {
        // Ensure the UI panel is assigned in the Inspector to avoid errors
        if (dialogueUIPanel != null)
        {
            // Set initial Y scale to 0 (Flattened panel in the middle)
            Vector3 initialScale = dialogueUIPanel.transform.localScale;
            dialogueUIPanel.transform.localScale = new Vector3(initialScale.x, 0f, initialScale.z);

            float elapsedTime = 0f;

            while (elapsedTime < unfoldDuration)
            {
                elapsedTime += Time.deltaTime;

                // Calculate scale change smoothly using Lerp
                float currentY = Mathf.Lerp(0f, 1f, elapsedTime / unfoldDuration);
                dialogueUIPanel.transform.localScale = new Vector3(initialScale.x, currentY, initialScale.z);

                // Wait for the next frame
                yield return null;
            }

            // Ensure the scale returns to exactly 100% at the end of the animation
            dialogueUIPanel.transform.localScale = new Vector3(initialScale.x, 1f, initialScale.z);
        }
    }

    // Typewriter effect using maxVisibleCharacters to prevent word wrap jump.
    //
    // WHY audio-driven instead of WaitForSeconds:
    // Previously the reveal speed was timed independently with WaitForSeconds(typeSpeed),
    // which is scaled by Time.timeScale. AudioSource.pitch is *also* used to speed up audio
    // during fast-forward, but the two are not the same kind of "fast": WaitForSeconds still
    // needs at least one rendered frame to resume no matter how small the scaled duration is,
    // so at high fast-forward multipliers the typewriter hits a hard floor of ~1 character per
    // frame. Audio playback has no such floor — it speeds up exactly linearly with pitch. The
    // result was audio finishing well before the text (or vice versa), and sentences visibly
    // overlapping/out of sync.
    //
    // Fix: when a line has voice audio, don't time the text independently at all — derive the
    // number of revealed characters directly from how far the AudioSource has actually played
    // (audioSource.time / clip.length). Since AudioSource.time already reflects the true
    // playback position under any pitch/Time.timeScale, the caption is mathematically
    // guaranteed to always match the voice line, at 1x or during fast-forward.
    public IEnumerator TypeSentence(string sentence, AudioClip clip = null)
    {
        // 1. Insert the entire text from the beginning
        dialogueTextDisplay.text = sentence;

        // 2. Hide all characters initially
        dialogueTextDisplay.maxVisibleCharacters = 0;

        // 3. Force TextMeshPro to calculate layout/lines immediately
        dialogueTextDisplay.ForceMeshUpdate();

        // Get the total number of characters to be displayed
        int totalVisibleCharacters = dialogueTextDisplay.textInfo.characterCount;

        bool hasAudio = audioSource != null && clip != null && audioSource.isPlaying;

        if (hasAudio)
        {
            // 4a. Audio-driven reveal: always matches the voice line's real progress
            while (audioSource != null && audioSource.isPlaying)
            {
                float progress = Mathf.Clamp01(audioSource.time / clip.length);
                dialogueTextDisplay.maxVisibleCharacters = Mathf.FloorToInt(progress * totalVisibleCharacters);
                yield return null;
            }
        }
        else
        {
            // 4b. Fallback for silent lines (no dialogueAudio assigned): use the fixed
            // typeSpeed timer. Still affected by Time.timeScale during fast-forward, but
            // there's no audio to desync from here, so it's not a problem.
            int counter = 0;
            while (counter <= totalVisibleCharacters)
            {
                dialogueTextDisplay.maxVisibleCharacters = counter;
                counter++;
                yield return new WaitForSeconds(typeSpeed);
            }
        }

        // 5. Always finish with the full sentence fully revealed, even if the loop above
        // exited a frame early (e.g. audio stopped exactly on the last sample).
        dialogueTextDisplay.maxVisibleCharacters = totalVisibleCharacters;
    }

    public void ResumeDialogue()
    {
        isWaitingForInput = false;
        Debug.Log("Dialogue resumed!");
    }

    private void EndDialogueSequence()
    {
        isSequenceRunning = false;
        ToggleUI(false);
        if (npcAnimator != null && !string.IsNullOrEmpty(endAnimationStateName))
        {
            npcAnimator.CrossFade(endAnimationStateName, endTransitionDuration);
        }
        onDialogueSequenceEnded?.Invoke();
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