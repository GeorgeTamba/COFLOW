using UnityEngine;
using UnityEngine.Networking; // Required for our Waiter
using System.Collections;
using System.Text; // Required for text encoding

public class VRNetworkTester : MonoBehaviour
{
    // The address of your Node.js Waiter
    private string apiUrl = "http://localhost:3000/api/vr/validate";

    // 1. The Blueprint for our JSON box
    [System.Serializable]
    public class ValidationRequest
    {
        public string code;
    }

    // 2. The function we will attach to our VR Button
    public void TestCodeValidation()
    {
        Debug.Log("VR Button Clicked! Calling the Waiter...");
        
        // In Unity, web requests take time. We use "Coroutines" (similar to async/await in JS)
        StartCoroutine(SendPostRequest());
    }

    // 3. The actual Waiter logic
    private IEnumerator SendPostRequest()
    {
        // A. Pack the box (Put a real code from your database here!)
        ValidationRequest requestData = new ValidationRequest { code = "966716" }; 

        // B. Convert the C# object into JSON text (Like JSON.stringify in JS)
        string jsonPayload = JsonUtility.ToJson(requestData);
        Debug.Log("Sending JSON: " + jsonPayload);

        // C. Set up the Waiter's tray
        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
        
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        
        // D. Tell the server to expect JSON
        request.SetRequestHeader("Content-Type", "application/json");

        // E. Send the Waiter and WAIT for him to come back
        yield return request.SendWebRequest();

        // F. Check the response!
        if (request.result == UnityWebRequest.Result.Success)
        {
            // SUCCESS! (200 OK)
            Debug.Log("<color=green>SUCCESS!</color> The server said: " + request.downloadHandler.text);
        }
        else
        {
            // FAILURE! (400 Bad Request or server crash)
            Debug.LogError("<color=red>ERROR!</color> The server rejected it: " + request.error);
            Debug.LogError("Server Message: " + request.downloadHandler.text);
        }
    }
}