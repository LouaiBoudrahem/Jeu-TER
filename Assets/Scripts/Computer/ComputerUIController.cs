using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class ComputerUIController : MonoBehaviour
{
    private const string PythonChallengeDirectoryName = "CodeChallenges";
    private const string PythonChallengeFileName = "security_checksum_challenge.py";
    private const string PythonChallengeResultFileName = "security_checksum_result.txt";

    [Header("Data & Prefabs")]
    [SerializeField] private FileSystemController fileSystem;
    [SerializeField] private GameObject folderButtonPrefab;
    [SerializeField] private GameObject fileButtonPrefab;

    [Header("UI Refs")]
    [SerializeField] private GameObject computerRootPanel;
    [SerializeField] private Transform contentArea; 
    [SerializeField] private GameObject explorerPanel; 
    [SerializeField] private GameObject previewPanel; 
    [SerializeField] private TMP_Text previewText;
    [SerializeField] private TMP_Text successText;
    [SerializeField] private QuizController previewQuizController;
    [SerializeField] private Button explorerCloseButton;
    [SerializeField] private Button previewCloseButton;
    [SerializeField] private Button backButton;

    private FolderData currentFolder;
    private Stack<FolderData> history = new Stack<FolderData>();
    private QuestionData checksumValidationQuestion;
    private string activeChallengeResultFilePath;
    private bool waitingForChecksumResult;
    private bool checksumChallengeCompleted;

    private void Start()
    {
        if (explorerPanel != null)
            explorerPanel.SetActive(false);

        if (successText != null)
        {
            successText.text = string.Empty;
            successText.gameObject.SetActive(false);
        }

        if (previewQuizController == null && previewPanel != null)
            previewQuizController = previewPanel.GetComponentInChildren<QuizController>(true);

        HidePreview();

        if (explorerCloseButton != null)
            explorerCloseButton.onClick.AddListener(CloseExplorer);

        if (previewCloseButton != null)
            previewCloseButton.onClick.AddListener(ClosePreview);

        if (backButton != null)
            backButton.onClick.AddListener(OnBackPressed);
    }

    private void OnDestroy()
    {
        if (explorerCloseButton != null)
            explorerCloseButton.onClick.RemoveListener(CloseExplorer);

        if (previewCloseButton != null)
            previewCloseButton.onClick.RemoveListener(ClosePreview);

        if (backButton != null)
            backButton.onClick.RemoveListener(OnBackPressed);
    }

    private void OnDisable()
    {
        ClearContent();
        HidePreview();
        history.Clear();
        currentFolder = null;
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            HandleEscapePressed();
        }

        if (!waitingForChecksumResult || checksumChallengeCompleted)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(activeChallengeResultFilePath) || !File.Exists(activeChallengeResultFilePath))
        {
            return;
        }

        string printedValue = File.ReadAllText(activeChallengeResultFilePath).Trim();
        if (!ValidateSecurityChecksumOutput(printedValue))
        {
            return;
        }

        waitingForChecksumResult = false;
        checksumChallengeCompleted = true;
        QuizController.AddScore(100);
        UnityEngine.Debug.Log($"Security checksum validated: {printedValue}");

        if (successText != null)
        {
            successText.text = "Answer is correct +100";
            successText.gameObject.SetActive(true);
        }

        if (previewQuizController != null)
        {
            previewQuizController.gameObject.SetActive(false);
        }

        if (previewText != null)
        {
            previewText.gameObject.SetActive(true);
            previewText.text = "Checksum validated. You solved the Python challenge.";
        }

        Player player = FindObjectOfType<Player>();
        if (player != null)
        {
            player.ShowInteractionMessage("Checksum validated successfully.");
        }
    }

    private void HandleEscapePressed()
    {
        if (previewPanel != null && previewPanel.activeSelf)
        {
            ClosePreview();
            return;
        }

        HideComputerUIForExit();

        Player player = FindObjectOfType<Player>();
        if (player != null)
        {
            player.EndComputerInteraction();
        }

        MinigameManager minigameManager = FindObjectOfType<MinigameManager>();
        if (minigameManager != null)
        {
            minigameManager.ExitMinigame();
        }
    }

    public void OpenExplorer()
    {
        if (computerRootPanel != null)
            computerRootPanel.SetActive(true);

        if (explorerPanel != null)
            explorerPanel.SetActive(true);

        history.Clear();
        currentFolder = null;
        HidePreview();
        RenderCurrent();
    }

    public void CloseExplorer()
    {
        if (explorerPanel != null)
            explorerPanel.SetActive(false);

        if (successText != null)
        {
            successText.text = string.Empty;
            successText.gameObject.SetActive(false);
        }

        ClearContent();
        HidePreview();
        history.Clear();
        currentFolder = null;
    }

    public void HideComputerUIForExit()
    {
        if (computerRootPanel != null)
            computerRootPanel.SetActive(false);
        else
            SetCanvasParentsActive(false);

        if (explorerPanel != null)
            explorerPanel.SetActive(false);

        if (successText != null)
        {
            successText.text = string.Empty;
            successText.gameObject.SetActive(false);
        }

        ClearContent();
        HidePreview();
        history.Clear();
        currentFolder = null;
    }

    private void SetCanvasParentsActive(bool isActive)
    {
        SetParentCanvasActive(explorerPanel, isActive);
        SetParentCanvasActive(previewPanel, isActive);
        SetParentCanvasActive(previewText != null ? previewText.gameObject : null, isActive);
        SetParentCanvasActive(successText != null ? successText.gameObject : null, isActive);
        SetParentCanvasActive(previewQuizController != null ? previewQuizController.gameObject : null, isActive);
        SetParentCanvasActive(explorerCloseButton != null ? explorerCloseButton.gameObject : null, isActive);
        SetParentCanvasActive(previewCloseButton != null ? previewCloseButton.gameObject : null, isActive);
        SetParentCanvasActive(backButton != null ? backButton.gameObject : null, isActive);
    }

    private static void SetParentCanvasActive(GameObject source, bool isActive)
    {
        if (source == null)
        {
            return;
        }

        Canvas parentCanvas = source.GetComponentInParent<Canvas>(true);
        if (parentCanvas != null)
        {
            parentCanvas.gameObject.SetActive(isActive);
        }
    }

    private void RenderCurrent()
    {
        ClearContent();

        IEnumerable<FolderData> folders = currentFolder == null ? fileSystem.GetVisibleRootFolders() : fileSystem.GetVisibleSubfolders(currentFolder);
        IEnumerable<FileData> files = currentFolder == null ? fileSystem.GetVisibleRootFiles() : fileSystem.GetVisibleFiles(currentFolder);

        if (folders != null)
        {
            foreach (var f in folders)
            {
                GameObject go = Instantiate(folderButtonPrefab, contentArea);
                ExplorerEntryButton eb = go.GetComponent<ExplorerEntryButton>();
                eb.Setup(f, OnFolderClicked);
            }
        }

        if (files != null)
        {
            foreach (var fi in files)
            {
                GameObject go = Instantiate(fileButtonPrefab, contentArea);
                ExplorerEntryButton eb = go.GetComponent<ExplorerEntryButton>();
                eb.Setup(fi, OnFileClicked);
            }
        }

        if (backButton != null)
            backButton.interactable = history.Count > 0;
    }

    private void ClearContent()
    {
        if (contentArea == null) return;
        for (int i = contentArea.childCount - 1; i >= 0; i--)
        {
            Destroy(contentArea.GetChild(i).gameObject);
        }
    }

    private void OnFolderClicked(FolderData folder)
    {
        history.Push(currentFolder);
        currentFolder = folder;
        HidePreview();
        RenderCurrent();
    }

    private void OnFileClicked(FileData file)
    {
        if (previewPanel != null)
            previewPanel.SetActive(true);

        bool isQuizFile = file != null && file.quizQuestionIndex >= 0;
        if (previewText != null)
            previewText.gameObject.SetActive(!isQuizFile);

        if (previewQuizController != null)
            previewQuizController.gameObject.SetActive(isQuizFile);

        if (isQuizFile)
        {
            if (previewQuizController != null)
            {
                previewQuizController.SetCloseButtonAction(ClosePreview);
            }

            QuestionData question = QuestionBankStorage.GetQuestionByIndex(file.quizQuestionIndex);
            if (question != null && previewQuizController != null)
            {
                previewQuizController.SetQuestion(question);
                return;
            }

            if (previewText != null)
            {
                previewText.gameObject.SetActive(true);
                previewText.text = $"Quiz question index {file.quizQuestionIndex} was not found.";
            }

            if (previewQuizController != null)
                previewQuizController.gameObject.SetActive(false);

            return;
        }

        if (previewText != null)
            previewText.text = file.content != null ? file.content.text : "(empty)";
    }

    private void OnBackPressed()
    {
        if (history.Count == 0)
        {
            CloseExplorer();
            return;
        }

        currentFolder = history.Pop();
        HidePreview();
        RenderCurrent();
    }

    private void HidePreview()
    {
        if (previewPanel != null)
            previewPanel.SetActive(false);

        if (previewQuizController != null)
        {
            previewQuizController.SetCloseButtonAction(null);
            previewQuizController.gameObject.SetActive(false);
        }

        if (previewText != null)
            previewText.gameObject.SetActive(true);

        if (previewText != null)
            previewText.text = string.Empty;
    }

    public void ClosePreview()
    {
        HidePreview();
    }

    public void OpenVSCode()
    {
        try
        {
            string challengeFilePath = EnsureSecurityChecksumChallengeFile();
            activeChallengeResultFilePath = Path.Combine(Path.GetDirectoryName(challengeFilePath) ?? Application.persistentDataPath, PythonChallengeResultFileName);
            if (File.Exists(activeChallengeResultFilePath))
            {
                File.Delete(activeChallengeResultFilePath);
            }

            waitingForChecksumResult = true;
            checksumChallengeCompleted = false;
            UnityEngine.Debug.Log($"Security checksum challenge opened: {challengeFilePath}");

            Process.Start(new ProcessStartInfo
            {
                FileName = "code",
                Arguments = $"\"{challengeFilePath}\"",
                UseShellExecute = true
            });

            ShowChecksumValidationPrompt();
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError("Failed to open VS Code: " + e.Message);
        }
    }

    public bool ValidateSecurityChecksumOutput(string printedValue)
    {
        if (!int.TryParse(printedValue?.Trim(), out int parsedValue))
        {
            return false;
        }

        return parsedValue == CalculateSecurityChecksum();
    }

    private void ShowChecksumValidationPrompt()
    {
        if (previewPanel != null)
        {
            previewPanel.SetActive(true);
        }

        if (previewText != null)
        {
            previewText.gameObject.SetActive(false);
        }

        if (previewQuizController != null)
        {
            previewQuizController.gameObject.SetActive(true);
            previewQuizController.SetCloseButtonAction(ClosePreview);
            previewQuizController.SetQuestion(GetOrCreateChecksumValidationQuestion());
            return;
        }

        if (previewText != null)
        {
            previewText.gameObject.SetActive(true);
            previewText.text = "Run the Python file in VS Code, then enter the printed checksum here.";
        }
    }

    private QuestionData GetOrCreateChecksumValidationQuestion()
    {
        if (checksumValidationQuestion != null)
        {
            return checksumValidationQuestion;
        }

        checksumValidationQuestion = ScriptableObject.CreateInstance<QuestionData>();
        checksumValidationQuestion.name = "SecurityChecksumValidation";
        checksumValidationQuestion.questionType = QuestionData.QuestionType.ShortAnswer;
        checksumValidationQuestion.difficultyLevel = QuestionData.DifficultyLevel.Easy;
        checksumValidationQuestion.subject = QuestionData.Subject.Science;
        checksumValidationQuestion.question = "Run the Python file in VS Code, then enter the printed checksum.";
        checksumValidationQuestion.answer = CalculateSecurityChecksum().ToString();
        checksumValidationQuestion.scoreReward = 1;
        checksumValidationQuestion.hint = "Keep only numbers divisible by 3, double them, then add the results.";

        return checksumValidationQuestion;
    }

    private string EnsureSecurityChecksumChallengeFile()
    {
        string challengeDirectory = Path.Combine(Application.persistentDataPath, PythonChallengeDirectoryName);
        if (!Directory.Exists(challengeDirectory))
        {
            Directory.CreateDirectory(challengeDirectory);
        }

        string challengeFilePath = Path.Combine(challengeDirectory, PythonChallengeFileName);
        if (!File.Exists(challengeFilePath))
        {
            File.WriteAllText(challengeFilePath, BuildSecurityChecksumChallenge());
        }

        return challengeFilePath;
    }

    private int CalculateSecurityChecksum()
    {
        int[] data = { 14, 3, 9, 27, 18, 6 };
        int checksum = 0;

        foreach (int value in data)
        {
            if (value % 3 == 0)
            {
                checksum += value * 2;
            }
        }

        return checksum;
    }

    private string BuildSecurityChecksumChallenge()
    {
        return @"data = [14, 3, 9, 27, 18, 6]

import builtins
from pathlib import Path

_result_file = Path(__file__).with_name('security_checksum_result.txt')
_original_print = builtins.print

def print(*args, **kwargs):
    _original_print(*args, **kwargs)
    _result_file.write_text(' '.join(str(arg) for arg in args), encoding='utf-8')

# SECURITY CHECKSUM ALGORITHM
# Step 1:
# Keep only numbers divisible by 3
#
# Step 2:
# Multiply each remaining number by 2
#
# Step 3:
# Add everything together
#
# Print final checksum below

# Write your code here
";
    }
}
