using UnityEngine;

public class TimeController : MonoBehaviour
{
    [Header("Time Settings (Editor Only)")]
    [Tooltip("Time speed multiplier. 1 = Normal, 2 = 2x faster, 0.5 = Slow motion")]
    [Range(0.1f, 30f)]
    public float timeMultiplier = 3.0f;

    [Header("Button Mode")]
    [Tooltip("If checked, time is only sped up while HOLDING the key. If unchecked, time is always sped up.")]
    public bool holdToFastForward = true;
    [Tooltip("Keyboard key to fast forward time")]
    public KeyCode fastForwardKey = KeyCode.F;

    private void Update()
    {
#if UNITY_EDITOR
        if (holdToFastForward)
        {
            if (Input.GetKeyDown(fastForwardKey))
            {
                Time.timeScale = timeMultiplier;
            }
            else if (Input.GetKeyUp(fastForwardKey))
            {
                Time.timeScale = 1f;
            }
        }
        else
        {
            Time.timeScale = timeMultiplier;
        }
#endif
    }

    private void OnDisable()
    {
        Time.timeScale = 1f;
    }
}