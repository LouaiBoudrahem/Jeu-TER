using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class DoorLockKeypadInteractable : MonoBehaviour, IInteractable
{
    [Header("Keypad")]
    [SerializeField] private GameObject keypadRoot;
    [SerializeField] private DoorLockKeypad keypadController;
    [SerializeField] private GameObject keypadVirtualCamera;

    private bool isInUse;
    private Player activePlayer;
    private bool usingVirtualCamera;

    public void Interact()
    {
        if (isInUse)
        {
            return;
        }

        Player player = FindObjectOfType<Player>();
        if (player == null)
        {
            Debug.LogWarning("DoorLockKeypadInteractable.Interact: no Player found in scene.");
            return;
        }

        if (keypadController == null && keypadRoot != null)
        {
            keypadController = keypadRoot.GetComponentInChildren<DoorLockKeypad>(true);
        }

        if (keypadController == null)
        {
            Debug.LogWarning("DoorLockKeypadInteractable.Interact: no DoorLockKeypad found.");
            return;
        }

        if (keypadRoot == null)
        {
            keypadRoot = keypadController.gameObject;
        }

        if (EventSystem.current == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem (Auto)");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
            Debug.Log("DoorLockKeypadInteractable: created EventSystem with InputSystemUIInputModule for keypad UI input.");
        }

        isInUse = true;
        activePlayer = player;
        usingVirtualCamera = keypadVirtualCamera != null;

        if (usingVirtualCamera)
        {
            player.BeginComputerInteraction(keypadVirtualCamera, null);
        }
        else
        {
            player.SetInputLocked(true);
        }

        keypadRoot.SetActive(true);
        keypadController.Begin(player, HandleKeypadClosed);
    }

    private void HandleKeypadClosed()
    {
        isInUse = false;

        if (keypadRoot != null)
        {
            keypadRoot.SetActive(false);
        }

        if (activePlayer != null)
        {
            if (usingVirtualCamera)
            {
                activePlayer.EndComputerInteraction();
            }
            else
            {
                activePlayer.SetInputLocked(false);
            }
        }

        activePlayer = null;
        usingVirtualCamera = false;
    }
}