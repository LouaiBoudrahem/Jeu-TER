using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    [SerializeField] private Inventory playerInventory;
    
    private static InventoryManager instance;

    public static Inventory PlayerInventory => instance?.playerInventory;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        if (playerInventory == null)
        {
            playerInventory = GetComponent<Inventory>();

            if (playerInventory == null)
                playerInventory = FindObjectOfType<Inventory>();

            if (playerInventory == null)
            {
                Debug.LogError("InventoryManager could not find an Inventory in the scene. Assign playerInventory in the Inspector.");
            }
            else
            {
                Debug.LogWarning("InventoryManager playerInventory was empty. Auto-assigned an Inventory component.");
            }
        }

        DontDestroyOnLoad(gameObject);
    }

    public static bool AddItem(InventoryItem item, int quantity = 1)
    {
        if (instance == null || instance.playerInventory == null)
        {
            Debug.LogWarning("InventoryManager.AddItem failed: playerInventory is not assigned.");
            return false;
        }

        return instance.playerInventory.AddItem(item, quantity);
    }

    public static bool RemoveItem(InventoryItem item, int quantity = 1)
    {
        if (instance == null || instance.playerInventory == null)
        {
            Debug.LogWarning("InventoryManager.RemoveItem failed: playerInventory is not assigned.");
            return false;
        }

        return instance.playerInventory.RemoveItem(item, quantity) == quantity;
    }

    public static int GetItemCount(InventoryItem item)
    {
        if (instance == null || instance.playerInventory == null)
            return 0;

        return instance.playerInventory.GetItemCount(item);
    }

    public static bool HasItem(InventoryItem item, int quantity = 1)
    {
        return GetItemCount(item) >= quantity;
    }

    public static bool HasSpace(InventoryItem item, int quantity = 1)
    {
        if (instance == null || instance.playerInventory == null)
        {
            Debug.LogWarning("InventoryManager.HasSpace failed: playerInventory is not assigned.");
            return false;
        }

        return instance.playerInventory.HasSpace(item, quantity);
    }
}
