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

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip buttonClickClip;
    [SerializeField] private AudioClip wrongCodeClip;
    [SerializeField] private AudioClip correctCodeClipA;
    [SerializeField] private AudioClip correctCodeClipB;
    [Header("Indicators")]
    [SerializeField] private CanvasGroup correctIndicator;
    [SerializeField] private CanvasGroup wrongIndicator;
    [SerializeField, Min(1)] private int indicatorFlashCount = 2;
    [SerializeField, Min(0f)] private float indicatorFlashDuration = 0.25f;
    [SerializeField, Min(0f)] private float indicatorFadeTime = 0.08f;
    private Coroutine indicatorRoutine;

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

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (correctIndicator != null)
        {
            correctIndicator.alpha = 0f;
            correctIndicator.gameObject.SetActive(false);
        }

        if (wrongIndicator != null)
        {
            wrongIndicator.alpha = 0f;
            wrongIndicator.gameObject.SetActive(false);
        }
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

        if (buttonClickClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(buttonClickClip);
        }

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

            if (wrongCodeClip != null && audioSource != null)
            {
                audioSource.PlayOneShot(wrongCodeClip);
            }

            if (indicatorRoutine != null)
            {
                StopCoroutine(indicatorRoutine);
                indicatorRoutine = null;
            }

            if (wrongIndicator != null)
            {
                indicatorRoutine = StartCoroutine(FlashCanvasGroup(wrongIndicator));
            }

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

        if (indicatorRoutine != null)
        {
            StopCoroutine(indicatorRoutine);
            indicatorRoutine = null;
        }

        if (correctIndicator != null)
        {
            indicatorRoutine = StartCoroutine(FlashCanvasGroup(correctIndicator));
        }

        if (correctCodeClipA != null && audioSource != null)
        {
            audioSource.PlayOneShot(correctCodeClipA);
            yield return new WaitForSeconds(Mathf.Max(0.05f, correctCodeClipA.length));
        }

        if (correctCodeClipB != null && audioSource != null)
        {
            audioSource.PlayOneShot(correctCodeClipB);
            yield return new WaitForSeconds(Mathf.Max(0.05f, correctCodeClipB.length));
        }

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

    private IEnumerator FlashCanvasGroup(CanvasGroup cg)
    {
        if (cg == null) yield break;
        cg.alpha = 0f;
        cg.gameObject.SetActive(true);
        for (int i = 0; i < indicatorFlashCount; i++)
        {
            float t = 0f;
            while (t < indicatorFadeTime)
            {
                t += Time.deltaTime;
                cg.alpha = Mathf.Lerp(0f, 1f, t / Mathf.Max(0.0001f, indicatorFadeTime));
                yield return null;
            }
            cg.alpha = 1f;
            yield return new WaitForSeconds(indicatorFlashDuration);
            t = 0f;
            while (t < indicatorFadeTime)
            {
                t += Time.deltaTime;
                cg.alpha = Mathf.Lerp(1f, 0f, t / Mathf.Max(0.0001f, indicatorFadeTime));
                yield return null;
            }
            cg.alpha = 0f;
            yield return new WaitForSeconds(0.05f);
        }
        cg.gameObject.SetActive(false);
        indicatorRoutine = null;
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