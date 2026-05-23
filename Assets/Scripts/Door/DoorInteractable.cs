using UnityEngine;

public class DoorInteractable : MonoBehaviour, IInteractable
{
    [Header("Door")]
    [SerializeField] private DoorController doorController;

    [Header("Score Requirement")]
    [SerializeField] private bool requiresMinimumScore = false;
    [SerializeField, Min(0)] private int minimumScoreRequired = 0;
    [SerializeField] private string lowScoreMessage = "The door is locked. You need a higher score to open it.";

    [Header("Key Requirement")]
    [SerializeField] private bool requiresKey = true;
    [SerializeField] private InventoryItem requiredKeyItem;
    [SerializeField, Min(1)] private int requiredQuantity = 1;
    [SerializeField] private bool consumeOnUse = true;

    [Header("Messages")]
    [SerializeField] private string noKeyMessage = "The door is locked. You need a key.";
    [SerializeField] private string openedMessage = "The door unlocked and opened.";

    private void Awake()
    {
        if (doorController == null)
            doorController = GetComponent<DoorController>();

        if (doorController == null)
            TransientDebugConsoleUI.LogWarning("DoorInteractable: doorController not assigned and not found on the same GameObject.");
    }

    public void Interact()
    {
        if (doorController == null)
        {
            TransientDebugConsoleUI.LogWarning("DoorInteractable.Interact: No DoorController assigned.");
            PlayFailSound();
            return;
        }

        if (requiresMinimumScore && QuizController.CurrentScore < minimumScoreRequired)
        {
            ShowMessage(lowScoreMessage);
            PlayFailSound();
            return;
        }

        if (!requiresKey)
        {
            doorController.OpenDoor();
            ShowMessage(openedMessage);
            return;
        }

        if (requiredKeyItem == null)
        {
            TransientDebugConsoleUI.LogWarning("DoorInteractable: requiresKey is true but requiredKeyItem is not assigned.");
            doorController.OpenDoor();
            return;
        }

        if (InventoryManager.HasItem(requiredKeyItem, requiredQuantity))
        {
            if (consumeOnUse)
            {
                bool removed = InventoryManager.RemoveItem(requiredKeyItem, requiredQuantity);
                if (!removed)
                {
                    ShowMessage("Unable to use the key right now.");
                    PlayFailSound();
                    return;
                }
            }

            doorController.OpenDoor();
            ShowMessage(openedMessage);
        }
        else
        {
            ShowMessage(noKeyMessage);
            PlayFailSound();
        }
    }

    private void ShowMessage(string message)
    {
        TransientDebugConsoleUI.Log(message);

        Player player = FindObjectOfType<Player>();
        if (player != null)
        {
            player.ShowInteractionMessage(message);
        }
    }

    private void PlayFailSound()
    {
        if (doorController != null)
        {
            doorController.PlayFailSound();
        }
    }
}
