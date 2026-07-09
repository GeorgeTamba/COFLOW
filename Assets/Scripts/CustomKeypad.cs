using UnityEngine;
using TMPro;

public class CustomKeypad : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text displayField;
    public GameObject validatingText;
    public TMP_Text numLineText;

    [Header("Logic References")]
    public VRLoginManager loginManager;

    private string currentInput = "";
    private int maxDigits = 6;

    private void Awake()
    {
        validatingText.SetActive(false);
    }

    public void ButtonPress_Number(string numberString)
    {
        if (currentInput.Length < maxDigits)
        {
            currentInput += numberString;
            UpdateDisplay();
        }
    }

    public void ButtonPress_Backspace()
    {
        if (currentInput.Length > 0)
        {
            currentInput = currentInput.Substring(0, currentInput.Length - 1);
            UpdateDisplay();
        }
    }

    public void ButtonPress_Enter()
    {
        if (currentInput.Length > 0)
        {
            Debug.Log("Sending code from Keypad: " + currentInput);
            displayField.text = "";
            numLineText.text = "";
            validatingText.SetActive(true);

            loginManager.OnSubmitCode(currentInput);

            currentInput = "";
        }
    }

    private void UpdateDisplay()
    {
        displayField.text = currentInput;
    }
}