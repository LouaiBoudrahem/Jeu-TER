using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Item", fileName = "NewInventoryItem")]
public class InventoryItem : ScriptableObject
{
    [SerializeField] private string itemName;
    [SerializeField] private string itemDescription;
    [SerializeField] private Sprite itemIcon;
    [SerializeField] private int maxStackSize = 1;
    [SerializeField] [Min(1)] private int gridWidth = 1;
    [SerializeField] [Min(1)] private int gridHeight = 1;
    [SerializeField] private bool isConsumable;
    
    public string ItemName => itemName;
    public string ItemDescription => itemDescription;
    public Sprite ItemIcon => itemIcon;
    public int MaxStackSize => maxStackSize;
    public int GridWidth => Mathf.Max(1, gridWidth);
    public int GridHeight => Mathf.Max(1, gridHeight);
    public bool IsConsumable => isConsumable;
}
