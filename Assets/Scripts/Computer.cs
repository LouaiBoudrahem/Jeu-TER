using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class Computer : MonoBehaviour, IInteractable
{
    [SerializeField] private InteractionData interactionData;
    [SerializeField] private string minigameSceneName;
    [SerializeField] private GameObject computerVirtualCamera;

    [Header("Access Code Mode")]
    [SerializeField] private bool useAccessCodeMode = false;
    [SerializeField] private GameObject accessCodeRoot;
    [SerializeField] private ComputerAccessCodeTerminal accessCodeController;
    [SerializeField] private string accessCode = "1234";
    [SerializeField] private Image accessGrantedImage;
    [SerializeField] private string accessCodePrompt = "Enter access code";
    [SerializeField] private string accessGrantedMessage = "Access granted";
    [SerializeField] private string accessDeniedMessage = "Incorrect access code";

    [Header("Solve Order")]
    [SerializeField] private bool requireSolveOrder = false;
    [SerializeField] private TMP_Text solveOrderNumberText;
    [SerializeField] private string outOfOrderMessage = "Solve the computers in the shown order.";

    [SerializeField] private int questionIndex = -1;
    [SerializeField] private string questionId;
    [SerializeField] private bool useRandomQuestionIfIdMissing = true;
    [SerializeField] private InventoryItem requiredItem;
    [SerializeField] private int requiredItemQuantity = 1;
    [SerializeField] private string missingItemMessage = "You need {0} to use this.";

    public Player Player { get; set; }
    private static Computer activeQuizComputer;
    private bool waitingForQuizResult;
    private bool isSolved;

    public void ApplySolveNumber(int number)
    {
        if (solveOrderNumberText != null)
        {
            solveOrderNumberText.text = number.ToString();
        }
        requireSolveOrder = true;
    }

    public void Interact()
    {
        if (Player == null)
        {
            return;
        }

        if (isSolved)
        {
            Player.ShowInteractionMessage("Ce cahier est déjà résolu.");
            return;
        }

        if (!HasRequiredItem())
        {
            string itemName = requiredItem != null ? requiredItem.ItemName : "required item";
            string message = string.Format(missingItemMessage, itemName);
            Player.ShowInteractionMessage(message);
            TransientDebugConsoleUI.LogWarning($"Le joueur ne possède pas l'objet requis '{itemName}'.");
            return;
        }

        if (useAccessCodeMode)
        {
            OpenAccessCodeMode();
            return;
        }

        if (IsLinuxScene() || IsComputerScene())
        {
            OpenLinuxMinigame();
            return;
        }

        int solveOrderNumber = ResolveSolveOrderNumber();
        if (requireSolveOrder && !ComputerSolveOrderState.CanAttempt(solveOrderNumber))
        {
            Player.ShowInteractionMessage(outOfOrderMessage);
            return;
        }

        QuestionData selectedQuestion = ResolveQuestionForThisComputer();
        if (selectedQuestion == null)
        {
            return;
        }

        activeQuizComputer = this;
        SubscribeToQuizResult();

        QuizController.PendingQuestion = selectedQuestion;

        if (string.IsNullOrWhiteSpace(minigameSceneName))
        {
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(minigameSceneName))
        {
            return;
        }

        MinigameManager.SetActiveMinigameSceneName(minigameSceneName);
        Player.BeginComputerInteraction(computerVirtualCamera, null);

        if (!SceneManager.GetSceneByName(minigameSceneName).isLoaded)
        {
            SceneManager.sceneLoaded -= HandleMinigameSceneLoaded;
            SceneManager.sceneLoaded += HandleMinigameSceneLoaded;
            SceneManager.LoadScene(minigameSceneName, LoadSceneMode.Additive);
        }
        else
        {
            ActivateSceneRoots(SceneManager.GetSceneByName(minigameSceneName));
            OpenComputerExplorerUI();

            QuizController quizController = FindObjectOfType<QuizController>();
            if (quizController != null)
            {
                quizController.SetQuestion(selectedQuestion);
            }
        }
    }

    private void OpenAccessCodeMode()
    {
        if (computerVirtualCamera == null)
        {
            TransientDebugConsoleUI.LogWarning($"Computer.Interact on '{name}': computerVirtualCamera is not assigned.");
            return;
        }

        ComputerAccessCodeTerminal controller = accessCodeController;
        if (controller == null && accessCodeRoot != null)
        {
            controller = accessCodeRoot.GetComponentInChildren<ComputerAccessCodeTerminal>(true);
        }

        if (controller == null)
        {
            TransientDebugConsoleUI.LogWarning($"Computer.Interact on '{name}': accessCodeController is not assigned.");
            return;
        }

        GameObject terminalRoot = accessCodeRoot != null ? accessCodeRoot : controller.gameObject;
        terminalRoot.SetActive(true);

        Player.BeginComputerInteraction(computerVirtualCamera, null);
        controller.Begin(
            accessCode,
            accessGrantedImage,
            accessCodePrompt,
            accessGrantedMessage,
            accessDeniedMessage,
            HandleAccessCodeTerminalClosed);
    }

    private void OpenLinuxMinigame()
    {
        OpenAdditiveSceneMinigame();
    }

    private void OpenAdditiveSceneMinigame()
    {
        if (computerVirtualCamera == null)
        {
            TransientDebugConsoleUI.LogWarning($"Computer.Interact on '{name}': computerVirtualCamera is not assigned.");
            return;
        }

        AdditiveMinigameController controller = FindObjectOfType<AdditiveMinigameController>(true);
        if (controller == null)
        {
            Debug.LogWarning($"Computer.Interact on '{name}': no AdditiveMinigameController found.");
            return;
        }

        controller.OpenMinigame(computerVirtualCamera, minigameSceneName);
    }

    private void HandleAccessCodeTerminalClosed()
    {
        Player.EndComputerInteraction();

        if (accessCodeRoot != null)
        {
            accessCodeRoot.SetActive(false);
        }
        else if (accessCodeController != null)
        {
            accessCodeController.gameObject.SetActive(false);
        }
    }

    private void HandleQuizResult(QuestionData question, bool isCorrect)
    {
        if (!waitingForQuizResult || activeQuizComputer != this)
        {
            return;
        }

        isSolved = true;

        int solveOrderNumber = ResolveSolveOrderNumber();
        if (requireSolveOrder && solveOrderNumber > 0)
        {
            ComputerSolveOrderState.CompleteCurrent(solveOrderNumber);
        }

        activeQuizComputer = null;
        UnsubscribeFromQuizResult();

        MinigameManager minigameManager = FindObjectOfType<MinigameManager>();
        if (minigameManager != null)
        {
            minigameManager.ExitMinigame();
        }
        else if (Player != null)
        {
            Player.EndComputerInteraction();
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        this.enabled = false;
    }

    private void HandleMinigameSceneLoaded(Scene loadedScene, LoadSceneMode loadMode)
    {
        if (!string.Equals(loadedScene.name, minigameSceneName, System.StringComparison.Ordinal))
        {
            return;
        }

        SceneManager.sceneLoaded -= HandleMinigameSceneLoaded;
        OpenComputerExplorerUI();
    }

    private void OpenComputerExplorerUI()
    {
        ComputerUIController uiController = FindObjectOfType<ComputerUIController>(true);
        if (uiController != null)
        {
            uiController.OpenExplorer();
        }
    }

    private static void ActivateSceneRoots(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return;
        }

        foreach (GameObject rootObject in scene.GetRootGameObjects())
        {
            if (rootObject != null)
            {
                rootObject.SetActive(true);
            }
        }
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleMinigameSceneLoaded;
        UnsubscribeFromQuizResult();
    }

    private bool IsLinuxScene()
    {
        return !string.IsNullOrWhiteSpace(minigameSceneName) &&
               string.Equals(minigameSceneName, "Linux", System.StringComparison.OrdinalIgnoreCase);
    }

    private bool IsComputerScene()
    {
        return !string.IsNullOrWhiteSpace(minigameSceneName) &&
               string.Equals(minigameSceneName, "ComputerScene", System.StringComparison.OrdinalIgnoreCase);
    }

    private bool HasRequiredItem()
    {
        if (requiredItem == null)
        {
            return true;
        }

        return InventoryManager.HasItem(requiredItem, Mathf.Max(1, requiredItemQuantity));
    }

    private QuestionData ResolveQuestionForThisComputer()
    {
        if (questionIndex >= 0)
        {
            QuestionData fromIndex = QuestionBankStorage.GetQuestionByIndex(questionIndex);
            if (fromIndex != null)
            {
                return fromIndex;
            }

                TransientDebugConsoleUI.LogWarning($"Computer '{name}': questionIndex '{questionIndex}' is out of range.");
        }

        if (!string.IsNullOrWhiteSpace(questionId))
        {
            QuestionData fromId = QuestionBankStorage.GetQuestionById(questionId);
            if (fromId != null)
            {
                return fromId;
            }

                TransientDebugConsoleUI.LogWarning($"Computer '{name}': questionId '{questionId}' was not found in question_bank.json.");
        }

        if (useRandomQuestionIfIdMissing)
        {
            return QuestionBankStorage.GetRandomQuestion();
        }

        return null;
    }

    private void Update()
    {
        if (!useAccessCodeMode)
        {
            return;
        }

        GameObject terminalRoot = accessCodeRoot != null ? accessCodeRoot : (accessCodeController != null ? accessCodeController.gameObject : null);
        if (terminalRoot == null || !terminalRoot.activeInHierarchy)
        {
            return;
        }

        if (IsEscapePressed())
        {
            HandleAccessCodeTerminalClosed();
        }
    }

    private static bool IsEscapePressed()
    {
        return (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame);
    }

    private int ResolveSolveOrderNumber()
    {
        if (solveOrderNumberText == null)
        {
            return 0;
        }

        string rawValue = solveOrderNumberText.text;
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return 0;
        }

        if (int.TryParse(rawValue.Trim(), out int parsedValue))
        {
            return parsedValue;
        }

        TransientDebugConsoleUI.LogWarning($"Computer '{name}': solveOrderNumberText on '{solveOrderNumberText.name}' does not contain a valid integer: '{rawValue}'");
        return 0;
    }

    private void SubscribeToQuizResult()
    {
        UnsubscribeFromQuizResult();
        QuizController.QuestionResultEvaluated += HandleQuizResult;
        waitingForQuizResult = true;
    }

    private void UnsubscribeFromQuizResult()
    {
        if (!waitingForQuizResult)
        {
            return;
        }

        QuizController.QuestionResultEvaluated -= HandleQuizResult;
        waitingForQuizResult = false;

        if (activeQuizComputer == this)
        {
            activeQuizComputer = null;
        }
    }
}