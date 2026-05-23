using System.Collections;
using TMPro;
using UnityEngine;

public class TransientDebugConsoleUI : MonoBehaviour
{
    private static TransientDebugConsoleUI instance;

    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text messageText;

    [Header("Timing")]
    [SerializeField, Min(0.1f)] private float displaySeconds = 2.5f;

    [Header("Colors")]
    [SerializeField] private Color logColor = Color.white;
    [SerializeField] private Color warningColor = new Color(1f, 0.85f, 0.35f);
    [SerializeField] private Color errorColor = new Color(1f, 0.35f, 0.35f);

    private Coroutine hideRoutine;

    private void Awake()
    {
        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }

        if (messageText == null)
        {
            messageText = GetComponentInChildren<TMP_Text>(true);
        }

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        if (instance == null)
        {
            instance = this;
        }
    }

    private void OnEnable()
    {
        instance = this;
    }

    private void OnDisable()
    {
        if (instance == this)
        {
            instance = null;
        }

        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
            hideRoutine = null;
        }
    }

    public static void Log(string message)
    {
        UnityEngine.Debug.Log(message);
        Show(message, LogType.Log);
    }

    public static void LogWarning(string message)
    {
        UnityEngine.Debug.LogWarning(message);
        Show(message, LogType.Warning);
    }

    public static void LogError(string message)
    {
        UnityEngine.Debug.LogError(message);
        Show(message, LogType.Error);
    }

    private static void Show(string message, LogType logType)
    {
        TransientDebugConsoleUI ui = GetInstance();
        if (ui != null)
        {
            ui.ShowMessage(message, logType);
        }
    }

    private static TransientDebugConsoleUI GetInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        instance = FindObjectOfType<TransientDebugConsoleUI>(true);
        return instance;
    }

    private void ShowMessage(string message, LogType logType)
    {
        if (panelRoot == null || messageText == null)
        {
            return;
        }

        messageText.text = message ?? string.Empty;
        messageText.color = GetColor(logType);
        panelRoot.SetActive(true);

        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
        }

        hideRoutine = StartCoroutine(HideAfterDelayRoutine());
    }

    private IEnumerator HideAfterDelayRoutine()
    {
        yield return new WaitForSeconds(displaySeconds);

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        hideRoutine = null;
    }

    private Color GetColor(LogType logType)
    {
        switch (logType)
        {
            case LogType.Warning:
                return warningColor;
            case LogType.Error:
            case LogType.Exception:
                return errorColor;
            default:
                return logColor;
        }
    }
}