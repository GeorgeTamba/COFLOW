using UnityEngine;

public class MissionFlowManager : MonoBehaviour
{
    [Header("Daftar Trigger Misi (Teleport/Dialog)")]
    [Tooltip("Masukkan objek Trigger dari Step 1 sampai akhir secara berurutan")]
    public GameObject[] missionTriggers;

    [Header("Daftar Target Pathway (Opsional)")]
    [Tooltip("Masukkan objek Pathway (yang memiliki script PathfinderDestination) sejajar dengan urutan Misi di atas. Biarkan slot kosong/None jika step tersebut tidak butuh pathway.")]
    public GameObject[] pathwayDestinations;

    private int currentStep = 0;

    void Start()
    {
        // Set up awal saat game dimulai: matikan semua kecuali urutan pertama (index 0)
        for (int i = 0; i < missionTriggers.Length; i++)
        {
            if (missionTriggers[i] != null) missionTriggers[i].SetActive(i == 0);
        }

        for (int i = 0; i < pathwayDestinations.Length; i++)
        {
            if (pathwayDestinations[i] != null) pathwayDestinations[i].SetActive(i == 0);
        }
    }

    public void CompleteCurrentStep()
    {
        // Matikan trigger dan pathway dari step yang baru saja diselesaikan
        if (currentStep < missionTriggers.Length && missionTriggers[currentStep] != null)
        {
            missionTriggers[currentStep].SetActive(false);
        }
        if (currentStep < pathwayDestinations.Length && pathwayDestinations[currentStep] != null)
        {
            pathwayDestinations[currentStep].SetActive(false);
        }

        currentStep++; // Naik ke misi selanjutnya

        // Nyalakan trigger dan pathway untuk misi berikutnya
        if (currentStep < missionTriggers.Length && missionTriggers[currentStep] != null)
        {
            missionTriggers[currentStep].SetActive(true);
            Debug.Log("<color=cyan>Akses Terbuka! Trigger untuk " + missionTriggers[currentStep].name + " aktif.</color>");
        }

        if (currentStep < pathwayDestinations.Length && pathwayDestinations[currentStep] != null)
        {
            pathwayDestinations[currentStep].SetActive(true);
        }
    }
}