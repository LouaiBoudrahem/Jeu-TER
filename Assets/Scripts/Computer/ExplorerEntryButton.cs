using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ExplorerEntryButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text labelText;

    private Sprite defaultIconSprite;

    private void Reset()
    {
        button = GetComponent<Button>();
        iconImage = GetComponentInChildren<Image>();
        labelText = GetComponentInChildren<TMP_Text>();
    }

    private void Awake()
    {
        if (iconImage != null)
            defaultIconSprite = iconImage.sprite;
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveAllListeners();
    }

    public void Setup(FolderData folder, Action<FolderData> onClick)
    {
        if (labelText != null) labelText.text = folder.displayName;
        if (iconImage != null)
            iconImage.sprite = folder.icon != null ? folder.icon : defaultIconSprite;
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(folder));
        }
    }

    public void Setup(FileData file, Action<FileData> onClick)
    {
        if (labelText != null) labelText.text = file.displayName;
        if (iconImage != null)
            iconImage.sprite = file.icon != null ? file.icon : defaultIconSprite;
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(file));
        }
    }
}
