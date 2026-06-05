using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class VRPathfinderManager : MonoBehaviour
{
    [Header("Setup References")]
    [Tooltip("Drag the VR Player Rig (or Main Camera) here")]
    public Transform playerRig;
    [Tooltip("Drag your glowing Chevron Prefab from the Project window here")]
    public GameObject chevronPrefab;

    [Header("Guidance Settings")]
    public float chevronSpacing = 1.0f;
    public float heightOffset = 1.2f;
    public float updateInterval = 0.5f;
    public float hideDistance = 1.5f;

    private Transform currentTarget;
    private NavMeshPath path;
    private List<GameObject> chevronPool = new List<GameObject>();
    private Coroutine pathRoutine;

    void Awake()
    {
        path = new NavMeshPath();
    }

    public void StartGuidingPlayer(Transform target)
    {
        currentTarget = target;

        if (pathRoutine != null) StopCoroutine(pathRoutine);
        pathRoutine = StartCoroutine(UpdatePathRoutine());
    }

    public void StopGuiding()
    {
        if (pathRoutine != null) StopCoroutine(pathRoutine);
        HideAllChevrons();
    }

    private IEnumerator UpdatePathRoutine()
    {
        while (currentTarget != null)
        {
            DrawPath();
            yield return new WaitForSeconds(updateInterval);
        }
    }

    private void DrawPath()
    {
        if (playerRig == null || currentTarget == null)
        {
            Debug.LogWarning("<color=orange>GPS Pause: Player Rig or Target is missing!</color>");
            return;
        }

        // Calculate the path
        NavMesh.CalculatePath(playerRig.position, currentTarget.position, NavMesh.AllAreas, path);

        // DEBUG 1: Did the NavMesh fail?
        if (path.status != NavMeshPathStatus.PathComplete)
        {
            Debug.LogWarning($"<color=red>GPS FAILED: Path Status is {path.status}. The Player or Target is likely NOT touching the NavMesh!</color>");
            HideAllChevrons();
            return;
        }

        List<Vector3> chevronPoints = new List<Vector3>();

        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            Vector3 startPoint = path.corners[i];
            Vector3 endPoint = path.corners[i + 1];
            float distance = Vector3.Distance(startPoint, endPoint);

            int chevronsInSegment = Mathf.FloorToInt(distance / chevronSpacing);

            for (int j = 1; j <= chevronsInSegment; j++)
            {
                float t = j / (float)chevronsInSegment;
                Vector3 point = Vector3.Lerp(startPoint, endPoint, t);
                float distanceToPlayer = Vector3.Distance(playerRig.position, point);
                if (distanceToPlayer > hideDistance)
                {
                    point.y += heightOffset;
                    chevronPoints.Add(point);
                }
            }
        }

        // DEBUG 2: Is the distance too short?
        if (chevronPoints.Count == 0)
        {
            Debug.Log("<color=yellow>GPS Active, but Target is too close to spawn any chevrons!</color>");
        }
        else
        {
            Debug.Log($"<color=green>GPS SUCCESS! Spawning {chevronPoints.Count} chevrons.</color>");
        }

        // Move Chevrons
        for (int i = 0; i < chevronPoints.Count; i++)
        {
            if (i >= chevronPool.Count)
            {
                GameObject newChevron = Instantiate(chevronPrefab, this.transform);
                chevronPool.Add(newChevron);
            }

            chevronPool[i].transform.position = chevronPoints[i];
            chevronPool[i].SetActive(true);

            if (i < chevronPoints.Count - 1)
            {
                Vector3 direction = chevronPoints[i + 1] - chevronPoints[i];
                if (direction != Vector3.zero)
                {
                    chevronPool[i].transform.rotation = Quaternion.LookRotation(direction);
                }
            }
        }

        // Hide leftovers
        for (int i = chevronPoints.Count; i < chevronPool.Count; i++)
        {
            chevronPool[i].SetActive(false);
        }
    }

    private void HideAllChevrons()
    {
        foreach (GameObject chevron in chevronPool)
        {
            chevron.SetActive(false);
        }
    }
}