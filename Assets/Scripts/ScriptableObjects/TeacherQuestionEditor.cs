using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TeacherQuestionEditor : MonoBehaviour
{
    [Header("List")]
    [SerializeField] private Transform questionListContainer;
    [SerializeField] private TeacherQuestionListItem questionListItemPrefab;

    [Header("Question Fields")]
    [SerializeField] private TMP_InputField questionInput;
    [SerializeField] private TMP_Dropdown questionTypeDropdown;
    [SerializeField] private TMP_Dropdown difficultyDropdown;
    [SerializeField] private TMP_Dropdown subjectDropdown;
    [SerializeField] private TMP_InputField scoreInput;
    [SerializeField] private TMP_InputField hintInput;

    [Header("Multiple Choice Fields")]
    [SerializeField] private TMP_InputField option1Input;
    [SerializeField] private TMP_InputField option2Input;
    [SerializeField] private TMP_InputField option3Input;
    [SerializeField] private TMP_InputField option4Input;
    [SerializeField] private TMP_Dropdown correctOptionDropdown;

    [Header("Answer Fields")]
    [SerializeField] private TMP_InputField answerInput;

    [Header("Buttons")]
    [SerializeField] private Button newButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private Button refreshButton;

    [Header("Status")]
    [SerializeField] private TMP_Text statusText;

    private readonly List<QuestionRecord> cachedRecords = new List<QuestionRecord>();
    private QuestionRecord selectedRecord;

    private void Awake()
    {
        SetupDropdowns();
        BindButtons();
    }

    private void Start()
    {
        RefreshList();
        ClearForm();
    }

    private void OnDestroy()
    {
        UnbindButtons();
    }

    private void SetupDropdowns()
    {
        if (questionTypeDropdown != null && questionTypeDropdown.options.Count == 0)
        {
            questionTypeDropdown.ClearOptions();
            questionTypeDropdown.AddOptions(new List<string>
            {
                QuestionData.QuestionType.MultipleChoice.ToString(),
                QuestionData.QuestionType.TrueFalse.ToString(),
                QuestionData.QuestionType.ShortAnswer.ToString()
            });
        }

        if (difficultyDropdown != null && difficultyDropdown.options.Count == 0)
        {
            difficultyDropdown.ClearOptions();
            difficultyDropdown.AddOptions(new List<string>
            {
                QuestionData.DifficultyLevel.Easy.ToString(),
                QuestionData.DifficultyLevel.Medium.ToString(),
                QuestionData.DifficultyLevel.Hard.ToString()
            });
        }

        if (subjectDropdown != null && subjectDropdown.options.Count == 0)
        {
            subjectDropdown.ClearOptions();
            subjectDropdown.AddOptions(new List<string>
            {
                QuestionData.Subject.Math.ToString(),
                QuestionData.Subject.Science.ToString(),
                QuestionData.Subject.History.ToString(),
                QuestionData.Subject.Literature.ToString(),
                QuestionData.Subject.Geography.ToString()
            });
        }

        if (correctOptionDropdown != null && correctOptionDropdown.options.Count == 0)
        {
            correctOptionDropdown.ClearOptions();
            correctOptionDropdown.AddOptions(new List<string>
            {
                "Option 1",
                "Option 2",
                "Option 3",
                "Option 4"
            });
        }

        ApplyQuestionTypeVisibility();
    }

    private void BindButtons()
    {
        if (newButton != null)
        {
            newButton.onClick.AddListener(CreateNewQuestion);
        }

        if (saveButton != null)
        {
            saveButton.onClick.AddListener(SaveQuestion);
        }

        if (deleteButton != null)
        {
            deleteButton.onClick.AddListener(DeleteSelectedQuestion);
        }

        if (refreshButton != null)
        {
            refreshButton.onClick.AddListener(RefreshList);
        }

        if (questionTypeDropdown != null)
        {
            questionTypeDropdown.onValueChanged.AddListener(OnQuestionTypeChanged);
        }
    }

    private void UnbindButtons()
    {
        if (newButton != null)
        {
            newButton.onClick.RemoveAllListeners();
        }

        if (saveButton != null)
        {
            saveButton.onClick.RemoveAllListeners();
        }

        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
        }

        if (refreshButton != null)
        {
            refreshButton.onClick.RemoveAllListeners();
        }

        if (questionTypeDropdown != null)
        {
            questionTypeDropdown.onValueChanged.RemoveAllListeners();
        }
    }

    private void OnQuestionTypeChanged(int _)
    {
        ApplyQuestionTypeVisibility();
    }

    private void ApplyQuestionTypeVisibility()
    {
        bool multipleChoiceSelected = GetSelectedQuestionType() == QuestionData.QuestionType.MultipleChoice;

        if (option1Input != null) option1Input.gameObject.SetActive(multipleChoiceSelected);
        if (option2Input != null) option2Input.gameObject.SetActive(multipleChoiceSelected);
        if (option3Input != null) option3Input.gameObject.SetActive(multipleChoiceSelected);
        if (option4Input != null) option4Input.gameObject.SetActive(multipleChoiceSelected);
        if (correctOptionDropdown != null) correctOptionDropdown.gameObject.SetActive(multipleChoiceSelected);

        bool shortAnswerSelected = GetSelectedQuestionType() == QuestionData.QuestionType.ShortAnswer;
        if (answerInput != null)
        {
            answerInput.gameObject.SetActive(shortAnswerSelected || GetSelectedQuestionType() == QuestionData.QuestionType.TrueFalse);
        }
    }

    public void RefreshList()
    {
        cachedRecords.Clear();

        QuestionBank bank = QuestionBankStorage.Load();
        if (bank != null && bank.questions != null)
        {
            foreach (QuestionRecord record in bank.questions)
            {
                if (record != null)
                {
                    cachedRecords.Add(record);
                }
            }
        }

        if (questionListContainer == null || questionListItemPrefab == null)
        {
            UpdateStatus($"Loaded {cachedRecords.Count} question(s).\nAssign list references to show the editor list.");
            return;
        }

        for (int i = questionListContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(questionListContainer.GetChild(i).gameObject);
        }

        foreach (QuestionRecord record in cachedRecords)
        {
            TeacherQuestionListItem item = Instantiate(questionListItemPrefab, questionListContainer);
            item.Bind(record, SelectQuestion);
        }

        UpdateStatus($"Loaded {cachedRecords.Count} question(s).\nSelect one to edit or create a new one.");
    }

    public void CreateNewQuestion()
    {
        selectedRecord = null;
        ClearForm();
        UpdateStatus("Creating a new question.");
    }

    public void SelectQuestion(QuestionRecord record)
    {
        if (record == null)
        {
            return;
        }

        selectedRecord = record;
        PopulateForm(record);
        UpdateStatus("Question loaded for editing.");
    }

    public void SaveQuestion()
    {
        QuestionRecord record = ReadForm();
        if (record == null)
        {
            UpdateStatus("Could not save question. Check the form.");
            return;
        }

        if (selectedRecord != null && !string.IsNullOrWhiteSpace(selectedRecord.id))
        {
            record.id = selectedRecord.id;
        }
        else if (string.IsNullOrWhiteSpace(record.id))
        {
            record.id = Guid.NewGuid().ToString("N");
        }

        QuestionBankStorage.AddOrUpdate(record);
        selectedRecord = record;
        RefreshList();
        UpdateStatus("Question saved.");
    }

    public void DeleteSelectedQuestion()
    {
        if (selectedRecord == null || string.IsNullOrWhiteSpace(selectedRecord.id))
        {
            UpdateStatus("Select a question first.");
            return;
        }

        if (QuestionBankStorage.Remove(selectedRecord.id))
        {
            selectedRecord = null;
            ClearForm();
            RefreshList();
            UpdateStatus("Question deleted.");
        }
        else
        {
            UpdateStatus("Question could not be deleted.");
        }
    }

    private QuestionRecord ReadForm()
    {
        string questionText = questionInput != null ? questionInput.text.Trim() : string.Empty;
        if (string.IsNullOrWhiteSpace(questionText))
        {
            return null;
        }

        QuestionRecord record = new QuestionRecord
        {
            id = selectedRecord != null ? selectedRecord.id : Guid.NewGuid().ToString("N"),
            questionType = GetSelectedQuestionType(),
            difficultyLevel = GetSelectedDifficulty(),
            subject = GetSelectedSubject(),
            question = questionText,
            scoreReward = GetScoreInputValue(),
            hint = hintInput != null ? hintInput.text.Trim() : string.Empty
        };

        if (record.questionType == QuestionData.QuestionType.MultipleChoice)
        {
            record.options = new[]
            {
                option1Input != null ? option1Input.text.Trim() : string.Empty,
                option2Input != null ? option2Input.text.Trim() : string.Empty,
                option3Input != null ? option3Input.text.Trim() : string.Empty,
                option4Input != null ? option4Input.text.Trim() : string.Empty
            };
            record.correctOptionIndex = correctOptionDropdown != null ? correctOptionDropdown.value : 0;
            record.answer = string.Empty;
        }
        else if (record.questionType == QuestionData.QuestionType.TrueFalse)
        {
            record.answer = answerInput != null ? answerInput.text.Trim() : string.Empty;
            record.options = new[] { "True", "False" };
            record.correctOptionIndex = 0;
        }
        else
        {
            record.answer = answerInput != null ? answerInput.text.Trim() : string.Empty;
            record.options = Array.Empty<string>();
            record.correctOptionIndex = 0;
        }

        if (record.questionType == QuestionData.QuestionType.TrueFalse)
        {
            string normalized = record.answer.ToLowerInvariant();
            if (normalized != "true" && normalized != "false")
            {
                return null;
            }
        }

        return record;
    }

    private void PopulateForm(QuestionRecord record)
    {
        if (record == null)
        {
            return;
        }

        SetDropdownValue(questionTypeDropdown, (int)record.questionType);
        SetDropdownValue(difficultyDropdown, (int)record.difficultyLevel);
        SetDropdownValue(subjectDropdown, (int)record.subject);

        if (questionInput != null) questionInput.text = record.question ?? string.Empty;
        if (scoreInput != null) scoreInput.text = Mathf.Max(0, record.scoreReward).ToString();
        if (hintInput != null) hintInput.text = record.hint ?? string.Empty;

        if (record.questionType == QuestionData.QuestionType.MultipleChoice)
        {
            if (option1Input != null) option1Input.text = GetOption(record, 0);
            if (option2Input != null) option2Input.text = GetOption(record, 1);
            if (option3Input != null) option3Input.text = GetOption(record, 2);
            if (option4Input != null) option4Input.text = GetOption(record, 3);
            SetDropdownValue(correctOptionDropdown, Mathf.Clamp(record.correctOptionIndex, 0, 3));
            if (answerInput != null) answerInput.text = string.Empty;
        }
        else
        {
            if (answerInput != null) answerInput.text = record.answer ?? string.Empty;
        }

        ApplyQuestionTypeVisibility();
    }

    private void ClearForm()
    {
        if (questionInput != null) questionInput.text = string.Empty;
        if (scoreInput != null) scoreInput.text = "1";
        if (hintInput != null) hintInput.text = string.Empty;
        if (option1Input != null) option1Input.text = string.Empty;
        if (option2Input != null) option2Input.text = string.Empty;
        if (option3Input != null) option3Input.text = string.Empty;
        if (option4Input != null) option4Input.text = string.Empty;
        if (answerInput != null) answerInput.text = string.Empty;

        SetDropdownValue(questionTypeDropdown, (int)QuestionData.QuestionType.MultipleChoice);
        SetDropdownValue(difficultyDropdown, (int)QuestionData.DifficultyLevel.Easy);
        SetDropdownValue(subjectDropdown, (int)QuestionData.Subject.Math);
        SetDropdownValue(correctOptionDropdown, 0);

        ApplyQuestionTypeVisibility();
    }

    private QuestionData.QuestionType GetSelectedQuestionType()
    {
        return questionTypeDropdown != null
            ? (QuestionData.QuestionType)questionTypeDropdown.value
            : QuestionData.QuestionType.MultipleChoice;
    }

    private QuestionData.DifficultyLevel GetSelectedDifficulty()
    {
        return difficultyDropdown != null
            ? (QuestionData.DifficultyLevel)difficultyDropdown.value
            : QuestionData.DifficultyLevel.Easy;
    }

    private QuestionData.Subject GetSelectedSubject()
    {
        return subjectDropdown != null
            ? (QuestionData.Subject)subjectDropdown.value
            : QuestionData.Subject.Math;
    }

    private void SetDropdownValue(TMP_Dropdown dropdown, int value)
    {
        if (dropdown == null || dropdown.options.Count == 0)
        {
            return;
        }

        dropdown.value = Mathf.Clamp(value, 0, dropdown.options.Count - 1);
        dropdown.RefreshShownValue();
    }

    private string GetOption(QuestionRecord record, int index)
    {
        if (record == null || record.options == null || index < 0 || index >= record.options.Length)
        {
            return string.Empty;
        }

        return record.options[index] ?? string.Empty;
    }

    private int GetScoreInputValue()
    {
        if (scoreInput == null)
        {
            return 1;
        }

        if (int.TryParse(scoreInput.text, out int parsedScore))
        {
            return Mathf.Max(0, parsedScore);
        }

        return 1;
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        else
        {
            Debug.Log(message);
        }
    }
}