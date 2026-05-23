using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private Image slotBackground;

    [Space]
    [SerializeField] private Color emptySlotColor = Color.gray;
    [SerializeField] private Color filledSlotColor = Color.white;
    [SerializeField] private Color occupiedSlotColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    [SerializeField] private Color selectedSlotColor = Color.yellow;
    [SerializeField] private Color draggingSlotColor = new Color(1f, 1f, 1f, 0.6f);

    private int slotIndex;
    private bool isSelected;
    private bool isDragging;
    private InventoryUI inventoryUI;
    private InventoryItem currentItem;
    private int currentQuantity;
    private bool isReferenceCell;

    public int SlotIndex => slotIndex;
    public RectTransform SlotRectTransform => transform as RectTransform;

    void Awake()
    {
        if (slotBackground != null)
            slotBackground.raycastTarget = true;

        if (itemIcon != null)
            itemIcon.raycastTarget = false;

        if (quantityText != null)
            quantityText.raycastTarget = false;
    }

    public void Initialize(int index, InventoryUI owner)
    {
        slotIndex = index;
        inventoryUI = owner;
        SetEmpty();
    }

    public void SetItem(InventoryItem item, int quantity)
    {
        currentItem = item;
        currentQuantity = quantity;
        isReferenceCell = false;
        RefreshVisual();
    }

    public void SetReferencedItem(InventoryItem item)
    {
        currentItem = item;
        currentQuantity = 0;
        isReferenceCell = true;
        RefreshVisual();
    }

    public void SetEmpty()
    {
        currentItem = null;
        currentQuantity = 0;
        isReferenceCell = false;
        isDragging = false;
        RefreshVisual();
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        RefreshVisual();
    }

    public void SetDragging(bool dragging)
    {
        isDragging = dragging;
        RefreshVisual();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.dragging || inventoryUI == null)
        {
            return;
        }

        inventoryUI.SelectSlot(slotIndex);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (inventoryUI == null || currentItem == null || eventData.button != PointerEventData.InputButton.Left)
            return;

        inventoryUI.BeginDrag(slotIndex, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (inventoryUI == null)
            return;

        inventoryUI.UpdateDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (inventoryUI == null)
            return;

        inventoryUI.EndDrag(eventData);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (inventoryUI == null)
            return;

        inventoryUI.DropOnSlot(slotIndex, eventData);
    }

    private void RefreshVisual()
    {
        bool hasItem = currentItem != null;

        if (hasItem && !isDragging)
        {
            if (isReferenceCell)
            {
                itemIcon.sprite = null;
                itemIcon.enabled = false;
                quantityText.gameObject.SetActive(false);
                slotBackground.color = isSelected ? selectedSlotColor : occupiedSlotColor;
            }
            else
            {
                bool isMultiCellItem = currentItem.GridWidth > 1 || currentItem.GridHeight > 1;

                itemIcon.sprite = currentItem.ItemIcon;
                itemIcon.enabled = itemIcon.sprite != null && !isMultiCellItem;
                slotBackground.color = isSelected ? selectedSlotColor : filledSlotColor;

                if (currentQuantity > 1)
                {
                    quantityText.text = currentQuantity.ToString();
                    quantityText.gameObject.SetActive(true);
                }
                else
                {
                    quantityText.gameObject.SetActive(false);
                }
            }
        }
        else
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
            quantityText.gameObject.SetActive(false);
            slotBackground.color = isDragging ? draggingSlotColor : (isSelected ? selectedSlotColor : emptySlotColor);
        }
    }
}
