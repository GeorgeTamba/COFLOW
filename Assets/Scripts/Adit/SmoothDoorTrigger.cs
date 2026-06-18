using System.Collections;
using UnityEngine;

public enum RotationAxis { X, Y, Z }

[System.Serializable]
public class DoorPanelSettings
{
    public Transform panelTransform;
    public RotationAxis axis = RotationAxis.Y;
    public float openAngle = 90f;

    [HideInInspector] public Quaternion closedRotation;
    [HideInInspector] public Quaternion openRotation;
}

[System.Serializable]
public class DoorGroup
{
    public string groupName = "Main Double Door";
    public GameObject triggerZone;
    public float animationSpeed = 2.5f;

    [Tooltip("Jika true, pintu mulai nonaktif dan baru merespons setelah ActivateReturnRoute() dipanggil. Untuk pintu rute pulang.")]
    public bool requiresManualActivation = false;

    [Tooltip("Centang agar trigger menonaktifkan dirinya setelah dipakai sekali")]
    public bool oneTimeUseOnly = false;

    public DoorPanelSettings[] doorPanels;

    [HideInInspector] public bool isOpen = false;
    [HideInInspector] public Coroutine activeAnimation;
    [HideInInspector] public DoorTriggerHelper helper;
}

public class SmoothDoorTrigger : MonoBehaviour
{
    [Header("Hospital Doors Master Manager")]
    public DoorGroup[] allDoorGroups;

    private void Start()
    {
        for (int i = 0; i < allDoorGroups.Length; i++)
        {
            DoorGroup group = allDoorGroups[i];

            if (group.triggerZone != null)
            {
                DoorTriggerHelper helper = group.triggerZone.AddComponent<DoorTriggerHelper>();
                helper.Setup(this, i);
                helper.isEnabled = !group.requiresManualActivation; // pintu rute pulang mulai nonaktif
                group.helper = helper;
            }

            if (group.doorPanels == null) continue;

            foreach (var panel in group.doorPanels)
            {
                if (panel == null || panel.panelTransform == null) continue;

                panel.closedRotation = panel.panelTransform.localRotation;

                Vector3 dir = Vector3.zero;
                if (panel.axis == RotationAxis.X) dir.x = panel.openAngle;
                else if (panel.axis == RotationAxis.Y) dir.y = panel.openAngle;
                else dir.z = panel.openAngle;

                panel.openRotation = panel.closedRotation * Quaternion.Euler(dir);
            }
        }
    }

    // Dipanggil oleh event Dialog UI saat rute pulang harus diaktifkan
    public void ActivateReturnRoute()
    {
        foreach (var group in allDoorGroups)
        {
            if (group.requiresManualActivation && group.helper != null)
                group.helper.isEnabled = true;
        }
    }

    public void OnTriggerZoneEnter(int groupIndex, Collider other)
    {
        DoorGroup group = allDoorGroups[groupIndex];
        if (!IsValidMover(other) || group.isOpen) return;

        group.isOpen = true;
        RunAnimation(group, true);
    }

    public void OnTriggerZoneExit(int groupIndex, Collider other)
    {
        DoorGroup group = allDoorGroups[groupIndex];
        if (!IsValidMover(other) || !group.isOpen) return;

        group.isOpen = false;
        RunAnimation(group, false);

        if (group.oneTimeUseOnly && group.helper != null)
            group.helper.isEnabled = false; // berhenti merespons setelah dipakai
    }

    private bool IsValidMover(Collider other)
    {
        return other.CompareTag("Player")
            || other.name.Contains("Kasur")
            || other.name.Contains("Driver");
    }

    private void RunAnimation(DoorGroup group, bool open)
    {
        if (group.activeAnimation != null) StopCoroutine(group.activeAnimation);
        group.activeAnimation = StartCoroutine(AnimateDoorGroup(group, open));
    }

    private IEnumerator AnimateDoorGroup(DoorGroup group, bool open)
    {
        bool isMoving = true;

        while (isMoving)
        {
            isMoving = false;

            foreach (var panel in group.doorPanels)
            {
                if (panel == null || panel.panelTransform == null) continue;

                Quaternion target = open ? panel.openRotation : panel.closedRotation;
                panel.panelTransform.localRotation = Quaternion.Slerp(
                    panel.panelTransform.localRotation, target, Time.deltaTime * group.animationSpeed);

                if (Quaternion.Angle(panel.panelTransform.localRotation, target) > 0.1f)
                    isMoving = true;
            }
            yield return null;
        }

        // Snap ke posisi final agar presisi
        foreach (var panel in group.doorPanels)
        {
            if (panel != null && panel.panelTransform != null)
                panel.panelTransform.localRotation = open ? panel.openRotation : panel.closedRotation;
        }
    }
}

public class DoorTriggerHelper : MonoBehaviour
{
    private SmoothDoorTrigger manager;
    private int groupIndex;
    public bool isEnabled = true;

    public void Setup(SmoothDoorTrigger manager, int groupIndex)
    {
        this.manager = manager;
        this.groupIndex = groupIndex;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isEnabled && manager != null) manager.OnTriggerZoneEnter(groupIndex, other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (isEnabled && manager != null) manager.OnTriggerZoneExit(groupIndex, other);
    }
}