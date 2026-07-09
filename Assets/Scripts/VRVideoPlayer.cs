using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class VRVideoPlayer : MonoBehaviour
{
    [Header("Attach your Video Player here")]
    public VideoPlayer myVideoPlayer;

    [Header("VR Theater Settings")]
    [Tooltip("Masukkan objek DarkRoomBox ke sini")]
    public MeshRenderer darkRoomRenderer;

    public float fadeSpeed = 2f;

    [Range(0f, 1f)]
    public float maxDarkness = 0.85f;

    private void Start()
    {
        if (darkRoomRenderer != null)
        {
            SetBoxAlpha(0f);
            darkRoomRenderer.gameObject.SetActive(false);
        }
    }

    public void PlayTheVideo()
    {
        if (myVideoPlayer != null)
        {
            myVideoPlayer.Play();
            if (darkRoomRenderer != null)
            {
                darkRoomRenderer.gameObject.SetActive(true);
                StopAllCoroutines();
                StartCoroutine(FadeBox(maxDarkness));
            }
        }
    }

    public void PauseTheVideo()
    {
        if (myVideoPlayer != null)
        {
            myVideoPlayer.Pause();
            TurnOffDarkRoom();
        }
    }

    public void TurnOffDarkRoom()
    {
        if (darkRoomRenderer != null && darkRoomRenderer.gameObject.activeInHierarchy)
        {
            StopAllCoroutines();
            StartCoroutine(FadeBox(0f, true));
        }
    }

    private IEnumerator FadeBox(float targetAlpha, bool disableAfterFade = false)
    {
        if (darkRoomRenderer == null) yield break;

        Material boxMat = darkRoomRenderer.material;
        Color matColor = boxMat.color;

        while (Mathf.Abs(matColor.a - targetAlpha) > 0.01f)
        {
            matColor.a = Mathf.MoveTowards(matColor.a, targetAlpha, fadeSpeed * Time.deltaTime);
            boxMat.color = matColor;
            yield return null;
        }

        matColor.a = targetAlpha;
        boxMat.color = matColor;

        if (disableAfterFade)
        {
            darkRoomRenderer.gameObject.SetActive(false);
        }
    }

    private void SetBoxAlpha(float alpha)
    {
        Material boxMat = darkRoomRenderer.material;
        Color matColor = boxMat.color;
        matColor.a = alpha;
        boxMat.color = matColor;
    }
}