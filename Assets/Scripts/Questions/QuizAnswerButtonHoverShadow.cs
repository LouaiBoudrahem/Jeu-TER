using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class QuizAnswerButtonHoverShadow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private TMP_Text tmpText;
    private Text uiText;
    private Outline uiOutline;

    private float outlineWidthOnHover = 0.25f;
    private float baseTmpOutlineWidth;
    private Color baseTextColor;

    public void Configure(float hoverOutlineWidth)
    {
        outlineWidthOnHover = Mathf.Clamp01(hoverOutlineWidth);
        CacheReferences();
        SetGlow(false);
    }

    private void Awake()
    {
        CacheReferences();
        SetGlow(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetGlow(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetGlow(false);
    }

    private void CacheReferences()
    {
        if (tmpText == null)
        {
            tmpText = GetComponentInChildren<TMP_Text>(true);
            if (tmpText != null)
            {
                baseTmpOutlineWidth = tmpText.outlineWidth;
                baseTextColor = tmpText.color;
            }
        }

        if (uiText == null)
        {
            uiText = GetComponentInChildren<Text>(true);
            if (uiText != null)
            {
                baseTextColor = uiText.color;
                uiOutline = uiText.GetComponent<Outline>();
                if (uiOutline == null)
                {
                    uiOutline = uiText.gameObject.AddComponent<Outline>();
                    uiOutline.effectColor = new Color(1f, 1f, 1f, 0.8f);
                    uiOutline.effectDistance = new Vector2(2f, 2f);
                }
            }
        }
    }

    private void SetGlow(bool enabled)
    {
        if (tmpText != null)
        {
            tmpText.outlineWidth = enabled ? outlineWidthOnHover : baseTmpOutlineWidth;
            tmpText.color = enabled ? Color.white : baseTextColor;
        }

        if (uiText != null)
        {
            uiText.color = enabled ? Color.white : baseTextColor;
        }

        if (uiOutline != null)
        {
            uiOutline.enabled = enabled;
        }
    }
}
