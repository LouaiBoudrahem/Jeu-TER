using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SupervisorTeleportTrigger : MonoBehaviour
{
    [Header("Unlock Condition")]
    [SerializeField] private InventoryItem requiredItem;
    [SerializeField] private bool startDisabled = true;

    [Header("Teleport")]
    [SerializeField] private EnemyAI enemyAI;
    [SerializeField] private Transform teleportPoint;
    [SerializeField] private bool disableAfterTeleport = true;

    private Collider triggerCollider;
    private bool isUnlocked;
    private bool hasTeleported;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();

        if (triggerCollider == null)
        {
            Debug.LogError("SupervisorTeleportTrigger: missing Collider component.");
            enabled = false;
            return;
        }

        triggerCollider.isTrigger = true;

        Rigidbody rigidbody = GetComponent<Rigidbody>();
        if (rigidbody == null)
        {
            rigidbody = gameObject.AddComponent<Rigidbody>();
        }

        rigidbody.isKinematic = true;
        rigidbody.constraints = RigidbodyConstraints.FreezeAll;

        if (enemyAI == null)
        {
            enemyAI = FindObjectOfType<EnemyAI>();
        }

        if (startDisabled)
        {
            triggerCollider.enabled = false;
        }
    }

    private void OnEnable()
    {
        PickupItem.ItemPickedUp += HandleItemPickedUp;
    }

    private void OnDisable()
    {
        PickupItem.ItemPickedUp -= HandleItemPickedUp;
    }

    private void HandleItemPickedUp(InventoryItem pickedItem, PickupItem pickupItem)
    {
        if (isUnlocked || hasTeleported)
        {
            return;
        }

        if (requiredItem != null && pickedItem != requiredItem)
        {
            return;
        }

        isUnlocked = true;

        if (triggerCollider != null)
        {
            triggerCollider.enabled = true;
        }

        Debug.Log($"SupervisorTeleportTrigger: enabled after picking up {(requiredItem != null ? requiredItem.ItemName : pickedItem.ItemName)}.");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isUnlocked || hasTeleported || triggerCollider == null || !triggerCollider.enabled)
        {
            return;
        }

        Player player = other.GetComponent<Player>();
        if (player == null)
        {
            player = other.GetComponentInParent<Player>();
        }

        if (player == null)
        {
            return;
        }

        if (enemyAI == null)
        {
            Debug.LogWarning("SupervisorTeleportTrigger: no EnemyAI assigned.");
            return;
        }

        if (teleportPoint == null)
        {
            Debug.LogWarning("SupervisorTeleportTrigger: no teleportPoint assigned.");
            return;
        }

        enemyAI.TeleportTo(teleportPoint);
        hasTeleported = true;
        ObjectiveManager.Instance?.AdvanceObjective();

        if (disableAfterTeleport)
        {
            triggerCollider.enabled = false;
        }
    }
}