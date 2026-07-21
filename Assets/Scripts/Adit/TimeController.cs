using UnityEngine;
using UnityEngine.Video;
using BNG;

public class TimeController : MonoBehaviour
{
    [Header("Time Settings")]
    [Tooltip("Time speed multiplier. 1 = Normal, 2 = 2x faster, 0.5 = Slow motion")]
    [Range(0.1f, 30f)]
    public float timeMultiplier = 3.0f;

    [Header("Button Mode")]
    [Tooltip("If checked, time is only sped up while HOLDING the key/button. If unchecked, pressing the key/button toggles it on/off.")]
    public bool holdToFastForward = true;
    [Tooltip("Keyboard key to fast forward time (Editor/Desktop testing)")]
    public KeyCode fastForwardKey = KeyCode.F;
    [Tooltip("VR controller button to fast forward time")]
    public FastForwardButtonOption fastForwardButton = FastForwardButtonOption.X_Button;

    public enum FastForwardButtonOption
    {
        A_Button,
        B_Button,
        X_Button,
        Y_Button
    }

    private bool isFastForwarding = false;

    private void Update()
    {
        bool buttonDown = Input.GetKeyDown(fastForwardKey) || CheckVRButtonDown();
        bool buttonUp = Input.GetKeyUp(fastForwardKey) || CheckVRButtonUp();

        if (holdToFastForward)
        {
            if (buttonDown && !isFastForwarding) SetFastForward(true);
            else if (buttonUp && isFastForwarding) SetFastForward(false);
        }
        else if (buttonDown)
        {
            SetFastForward(!isFastForwarding);
        }
    }

    bool CheckVRButtonDown()
    {
        if (InputBridge.Instance == null) return false;

        switch (fastForwardButton)
        {
            case FastForwardButtonOption.A_Button: return InputBridge.Instance.AButtonDown;
            case FastForwardButtonOption.B_Button: return InputBridge.Instance.BButtonDown;
            case FastForwardButtonOption.X_Button: return InputBridge.Instance.XButtonDown;
            case FastForwardButtonOption.Y_Button: return InputBridge.Instance.YButtonDown;
            default: return false;
        }
    }

    bool CheckVRButtonUp()
    {
        if (InputBridge.Instance == null) return false;

        switch (fastForwardButton)
        {
            case FastForwardButtonOption.A_Button: return InputBridge.Instance.AButtonUp;
            case FastForwardButtonOption.B_Button: return InputBridge.Instance.BButtonUp;
            case FastForwardButtonOption.X_Button: return InputBridge.Instance.XButtonUp;
            case FastForwardButtonOption.Y_Button: return InputBridge.Instance.YButtonUp;
            default: return false;
        }
    }

    void SetFastForward(bool fastForward)
    {
        isFastForwarding = fastForward;
        float speed = fastForward ? timeMultiplier : 1f;

        Time.timeScale = speed;

        // Keep in-game audio and video in sync with the time speed so they don't clash
        foreach (AudioSource source in FindObjectsOfType<AudioSource>())
        {
            source.pitch = speed;
        }

        foreach (VideoPlayer video in FindObjectsOfType<VideoPlayer>())
        {
            video.playbackSpeed = speed;
        }
    }

    private void OnDisable()
    {
        SetFastForward(false);
    }
}
