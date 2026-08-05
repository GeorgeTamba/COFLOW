using UnityEngine;
using TMPro;

public class CustomKeypad : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text displayField;
    public GameObject validatingText;
    public GameObject numLineText;
    public GameObject tryAgainText;
    public GameObject successText;

    [Header("Logic References")]
    public VRLoginManager loginManager;

    private string currentInput = "";
    private int maxDigits = 6;

    private bool isLocked = false;

    private void Awake()
    {
        numLineText.SetActive(true);
        validatingText.SetActive(false);
        tryAgainText.SetActive(false);
        successText.SetActive(false);
    }

    public void ButtonPress_Number(string numberString)
    {
        if (isLocked) return;
        numLineText.SetActive(true);
        tryAgainText.SetActive(false);
        successText.SetActive(false);

        if (currentInput.Length < maxDigits)
        {
            currentInput += numberString;
            UpdateDisplay();
        }
    }

    public void ButtonPress_Backspace()
    {
        if (isLocked) return;
        tryAgainText.SetActive(false);
        successText.SetActive(false);

        if (currentInput.Length > 0)
        {
            currentInput = currentInput.Substring(0, currentInput.Length - 1);
            UpdateDisplay();
        }
    }

    public void ButtonPress_Enter()
    {
        if (isLocked) return;
        tryAgainText.SetActive(false);
        successText.SetActive(false);

        if (currentInput.Length > 0)
        {
            Debug.Log("Sending code from Keypad: " + currentInput);
            displayField.text = "";
            numLineText.SetActive(false);
            validatingText.SetActive(true);

            loginManager.OnSubmitCode(currentInput, HandleResponse);

            currentInput = "";
        }
    }

    private void HandleResponse(bool isSuccess, string message)
    {
        // 1. Instantly turn OFF the "Validating..." text
        validatingText.SetActive(false);

        if (!isSuccess)
        {
            // 2. If it failed, show the error in RED directly on the display screen!
            tryAgainText.SetActive(true);
            isLocked = false;
        }
        else
        {
            // 3. If it succeeded, show a success message right before the scene loads
            successText.SetActive(true);
        }
    }

    private void UpdateDisplay()
    {
        displayField.text = currentInput;
    }
}