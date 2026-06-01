using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class ComputerSceneScript : MonoBehaviour, IInteractable
{
    [SerializeField] private InteractionData interactionData;
    [SerializeField] private string minigameSceneName;
    [SerializeField] private GameObject computerVirtualCamera;

    // Access code mode removed per request.

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
    private static ComputerSceneScript activeQuizComputer;
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
        Debug.Log($"ComputerSceneScript.Interact called on '{name}'; minigameSceneName='{minigameSceneName}'");

        if (Player == null)
        {
            Debug.LogWarning($"ComputerSceneScript.Interact: Player is null on '{name}'");
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
            Debug.LogWarning($"ComputerSceneScript.Interact on '{name}': minigameSceneName is empty.");
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
            Debug.Log($"ComputerSceneScript: Loading additive scene '{minigameSceneName}' for '{name}'");
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


    private void HandleQuizResult(QuestionData question, bool isCorrect)
    {
        if (!waitingForQuizResult || activeQuizComputer != this)
        {
            return;
        }

        int solveOrderNumber = ResolveSolveOrderNumber();
        if (requireSolveOrder && solveOrderNumber > 0)
        {
            ComputerSolveOrderState.CompleteCurrent(solveOrderNumber);
        }

        activeQuizComputer = null;
        UnsubscribeFromQuizResult();

        ComputerUIController uiController = FindObjectOfType<ComputerUIController>(true);
        if (uiController != null)
        {
            uiController.ClosePreview();
        }
    }

    private void HandleMinigameSceneLoaded(Scene loadedScene, LoadSceneMode loadMode)
    {
        Debug.Log($"HandleMinigameSceneLoaded called for scene '{loadedScene.name}' (expected '{minigameSceneName}')");

        if (!string.Equals(loadedScene.name, minigameSceneName, System.StringComparison.Ordinal))
        {
            return;
        }

        SceneManager.sceneLoaded -= HandleMinigameSceneLoaded;
        Debug.Log($"Additive scene '{loadedScene.name}' matched expected; activating roots and opening explorer for '{name}'");
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

    private void Update()
    {
        ComputerUIController uiController = FindObjectOfType<ComputerUIController>(true);

        if (uiController != null && uiController.IsPreviewOpen())
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                uiController.ClosePreview();
                return;
            }
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (Player != null)
            {
                Player.EndComputerInteraction();
            }

            Scene scene = SceneManager.GetSceneByName(minigameSceneName);
            if (scene.IsValid() && scene.isLoaded)
            {
                SceneManager.sceneLoaded -= HandleMinigameSceneLoaded;
                SceneManager.UnloadSceneAsync(scene.name);
            }

            MinigameManager minigameManager = FindObjectOfType<MinigameManager>();
            if (minigameManager != null)
            {
                minigameManager.ExitMinigame();
            }
        }
    }

    private bool IsLinuxScene()
    {
        return !string.IsNullOrWhiteSpace(minigameSceneName) &&
               string.Equals(minigameSceneName, "Linux", System.StringComparison.OrdinalIgnoreCase);
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