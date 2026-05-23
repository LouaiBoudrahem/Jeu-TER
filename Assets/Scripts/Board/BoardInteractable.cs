using UnityEngine;

public class BoardInteractable : MonoBehaviour, IInteractable
{
    [Header("Board Access")]
    [SerializeField] private InventoryItem requiredBrush;
    [SerializeField] [Min(1)] private int requiredBrushQuantity = 1;

    [Header("Missing Brush Feedback")]
    [SerializeField] private string symbolsPreview = "@#&* ?! <> +++";
    [SerializeField] private string missingBrushMessage = "You stare at the symbols: {0}";

    [Header("Minigame Setup")]
    [SerializeField] private GameObject boardVirtualCamera;
    [SerializeField] private GameObject boardMinigameRoot;
    [SerializeField] private BoardCleaningMinigame boardCleaningMinigame;

    private bool isInUse;

    public void Interact()
    {
        if (isInUse)
        {
            return;
        }

        Player player = FindObjectOfType<Player>();
        if (player == null)
        {
            Debug.LogWarning("BoardInteractable.Interact: no Player found in scene.");
            return;
        }

        bool hasBrush = HasBrush();

        if (boardVirtualCamera == null)
        {
            Debug.LogWarning("BoardInteractable.Interact: boardVirtualCamera is not assigned.");
            player.ShowInteractionMessage("Board camera is not assigned.");
            return;
        }

        if (boardMinigameRoot == null)
        {
            Debug.LogWarning("BoardInteractable.Interact: boardMinigameRoot is not assigned.");
            player.ShowInteractionMessage("Board minigame UI is not assigned.");
            return;
        }

        if (boardCleaningMinigame == null)
        {
            boardCleaningMinigame = boardMinigameRoot.GetComponentInChildren<BoardCleaningMinigame>(true);
        }

        if (boardCleaningMinigame == null)
        {
            Debug.LogWarning("BoardInteractable.Interact: no BoardCleaningMinigame found.");
            return;
        }

        isInUse = true;

        player.BeginComputerInteraction(boardVirtualCamera, null);

        string lockedInstruction = hasBrush
            ? null
            : string.Format(missingBrushMessage, symbolsPreview);

        boardCleaningMinigame.Begin(player, HandleMinigameClosed, hasBrush, lockedInstruction);
    }

    private bool HasBrush()
    {
        if (requiredBrush == null)
        {
            return true;
        }

        return InventoryManager.HasItem(requiredBrush, Mathf.Max(1, requiredBrushQuantity));
    }

    private void HandleMinigameClosed()
    {
        isInUse = false;
    }
}
