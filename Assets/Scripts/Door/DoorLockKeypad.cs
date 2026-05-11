using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DoorLockKeypad : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text codeText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private string placeholderCharacter = "_";
    [SerializeField] private string separator = " ";

    [Header("Code")]
    [SerializeField, Min(1)] private int codeLength = 8;
    [SerializeField] private string accessCode = "12345678";
    [SerializeField] private string idleMessage = "Enter code";
    [SerializeField] private string successMessage = "Access granted";
    [SerializeField] private string failureMessage = "Incorrect code";

    [Header("Door")]
    [SerializeField] private DoorController doorController;
    [SerializeField, Min(0f)] private float successCloseDelay = 0.5f;
    [SerializeField, Min(0f)] private float failureFeedbackDuration = 0.4f;

    private readonly StringBuilder enteredCode = new StringBuilder();
    private Coroutine feedbackRoutine;
    private Action closedCallback;
    private bool isOpen;
    private bool isSubmitting;

    private void Awake()
    {
        RefreshDisplay();
        SetFeedback(idleMessage, false);
    }

    private void OnDisable()
    {
        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
            feedbackRoutine = null;
        }

        enteredCode.Clear();
        isOpen = false;
        isSubmitting = false;
    }

    private void Update()
    {
        if (!isOpen)
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseKeypad();
        }
    }

    public void Begin(Player interactingPlayer, Action onClosed)
    {
        closedCallback = onClosed;
        isOpen = true;
        isSubmitting = false;
        enteredCode.Clear();

        EnsureCanvasCanReceiveClicks();

        RefreshDisplay();
        SetFeedback(idleMessage, false);
    }

    public void PressSymbol(string symbol)
    {
        if (!isOpen || isSubmitting || string.IsNullOrEmpty(symbol))
        {
            return;
        }

        if (symbol.Length != 1)
        {
            symbol = symbol.Substring(0, 1);
        }

        if (enteredCode.Length >= codeLength)
        {
            return;
        }

        enteredCode.Append(symbol[0]);
        RefreshDisplay();

        if (enteredCode.Length >= codeLength)
        {
            SubmitCode();
        }
    }

    public void CloseKeypad()
    {
        if (!isOpen)
        {
            return;
        }

        isOpen = false;
        isSubmitting = false;

        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
            feedbackRoutine = null;
        }

        enteredCode.Clear();
        RefreshDisplay();
        SetFeedback(string.Empty, false);

        closedCallback?.Invoke();
        closedCallback = null;
    }

    private void SubmitCode()
    {
        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
            feedbackRoutine = null;
        }

        bool codeMatches = !string.IsNullOrEmpty(accessCode) && string.Equals(enteredCode.ToString(), accessCode, StringComparison.Ordinal);

        if (codeMatches)
        {
            feedbackRoutine = StartCoroutine(SuccessRoutine());
        }
        else
        {
            enteredCode.Clear();
            RefreshDisplay();
            SetFeedback(failureMessage, false);

            if (failureFeedbackDuration > 0f)
            {
                feedbackRoutine = StartCoroutine(RestoreIdleFeedbackRoutine());
            }
            else
            {
                SetFeedback(idleMessage, false);
            }
        }
    }

    private IEnumerator SuccessRoutine()
    {
        isSubmitting = true;
        SetFeedback(successMessage, true);

        if (doorController != null)
        {
            doorController.OpenDoor();
        }
        else
        {
            Debug.LogWarning($"DoorLockKeypad on '{name}': doorController is not assigned.");
        }

        yield return new WaitForSeconds(successCloseDelay);
        feedbackRoutine = null;
        CloseKeypad();
    }

    private IEnumerator RestoreIdleFeedbackRoutine()
    {
        yield return new WaitForSeconds(failureFeedbackDuration);
        SetFeedback(idleMessage, false);
        feedbackRoutine = null;
    }

    private void RefreshDisplay()
    {
        if (codeText == null)
        {
            return;
        }

        StringBuilder displayBuilder = new StringBuilder();

        for (int i = 0; i < codeLength; i++)
        {
            if (i > 0)
            {
                displayBuilder.Append(separator);
            }

            if (i < enteredCode.Length)
            {
                displayBuilder.Append(enteredCode[i]);
            }
            else
            {
                displayBuilder.Append(placeholderCharacter);
            }
        }

        codeText.text = displayBuilder.ToString();
    }

    private void SetFeedback(string message, bool isSuccess)
    {
        if (feedbackText == null)
        {
            return;
        }

        feedbackText.text = message;
        feedbackText.color = isSuccess ? new Color(0.35f, 1f, 0.45f) : Color.white;
    }

    private void EnsureCanvasCanReceiveClicks()
    {
        Canvas canvas = GetComponentInParent<Canvas>(true);
        if (canvas == null)
        {
            return;
        }

        if (canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        if (canvas.renderMode != RenderMode.WorldSpace)
        {
            return;
        }

        if (canvas.worldCamera != null)
        {
            return;
        }

        Camera fallbackCamera = Camera.main;
        if (fallbackCamera == null || !fallbackCamera.enabled)
        {
            Camera[] allCameras = Camera.allCameras;
            for (int i = 0; i < allCameras.Length; i++)
            {
                Camera currentCamera = allCameras[i];
                if (currentCamera != null && currentCamera.enabled && currentCamera.gameObject.activeInHierarchy)
                {
                    fallbackCamera = currentCamera;
                    break;
                }
            }
        }

        if (fallbackCamera != null)
        {
            canvas.worldCamera = fallbackCamera;
        }
        else
        {
            Debug.LogWarning("DoorLockKeypad: no enabled camera found for world-space canvas event camera.");
        }
    }
}