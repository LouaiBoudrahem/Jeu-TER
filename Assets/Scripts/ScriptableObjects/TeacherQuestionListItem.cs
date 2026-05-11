using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TeacherQuestionListItem : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text label;

    private QuestionRecord boundRecord;
    private System.Action<QuestionRecord> clickHandler;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (label == null)
        {
            label = GetComponentInChildren<TMP_Text>(true);
        }
    }

    public void Bind(QuestionRecord record, System.Action<QuestionRecord> onClick)
    {
        boundRecord = record;
        clickHandler = onClick;

        if (label != null)
        {
            string questionText = record != null ? record.question : string.Empty;
            if (!string.IsNullOrWhiteSpace(questionText) && questionText.Length > 60)
            {
                questionText = questionText.Substring(0, 60) + "...";
            }

            label.text = record == null
                ? "<empty>"
                : $"[{record.subject}] {questionText}";
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);
        }
    }

    private void HandleClick()
    {
        clickHandler?.Invoke(boundRecord);
    }
}