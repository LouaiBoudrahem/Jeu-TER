using UnityEngine;
public class PickupItem : MonoBehaviour, IInteractable
{
    [SerializeField] private InventoryItem itemToPickup;
    [SerializeField] private int quantity = 1;
    [SerializeField] private bool destroyAfterPickup = true;
    [SerializeField] private string interactionPrompt = "Pick up ";

    void Start()
    {
        if (itemToPickup != null)
        {
            GetComponent<Renderer>().material.color = GetColorForItem(itemToPickup);
        }
    }

    public void Interact()
    {
        if (itemToPickup == null) return;

        if (InventoryManager.HasSpace(itemToPickup, quantity))
        {
            if (InventoryManager.AddItem(itemToPickup, quantity))
            {
                if (destroyAfterPickup)
                {
                    Destroy(gameObject);
                }
            }
        }
    }

    public string GetInteractionPrompt()
    {
        return interactionPrompt + itemToPickup.ItemName;
    }

    private Color GetColorForItem(InventoryItem item)
    {
        if (item.IsConsumable)
            return Color.green;
        return Color.cyan;
    }
}
