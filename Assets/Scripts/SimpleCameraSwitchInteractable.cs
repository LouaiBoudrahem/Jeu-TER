using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleCameraSwitchInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject virtualCamera;
    
    private bool isActive = false;
    private Player activePlayer;

    public void Interact()
    {
        Player player = FindObjectOfType<Player>();
        if (player == null)
        {
            Debug.LogWarning("SimpleCameraSwitchInteractable.Interact: no Player found in scene.");
            return;
        }

        if (virtualCamera == null)
        {
            Debug.LogWarning("SimpleCameraSwitchInteractable.Interact: virtualCamera is not assigned.");
            return;
        }

        isActive = true;
        activePlayer = player;
        player.BeginComputerInteraction(virtualCamera, null);
    }

    private void Update()
    {
        if (!isActive)
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ExitInteraction();
        }
    }

    private void ExitInteraction()
    {
        if (activePlayer != null)
        {
            activePlayer.EndComputerInteraction();
        }

        isActive = false;
        activePlayer = null;
    }
}
