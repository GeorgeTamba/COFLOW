using UnityEngine;
using TMPro; // Assuming you are using TextMeshPro for your InputField/Text!

public class CustomKeypad : MonoBehaviour
{
    [Header("UI References")]
    // Drag your UI Text or InputField here in the Unity Inspector!
    public TMP_Text displayField; 

    [Header("Logic References")]
    // Drag the GameObject holding your VRLoginManager here!
    public VRLoginManager loginManager; 

    // The secret string holding what the player is typing
    private string currentInput = "";
    // Access codes are usually 6 digits, let's enforce a limit!
    private int maxDigits = 6; 

    // 1. Connect this to your 0-9 Buttons!
    // In the Unity Inspector OnClick(), type the number in the little box (e.g., "1", "2")
    public void ButtonPress_Number(string numberString)
    {
        if (currentInput.Length < maxDigits)
        {
            currentInput += numberString;
            UpdateDisplay();
        }
    }

    // 2. Connect this to your Backspace Button!
    public void ButtonPress_Backspace()
    {
        if (currentInput.Length > 0)
        {
            // Chop off the last letter
            currentInput = currentInput.Substring(0, currentInput.Length - 1);
            UpdateDisplay();
        }
    }

    // 3. Connect this to your Enter Button!
    public void ButtonPress_Enter()
    {
        if (currentInput.Length > 0)
        {
            Debug.Log("Sending code from Keypad: " + currentInput);
            displayField.text = "Validating...";
            
            // Hand the code over to the big API manager we built earlier!
            loginManager.OnSubmitCode(currentInput);
            
            // Clear the keypad for the next try
            currentInput = ""; 
        }
    }

    // Updates the screen so the player sees what they typed
    private void UpdateDisplay()
    {
        displayField.text = currentInput;
    }
}