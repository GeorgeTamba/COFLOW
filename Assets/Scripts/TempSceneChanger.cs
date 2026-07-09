using UnityEngine;
using UnityEngine.SceneManagement;

public class TempSceneChanger : MonoBehaviour
{
    [Header("Option 1: Set in Inspector")]
    [Tooltip("Type the exact name of the scene you want to load here.")]
    public string targetSceneName;

    // Call this if you typed the name in the variable above
    public void LoadTargetScene()
    {
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            Debug.Log($"Loading Scene: {targetSceneName}");
            SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogError("Scene name is empty! Type the name in the Inspector.");
        }
    }

    // Call this if you want to type the name directly in the Button's OnClick box
    public void LoadSceneByName(string sceneName)
    {
        Debug.Log($"Loading Scene: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }
}