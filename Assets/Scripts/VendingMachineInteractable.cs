using UnityEngine;

public class VendingMachineInteractable : MonoBehaviour, IInteractable
{
    [Header("Built-In Minigame")]
    [SerializeField] private GameObject minigameRoot;
    [SerializeField] private VendingMachineMinigame minigameController;
    [SerializeField] private GameObject vendingVirtualCamera;

    [Header("Reward")]
    [SerializeField] private InventoryItem keyRewardItem;
    [SerializeField, Min(1)] private int keyRewardQuantity = 1;

    private bool isInUse;
    private bool usingVirtualCamera;
    private Player activePlayer;

    public void Interact()
    {
        if (isInUse)
        {
            return;
        }

        Player player = FindObjectOfType<Player>();
        if (player == null)
        {
            Debug.LogWarning("VendingMachineInteractable.Interact: no Player found in scene.");
            return;
        }

        if (minigameController == null && minigameRoot != null)
        {
            minigameController = minigameRoot.GetComponentInChildren<VendingMachineMinigame>(true);
        }

        if (minigameController == null)
        {
            Debug.LogWarning("VendingMachineInteractable.Interact: assign minigameRoot or minigameController for the built-in minigame.");
            return;
        }

        activePlayer = player;
        isInUse = true;
        usingVirtualCamera = vendingVirtualCamera != null;

        if (usingVirtualCamera)
        {
            activePlayer.BeginComputerInteraction(vendingVirtualCamera, null);
        }
        else
        {
            activePlayer.SetInputLocked(true);
        }

        if (minigameRoot != null)
        {
            minigameRoot.SetActive(true);
        }
        else
        {
            minigameController.gameObject.SetActive(true);
        }

        minigameController.Begin(activePlayer, HandleMinigameClosed, keyRewardItem, keyRewardQuantity);
    }

    private void HandleMinigameClosed(bool rewardGiven)
    {
        if (rewardGiven)
        {
            if (minigameRoot != null)
            {
                Destroy(minigameRoot);
            }
            else if (minigameController != null)
            {
                Destroy(minigameController.gameObject);
            }
        }
        else
        {
            if (minigameRoot != null)
            {
                minigameRoot.SetActive(false);
            }
            else if (minigameController != null)
            {
                minigameController.gameObject.SetActive(false);
            }
        }

        ReleasePlayer();
        isInUse = false;
    }

    private void ReleasePlayer()
    {
        if (activePlayer == null)
        {
            usingVirtualCamera = false;
            return;
        }

        if (usingVirtualCamera)
        {
            activePlayer.EndComputerInteraction();
        }
        else
        {
            activePlayer.SetInputLocked(false);
        }

        activePlayer = null;
        usingVirtualCamera = false;
    }
}