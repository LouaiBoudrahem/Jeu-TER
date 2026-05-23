using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [SerializeField] [Min(1)] private int gridWidth = 5;
    [SerializeField] [Min(1)] private int gridHeight = 4;
    
    private InventorySlot[] slots;
    
    public event Action<int> OnInventoryChanged;
    public event Action<int> OnSlotUpdated;
    
    public int SlotCount => gridWidth * gridHeight;
    public int GridWidth => gridWidth;
    public int GridHeight => gridHeight;
    public InventorySlot[] Slots => slots;

    void Awake()
    {
        InitializeInventory();
    }

    private void InitializeInventory()
    {
        slots = new InventorySlot[SlotCount];
        
        for (int i = 0; i < SlotCount; i++)
        {
            slots[i] = new InventorySlot();
        }
    }

    public bool AddItem(InventoryItem item, int quantity = 1)
    {
        if (item == null || quantity <= 0)
            return false;

        int remainingQuantity = quantity;

        for (int i = 0; i < slots.Length && remainingQuantity > 0; i++)
        {
            if (slots[i].Item == item && slots[i].IsAnchor)
            {
                int stackSpace = GetStackSpace(i);
                if (stackSpace <= 0)
                    continue;

                int amountToAdd = Mathf.Min(remainingQuantity, stackSpace);

                if (slots[i].AddQuantity(amountToAdd))
                {
                    remainingQuantity -= amountToAdd;
                    NotifyItemCellsUpdated(i);
                }
            }
        }

        while (remainingQuantity > 0)
        {
            int amountToAdd = Mathf.Min(remainingQuantity, item.MaxStackSize);
            if (!TryPlaceNewItem(item, amountToAdd, out int anchorIndex))
                break;

            remainingQuantity -= amountToAdd;
            NotifyItemCellsUpdated(anchorIndex);
        }

        OnInventoryChanged?.Invoke(remainingQuantity);
        return remainingQuantity == 0;
    }

    public bool RemoveItemAtSlot(int slotIndex, int quantity = 1)
    {
        int rootIndex = GetRootIndex(slotIndex);
        if (!IsValidSlot(rootIndex) || slots[rootIndex].IsEmpty || !slots[rootIndex].IsAnchor)
            return false;

        bool success;
        if (quantity >= slots[rootIndex].Quantity)
        {
            ClearPlacedItem(rootIndex);
            success = true;
        }
        else
        {
            success = slots[rootIndex].RemoveQuantity(quantity);
        }

        NotifyItemCellsUpdated(rootIndex);
        OnInventoryChanged?.Invoke(0);
        return success;
    }

    public int RemoveItem(InventoryItem item, int quantity = 1)
    {
        if (item == null)
            return 0;

        int removedCount = 0;

        for (int i = slots.Length - 1; i >= 0 && removedCount < quantity; i--)
        {
            if (slots[i].Item == item && slots[i].IsAnchor)
            {
                int toRemove = Mathf.Min(quantity - removedCount, slots[i].Quantity);

                if (toRemove >= slots[i].Quantity)
                {
                    removedCount += slots[i].Quantity;
                    ClearPlacedItem(i);
                    NotifyItemCellsUpdated(i);
                }
                else if (slots[i].RemoveQuantity(toRemove))
                {
                    removedCount += toRemove;
                    NotifyItemCellsUpdated(i);
                }
            }
        }

        OnInventoryChanged?.Invoke(0);
        return removedCount;
    }

    public int GetItemCount(InventoryItem item)
    {
        if (item == null)
            return 0;

        int count = 0;
        
        foreach (InventorySlot slot in slots)
        {
            if (slot.Item == item && slot.IsAnchor)
                count += slot.Quantity;
        }

        return count;
    }

    public int FindItem(InventoryItem item)
    {
        if (item == null)
            return -1;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].Item == item && slots[i].IsAnchor)
                return i;
        }

        return -1;
    }

    public bool MoveItem(int fromSlot, int toSlot)
    {
        return TryMoveItem(fromSlot, toSlot);
    }

    public bool TryMoveItem(int fromSlot, int toSlot)
    {
        return TryMoveItem(fromSlot, toSlot, false);
    }

    public bool TryMoveItem(int fromSlot, int toSlot, bool rotateDuringMove)
    {
        int fromRoot = GetRootIndex(fromSlot);
        if (!IsValidSlot(fromRoot) || !IsValidSlot(toSlot) || slots[fromRoot].IsEmpty || !slots[fromRoot].IsAnchor)
        {
            return false;
        }

        int toRoot = GetRootIndex(toSlot);
        if (toRoot == fromRoot)
        {
            return false;
        }

        InventoryItem movingItem = slots[fromRoot].Item;
        int movingQuantity = slots[fromRoot].Quantity;
        bool rotatedPlacement = rotateDuringMove ? !slots[fromRoot].IsRotated : slots[fromRoot].IsRotated;
        GetItemDimensions(movingItem, rotatedPlacement, out int movingWidth, out int movingHeight);

        if (IsValidSlot(toRoot) && !slots[toRoot].IsEmpty && slots[toRoot].IsAnchor && slots[toRoot].Item == movingItem)
        {
            int stackSpace = GetStackSpace(toRoot);
            if (stackSpace <= 0)
            {
                return false;
            }

            int amountToTransfer = Mathf.Min(movingQuantity, stackSpace);
            if (amountToTransfer <= 0)
            {
                return false;
            }

            slots[toRoot].AddQuantity(amountToTransfer);
            movingQuantity -= amountToTransfer;

            if (movingQuantity <= 0)
                ClearPlacedItem(fromRoot);
            else
                slots[fromRoot].SetQuantity(movingQuantity);

            NotifyItemCellsUpdated(fromRoot);
            NotifyItemCellsUpdated(toRoot);
            OnInventoryChanged?.Invoke(0);
            return true;
        }

        if (!CanPlaceAt(toSlot, movingWidth, movingHeight, fromRoot))
        {
            return false;
        }

        ClearPlacedItem(fromRoot);
        OccupyItemCells(toSlot, movingItem, movingQuantity, rotatedPlacement);
        NotifyItemCellsUpdated(fromRoot);
        NotifyItemCellsUpdated(toSlot);
        OnInventoryChanged?.Invoke(0);
        return true;
    }

    public int GetRootIndex(int slotIndex)
    {
        if (!IsValidSlot(slotIndex) || slots[slotIndex].IsEmpty)
            return -1;

        return slots[slotIndex].RootIndex;
    }

    public bool HasSpace(InventoryItem item, int quantity = 1)
    {
        if (item == null || quantity <= 0)
            return false;

        int remainingQuantity = quantity;

        for (int i = 0; i < slots.Length && remainingQuantity > 0; i++)
        {
            if (slots[i].Item == item && slots[i].IsAnchor)
            {
                remainingQuantity -= GetStackSpace(i);
            }
        }

        if (remainingQuantity <= 0)
            return true;

        bool[] occupied = new bool[slots.Length];
        for (int i = 0; i < slots.Length; i++)
        {
            occupied[i] = !slots[i].IsEmpty;
        }

        while (remainingQuantity > 0)
        {
            int itemWidth = item.GridWidth;
            int itemHeight = item.GridHeight;
            int anchor = FindFirstFitInMap(occupied, itemWidth, itemHeight);

            if (anchor < 0 && itemWidth != itemHeight)
            {
                itemWidth = item.GridHeight;
                itemHeight = item.GridWidth;
                anchor = FindFirstFitInMap(occupied, itemWidth, itemHeight);
            }

            if (anchor < 0)
                break;

            MarkCellsInMap(occupied, anchor, itemWidth, itemHeight, true);
            remainingQuantity -= item.MaxStackSize;
        }

        return remainingQuantity <= 0;
    }

    public int GetFirstEmptySlot()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].IsEmpty)
                return i;
        }

        return -1;
    }

    public void ClearInventory()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].ClearSlot();
            OnSlotUpdated?.Invoke(i);
        }

        OnInventoryChanged?.Invoke(0);
    }

    private bool IsValidSlot(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < slots.Length;
    }

    private int GetStackSpace(int slotIndex)
    {
        if (!IsValidSlot(slotIndex) || slots[slotIndex].IsEmpty || slots[slotIndex].Item == null || !slots[slotIndex].IsAnchor)
            return 0;

        return Mathf.Max(0, slots[slotIndex].Item.MaxStackSize - slots[slotIndex].Quantity);
    }

    private bool TryPlaceNewItem(InventoryItem item, int quantity, out int anchorIndex)
    {
        bool rotatedPlacement = false;

        anchorIndex = FindFirstFit(item.GridWidth, item.GridHeight);
        if (anchorIndex < 0 && item.GridWidth != item.GridHeight)
        {
            anchorIndex = FindFirstFit(item.GridHeight, item.GridWidth);
            rotatedPlacement = anchorIndex >= 0;
        }

        if (anchorIndex < 0)
            return false;

        OccupyItemCells(anchorIndex, item, quantity, rotatedPlacement);
        return true;
    }

    private int FindFirstFit(int itemWidth, int itemHeight, int ignoreRoot = -1)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (CanPlaceAt(i, itemWidth, itemHeight, ignoreRoot))
                return i;
        }

        return -1;
    }

    private bool CanPlaceAt(int anchorIndex, int itemWidth, int itemHeight, int ignoreRoot = -1)
    {
        if (!IsValidSlot(anchorIndex))
            return false;

        GetGridPosition(anchorIndex, out int startX, out int startY);
        if (startX + itemWidth > gridWidth || startY + itemHeight > gridHeight)
            return false;

        for (int y = startY; y < startY + itemHeight; y++)
        {
            for (int x = startX; x < startX + itemWidth; x++)
            {
                int cellIndex = ToIndex(x, y);
                if (slots[cellIndex].IsEmpty)
                    continue;

                if (slots[cellIndex].RootIndex == ignoreRoot)
                    continue;

                return false;
            }
        }

        return true;
    }

    private void OccupyItemCells(int anchorIndex, InventoryItem item, int quantity, bool isRotated = false)
    {
        GetGridPosition(anchorIndex, out int startX, out int startY);
        GetItemDimensions(item, isRotated, out int itemWidth, out int itemHeight);

        for (int y = startY; y < startY + itemHeight; y++)
        {
            for (int x = startX; x < startX + itemWidth; x++)
            {
                int cellIndex = ToIndex(x, y);
                slots[cellIndex].SetAsReference(item, anchorIndex, isRotated);
            }
        }

        slots[anchorIndex].SetAsAnchor(item, quantity, anchorIndex, isRotated);
    }

    private void ClearPlacedItem(int rootIndex)
    {
        if (!IsValidSlot(rootIndex) || slots[rootIndex].IsEmpty)
            return;

        InventoryItem item = slots[rootIndex].Item;
        if (item == null)
            return;

        bool isRotated = slots[rootIndex].IsRotated;
        GetItemDimensions(item, isRotated, out int itemWidth, out int itemHeight);

        GetGridPosition(rootIndex, out int startX, out int startY);

        for (int y = startY; y < startY + itemHeight; y++)
        {
            for (int x = startX; x < startX + itemWidth; x++)
            {
                int cellIndex = ToIndex(x, y);
                if (IsValidSlot(cellIndex) && slots[cellIndex].RootIndex == rootIndex)
                {
                    slots[cellIndex].ClearSlot();
                }
            }
        }
    }

    private void NotifyItemCellsUpdated(int rootIndex)
    {
        if (!IsValidSlot(rootIndex))
            return;

        HashSet<int> updatedCells = new HashSet<int> { rootIndex };

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].RootIndex == rootIndex)
                updatedCells.Add(i);
        }

        foreach (int cell in updatedCells)
        {
            OnSlotUpdated?.Invoke(cell);
        }
    }

    private int FindFirstFitInMap(bool[] occupiedMap, int itemWidth, int itemHeight)
    {
        for (int i = 0; i < occupiedMap.Length; i++)
        {
            GetGridPosition(i, out int startX, out int startY);
            if (startX + itemWidth > gridWidth || startY + itemHeight > gridHeight)
                continue;

            bool canPlace = true;
            for (int y = startY; y < startY + itemHeight && canPlace; y++)
            {
                for (int x = startX; x < startX + itemWidth; x++)
                {
                    if (occupiedMap[ToIndex(x, y)])
                    {
                        canPlace = false;
                        break;
                    }
                }
            }

            if (canPlace)
                return i;
        }

        return -1;
    }

    private void MarkCellsInMap(bool[] occupiedMap, int anchorIndex, int itemWidth, int itemHeight, bool value)
    {
        GetGridPosition(anchorIndex, out int startX, out int startY);
        for (int y = startY; y < startY + itemHeight; y++)
        {
            for (int x = startX; x < startX + itemWidth; x++)
            {
                occupiedMap[ToIndex(x, y)] = value;
            }
        }
    }

    private void GetGridPosition(int slotIndex, out int x, out int y)
    {
        x = slotIndex % gridWidth;
        y = slotIndex / gridWidth;
    }

    private void GetItemDimensions(InventoryItem item, bool isRotated, out int width, out int height)
    {
        if (item == null)
        {
            width = 1;
            height = 1;
            return;
        }

        if (isRotated)
        {
            width = item.GridHeight;
            height = item.GridWidth;
        }
        else
        {
            width = item.GridWidth;
            height = item.GridHeight;
        }
    }

    private int ToIndex(int x, int y)
    {
        return y * gridWidth + x;
    }
}
