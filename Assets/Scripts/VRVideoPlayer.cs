using UnityEngine;
using UnityEngine.Video; // You must include this to use the VideoPlayer!

public class VRVideoPlayer : MonoBehaviour
{
    [Header("Attach your Video Player here")]
    public VideoPlayer myVideoPlayer;

    // We make this public so the Button can see it!
    public void PlayTheVideo()
    {
        // Safety check to make sure you didn't forget to attach the video player
        if (myVideoPlayer != null)
        {
            myVideoPlayer.Play();
            Debug.Log("<color=green>Video Started Playing!</color>");
        }
        else
        {
            Debug.LogError("Whoops! You forgot to attach the Video Player to the script.");
        }
    }

    // BONUS: I added a Pause/Stop function just in case you need it later!
    public void PauseTheVideo()
    {
        if (myVideoPlayer != null)
        {
            myVideoPlayer.Pause();
        }
    }
}