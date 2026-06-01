using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ItemGateCameraTransitionTrigger : MonoBehaviour
{
    [Header("Unlock Condition")]
    [SerializeField] private InventoryItem requiredItem;
    [SerializeField] private int requiredQuantity = 1;
    [SerializeField] private bool consumeRequiredItem = false;

    [Header("References")]
    [SerializeField] private Player player;
    [SerializeField] private GameObject targetCinemachineCamera;
    [SerializeField] private CanvasGroup blackScreenCanvasGroup;
    [SerializeField] private ItemGateWorldCanvasCameraTrigger scoreCanvasTrigger;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    [Header("Timing")]
    [SerializeField] private float fadeToBlackDuration = 1f;
    [SerializeField] private float holdOnBlackDuration = 0f;
    [SerializeField] private float fadeFromBlackDuration = 1f;

    [Header("Behavior")]
    [SerializeField] private bool disableAfterUse = true;

    private Collider triggerCollider;
    private bool isTransitioning;
    private bool hasUsed;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();

        if (triggerCollider == null)
        {
            Debug.LogError($"ItemGateCameraTransitionTrigger: missing Collider component on '{name}'.");
            enabled = false;
            return;
        }

        triggerCollider.isTrigger = true;

        Rigidbody body = GetComponent<Rigidbody>();
        if (body == null)
        {
            body = gameObject.AddComponent<Rigidbody>();
        }

        body.isKinematic = true;
        body.constraints = RigidbodyConstraints.FreezeAll;

        if (player == null)
        {
            player = FindObjectOfType<Player>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"ItemGateCameraTransitionTrigger: OnTriggerEnter from '{other.name}'.");

        if (isTransitioning || hasUsed || triggerCollider == null || !triggerCollider.enabled)
        {
            return;
        }

        Player enteringPlayer = other.GetComponent<Player>();
        if (enteringPlayer == null)
        {
            enteringPlayer = other.GetComponentInParent<Player>();
        }

        if (enteringPlayer == null)
        {
            Debug.Log("ItemGateCameraTransitionTrigger: trigger enter ignored, no Player found on collider or parent.");
            return;
        }

        if (requiredItem != null)
        {
            int qty = Mathf.Max(1, requiredQuantity);
            if (!InventoryManager.HasItem(requiredItem, qty))
            {
                Debug.Log($"ItemGateCameraTransitionTrigger: player missing required item '{requiredItem.ItemName}' x{qty}.");
                return;
            }

            if (consumeRequiredItem && !InventoryManager.RemoveItem(requiredItem, qty))
            {
                Debug.Log($"ItemGateCameraTransitionTrigger: failed to consume required item '{requiredItem.ItemName}' x{qty}.");
                return;
            }
        }

        if (targetCinemachineCamera == null || blackScreenCanvasGroup == null)
        {
            Debug.LogWarning($"{nameof(ItemGateCameraTransitionTrigger)} on '{name}': missing targetCinemachineCamera or blackScreenCanvasGroup.");
            return;
        }

        player = enteringPlayer;
        if (player != null)
        {
            player.SetInputLocked(true);
        }

        PlayActivationSound();

        StartCoroutine(RunTransitionSequence());
    }

    private IEnumerator RunTransitionSequence()
    {
        isTransitioning = true;

        blackScreenCanvasGroup.gameObject.SetActive(true);
        blackScreenCanvasGroup.alpha = 0f;
        blackScreenCanvasGroup.blocksRaycasts = true;
        blackScreenCanvasGroup.interactable = true;

        yield return FadeCanvasGroup(0f, 1f, fadeToBlackDuration);

        if (player != null)
        {
            player.BeginCinemachineCameraTransition(targetCinemachineCamera);
        }

        yield return null;

        if (scoreCanvasTrigger != null)
        {
            scoreCanvasTrigger.ShowScores();
        }
        else
        {
            Debug.LogWarning($"ItemGateCameraTransitionTrigger on '{name}': scoreCanvasTrigger is not assigned.");
        }

        if (holdOnBlackDuration > 0f)
        {
            yield return new WaitForSeconds(holdOnBlackDuration);
        }

        yield return FadeCanvasGroup(1f, 0f, fadeFromBlackDuration);

        blackScreenCanvasGroup.blocksRaycasts = false;
        blackScreenCanvasGroup.interactable = false;
        blackScreenCanvasGroup.gameObject.SetActive(false);

        hasUsed = true;

        if (disableAfterUse && triggerCollider != null)
            triggerCollider.enabled = false;

        isTransitioning = false;
    }

    private void PlayActivationSound()
    {
        if (audioSource == null)
        {
            Debug.LogWarning($"ItemGateCameraTransitionTrigger on '{name}': audioSource is not assigned.");
            return;
        }

        audioSource.Play();
    }

    private IEnumerator FadeCanvasGroup(float from, float to, float duration)
    {
        if (duration <= 0f) { blackScreenCanvasGroup.alpha = to; yield break; }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            blackScreenCanvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        blackScreenCanvasGroup.alpha = to;
    }
}