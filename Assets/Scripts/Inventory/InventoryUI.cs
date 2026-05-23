using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Transform slotContainer;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Inventory inventory;

    [Space]
    [SerializeField] private Image itemDetailIcon;
    [SerializeField] private TextMeshProUGUI itemDetailName;
    [SerializeField] private TextMeshProUGUI itemDetailDescription;
    [SerializeField] private TextMeshProUGUI itemDetailQuantity;

    private InventorySlotUI[] slotUIs;
    private Canvas rootCanvas;
    private GraphicRaycaster rootGraphicRaycaster;
    private GridLayoutGroup slotGridLayout;
    private RectTransform footprintOverlayContainer;
    private RectTransform dragIconRect;
    private Image dragIconImage;
    private CanvasGroup dragIconCanvasGroup;
    private int selectedRootIndex = -1;
    private int draggingRootIndex = -1;
    private bool dropHandledThisDrag;
    private bool mouseButtonDown;
    private bool manualDragActive;
    private bool draggingRotated;
    private int pressedSlotIndex = -1;
    private Vector2 pressedPointerPosition;
    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();
    private readonly Dictionary<int, RectTransform> footprintOverlays = new Dictionary<int, RectTransform>();
    private const float dragStartThreshold = 10f;

    void Start()
    {
        if (inventory == null)
        {
            Debug.LogError("InventoryUI requires an Inventory reference.");
            enabled = false;
            return;
        }

        if (slotContainer == null || slotPrefab == null)
        {
            Debug.LogError("InventoryUI requires a slot container and slot prefab.");
            enabled = false;
            return;
        }

        InitializeUI();

        inventory.OnSlotUpdated += UpdateSlotUI;
        inventory.OnInventoryChanged += RefreshUI;
    }

    void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.OnSlotUpdated -= UpdateSlotUI;
            inventory.OnInventoryChanged -= RefreshUI;
        }

        DestroyFootprintOverlays();
        DestroyDragVisual();
    }

    void Update()
    {
        if (Mouse.current == null || rootCanvas == null || EventSystem.current == null)
            return;

        Vector2 pointerPosition = Mouse.current.position.ReadValue();

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            mouseButtonDown = true;
            manualDragActive = false;
            pressedPointerPosition = pointerPosition;
            pressedSlotIndex = ResolveSlotIndexFromScreenPosition(pointerPosition);
        }

        if (mouseButtonDown && Mouse.current.leftButton.isPressed && !manualDragActive)
        {
            if (pressedSlotIndex >= 0 && Vector2.Distance(pressedPointerPosition, pointerPosition) >= dragStartThreshold)
            {
                PointerEventData startEventData = CreatePointerEventData(pointerPosition);
                BeginDrag(pressedSlotIndex, startEventData);
                manualDragActive = draggingRootIndex >= 0;
            }
        }

        if (mouseButtonDown && manualDragActive && Mouse.current.leftButton.isPressed)
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                draggingRotated = !draggingRotated;
                UpdateDragVisualSize();
            }

            UpdateDrag(CreatePointerEventData(pointerPosition));
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            PointerEventData endEventData = CreatePointerEventData(pointerPosition);

            if (manualDragActive)
            {
                EndDrag(endEventData);
            }
            else if (pressedSlotIndex >= 0)
            {
                SelectSlot(pressedSlotIndex);
            }

            mouseButtonDown = false;
            manualDragActive = false;
            pressedSlotIndex = -1;
        }
    }

    private void InitializeUI()
    {
        rootCanvas = slotContainer != null ? slotContainer.GetComponentInParent<Canvas>(true) : null;
        rootGraphicRaycaster = rootCanvas != null ? rootCanvas.GetComponent<GraphicRaycaster>() : null;
        slotGridLayout = slotContainer != null ? slotContainer.GetComponent<GridLayoutGroup>() : null;
        slotUIs = new InventorySlotUI[inventory.SlotCount];

        for (int i = 0; i < inventory.SlotCount; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotContainer);
            InventorySlotUI slotUI = slotObj.GetComponent<InventorySlotUI>();
            slotUI.Initialize(i, this);
            slotUIs[i] = slotUI;
        }

        CreateFootprintOverlayContainer();
        CreateDragVisual();
        RefreshUI(0);
        ClearDetailDisplay();
    }

    private void UpdateSlotUI(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slotUIs.Length)
            return;

        InventorySlot slot = inventory.Slots[slotIndex];

        if (slot.IsEmpty)
        {
            slotUIs[slotIndex].SetEmpty();
        }
        else if (slot.IsAnchor)
        {
            slotUIs[slotIndex].SetItem(slot.Item, slot.Quantity);
        }
        else
        {
            slotUIs[slotIndex].SetReferencedItem(slot.Item);
        }

        slotUIs[slotIndex].SetSelected(!slot.IsEmpty && slot.RootIndex == selectedRootIndex);

        if (slotIndex == draggingRootIndex)
        {
            slotUIs[slotIndex].SetDragging(true);
        }
    }

    private void RefreshUI(int unused)
    {
        for (int i = 0; i < inventory.SlotCount; i++)
        {
            UpdateSlotUI(i);
        }

        UpdateFootprintOverlays();

        if (draggingRootIndex >= 0)
            return;

        if (selectedRootIndex >= 0)
        {
            InventorySlot selectedSlot = inventory.Slots[selectedRootIndex];
            if (selectedSlot.IsEmpty || !selectedSlot.IsAnchor)
            {
                selectedRootIndex = -1;
                ClearDetailDisplay();
            }
            else
            {
                UpdateDetails(selectedSlot);
            }
        }
    }

    public void SelectSlot(int slotIndex)
    {
        int rootIndex = inventory.GetRootIndex(slotIndex);
        if (rootIndex < 0 || rootIndex >= inventory.SlotCount)
        {
            selectedRootIndex = -1;
            RefreshUI(0);
            ClearDetailDisplay();
            return;
        }

        if (selectedRootIndex == rootIndex)
        {
            selectedRootIndex = -1;
            RefreshUI(0);
            ClearDetailDisplay();
            return;
        }

        selectedRootIndex = rootIndex;
        InventorySlot slot = inventory.Slots[rootIndex];
        UpdateDetails(slot);
        RefreshUI(0);
    }

    public void BeginDrag(int slotIndex, PointerEventData eventData)
    {
        int rootIndex = inventory.GetRootIndex(slotIndex);
        
        if (rootIndex < 0 || rootIndex >= inventory.SlotCount)
        {
            return;
        }

        InventorySlot slot = inventory.Slots[rootIndex];
        if (slot.IsEmpty)
        {
            return;
        }

        draggingRootIndex = rootIndex;
        dropHandledThisDrag = false;
        draggingRotated = slot.IsRotated;
        slotUIs[rootIndex].SetDragging(true);
        ClearDetailDisplay();

        if (dragIconRect != null)
        {
            dragIconRect.gameObject.SetActive(true);
            dragIconRect.SetAsLastSibling();
            dragIconImage.sprite = slot.Item.ItemIcon;
            dragIconImage.enabled = dragIconImage.sprite != null;
            UpdateDragVisualSize();
            dragIconRect.position = eventData.position;
        }
    }

    public void UpdateDrag(PointerEventData eventData)
    {
        if (draggingRootIndex < 0 || dragIconRect == null)
            return;

        dragIconRect.SetAsLastSibling();
        dragIconRect.position = eventData.position;
    }

    public void EndDrag(PointerEventData eventData)
    {
        if (draggingRootIndex < 0)
            return;

        if (!dropHandledThisDrag)
        {
            int slotUnderPointer = ResolveSlotIndexFromPointer(eventData);
            
            if (slotUnderPointer >= 0)
            {
                int rootUnderPointer = inventory.GetRootIndex(slotUnderPointer);
                
                if (rootUnderPointer != draggingRootIndex)
                {
                    bool moveSuccess = inventory.TryMoveItem(draggingRootIndex, slotUnderPointer, draggingRotated != inventory.Slots[draggingRootIndex].IsRotated);
                    if (moveSuccess)
                    {
                        SelectSlot(slotUnderPointer);
                    }
                }
            }
        }

        int finishedDraggingSlot = draggingRootIndex;
        draggingRootIndex = -1;
        dropHandledThisDrag = false;
        draggingRotated = false;

        if (finishedDraggingSlot >= 0 && finishedDraggingSlot < slotUIs.Length)
        {
            slotUIs[finishedDraggingSlot].SetDragging(false);
            slotUIs[finishedDraggingSlot].SetSelected(!inventory.Slots[finishedDraggingSlot].IsEmpty && inventory.Slots[finishedDraggingSlot].RootIndex == selectedRootIndex);
        }

        if (dragIconRect != null)
        {
            dragIconRect.gameObject.SetActive(false);
        }
    }

    public void DropOnSlot(int slotIndex, PointerEventData eventData)
    {
        if (draggingRootIndex < 0 || slotIndex < 0 || slotIndex >= inventory.SlotCount)
        {
            return;
        }

        int targetRoot = inventory.GetRootIndex(slotIndex);
        
        if (targetRoot == draggingRootIndex)
        {
            return;
        }

        bool moveSuccess = inventory.TryMoveItem(draggingRootIndex, slotIndex, draggingRotated != inventory.Slots[draggingRootIndex].IsRotated);
        
        if (moveSuccess)
        {
            dropHandledThisDrag = true;
            SelectSlot(slotIndex);
        }

        RefreshUI(0);
    }

    private void UpdateFootprintOverlays()
    {
        if (footprintOverlayContainer == null || slotUIs == null)
            return;

        List<int> activeRoots = new List<int>();

        for (int i = 0; i < inventory.SlotCount; i++)
        {
            InventorySlot slot = inventory.Slots[i];
            if (slot.IsEmpty || !slot.IsAnchor || i == draggingRootIndex)
                continue;

            activeRoots.Add(i);

            RectTransform overlayRect = GetOrCreateFootprintOverlay(i);
            if (overlayRect == null)
                continue;

            PositionFootprintOverlay(i, slot.Item, overlayRect);
        }

        List<int> rootsToRemove = new List<int>();
        foreach (int rootIndex in footprintOverlays.Keys)
        {
            if (!activeRoots.Contains(rootIndex))
                rootsToRemove.Add(rootIndex);
        }

        foreach (int rootIndex in rootsToRemove)
        {
            if (footprintOverlays.TryGetValue(rootIndex, out RectTransform footprintRect) && footprintRect != null)
                Destroy(footprintRect.gameObject);

            footprintOverlays.Remove(rootIndex);
        }
    }

    private RectTransform GetOrCreateFootprintOverlay(int rootIndex)
    {
        if (footprintOverlays.TryGetValue(rootIndex, out RectTransform footprintRect) && footprintRect != null)
            return footprintRect;

        if (footprintOverlayContainer == null)
            return null;

        GameObject overlayObject = new GameObject($"Footprint_{rootIndex}", typeof(RectTransform), typeof(Image));
        overlayObject.transform.SetParent(footprintOverlayContainer, false);
        overlayObject.transform.SetAsLastSibling();

        RectTransform rectTransform = overlayObject.GetComponent<RectTransform>();
        Image image = overlayObject.GetComponent<Image>();
        image.raycastTarget = false;
        image.preserveAspect = false;

        footprintOverlays[rootIndex] = rectTransform;
        return rectTransform;
    }

    private void PositionFootprintOverlay(int rootIndex, InventoryItem item, RectTransform footprintRect)
    {
        if (item == null || footprintRect == null || rootIndex < 0 || rootIndex >= slotUIs.Length)
            return;

        RectTransform slotRect = slotUIs[rootIndex].SlotRectTransform;
        if (slotRect == null)
            return;

        Vector2 cellSize = slotGridLayout != null ? slotGridLayout.cellSize : slotRect.rect.size;
        Vector2 spacing = slotGridLayout != null ? slotGridLayout.spacing : Vector2.zero;

        bool isRotated = inventory.Slots[rootIndex].IsRotated;
        int itemWidth = isRotated ? item.GridHeight : item.GridWidth;
        int itemHeight = isRotated ? item.GridWidth : item.GridHeight;

        float width = cellSize.x * itemWidth + spacing.x * Mathf.Max(0, itemWidth - 1);
        float height = cellSize.y * itemHeight + spacing.y * Mathf.Max(0, itemHeight - 1);

        footprintRect.SetAsLastSibling();
        footprintRect.sizeDelta = new Vector2(width, height);

        Camera uiCamera = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? rootCanvas.worldCamera : null;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, slotRect.position);
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            footprintOverlayContainer,
            screenPoint,
            uiCamera,
            out localPoint);

        float offsetX = (width - cellSize.x) * 0.5f;
        float offsetY = -(height - cellSize.y) * 0.5f;
        footprintRect.anchoredPosition = localPoint + new Vector2(offsetX, offsetY);

        Image footprintImage = footprintRect.GetComponent<Image>();
        if (footprintImage != null)
        {
            footprintImage.sprite = item.ItemIcon;
            footprintImage.enabled = footprintImage.sprite != null;
        }
    }

    private void CreateFootprintOverlayContainer()
    {
        GameObject overlayObject = new GameObject("FootprintOverlayContainer", typeof(RectTransform));
        overlayObject.transform.SetParent(transform, false);
        overlayObject.transform.SetAsLastSibling();

        footprintOverlayContainer = overlayObject.GetComponent<RectTransform>();
        footprintOverlayContainer.anchorMin = Vector2.zero;
        footprintOverlayContainer.anchorMax = Vector2.one;
        footprintOverlayContainer.offsetMin = Vector2.zero;
        footprintOverlayContainer.offsetMax = Vector2.zero;

        Canvas overlayCanvas = overlayObject.AddComponent<Canvas>();
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 1000;
    }

    private void DestroyFootprintOverlays()
    {
        foreach (RectTransform footprintRect in footprintOverlays.Values)
        {
            if (footprintRect != null)
                Destroy(footprintRect.gameObject);
        }

        footprintOverlays.Clear();

        if (footprintOverlayContainer != null)
            Destroy(footprintOverlayContainer.gameObject);

        footprintOverlayContainer = null;
    }

    private int ResolveSlotIndexFromPointer(PointerEventData eventData)
    {
        if (eventData == null)
        {
            return -1;
        }
        
        GameObject hitObject = eventData.pointerCurrentRaycast.gameObject;
        
        if (hitObject == null)
            return -1;

        InventorySlotUI slotUI = hitObject.GetComponentInParent<InventorySlotUI>();
        int result = slotUI != null ? slotUI.SlotIndex : -1;
        return ResolveSlotIndexFromScreenPosition(eventData.position);
    }

    private int ResolveSlotIndexFromScreenPosition(Vector2 screenPosition)
    {
        if (EventSystem.current == null || rootGraphicRaycaster == null)
            return -1;

        PointerEventData pointerEventData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        raycastResults.Clear();
        rootGraphicRaycaster.Raycast(pointerEventData, raycastResults);

        foreach (RaycastResult result in raycastResults)
        {
            InventorySlotUI slotUI = result.gameObject.GetComponentInParent<InventorySlotUI>();
            if (slotUI != null)
                return slotUI.SlotIndex;
        }

        return -1;
    }

    private PointerEventData CreatePointerEventData(Vector2 screenPosition)
    {
        PointerEventData pointerEventData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        int slotIndex = ResolveSlotIndexFromScreenPosition(screenPosition);
        if (slotIndex >= 0)
            pointerEventData.pointerCurrentRaycast = new RaycastResult { gameObject = slotUIs[slotIndex].gameObject };

        return pointerEventData;
    }

    private void UpdateDetails(InventorySlot slot)
    {
        if (draggingRootIndex >= 0)
        {
            ClearDetailDisplay();
            return;
        }

        if (slot.IsEmpty)
        {
            ClearDetailDisplay();
            return;
        }

        itemDetailIcon.sprite = slot.Item.ItemIcon;
        itemDetailIcon.enabled = itemDetailIcon.sprite != null;
        itemDetailName.text = slot.Item.ItemName;
        itemDetailDescription.text = slot.Item.ItemDescription;
        itemDetailQuantity.text = slot.Quantity.ToString();
    }

    private void ClearDetailDisplay()
    {
        itemDetailIcon.sprite = null;
        itemDetailIcon.enabled = false;
        itemDetailName.text = string.Empty;
        itemDetailDescription.text = string.Empty;
        itemDetailQuantity.text = string.Empty;
    }

    private void CreateDragVisual()
    {
        DestroyDragVisual();

        Transform parent = transform;
        GameObject dragVisual = new GameObject("InventoryDragVisual", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        dragVisual.transform.SetParent(parent, false);
        dragVisual.transform.SetAsLastSibling();

        Canvas dragCanvas = dragVisual.AddComponent<Canvas>();
        dragCanvas.overrideSorting = true;
        dragCanvas.sortingOrder = 5000;
        dragCanvas.sortingLayerID = rootCanvas != null ? rootCanvas.sortingLayerID : 0;

        dragIconRect = dragVisual.GetComponent<RectTransform>();
        dragIconRect.sizeDelta = new Vector2(72f, 72f);
        dragIconImage = dragVisual.GetComponent<Image>();
        dragIconImage.raycastTarget = false;
        dragIconImage.preserveAspect = true;
        dragIconCanvasGroup = dragVisual.GetComponent<CanvasGroup>();
        dragIconCanvasGroup.blocksRaycasts = false;
        dragIconCanvasGroup.alpha = 0.85f;
        dragVisual.SetActive(false);
    }

    private void UpdateDragVisualSize()
    {
        if (dragIconRect == null || draggingRootIndex < 0 || draggingRootIndex >= inventory.SlotCount)
            return;

        InventorySlot draggingSlot = inventory.Slots[draggingRootIndex];
        if (draggingSlot.IsEmpty || draggingSlot.Item == null)
            return;

        Vector2 cellSize = slotGridLayout != null ? slotGridLayout.cellSize : new Vector2(72f, 72f);
        Vector2 spacing = slotGridLayout != null ? slotGridLayout.spacing : Vector2.zero;

        int itemWidth = draggingRotated ? draggingSlot.Item.GridHeight : draggingSlot.Item.GridWidth;
        int itemHeight = draggingRotated ? draggingSlot.Item.GridWidth : draggingSlot.Item.GridHeight;

        float width = cellSize.x * itemWidth + spacing.x * Mathf.Max(0, itemWidth - 1);
        float height = cellSize.y * itemHeight + spacing.y * Mathf.Max(0, itemHeight - 1);
        dragIconRect.sizeDelta = new Vector2(width, height);
    }

    private void DestroyDragVisual()
    {
        if (dragIconRect != null)
        {
            Destroy(dragIconRect.gameObject);
        }

        dragIconRect = null;
        dragIconImage = null;
        dragIconCanvasGroup = null;
    }
}
