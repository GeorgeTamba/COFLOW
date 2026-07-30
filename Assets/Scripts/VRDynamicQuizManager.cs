using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VRDynamicQuizManager : MonoBehaviour
{
    [Header("GameObject References")]
    public GameObject anxietyPanel;
    public GameObject resultPanel;
    public GameObject alertPanel;

    [Header("UI Text References")]
    public TMP_Text questionTextUI;
    public TMP_Text progressTextUI;
    public TMP_Text scoreTextUI;
    public TMP_Text anxietyStatusTextUI;
    public GameObject warningTextObj;

    [Header("Radio Button Toggles")]
    public Toggle[] optionToggles;
    public TMP_Text[] optionLabelsUI;

    [Header("Action Buttons")]
    public GameObject prevButton;
    public GameObject nextButton;
    public GameObject submitButton;

    [System.Serializable]
    public class QuizQuestion
    {
        public string questionText;
        public string[] options;
    }

    [Header("Your Questions")]
    public QuizQuestion[] questions;

    private int currentIndex = 0;
    private int[] savedAnswers;

    void Start()
    {
        savedAnswers = new int[questions.Length];
        for (int i = 0; i < savedAnswers.Length; i++) savedAnswers[i] = -1;

        if (warningTextObj != null) warningTextObj.SetActive(false);

        UpdatePanel();
    }

    // Attach to the "Next" Button OnClick()
    public void OnNextClick()
    {
        // 1. VALIDATION CHECK
        if (!SaveCurrentAnswer())
        {
            // If they didn't pick an option, show warning and STOP!
            warningTextObj.SetActive(true);
            return;
        }

        // 2. If successful, proceed to next question
        if (currentIndex < questions.Length - 1)
        {
            currentIndex++;
            UpdatePanel();
        }
    }

    // Attach to the "Prev" Button OnClick()
    public void OnPrevClick()
    {
        SaveCurrentAnswer();

        if (currentIndex > 0)
        {
            currentIndex--;
            UpdatePanel();
        }
    }

    // Attach to the "Submit" Button OnClick()
    public void OnSubmitClick()
    {
        // 1. VALIDATION CHECK FOR THE LAST QUESTION
        if (!SaveCurrentAnswer())
        {
            warningTextObj.SetActive(true);
            return;
        }

        // 2. Calculate the score
        int totalScore = 0;
        foreach (int answerIndex in savedAnswers)
        {
            if (answerIndex != -1)
            {
                totalScore += (answerIndex + 1);
            }
        }

        if (totalScore == 6)
        {
            anxietyStatusTextUI.text = "Tidak Cemas";
        }
        else if (totalScore >= 7 && totalScore <= 12)
        {
            anxietyStatusTextUI.text = "Cemas Ringan";
        }
        else if (totalScore >= 13 && totalScore <= 18)
        {
            anxietyStatusTextUI.text = "Cemas Sedang";
        }
        else if (totalScore >= 19 && totalScore <= 24)
        {
            anxietyStatusTextUI.text = "Cemas Berat";
        }
        else if (totalScore >= 25 && totalScore <= 30)
        {
            anxietyStatusTextUI.text = "Panik";
        }
        else
        {
            anxietyStatusTextUI.text = "N/A";
        }

        // 3. Drop it in the Backpack
        SessionDataStore.anxietyScore = totalScore;
        SessionDataStore.anxietyAnswers = new List<int>(savedAnswers);
        Debug.Log($"<color=green>ANXIETY TEST COMPLETE!</color> Total Score: {SessionDataStore.anxietyScore} saved to Backpack.");

        // 4. Disable UI 
        scoreTextUI.text = totalScore + " / " + questions.Length * 5;
        anxietyPanel.gameObject.SetActive(false);
        if (totalScore >= 19) alertPanel.gameObject.SetActive(true);
        resultPanel.gameObject.SetActive(true);

        foreach (Toggle t in optionToggles) t.gameObject.SetActive(false);
    }

    // --- INTERNAL HELPER FUNCTIONS ---

    private bool SaveCurrentAnswer()
    {
        for (int i = 0; i < optionToggles.Length; i++)
        {
            if (optionToggles[i].gameObject.activeSelf && optionToggles[i].isOn)
            {
                savedAnswers[currentIndex] = i;
                return true;
            }
        }
        return false;
    }

    private void UpdatePanel()
    {

        if (warningTextObj != null) warningTextObj.SetActive(false);

        progressTextUI.text = $"{currentIndex + 1} / {questions.Length}";
        questionTextUI.text = questions[currentIndex].questionText;

        for (int i = 0; i < optionToggles.Length; i++)
        {
            if (i < questions[currentIndex].options.Length)
            {
                optionToggles[i].gameObject.SetActive(true);
                optionLabelsUI[i].text = questions[currentIndex].options[i];
            }
            else
            {
                optionToggles[i].gameObject.SetActive(false);
            }
        }

        int previousAnswer = savedAnswers[currentIndex];
        for (int i = 0; i < optionToggles.Length; i++)
        {
            optionToggles[i].SetIsOnWithoutNotify(i == previousAnswer);
        }

        prevButton.SetActive(currentIndex > 0);

        if (currentIndex == questions.Length - 1)
        {
            nextButton.SetActive(false);
            submitButton.SetActive(true);
        }
        else
        {
            nextButton.SetActive(true);
            submitButton.SetActive(false);
        }
    }
}