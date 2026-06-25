using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VREducationalQuizManager : MonoBehaviour
{
    [Header("UI Text References")]
    public TMP_Text questionTextUI;
    public TMP_Text progressTextUI;
    public GameObject warningTextObj;
    public TMP_Text completeText;

    [Header("Radio Button Toggles")]
    public GameObject choicesContainer;
    public Toggle[] optionToggles;
    public TMP_Text[] optionLabelsUI;

    [Header("Action Buttons")]
    public GameObject prevButton;
    public GameObject nextButton;
    public GameObject submitButton;
    public GameObject finishButton;

    // --- UPGRADE: Added correctAnswerIndex to grade the quiz! ---
    [System.Serializable]
    public class EducationalQuestion
    {
        public string questionText;
        public string[] options;
        [Tooltip("The index of the correct option (0 for first, 1 for second, etc.)")]
        public int correctAnswerIndex;
    }

    [Header("Your Quiz Questions")]
    public EducationalQuestion[] questions;

    private int currentIndex = 0;
    private int[] savedAnswers;

    void Start()
    {
        savedAnswers = new int[questions.Length];
        for (int i = 0; i < savedAnswers.Length; i++) savedAnswers[i] = -1;

        if (warningTextObj != null) warningTextObj.SetActive(false);

        UpdatePanel();
    }

    public void OnNextClick()
    {
        if (!SaveCurrentAnswer())
        {
            warningTextObj.SetActive(true);
            return;
        }

        if (currentIndex < questions.Length - 1)
        {
            currentIndex++;
            UpdatePanel();
        }
    }

    public void OnPrevClick()
    {
        SaveCurrentAnswer();
        if (currentIndex > 0)
        {
            currentIndex--;
            UpdatePanel();
        }
    }

    public void OnSubmitClick()
    {
        if (!SaveCurrentAnswer())
        {
            warningTextObj.SetActive(true);
            return;
        }

        // --- UPGRADE: The Grading Logic ---
        int totalCorrect = 0;
        for (int i = 0; i < questions.Length; i++)
        {
            // Check if what they clicked matches the correct answer key
            if (savedAnswers[i] == questions[i].correctAnswerIndex)
            {
                totalCorrect++;
            }
        }

        int finalScore = totalCorrect;

        // REROUTED: Drop it into the quizScore slot in the Backpack!
        SessionDataStore.quizScore = finalScore;
        SessionDataStore.quizAnswers = new List<int>(savedAnswers);

        Debug.Log($"<color=green>QUIZ GRADED!</color> They got {totalCorrect}/{questions.Length} correct. Final Scaled Score: {SessionDataStore.quizScore}/10 saved to Backpack.");

        //UI Actions 
        completeText.text = "Tes Penilaian Slesai";
        finishButton.SetActive(true);
        completeText.gameObject.SetActive(true);
        questionTextUI.gameObject.SetActive(false);
        submitButton.SetActive(false);
        prevButton.SetActive(false);
        warningTextObj.SetActive(false);

        choicesContainer.SetActive(false);
        foreach (Toggle t in optionToggles) t.gameObject.SetActive(false);
    }

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

        progressTextUI.text = $"Question {currentIndex + 1} / {questions.Length}";
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
                optionToggles[i].transform.parent.gameObject.SetActive(false);
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