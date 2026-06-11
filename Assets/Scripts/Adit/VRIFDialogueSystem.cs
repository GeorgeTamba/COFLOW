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

    [Tooltip("Masukkan audio suara untuk dialog ini (opsional)")]
    public AudioClip dialogueAudio; // --- FITUR BARU: Slot Audio ---

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

    [Tooltip("Komponen AudioSource untuk memutar suara dialog")]
    public AudioSource audioSource; // --- FITUR BARU: Referensi AudioSource ---

    [Header("End Sequence Actions")]
    [Tooltip("Hapus centang ini jika ingin teleport dikendalikan oleh hal lain (misal: Video Selesai)")]
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

            // Memutar Audio Dialog
            if (audioSource != null && line.dialogueAudio != null)
            {
                audioSource.Stop();
                audioSource.clip = line.dialogueAudio;
                audioSource.Play();
            }

            // Menunggu efek mesin tik selesai
            yield return StartCoroutine(TypeSentence(line.sentence));

            // --- PERBAIKAN LOGIKA EVENT ---
            if (line.waitForButtonPress)
            {
                // Jika dialog ini butuh tombol ditekan, Event HARUS dieksekusi duluan
                // agar panel (seperti opsi obat/tes kecemasan) muncul dan bisa diklik.
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
                // Jika otomatis (tanpa tombol), sistem WAJIB menunggu delay habis dulu,
                // barulah event panel/video dimunculkan.
                yield return new WaitForSeconds(delayBetweenSentences);
                line.onLineFinished?.Invoke();
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