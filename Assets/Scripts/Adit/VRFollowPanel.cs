using UnityEngine;

public class VRFollowPanel : MonoBehaviour
{
    [Header("Target References")]
    [Tooltip("Drag your PlayerController here")]
    public Transform playerRoot;

    [Tooltip("Drag the VR Camera (CenterEyeAnchor / Main Camera) here. If left empty, it will find it automatically.")]
    public Transform playerCamera;

    [Header("Spawn Offset Settings")]
    [Tooltip("X: Left/Right, Y: Height from floor, Z: Distance FORWARD from player's face")]
    public Vector3 spawnOffset = new Vector3(0f, 1.3f, 1.5f);

    [Tooltip("CENTANG HANYA SAAT TESTING DI EDITOR untuk menggeser offset secara live. MATIKAN saat game sudah rilis/build agar lebih ringan!")]
    public bool liveEditOffset = false;

    [Tooltip("How smoothly the panel follows the player (Higher = more responsive)")]
    public float followSpeed = 5f;

    private Quaternion sceneRotation;
    private Vector3 lockedForward;
    private Vector3 lockedRight;
    private Vector3 fixedWorldOffset;
    private bool isInitialized = false;

    void Awake()
    {
        // Simpan rotasi/kemiringan awal dari scene
        sceneRotation = transform.rotation;
    }

    void OnEnable()
    {
        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main.transform;
        }

        if (playerRoot != null && playerCamera != null)
        {
            // Kunci arah depan dan kanan saat pertama kali panel menyala
            lockedForward = playerCamera.forward;
            lockedForward.y = 0;
            lockedForward.Normalize();

            lockedRight = playerCamera.right;
            lockedRight.y = 0;
            lockedRight.Normalize();

            // Hitung jarak awal
            RecalculateOffset();

            // Langsung pindahkan ke posisi target seketika di frame pertama (tanpa Lerp)
            transform.position = playerRoot.position + fixedWorldOffset;
            transform.rotation = sceneRotation;

            isInitialized = true;
        }
    }

    void LateUpdate()
    {
        if (!isInitialized || playerRoot == null) return;

        // Jika mode edit nyala, CPU akan menghitung ulang offsetnya tiap frame (Real-time update)
        if (liveEditOffset)
        {
            RecalculateOffset();
        }

        // Panel bergerak mulus mengikuti posisi tersebut berdasarkan jarak paten yang sudah dikunci
        Vector3 targetPosition = playerRoot.position + fixedWorldOffset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * followSpeed);

        // Terus kunci rotasinya dengan kemiringan dari scene
        transform.rotation = sceneRotation;
    }

    // Fungsi pembantu untuk mengkalkulasi offset
    private void RecalculateOffset()
    {
        Vector3 targetSpawnPos = playerRoot.position
                                 + (lockedRight * spawnOffset.x)
                                 + (Vector3.up * spawnOffset.y)
                                 + (lockedForward * spawnOffset.z);

        // Mengunci jarak absolut antara badan player dan panel
        fixedWorldOffset = targetSpawnPos - playerRoot.position;
    }
}