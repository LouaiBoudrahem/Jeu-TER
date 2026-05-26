using UnityEngine;

public class BackpackInteractable : MonoBehaviour, IInteractable
{
    [Header("Reward")]
    [SerializeField] private InventoryItem rewardItem;
    [SerializeField, Min(1)] private int rewardQuantity = 1;

    [Header("Audio")]
    [SerializeField] private AudioClip pickupSound;
    [SerializeField] private AudioSource audioSource;

    private bool isCollected;

    public void Interact()
    {
        if (isCollected)
        {
            return;
        }

        if (rewardItem == null)
        {
            Debug.LogWarning("BackpackInteractable.Interact: rewardItem is not assigned.");
            return;
        }

        if (!InventoryManager.AddItem(rewardItem, rewardQuantity))
        {
            Player player = FindFirstObjectByType<Player>();
            if (player != null)
            {
                player.ShowInteractionMessage("Inventory is full.");
            }

            return;
        }

        PlayPickupSound();
        isCollected = true;
    }

    private void PlayPickupSound()
    {
        if (pickupSound == null)
        {
            return;
        }

        EnsureAudioSource();
        if (audioSource != null)
        {
            audioSource.PlayOneShot(pickupSound);
        }
    }

    private void EnsureAudioSource()
    {
        if (audioSource != null)
        {
            return;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }
}