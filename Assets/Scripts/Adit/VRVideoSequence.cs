using UnityEngine;
using UnityEngine.Video;
using UnityEngine.Events; // Required to show the Event column in the Inspector

[RequireComponent(typeof(VideoPlayer))]
public class VRVideoSequence : MonoBehaviour
{
    private VideoPlayer videoPlayer;

    [Header("Video Events")]
    [Tooltip("Everything in this list will execute automatically when the video finishes, acting exactly like an OnClick event for a Button")]
    public UnityEvent onVideoFinished;

    private void Start()
    {
        videoPlayer = GetComponent<VideoPlayer>();

        // Register event: run the OnVideoEnd function when the video reaches the last second
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached += OnVideoEnd;
        }
    }

    // This function is automatically called by the Unity system when the video finishes playing
    private void OnVideoEnd(VideoPlayer vp)
    {
        Debug.Log("<color=orange>Video Finished! Executing all events in the list...</color>");

        // Invoke/run all functions registered in the Inspector
        onVideoFinished?.Invoke();
    }
}