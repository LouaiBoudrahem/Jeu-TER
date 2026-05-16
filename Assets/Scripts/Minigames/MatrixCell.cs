using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class MatrixCell : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public int row;
    public int col;

    public Image iconImage;
    [Header("Highlight")]
    public Image backgroundImage; 
    public Color highlightColor = new Color(1f, 1f, 0f, 0.25f);

    private Color originalBgColor;

    private Button button;
    private MatrixMinigame manager;
    private MatrixMinigame.Symbol currentSymbol = MatrixMinigame.Symbol.None;

    public void Init(int r, int c, MatrixMinigame mgr)
    {
        row = r;
        col = c;
        manager = mgr;
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }
        if (backgroundImage != null)
            originalBgColor = backgroundImage.color;
        UpdateVisual();
    }

    private void OnClick()
    {
        manager?.PlaceSymbol(row, col, MatrixMinigame.Symbol.None);
    }

    public void SetSymbol(MatrixMinigame.Symbol sym, Sprite sprite)
    {
        currentSymbol = sym;
        if (iconImage != null)
        {
            iconImage.sprite = sprite;
            iconImage.enabled = sprite != null;
        }
        var bgImage = GetComponent<Image>();
        if (sprite == null && bgImage != null)
        {
            bgImage.color = Color.white;
            bgImage.enabled = true;
        }
    }

    private void UpdateVisual()
    {
        if (iconImage != null)
            iconImage.enabled = iconImage.sprite != null;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (manager == null) return;
        if (eventData == null || eventData.pointerDrag == null) return;
        var draggable = eventData.pointerDrag.GetComponent<DraggableSymbol>();
        if (draggable != null)
        {
            manager.PlaceSymbol(row, col, draggable.symbol);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (backgroundImage == null) return;
        if (eventData != null && eventData.pointerDrag != null && eventData.pointerDrag.GetComponent<DraggableSymbol>() != null)
        {
            backgroundImage.color = highlightColor;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (backgroundImage == null) return;
        backgroundImage.color = originalBgColor;
    }
}
