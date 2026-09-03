using UnityEngine;
using UnityEngine.UI;

public class VRMedicationSubmit : MonoBehaviour
{
    [Header("Canvas Transition (Use Canvas Groups!)")]
    public CanvasGroup medicationCanvasGroup;
    public CanvasGroup anxietyCanvasGroup;

    [Header("Status Placeholders")]
    private string statusChecked = "LANJUT";
    private string statusUnchecked = "STOP";

    [System.Serializable]
    public class MedToggleMap
    {
        public string drugCode;
        public Toggle continueMedToggle;
        public Toggle stopMedToggle;
    }

    [Header("Drug List")]
    public MedToggleMap[] drugsInUI;

    public void OnSubmitMedications()
    {
        // 1. Save the drugs to the Backpack
        foreach (var med in drugsInUI)
        {
            // Check which toggle is active
            if (med.continueMedToggle.isOn)
            {
                SessionDataStore.medications.Add(new SessionDataStore.MedRecord
                {
                    drugCode = med.drugCode,
                    status = statusChecked
                });
            }
            else if (med.stopMedToggle.isOn)
            {
                SessionDataStore.medications.Add(new SessionDataStore.MedRecord
                {
                    drugCode = med.drugCode,
                    status = statusUnchecked
                });
            }
            // If neither is checked (both are false), it skips adding anything to the list.
        }

        Debug.Log($"Packed {SessionDataStore.medications.Count} medications into the Backpack!");

        // ==========================================
        // THE SAFE UI TRANSITION
        // ==========================================

        // Hide Medication Canvas (Fade out and disable lasers)
        medicationCanvasGroup.alpha = 0f;
        medicationCanvasGroup.interactable = false;
        medicationCanvasGroup.blocksRaycasts = false;

        // Show Anxiety Canvas (Fade in and enable lasers)
        anxietyCanvasGroup.alpha = 1f;
        anxietyCanvasGroup.interactable = true;
        anxietyCanvasGroup.blocksRaycasts = true;
    }
}