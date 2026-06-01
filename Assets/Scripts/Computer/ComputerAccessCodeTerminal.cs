using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class ComputerAccessCodeTerminal : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject codeEntryGroup;
    [SerializeField] private TMP_InputField accessCodeInput;
    [SerializeField] private Button submitButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private TMP_Text feedbackText;

    [Header("Image")]
    [SerializeField] private GameObject imageGroup;
    [SerializeField] private Image accessGrantedImage;
    [SerializeField] private TMP_Text imageCaptionText;

    [Header("Shuffled Labels")]
    [SerializeField] private TMP_Text[] imageLabels = new TMP_Text[5];
    [SerializeField] private bool randomizeLabelsOnUnlock = true;
    [SerializeField] private bool randomizeSiblingOrder = false;
    [Header("Assign To Computers")]
    [Tooltip("Optional: assign the target Computer objects in the same order as the imageLabels array. When the terminal unlocks, each computer will receive the number shown on the corresponding label.")]
    [SerializeField] private Computer[] targetComputers = new Computer[5];

    private string accessCode;
    private string accessCodePrompt = "Enter access code";
    private string accessGrantedMessage = "Access granted";
    private string accessDeniedMessage = "Incorrect access code";
    private Action closedCallback;
    private bool isOpen;
    private bool isUnlocked;
    private InputAction cancelAction;
    [SerializeField] private float successCloseDelay = 0.75f;
    private Coroutine closeAfterSuccessRoutine;

    private void Awake()
    {
        if (accessCodeInput == null)
        {
            accessCodeInput = GetComponentInChildren<TMP_InputField>(true);
        }

        if (submitButton == null)
        {
            submitButton = GetComponentInChildren<Button>(true);
        }

        if (accessGrantedImage == null)
        {
            accessGrantedImage = GetComponentInChildren<Image>(true);
        }

        SetUnlockedState(false);
        SetPrompt(accessCodePrompt);
        SetFeedback(string.Empty, false);
        gameObject.SetActive(false);

        cancelAction = new InputAction("AccessCancel", InputActionType.Button, "<Keyboard>/escape");
        cancelAction.performed += OnCancelPerformed;
    }

    private void OnEnable()
    {
        BindUI();
    }

    private void OnDisable()
    {
        UnbindUI();
        isOpen = false;
        closedCallback = null;

        if (closeAfterSuccessRoutine != null)
        {
            StopCoroutine(closeAfterSuccessRoutine);
            closeAfterSuccessRoutine = null;
        }
    }

    private void Update()
    {
        if (!isOpen)
        {
            return;
        }

        if (IsEscapePressed())
        {
            CloseTerminal();
        }
    }

    public void Begin(string code, Image grantedImage, string prompt, string successMessage, string failureMessage, Action onClosed)
    {
        accessCode = code ?? string.Empty;
        if (accessGrantedImage != null)
        {
            accessGrantedImage.sprite = grantedImage != null ? grantedImage.sprite : null;
            if (!isUnlocked)
            {
                accessGrantedImage.gameObject.SetActive(false);
                accessGrantedImage.enabled = false;
            }
        }

        accessCodePrompt = string.IsNullOrWhiteSpace(prompt) ? "Enter access code" : prompt;
        accessGrantedMessage = string.IsNullOrWhiteSpace(successMessage) ? "Access granted" : successMessage;
        accessDeniedMessage = string.IsNullOrWhiteSpace(failureMessage) ? "Incorrect access code" : failureMessage;
        closedCallback = onClosed;
        isOpen = true;

        EnsureEventSystem();
        EnsureCanvasCanReceiveClicks();
        SetUnlockedState(isUnlocked);
        SetPrompt(accessCodePrompt);
        SetFeedback(string.Empty, false);

        if (accessCodeInput != null)
        {
            accessCodeInput.text = string.Empty;
            accessCodeInput.ActivateInputField();
            accessCodeInput.Select();
        }

        cancelAction?.Enable();

        if (EventSystem.current != null && accessCodeInput != null)
        {
            EventSystem.current.SetSelectedGameObject(accessCodeInput.gameObject);
        }
    }

    public void SubmitAccessCode()
    {
        if (!isOpen || isUnlocked || accessCodeInput == null)
        {
            return;
        }

        string enteredCode = accessCodeInput.text != null ? accessCodeInput.text.Trim() : string.Empty;
        if (string.Equals(enteredCode, accessCode, StringComparison.Ordinal))
        {
            HandleSuccess();
        }
        else
        {
            SetFeedback(accessDeniedMessage, false);
            accessCodeInput.text = string.Empty;
            accessCodeInput.ActivateInputField();
        }
    }

    public void CloseTerminal()
    {
        if (!isOpen)
        {
            return;
        }

        isOpen = false;
        SetFeedback(string.Empty, false);
        closedCallback?.Invoke();
        closedCallback = null;

        cancelAction?.Disable();
    }

    private void HandleSuccess()
    {
        isUnlocked = true;
        if (randomizeLabelsOnUnlock)
        {
            ShuffleLabels();
        }

        SetFeedback(accessGrantedMessage, true);
        SetUnlockedState(true);
        SetPrompt(string.Empty);

        if (accessCodeInput != null)
        {
            accessCodeInput.DeactivateInputField();
        }

        if (closeAfterSuccessRoutine != null)
        {
            StopCoroutine(closeAfterSuccessRoutine);
        }

        closeAfterSuccessRoutine = StartCoroutine(CloseAfterSuccessRoutine());
    }

    private System.Collections.IEnumerator CloseAfterSuccessRoutine()
    {
        if (successCloseDelay > 0f)
        {
            yield return new WaitForSeconds(successCloseDelay);
        }

        CloseTerminal();
        closeAfterSuccessRoutine = null;
    }

    private void ShuffleLabels()
    {
        if (imageLabels == null || imageLabels.Length == 0)
            return;

        int n = imageLabels.Length;

        List<int> numbers = new List<int>(n);
        for (int i = 1; i <= n; i++) numbers.Add(i);

        for (int i = numbers.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            int tmp = numbers[i];
            numbers[i] = numbers[j];
            numbers[j] = tmp;
        }


        for (int i = 0; i < n; i++)
        {
            TMP_Text lbl = imageLabels[i];
            if (lbl != null)
            {
                lbl.text = numbers[i].ToString();
            }
        }

        ComputerSolveOrderState.SetOrder(numbers);

        if (targetComputers != null && targetComputers.Length > 0)
        {
            int m = Mathf.Min(numbers.Count, targetComputers.Length);
            for (int i = 0; i < m; i++)
            {
                Computer c = targetComputers[i];
                if (c == null)
                    continue;

                c.ApplySolveNumber(numbers[i]);
            }
        }

        if (randomizeSiblingOrder)
        {

            List<int> indices = new List<int>(n);
            for (int i = 0; i < n; i++) indices.Add(i);
            for (int i = indices.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                int tmp = indices[i];
                indices[i] = indices[j];
                indices[j] = tmp;
            }

            for (int i = 0; i < n; i++)
            {
                int srcIdx = indices[i];
                TMP_Text lbl = imageLabels[srcIdx];
                if (lbl != null)
                {
                    lbl.transform.SetSiblingIndex(i);
                }
            }
        }
    }

    private void SetUnlockedState(bool unlocked)
    {
        if (codeEntryGroup != null)
        {
            codeEntryGroup.SetActive(!unlocked);
        }

        if (imageGroup != null)
        {
            imageGroup.SetActive(unlocked);
        }

        if (accessGrantedImage != null)
        {
            accessGrantedImage.gameObject.SetActive(unlocked);
            accessGrantedImage.enabled = unlocked;
        }

        if (imageCaptionText != null)
        {
            imageCaptionText.text = unlocked ? accessGrantedMessage : string.Empty;
        }
    }

    private void SetPrompt(string message)
    {
        if (promptText != null)
        {
            promptText.text = message;
        }
    }

    private void SetFeedback(string message, bool isSuccess)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
            feedbackText.color = isSuccess ? new Color(0.35f, 1f, 0.45f) : Color.white;
        }
    }

    private void BindUI()
    {
        if (accessCodeInput != null)
        {
            accessCodeInput.onSubmit.AddListener(HandleSubmit);
        }

        if (submitButton != null)
        {
            submitButton.onClick.AddListener(SubmitAccessCode);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseTerminal);
        }
    }

    private void UnbindUI()
    {
        if (accessCodeInput != null)
        {
            accessCodeInput.onSubmit.RemoveListener(HandleSubmit);
        }

        if (submitButton != null)
        {
            submitButton.onClick.RemoveListener(SubmitAccessCode);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseTerminal);
        }
    }

    private void HandleSubmit(string _)
    {
        SubmitAccessCode();
    }

    private static bool IsEscapePressed()
    {
        return (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame);
    }

    private void OnCancelPerformed(InputAction.CallbackContext ctx)
    {
        if (isOpen)
        {
            CloseTerminal();
        }
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem (Auto)");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
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

        if (canvas.worldCamera != null && canvas.worldCamera.isActiveAndEnabled)
        {
            return;
        }

        Camera fallbackCamera = Camera.main;
        if (fallbackCamera == null || !fallbackCamera.isActiveAndEnabled)
        {
            Camera[] allCameras = Camera.allCameras;
            for (int i = 0; i < allCameras.Length; i++)
            {
                Camera currentCamera = allCameras[i];
                if (currentCamera != null && currentCamera.isActiveAndEnabled)
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
    }
}