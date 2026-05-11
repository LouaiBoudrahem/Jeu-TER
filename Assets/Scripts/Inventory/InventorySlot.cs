using System;
using UnityEngine;

[System.Serializable]
public class InventorySlot
{
    public InventoryItem Item { get; private set; }
    public int Quantity { get; private set; }
    public int RootIndex { get; private set; } = -1;
    public bool IsRotated { get; private set; }
    public bool IsAnchor { get; private set; }
    public bool IsReference => !IsEmpty && !IsAnchor;

    public InventorySlot()
    {
        ClearSlot();
    }

    public InventorySlot(InventoryItem item, int quantity = 1)
    {
        SetAsAnchor(item, quantity, -1);
    }

    public void SetAsAnchor(InventoryItem item, int quantity, int rootIndex, bool isRotated = false)
    {
        Item = item;
        Quantity = Mathf.Max(1, quantity);
        RootIndex = rootIndex;
        IsRotated = isRotated;
        IsAnchor = true;
    }

    public void SetAsReference(InventoryItem item, int rootIndex, bool isRotated = false)
    {
        Item = item;
        Quantity = 0;
        RootIndex = rootIndex;
        IsRotated = isRotated;
        IsAnchor = false;
    }

    public bool SetQuantity(int quantity)
    {
        if (IsEmpty || !IsAnchor)
            return false;

        if (quantity <= 0)
            return ClearSlot();

        Quantity = quantity;
        return true;
    }

    public bool AddQuantity(int amount)
    {
        if (Item == null || !IsAnchor || amount <= 0)
            return false;

        int newQuantity = Quantity + amount;
        int maxAllowed = Item.MaxStackSize;

        if (newQuantity <= maxAllowed)
        {
            Quantity = newQuantity;
            return true;
        }
        
        Quantity = maxAllowed;
        return false;
    }

    public bool RemoveQuantity(int amount)
    {
        if (IsEmpty || !IsAnchor || amount <= 0)
            return false;

        int newQuantity = Quantity - amount;

        if (newQuantity < 0)
            return false;

        Quantity = newQuantity;
        return Quantity == 0 ? ClearSlot() : true;
    }

    public bool ClearSlot()
    {
        Item = null;
        Quantity = 0;
        RootIndex = -1;
        IsRotated = false;
        IsAnchor = false;
        return true;
    }

    public bool IsEmpty => Item == null;
}
