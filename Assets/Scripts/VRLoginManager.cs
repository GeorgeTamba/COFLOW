using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

public class VRLoginManager : MonoBehaviour
{
    private string validateUrl = "http://localhost:3000/api/vr/validate";
    private string startSessionUrl = "http://localhost:3000/api/vr/start-session";

    // Classes for packing/unpacking JSON
    [System.Serializable] public class ValidateReq { public string code; }
    [System.Serializable] public class ValidateRes { public string message; public ValidateData data; }
    [System.Serializable] public class ValidateData { public string code_id; public string patient_id; }

    [System.Serializable] public class StartSessionReq { public string patientId; public string codeId; public string device; public string appVersion; }
    [System.Serializable] public class StartSessionRes { public string message; public string sessionId; }

    // Connect this to your BNG Keypad "Enter" button!
    public void OnSubmitCode(string enteredCode)
    {
        Debug.Log("Player entered code: " + enteredCode);
        StartCoroutine(ProcessLoginChain(enteredCode));
    }

    private IEnumerator ProcessLoginChain(string code)
    {
        // ==========================================
        // STEP 1: VALIDATE THE CODE
        // ==========================================
        ValidateReq valReq = new ValidateReq { code = code };
        UnityWebRequest req1 = CreatePostRequest(validateUrl, JsonUtility.ToJson(valReq));

        yield return req1.SendWebRequest();

        if (req1.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Code Invalid! Turn keypad screen red.");
            // Stop the chain right here.
            yield break;
        }

        // Unpack the Waiter's box to get the IDs
        ValidateRes valResponse = JsonUtility.FromJson<ValidateRes>(req1.downloadHandler.text);
        string pId = valResponse.data.patient_id;
        string cId = valResponse.data.code_id;

        Debug.Log("Code Valid! Patient ID: " + pId);

        // ==========================================
        // STEP 2: START THE SESSION
        // ==========================================
        StartSessionReq startReq = new StartSessionReq
        {
            patientId = pId,
            codeId = cId,
            device = "Meta Quest 2",
            appVersion = "1.0"
        };

        UnityWebRequest req2 = CreatePostRequest(startSessionUrl, JsonUtility.ToJson(startReq));

        yield return req2.SendWebRequest();

        if (req2.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to start session: " + req2.error);
            yield break;
        }

        // Unpack the final box to get our Golden Ticket
        StartSessionRes startResponse = JsonUtility.FromJson<StartSessionRes>(req2.downloadHandler.text);

        // SAVE IT GLOBALLY!
        SessionDataStore.sessionId = startResponse.sessionId;
        Debug.Log("<color=green>SESSION STARTED!</color> Session ID: " + SessionDataStore.sessionId);

        // ==========================================
        // STEP 3: LOAD THE HOSPITAL
        // ==========================================
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainLobby");
    }

    // Helper function to keep our code clean
    private UnityWebRequest CreatePostRequest(string url, string json)
    {
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        return request;
    }
}