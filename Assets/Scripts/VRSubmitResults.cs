using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Text;

public class VRSubmitResults : MonoBehaviour
{
    private string submitUrl = "https://coflowbackendapi-production.up.railway.app/api/vr/submit-results";

    // --- JSON BLUEPRINTS ---
    [System.Serializable]
    public class SubmitResultReq
    {
        public string sessionId;
        public int anxietyScore;
        public int quizScore;

        // --- Add the arrays to the JSON payload ---
        public List<int> anxietyAnswers;
        public List<int> quizAnswers;

        public List<MedicationItem> medications;
    }

    [System.Serializable]
    public class MedicationItem
    {
        public string drugCode;
        public string status;
    }

    // Connect this to your final "Finish" UI button
    public void FinalSubmit()
    {
        StartCoroutine(SendResultsToDatabase());
    }

    private IEnumerator SendResultsToDatabase()
    {
        Debug.Log("Packing the final box...");

        // 1. Create the empty box
        SubmitResultReq finalBox = new SubmitResultReq();

        // 2. Dump everything from the Invisible Backpack into the Box
        finalBox.sessionId = SessionDataStore.sessionId;
        finalBox.anxietyScore = SessionDataStore.anxietyScore;
        finalBox.quizScore = SessionDataStore.quizScore;
        finalBox.anxietyAnswers = SessionDataStore.anxietyAnswers;
        finalBox.quizAnswers = SessionDataStore.quizAnswers;

        // Convert the backpack meds into the JSON format meds
        finalBox.medications = new List<MedicationItem>();
        foreach (var med in SessionDataStore.medications)
        {
            finalBox.medications.Add(new MedicationItem { drugCode = med.drugCode, status = med.status });
        }

        // 3. Send it to the Waiter
        string jsonPayload = JsonUtility.ToJson(finalBox);

        UnityWebRequest request = new UnityWebRequest(submitUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("<color=green>RESULTS SAVED!</color> The database is updated!");
            SceneManager.LoadScene("LoginLobby");
            // Clean out the backpack for the next player!
            SessionDataStore.ClearSession();
        }
        else
        {
            Debug.LogError("Failed to save results: " + request.error);
        }
    }
}