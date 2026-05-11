using UnityEngine;
using UnityEngine.SceneManagement;

public class Computer : MonoBehaviour, IInteractable
{
    [SerializeField] private InteractionData interactionData;
    [SerializeField] private string minigameSceneName;
    [SerializeField] private GameObject computerVirtualCamera;
    [SerializeField] private int questionIndex = -1;
    [SerializeField] private string questionId;
    [SerializeField] private bool useRandomQuestionIfIdMissing = true;
    [SerializeField] private InventoryItem requiredItem;
    [SerializeField] private int requiredItemQuantity = 1;
    [SerializeField] private string missingItemMessage = "You need {0} to use this.";

    public Player Player { get; set; }

    public void Interact()
    {
        if (Player == null)
        {
            Debug.LogWarning("Computer.Interact: no Player reference set.");
            return;
        }

        if (!HasRequiredItem())
        {
            string itemName = requiredItem != null ? requiredItem.ItemName : "required item";
            string message = string.Format(missingItemMessage, itemName);
            Player.ShowInteractionMessage(message);
            Debug.LogWarning($"Computer.Interact on '{name}': player is missing required item '{itemName}'.");
            return;
        }

        QuestionData selectedQuestion = ResolveQuestionForThisComputer();
        if (selectedQuestion == null)
        {
            Debug.LogWarning($"Computer.Interact on '{name}': no question found. Assign a valid questionId or enable random fallback.");
            return;
        }

        QuizController.PendingQuestion = selectedQuestion;

        if (string.IsNullOrWhiteSpace(minigameSceneName))
        {
            Debug.LogWarning("Computer.Interact: minigame scene name is empty.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(minigameSceneName))
        {
            Debug.LogWarning($"Computer.Interact: scene '{minigameSceneName}' is not in Build Settings.");
            return;
        }

        MinigameManager.SetActiveMinigameSceneName(minigameSceneName);
        Player.BeginComputerInteraction(computerVirtualCamera, null);

        if (!SceneManager.GetSceneByName(minigameSceneName).isLoaded)
        {
            SceneManager.LoadScene(minigameSceneName, LoadSceneMode.Additive);
        }
        else
        {
            QuizController quizController = FindObjectOfType<QuizController>();
            if (quizController != null)
            {
                quizController.SetQuestion(selectedQuestion);
            }
        }
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

            Debug.LogWarning($"Computer '{name}': questionIndex '{questionIndex}' is out of range.");
        }

        if (!string.IsNullOrWhiteSpace(questionId))
        {
            QuestionData fromId = QuestionBankStorage.GetQuestionById(questionId);
            if (fromId != null)
            {
                return fromId;
            }

            Debug.LogWarning($"Computer '{name}': questionId '{questionId}' was not found in question_bank.json.");
        }

        if (useRandomQuestionIfIdMissing)
        {
            return QuestionBankStorage.GetRandomQuestion();
        }

        return null;
    }
}