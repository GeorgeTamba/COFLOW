using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using BNG;

public class VRPauseManager : MonoBehaviour
{
    [Header("UI Pause Settings")]
    [Tooltip("Assign the Pause Canvas/Panel (UI elements only, no dark background).")]
    public GameObject pausePanel;

    [Header("Dark Screen Effect")]
    [Tooltip("Assign the dark screen Quad attached to the CenterEyeAnchor.")]
    public GameObject darkScreenEffect;

    [Header("UI to Hide")]
    [Tooltip("CanvasGroups to hide during pause (e.g., dialog panels).")]
    public CanvasGroup[] panelsToHide;

    [Header("Panel Follow Settings")]
    public Transform playerCamera;
    public Vector3 offset = new Vector3(0f, -0.2f, 1.5f);
    public float followSpeed = 8f;

    [Header("Input Settings")]
    public PauseButtonOption pauseButton = PauseButtonOption.B_Button;
    public KeyCode keyboardPauseKey = KeyCode.Escape;

    public enum PauseButtonOption
    {
        B_Button,
        Y_Button,
        Menu_Start_Button
    }

    private bool isPaused = false;
    private List<VideoPlayer> activeVideos = new List<VideoPlayer>();

    void Start()
    {
        // Ensure UI and effects are disabled on start
        if (pausePanel != null) pausePanel.SetActive(false);
        if (darkScreenEffect != null) darkScreenEffect.SetActive(false);
    }

    void Update()
    {
        bool vrButtonPressed = CheckVRPauseInput();
        bool keyboardButtonPressed = Input.GetKeyDown(keyboardPauseKey);

        if (vrButtonPressed || keyboardButtonPressed)
        {
            if (!isPaused) PauseGame();
            else ResumeGame();
        }

        // Handle panel follow logic when paused
        if (isPaused && pausePanel != null && playerCamera != null)
        {
            // Flatten camera directions to prevent the panel from tilting up/down
            Vector3 flatForward = playerCamera.forward;
            flatForward.y = 0;
            flatForward.Normalize();

            Vector3 flatRight = playerCamera.right;
            flatRight.y = 0;
            flatRight.Normalize();

            // Calculate target position
            Vector3 targetPosition = playerCamera.position + (flatForward * offset.z) + (flatRight * offset.x);
            targetPosition.y = playerCamera.position.y + offset.y;

            // Smoothly move the panel
            pausePanel.transform.position = Vector3.Lerp(pausePanel.transform.position, targetPosition, Time.unscaledDeltaTime * followSpeed);

            // Smoothly rotate the panel to face the player
            Vector3 directionToFace = pausePanel.transform.position - playerCamera.position;
            directionToFace.y = 0;

            if (directionToFace != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToFace);
                pausePanel.transform.rotation = Quaternion.Slerp(pausePanel.transform.rotation, targetRotation, Time.unscaledDeltaTime * followSpeed);
            }
        }
    }

    bool CheckVRPauseInput()
    {
        if (InputBridge.Instance == null) return false;

        switch (pauseButton)
        {
            case PauseButtonOption.B_Button: return InputBridge.Instance.BButtonDown;
            case PauseButtonOption.Y_Button: return InputBridge.Instance.YButtonDown;
            case PauseButtonOption.Menu_Start_Button: return InputBridge.Instance.StartButtonDown;
            default: return false;
        }
    }

    public void PauseGame()
    {
        isPaused = true;

        // 1. Activate dark screen quad (attached to camera)
        if (darkScreenEffect != null) darkScreenEffect.SetActive(true);

        // 2. Show and position pause panel
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);

            if (playerCamera != null)
            {
                Vector3 flatForward = playerCamera.forward;
                flatForward.y = 0;
                flatForward.Normalize();

                Vector3 targetPosition = playerCamera.position + (flatForward * offset.z);
                targetPosition.y = playerCamera.position.y + offset.y;

                pausePanel.transform.position = targetPosition;
            }
        }

        // 3. Hide other UI panels
        foreach (CanvasGroup panel in panelsToHide)
        {
            if (panel != null)
            {
                panel.alpha = 0f;
                panel.interactable = false;
                panel.blocksRaycasts = false;
            }
        }

        // 4. Pause all active videos and store them in the list
        activeVideos.Clear();
        VideoPlayer[] allVideos = FindObjectsOfType<VideoPlayer>();
        foreach (VideoPlayer vp in allVideos)
        {
            if (vp.isPlaying)
            {
                vp.Pause();
                activeVideos.Add(vp);
            }
        }

        // 5. Freeze time and mute audio
        Time.timeScale = 0f;
        AudioListener.pause = true;
    }

    public void ResumeGame()
    {
        isPaused = false;

        // 1. Disable UI and effects
        if (darkScreenEffect != null) darkScreenEffect.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);

        // 2. Restore other UI panels
        foreach (CanvasGroup panel in panelsToHide)
        {
            if (panel != null)
            {
                panel.alpha = 1f;
                panel.interactable = true;
                panel.blocksRaycasts = true;
            }
        }

        // 3. Resume previously active videos
        foreach (VideoPlayer vp in activeVideos)
        {
            if (vp != null)
            {
                vp.Play();
            }
        }
        activeVideos.Clear();

        // 4. Restore time and audio
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }
}