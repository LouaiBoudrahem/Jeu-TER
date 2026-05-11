using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using UnityEngine.UI;

public class QuizController : MonoBehaviour
{
    public static QuestionData PendingQuestion;
    public static int CurrentScore { get; private set; }

    [Header("Question Data")]
    [SerializeField] private QuestionData questionData;

    [Header("Root UI")]
    [SerializeField] private GameObject multipleChoiceGroup;
    [SerializeField] private GameObject trueFalseGroup;
    [SerializeField] private GameObject shortAnswerGroup;

    [Header("Shared UI")]
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private TMP_Text hintText;

    [Header("Score UI")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private string scorePrefix = "Score: ";

    [Header("Short Answer UI")]
    [SerializeField] private TMP_InputField shortAnswerInput;
    [SerializeField] private Button shortAnswerSubmitButton;

    [Header("Close UI")]
    [SerializeField] private Button closeButton;

    [Header("Answer Hover Shadow")]
    [SerializeField] private bool autoSetupAnswerHoverShadow = true;
    [SerializeField, Range(0f, 1f)] private float answerTmpOutlineWidthOnHover = 0.25f;

    private Button[] multipleChoiceButtons;
    private Button[] trueFalseButtons;
    private bool rewardGrantedForCurrentQuestion;
    private UnityAction closeButtonAction;

    private void Awake()
    {
        if (questionData == null)
        {
            questionData = PendingQuestion;
        }

        if (questionData == null)
        {
            questionData = QuestionBankStorage.GetRandomQuestion();
        }

        CacheUIReferences();

        if (autoSetupAnswerHoverShadow)
        {
            SetupAnswerHoverShadows();
        }
    }

    private void OnEnable()
    {
        BindButtonEvents();
        SetupQuestion();
    }

    private void OnDisable()
    {
        UnbindButtonEvents();
    }

    public void SetQuestion(QuestionData data)
    {
        questionData = data;
        SetupQuestion();
    }

    public void SetCloseButtonAction(UnityAction action)
    {
        closeButtonAction = action;

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(closeButtonAction ?? CloseQuiz);
        }
    }

    public static void AddScore(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        CurrentScore += amount;
    }

    private void CacheUIReferences()
    {
        if (multipleChoiceGroup != null)
        {
            multipleChoiceButtons = multipleChoiceGroup.GetComponentsInChildren<Button>(true);
        }

        if (trueFalseGroup != null)
        {
            trueFalseButtons = trueFalseGroup.GetComponentsInChildren<Button>(true);
        }

        if (shortAnswerGroup != null)
        {
            if (shortAnswerInput == null)
            {
                shortAnswerInput = shortAnswerGroup.GetComponentInChildren<TMP_InputField>(true);
            }

            if (shortAnswerSubmitButton == null)
            {
                shortAnswerSubmitButton = shortAnswerGroup.GetComponentInChildren<Button>(true);
            }
        }
    }

    private void BindButtonEvents()
    {
        if (multipleChoiceButtons != null)
        {
            for (int i = 0; i < multipleChoiceButtons.Length; i++)
            {
                int optionIndex = i;
                multipleChoiceButtons[i].onClick.AddListener(() => SubmitMultipleChoice(optionIndex));
            }
        }

        if (trueFalseButtons != null)
        {
            if (trueFalseButtons.Length > 0)
            {
                trueFalseButtons[0].onClick.AddListener(() => SubmitTrueFalse(true));
            }

            if (trueFalseButtons.Length > 1)
            {
                trueFalseButtons[1].onClick.AddListener(() => SubmitTrueFalse(false));
            }
        }

        if (shortAnswerSubmitButton != null)
        {
            shortAnswerSubmitButton.onClick.AddListener(SubmitShortAnswer);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(closeButtonAction ?? CloseQuiz);
        }
    }

    private void UnbindButtonEvents()
    {
        if (multipleChoiceButtons != null)
        {
            foreach (Button button in multipleChoiceButtons)
            {
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                }
            }
        }

        if (trueFalseButtons != null)
        {
            foreach (Button button in trueFalseButtons)
            {
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                }
            }
        }

        if (shortAnswerSubmitButton != null)
        {
            shortAnswerSubmitButton.onClick.RemoveAllListeners();
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
        }
    }

    private void SetupQuestion()
    {
        if (questionData == null)
        {
            if (feedbackText != null)
            {
                feedbackText.text = "No question assigned.";
            }

            return;
        }

        if (questionText != null)
        {
            questionText.text = questionData.question;
        }

        if (hintText != null)
        {
            hintText.text = string.IsNullOrWhiteSpace(questionData.hint) ? string.Empty : questionData.hint;
        }

        if (feedbackText != null)
        {
            feedbackText.text = string.Empty;
        }

        rewardGrantedForCurrentQuestion = false;
        UpdateScoreText();

        if (multipleChoiceGroup != null)
        {
            multipleChoiceGroup.SetActive(questionData.questionType == QuestionData.QuestionType.MultipleChoice);
        }

        if (trueFalseGroup != null)
        {
            trueFalseGroup.SetActive(questionData.questionType == QuestionData.QuestionType.TrueFalse);
        }

        if (shortAnswerGroup != null)
        {
            shortAnswerGroup.SetActive(questionData.questionType == QuestionData.QuestionType.ShortAnswer);
        }

        SetupMultipleChoiceButtons();
        SetupTrueFalseButtons();

        if (shortAnswerInput != null)
        {
            shortAnswerInput.text = string.Empty;
        }

        if (questionData.questionType == QuestionData.QuestionType.MultipleChoice)
        {
            SetupMultipleChoiceButtons();
        }
        else if (questionData.questionType == QuestionData.QuestionType.TrueFalse)
        {
            SetupTrueFalseButtons();
        }
    }

    private void SetupMultipleChoiceButtons()
    {
        if (questionData == null || multipleChoiceButtons == null)
        {
            return;
        }

        string[] options = questionData.options ?? new string[0];

        for (int i = 0; i < multipleChoiceButtons.Length; i++)
        {
            Button button = multipleChoiceButtons[i];
            if (button == null)
            {
                continue;
            }

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = i < options.Length ? options[i] : $"Option {i + 1}";
            }
        }
    }

    private void SetupTrueFalseButtons()
    {
        if (trueFalseButtons == null || trueFalseButtons.Length < 2)
        {
            return;
        }

        TMP_Text trueLabel = trueFalseButtons[0] != null ? trueFalseButtons[0].GetComponentInChildren<TMP_Text>(true) : null;
        TMP_Text falseLabel = trueFalseButtons[1] != null ? trueFalseButtons[1].GetComponentInChildren<TMP_Text>(true) : null;

        if (trueLabel != null)
        {
            trueLabel.text = "True";
        }

        if (falseLabel != null)
        {
            falseLabel.text = "False";
        }
    }

    private void SetupAnswerHoverShadows()
    {
        SetupHoverShadowForButtons(multipleChoiceButtons);
        SetupHoverShadowForButtons(trueFalseButtons);

        if (closeButton != null)
        {
            SetupHoverShadowForButtons(new[] { closeButton });
        }
    }

    private void SetupHoverShadowForButtons(Button[] buttons)
    {
        if (buttons == null)
        {
            return;
        }

        foreach (Button button in buttons)
        {
            if (button == null)
            {
                continue;
            }

            QuizAnswerButtonHoverShadow hoverShadow = button.GetComponent<QuizAnswerButtonHoverShadow>();
            if (hoverShadow == null)
            {
                hoverShadow = button.gameObject.AddComponent<QuizAnswerButtonHoverShadow>();
            }

            hoverShadow.Configure(answerTmpOutlineWidthOnHover);
        }
    }

    private void SubmitMultipleChoice(int selectedIndex)
    {
        if (questionData == null || questionData.questionType != QuestionData.QuestionType.MultipleChoice)
        {
            return;
        }

        bool isCorrect = selectedIndex == questionData.correctOptionIndex;
        ShowResult(isCorrect);
    }

    private void SubmitTrueFalse(bool selectedValue)
    {
        if (questionData == null || questionData.questionType != QuestionData.QuestionType.TrueFalse)
        {
            return;
        }

        string normalizedAnswer = questionData.answer == null ? string.Empty : questionData.answer.Trim().ToLowerInvariant();
        bool correctValue = normalizedAnswer == "true";
        ShowResult(selectedValue == correctValue);
    }

    private void SubmitShortAnswer()
    {
        if (questionData == null || questionData.questionType != QuestionData.QuestionType.ShortAnswer)
        {
            return;
        }

        string playerAnswer = shortAnswerInput != null ? shortAnswerInput.text.Trim() : string.Empty;
        string correctAnswer = questionData.answer == null ? string.Empty : questionData.answer.Trim();

        bool isCorrect = string.Equals(playerAnswer, correctAnswer, System.StringComparison.OrdinalIgnoreCase);
        ShowResult(isCorrect);
    }

    private void ShowResult(bool isCorrect)
    {
        int awardedPoints = 0;

        if (isCorrect && !rewardGrantedForCurrentQuestion && questionData != null)
        {
            awardedPoints = Mathf.Max(0, questionData.scoreReward);
            CurrentScore += awardedPoints;
            rewardGrantedForCurrentQuestion = true;
        }

        if (feedbackText != null)
        {
            feedbackText.text = isCorrect
                ? (awardedPoints > 0 ? $"Correct! +{awardedPoints} points." : "Correct!")
                : "Incorrect. Try again.";
        }

        UpdateScoreText();
    }

    private void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = $"{scorePrefix}{CurrentScore}";
        }
    }

    private void CloseQuiz()
    {
        MinigameManager minigameManager = FindObjectOfType<MinigameManager>();
        if (minigameManager != null)
        {
            minigameManager.ExitMinigame();
            return;
        }

        Player player = FindObjectOfType<Player>();
        if (player != null)
        {
            player.EndComputerInteraction();
        }

        Scene minigameScene = gameObject.scene;
        if (minigameScene.IsValid() && minigameScene.isLoaded)
        {
            SceneManager.UnloadSceneAsync(minigameScene.name);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}