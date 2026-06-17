using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class BedTransitionSequence : MonoBehaviour
{
    [Header("Referensi Transisi & UI")]
    public BNG.ScreenFader screenFader;
    public CanvasGroup nextDayTextCanvas;

    [Header("Durasi (Detik)")]
    public float fadeDuration = 1.0f;
    public float textDisplayDuration = 3.0f;

    [Header("Referensi Player VR")]
    public Transform playerRig;
    public MonoBehaviour[] movementScripts;

    [Header("Pergerakan Kasur (NavMesh)")]
    public NavMeshAgent bedAgent;
    public Transform bedMountPoint;

    [Tooltip("Titik lokasi kasur akan MUNCUL setelah layar gelap")]
    public Transform bedSpawnPoint;

    [Tooltip("Titik tujuan berurutan setelah kasur spawn.")]
    public Transform[] bedDestinations;

    [Header("Scene Berikutnya")]
    public string nextSceneName;

    private bool sequenceStarted = false;

    private void Start()
    {
        if (nextDayTextCanvas != null) nextDayTextCanvas.alpha = 0f;
        if (bedAgent != null) bedAgent.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!sequenceStarted && other.CompareTag("Player"))
        {
            sequenceStarted = true;
            StartCoroutine(PlayBedSequence());
        }
    }

    private IEnumerator PlayBedSequence()
    {
        // 0. KUNCI PERGERAKAN SEJAK DETIK PERTAMA!
        // Pemain langsung beku seketika saat menyentuh trigger
        LockPlayerMovement(true);

        // 1. Layar Gelap
        if (screenFader != null) screenFader.DoFadeIn();
        yield return new WaitForSeconds(fadeDuration);

        // 2. Munculkan Teks
        if (nextDayTextCanvas != null)
        {
            nextDayTextCanvas.alpha = 1f;
            yield return new WaitForSeconds(textDisplayDuration);
            nextDayTextCanvas.alpha = 0f;
        }

        // 3. Pindahkan Kasur & Player (Di Balik Layar Gelap)
        if (bedAgent != null && bedSpawnPoint != null)
        {
            bedAgent.enabled = false;
            bedAgent.transform.position = bedSpawnPoint.position;
            bedAgent.transform.rotation = bedSpawnPoint.rotation;
        }

        if (bedMountPoint != null)
        {
            playerRig.position = bedMountPoint.position;
            playerRig.rotation = bedMountPoint.rotation;
            playerRig.SetParent(bedAgent.transform);
        }

        // 4. Layar Terang Kembali
        if (screenFader != null) screenFader.DoFadeOut();
        yield return new WaitForSeconds(fadeDuration);

        // 5. Kasur mulai jalan menyusuri Waypoints
        if (bedAgent != null && bedDestinations.Length > 0)
        {
            bedAgent.enabled = true;

            foreach (Transform target in bedDestinations)
            {
                if (target == null) continue;

                bedAgent.SetDestination(target.position);

                while (bedAgent.pathPending || bedAgent.remainingDistance > 0.5f)
                {
                    yield return null;
                }
            }
        }

        // 6. Selesai, pindah scene
        if (screenFader != null) screenFader.DoFadeIn();
        yield return new WaitForSeconds(fadeDuration);

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }

    private void LockPlayerMovement(bool lockMovement)
    {
        foreach (var script in movementScripts)
        {
            if (script != null) script.enabled = !lockMovement;
        }

        CharacterController cc = playerRig.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = !lockMovement;
    }
}