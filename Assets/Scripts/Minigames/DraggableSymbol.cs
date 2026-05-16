using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))]
public class DraggableSymbol : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public MatrixMinigame.Symbol symbol = MatrixMinigame.Symbol.None;

    private Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private Vector2 originalAnchoredPos;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private Canvas FindCanvas()
    {
        if (canvas != null) return canvas;
        canvas = GetComponentInParent<Canvas>();
        return canvas;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        originalAnchoredPos = rectTransform.anchoredPosition;
        var c = FindCanvas();
        if (c != null)
            transform.SetParent(c.transform, true);
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.7f;
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        var c = FindCanvas();
        if (c == null) return;
        rectTransform.anchoredPosition += eventData.delta / c.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;
        if (transform.parent == FindCanvas().transform)
        {
            transform.SetParent(originalParent, true);
            rectTransform.anchoredPosition = originalAnchoredPos;
        }
    }
}
