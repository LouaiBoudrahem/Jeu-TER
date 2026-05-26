using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class ExamPaperMinigameController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject rootPanel;
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private Button validateButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button[] answerButtons = new Button[4];
    [SerializeField] private TMP_Text[] answerTexts = new TMP_Text[4];

    [Header("Timing")]
    [SerializeField, Min(0f)] private float feedbackDelay = 1f;
    [SerializeField, Min(0f)] private float finishCloseDelay = 1.5f;
    [Header("Modes")]
    [SerializeField] private bool showAllAtOnce = false;

    private QuestionData[] questions;
    private Action onClosed;
    private int currentQuestionIndex;
    private int selectedOptionIndex = -1;
    private int correctAnswerCount;
    private int totalAwardedScore;
    private bool isOpen;
    private bool isResolving;
    private Coroutine advanceRoutine;

    // Used for all-at-once mode
    private class PanelInfo
    {
        public Transform root;
        public TMP_Text questionText;
        public Button[] answerButtons;
        public int selectedIndex = -1;
        public QuestionData question;
    }

    private PanelInfo[] panels;

    private void Awake()
    {
        if (rootPanel == null)
        {
            rootPanel = gameObject;
        }

        rootPanel.SetActive(false);
        EnsureEventSystemExists();
        EnsureCanvasCanReceiveClicks();
        EnsureCanvasOnTopAndInteractive();
        BindButtons();
    }

    private void OnEnable()
    {
        BindButtons();
    }

    private void OnDisable()
    {
        UnbindButtons();

        if (advanceRoutine != null)
        {
            StopCoroutine(advanceRoutine);
            advanceRoutine = null;
        }

        isOpen = false;
        isResolving = false;
    }

    private void Update()
    {
        if (!isOpen)
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseExamPaper();
        }
    }

    public void Begin(QuestionData[] questionSet, Action closedCallback)
    {
        questions = NormalizeQuestions(questionSet);
        onClosed = closedCallback;
        currentQuestionIndex = 0;
        selectedOptionIndex = -1;
        correctAnswerCount = 0;
        totalAwardedScore = 0;
        isResolving = false;
        isOpen = true;

        EnsureEventSystemExists();
        EnsureCanvasCanReceiveClicks();
        EnsureCanvasOnTopAndInteractive();

        rootPanel.SetActive(true);
        if (rootCanvas != null)
        {
            rootCanvas.enabled = true;
        }

        if (EventSystem.current != null && validateButton != null)
        {
            EventSystem.current.SetSelectedGameObject(validateButton.gameObject);
        }

        if (showAllAtOnce && questions.Length > 1)
        {
            SetupAllAtOnceMode();
        }
        else
        {
            ShowQuestion();
        }
    }

    private void SetupAllAtOnceMode()
    {
        panels = FindQuestionPanels();

        if (panels == null || panels.Length == 0)
        {
            // No panels to populate — fallback to sequential
            ShowQuestion();
            return;
        }

        int count = Math.Min(panels.Length, questions.Length);
        for (int i = 0; i < panels.Length; i++)
        {
            PanelInfo p = panels[i];
            if (i < count)
            {
                QuestionData q = questions[i];
                p.question = q;
                p.selectedIndex = -1;
                if (p.questionText != null)
                    p.questionText.text = q.question;

                // Populate options
                string[] opts = q.options ?? Array.Empty<string>();
                for (int b = 0; b < p.answerButtons.Length; b++)
                {
                    Button btn = p.answerButtons[b];
                    if (btn == null) continue;
                    TMP_Text bt = btn.GetComponentInChildren<TMP_Text>(true);
                    string label = b < opts.Length ? opts[b] : string.Empty;
                    if (bt != null) bt.text = label;
                    btn.interactable = !string.IsNullOrWhiteSpace(label);

                    int panelIndex = i;
                    int optionIndex = b;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnPanelOptionSelected(panelIndex, optionIndex));
                }

                p.root.gameObject.SetActive(true);
            }
            else
            {
                p.root.gameObject.SetActive(false);
            }
        }

        // Rebind validate to the multi-validate handler
        if (validateButton != null)
        {
            validateButton.onClick.RemoveAllListeners();
            validateButton.onClick.AddListener(ValidateAllSelection);
        }

        // Ensure feedback is clear
        SetFeedback(string.Empty, false);
    }

    private PanelInfo[] FindQuestionPanels()
    {
        if (rootPanel == null)
            return Array.Empty<PanelInfo>();

        List<PanelInfo> found = new List<PanelInfo>();
        for (int i = 0; i < rootPanel.transform.childCount; i++)
        {
            Transform child = rootPanel.transform.GetChild(i);
            if (child == null) continue;

            TMP_Text qText = child.GetComponentInChildren<TMP_Text>(true);
            Button[] buttons = child.GetComponentsInChildren<Button>(true);
            if (qText != null && buttons != null && buttons.Length > 0)
            {
                PanelInfo p = new PanelInfo { root = child, questionText = qText, answerButtons = buttons };
                found.Add(p);
            }
        }

        return found.ToArray();
    }

    private void OnPanelOptionSelected(int panelIndex, int optionIndex)
    {
        if (panels == null || panelIndex < 0 || panelIndex >= panels.Length)
            return;

        PanelInfo p = panels[panelIndex];
        p.selectedIndex = optionIndex;
        // give small feedback in the panel (e.g., highlight) — use feedbackText for global
        SetFeedback($"Selected for Q{panelIndex + 1}: option {optionIndex + 1}", false);
    }

    private void ValidateAllSelection()
    {
        if (panels == null || panels.Length == 0)
            return;

        isResolving = true;
        int correct = 0;
        int awarded = 0;

        int count = Math.Min(panels.Length, questions.Length);
        for (int i = 0; i < count; i++)
        {
            PanelInfo p = panels[i];
            QuestionData q = p.question;
            int sel = p.selectedIndex;
            if (sel >= 0 && sel == q.correctOptionIndex)
            {
                correct++;
                int pts = Mathf.Max(0, q.scoreReward);
                if (pts > 0)
                {
                    QuizController.AddScore(pts);
                    awarded += pts;
                }
            }

            // disable buttons for this panel
            for (int b = 0; b < p.answerButtons.Length; b++)
            {
                if (p.answerButtons[b] != null)
                    p.answerButtons[b].interactable = false;
            }
        }

        correctAnswerCount = correct;
        totalAwardedScore = awarded;
        SetFeedback($"Exam complete. {correct}/{count} correct. +{awarded} points.", true);
        StartCloseAfterDelay(finishCloseDelay);
    }

    private void BindButtons()
    {
        if (validateButton != null)
        {
            validateButton.onClick.RemoveAllListeners();
            validateButton.onClick.AddListener(ValidateSelection);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseExamPaper);
            closeButton.onClick.AddListener(CloseExamPaper);
        }

        for (int i = 0; i < answerButtons.Length; i++)
        {
            Button button = answerButtons[i];
            if (button == null)
            {
                continue;
            }

            int optionIndex = i;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SelectOption(optionIndex));
        }
    }

    private void UnbindButtons()
    {
        if (validateButton != null)
        {
            validateButton.onClick.RemoveAllListeners();
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
        }

        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (answerButtons[i] != null)
            {
                answerButtons[i].onClick.RemoveAllListeners();
            }
        }
    }

    private QuestionData[] NormalizeQuestions(QuestionData[] sourceQuestions)
    {
        if (sourceQuestions == null || sourceQuestions.Length == 0)
        {
            return Array.Empty<QuestionData>();
        }

        QuestionData[] filtered = new QuestionData[sourceQuestions.Length];
        int count = 0;

        for (int i = 0; i < sourceQuestions.Length; i++)
        {
            if (sourceQuestions[i] == null)
            {
                continue;
            }

            filtered[count++] = sourceQuestions[i];
        }

        if (count == filtered.Length)
        {
            return filtered;
        }

        QuestionData[] resized = new QuestionData[count];
        Array.Copy(filtered, resized, count);
        return resized;
    }

    private void ShowQuestion()
    {
        // Sequential mode: ensure the main question panel remains active

        if (questions == null || questions.Length == 0)
        {
            SetFeedback("No exam questions assigned.", false);
            SetAnswerButtonsInteractable(false);
            if (validateButton != null)
            {
                validateButton.interactable = false;
            }

            StartCloseAfterDelay(1.5f);
            return;
        }

        if (currentQuestionIndex >= questions.Length)
        {
            FinishExam();
            return;
        }

        QuestionData currentQuestion = questions[currentQuestionIndex];
        selectedOptionIndex = -1;
        isResolving = false;

        if (questionText != null)
        {
            questionText.text = currentQuestion.question;
        }

        if (progressText != null)
        {
            progressText.text = $"Question {currentQuestionIndex + 1}/{questions.Length}";
        }

        SetFeedback(string.Empty, false);
        PopulateAnswerButtons(currentQuestion);

        if (validateButton != null)
        {
            validateButton.interactable = false;
        }
    }

    private void PopulateAnswerButtons(QuestionData question)
    {
        string[] options = question != null && question.options != null ? question.options : Array.Empty<string>();

        for (int i = 0; i < answerButtons.Length; i++)
        {
            Button button = answerButtons[i];
            if (button == null)
            {
                continue;
            }

            TMP_Text buttonText = i < answerTexts.Length ? answerTexts[i] : null;
            if (buttonText == null)
            {
                buttonText = button.GetComponentInChildren<TMP_Text>(true);
            }

            string optionLabel = i < options.Length ? options[i] : string.Empty;

            if (buttonText != null)
            {
                buttonText.text = optionLabel;
            }

            button.interactable = !string.IsNullOrWhiteSpace(optionLabel);
        }
    }

    private void SelectOption(int optionIndex)
    {
        if (!isOpen || isResolving)
        {
            return;
        }

        selectedOptionIndex = optionIndex;

        if (validateButton != null)
        {
            validateButton.interactable = true;
        }

        SetFeedback($"Selected answer {optionIndex + 1}.", false);
    }

    private void ValidateSelection()
    {
        if (!isOpen || isResolving)
        {
            return;
        }

        if (selectedOptionIndex < 0)
        {
            SetFeedback("Select an answer first.", false);
            return;
        }

        if (questions == null || currentQuestionIndex < 0 || currentQuestionIndex >= questions.Length)
        {
            return;
        }

        QuestionData currentQuestion = questions[currentQuestionIndex];
        bool isCorrect = selectedOptionIndex == currentQuestion.correctOptionIndex;

        isResolving = true;
        SetAnswerButtonsInteractable(false);

        if (validateButton != null)
        {
            validateButton.interactable = false;
        }

        if (isCorrect)
        {
            correctAnswerCount++;

            int awardedPoints = Mathf.Max(0, currentQuestion.scoreReward);
            if (awardedPoints > 0)
            {
                QuizController.AddScore(awardedPoints);
                totalAwardedScore += awardedPoints;
            }

            SetFeedback(awardedPoints > 0 ? $"Correct! +{awardedPoints} points." : "Correct!", true);
        }
        else
        {
            SetFeedback("Incorrect.", false);
        }

        StartAdvanceAfterDelay();
    }

    private void StartAdvanceAfterDelay()
    {
        if (advanceRoutine != null)
        {
            StopCoroutine(advanceRoutine);
        }

        advanceRoutine = StartCoroutine(AdvanceAfterDelayRoutine());
    }

    private IEnumerator AdvanceAfterDelayRoutine()
    {
        if (feedbackDelay > 0f)
        {
            yield return new WaitForSeconds(feedbackDelay);
        }

        currentQuestionIndex++;
        isResolving = false;
        advanceRoutine = null;
        ShowQuestion();
    }

    private void FinishExam()
    {
        SetAnswerButtonsInteractable(false);

        if (validateButton != null)
        {
            validateButton.interactable = false;
        }

        SetFeedback($"Exam complete. {correctAnswerCount}/{questions.Length} correct. +{totalAwardedScore} points.", true);
        StartCloseAfterDelay(finishCloseDelay);
    }

    private void StartCloseAfterDelay(float delay)
    {
        if (advanceRoutine != null)
        {
            StopCoroutine(advanceRoutine);
            advanceRoutine = null;
        }

        advanceRoutine = StartCoroutine(CloseAfterDelayRoutine(delay));
    }

    private IEnumerator CloseAfterDelayRoutine(float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        advanceRoutine = null;
        CloseExamPaper();
    }

    public void CloseExamPaper()
    {
        if (!isOpen)
        {
            return;
        }

        isOpen = false;
        isResolving = false;

        if (advanceRoutine != null)
        {
            StopCoroutine(advanceRoutine);
            advanceRoutine = null;
        }

        rootPanel.SetActive(false);
        if (rootCanvas != null)
        {
            rootCanvas.enabled = false;
        }

        onClosed?.Invoke();
        onClosed = null;
    }

    private void SetAnswerButtonsInteractable(bool interactable)
    {
        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (answerButtons[i] != null)
            {
                answerButtons[i].interactable = interactable;
            }
        }
    }

    private void SetFeedback(string message, bool isSuccess)
    {
        if (feedbackText == null)
        {
            return;
        }

        feedbackText.text = message;
        feedbackText.color = isSuccess ? new Color(0.35f, 0.95f, 0.55f) : Color.white;
    }

    private static void EnsureEventSystemExists()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem (Auto)");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
    }

    private void EnsureCanvasCanReceiveClicks()
    {
        if (rootCanvas == null)
        {
            return;
        }

        if (rootCanvas.GetComponent<GraphicRaycaster>() == null)
        {
            rootCanvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        if (rootCanvas.renderMode != RenderMode.WorldSpace)
        {
            return;
        }

        Camera fallbackCamera = rootCanvas.worldCamera;
        if (fallbackCamera == null || !fallbackCamera.isActiveAndEnabled)
        {
            fallbackCamera = Camera.main;
        }

        if (fallbackCamera == null || !fallbackCamera.isActiveAndEnabled)
        {
            Camera[] allCameras = Camera.allCameras;
            for (int i = 0; i < allCameras.Length; i++)
            {
                Camera currentCamera = allCameras[i];
                if (currentCamera != null && currentCamera.enabled && currentCamera.gameObject.activeInHierarchy)
                {
                    fallbackCamera = currentCamera;
                    break;
                }
            }
        }

        if (fallbackCamera != null)
        {
            rootCanvas.worldCamera = fallbackCamera;
        }
    }

    private void EnsureCanvasOnTopAndInteractive()
    {
        if (rootCanvas == null)
        {
            return;
        }

        CanvasGroup canvasGroup = rootCanvas.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = rootCanvas.gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        rootCanvas.overrideSorting = true;
        rootCanvas.sortingOrder = 6000;

        if (rootCanvas.GetComponent<GraphicRaycaster>() == null)
        {
            rootCanvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        Canvas.ForceUpdateCanvases();
    }
}