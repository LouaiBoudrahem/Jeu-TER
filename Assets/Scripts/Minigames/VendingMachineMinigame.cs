using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VendingMachineMinigame : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text pseudoCodeText;
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private TMP_InputField answerInput;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private Button submitButton;
    [SerializeField] private Button closeButton;

    [Header("Generation")]
    [SerializeField, Min(1)] private int minLoopCount = 3;
    [SerializeField, Min(1)] private int maxLoopCount = 6;
    [SerializeField, Min(0)] private int minStartValue = 1;
    [SerializeField, Min(0)] private int maxStartValue = 4;
    [SerializeField, Min(1)] private int minMultiplier = 2;
    [SerializeField, Min(1)] private int maxMultiplier = 4;
    [SerializeField, Min(0)] private int minAddition = 1;
    [SerializeField, Min(0)] private int maxAddition = 3;

    [Header("Outcome")]
    [SerializeField] private float successCloseDelay = 1.5f;
    [SerializeField] private string successMessage = "Correct. The key has been dispensed.";
    [SerializeField] private string failureMessage = "Incorrect value. Try again.";
    [SerializeField] private string inventoryFullMessage = "Correct answer, but your inventory is full.";
    [SerializeField] private string promptMessage = "Enter the final value of x, then submit it.";
    [SerializeField] private bool autoStartInStandalonePlayMode = true;

    private Action<bool> onClosed;
    private bool rewardWasGiven = false;
    private Player activePlayer;
    private InventoryItem rewardItem;
    private int rewardQuantity = 1;
    private bool isActive;
    private bool isClosing;
    private bool hasSolved;
    private long expectedAnswer;
    private string generatedPseudoCode;
    private Coroutine closeRoutine;

    private void Start()
    {
        EnsureEventSystemExists();

        if (!isActive && autoStartInStandalonePlayMode)
        {
            StartStandalonePreview();
        }
    }

    private void Awake()
    {
        EnsureEventSystemExists();
        EnsureCanvasCanReceiveClicks();
        AutoBindUIReferences();
        EnsureCanvasOnTopAndInteractive();

        if (feedbackText != null)
        {
            feedbackText.text = string.Empty;
            feedbackText.gameObject.SetActive(false);
        }

        if (instructionText != null)
        {
            instructionText.text = promptMessage;
        }
    }

    private void OnEnable()
    {
        if (submitButton != null)
        {
            submitButton.onClick.AddListener(SubmitAnswer);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseMinigame);
        }

        if (answerInput != null)
        {
            answerInput.onSubmit.AddListener(HandleInputSubmitted);
        }
    }

    private void OnDisable()
    {
        if (submitButton != null)
        {
            submitButton.onClick.RemoveListener(SubmitAnswer);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseMinigame);
        }

        if (answerInput != null)
        {
            answerInput.onSubmit.RemoveListener(HandleInputSubmitted);
        }

        if (closeRoutine != null)
        {
            StopCoroutine(closeRoutine);
            closeRoutine = null;
        }
    }

    private void Update()
    {
        if (!isActive || isClosing)
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseMinigame();
        }
    }

    public void Begin(Player interactingPlayer, Action<bool> closedCallback, InventoryItem reward, int quantity)
    {
        EnsureCanvasOnTopAndInteractive();
        if (EventSystem.current != null && answerInput != null)
        {
            EventSystem.current.SetSelectedGameObject(answerInput.gameObject);
        }
        EnsureCanvasCanReceiveClicks();
        AutoBindUIReferences();

        activePlayer = interactingPlayer;
        onClosed = closedCallback;
        rewardItem = reward;
        rewardQuantity = Mathf.Max(1, quantity);
        isActive = true;
        isClosing = false;
        hasSolved = false;

        GenerateChallenge();
        RenderChallenge();
        SetFeedback(promptMessage, false);

        if (answerInput != null)
        {
            answerInput.text = string.Empty;
            answerInput.ActivateInputField();
            answerInput.Select();
        }

        if (submitButton != null)
        {
            submitButton.interactable = true;
        }

        if (closeButton != null)
        {
            closeButton.interactable = true;
        }
    }

    private void StartStandalonePreview()
    {
        EnsureCanvasOnTopAndInteractive();
        if (EventSystem.current != null && answerInput != null)
        {
            EventSystem.current.SetSelectedGameObject(answerInput.gameObject);
        }
        EnsureCanvasCanReceiveClicks();
        AutoBindUIReferences();

        rewardItem = null;
        rewardQuantity = 1;
        onClosed = null;
        activePlayer = null;
        isActive = true;
        isClosing = false;
        hasSolved = false;

        GenerateChallenge();
        RenderChallenge();
        SetFeedback(promptMessage, false);

        if (answerInput != null)
        {
            answerInput.text = string.Empty;
            answerInput.interactable = true;
            answerInput.ActivateInputField();
            answerInput.Select();
        }

        if (submitButton != null)
        {
            submitButton.interactable = true;
        }

        if (closeButton != null)
        {
            closeButton.interactable = true;
        }
    }

    public void SubmitAnswer()
    {
        if (!isActive || isClosing || hasSolved)
        {
            return;
        }

        if (answerInput == null)
        {
            Debug.LogWarning("VendingMachineMinigame: answerInput is not assigned.");
            return;
        }

        string trimmedInput = answerInput.text != null ? answerInput.text.Trim() : string.Empty;
        if (!long.TryParse(trimmedInput, out long submittedAnswer))
        {
            SetFeedback("Enter a numeric value.", false);
            return;
        }

        if (submittedAnswer != expectedAnswer)
        {
            SetFeedback(failureMessage, false);
            return;
        }

        hasSolved = true;
        if (submitButton != null)
        {
            submitButton.interactable = false;
        }

        if (answerInput != null)
        {
            answerInput.interactable = false;
        }

        bool rewardAdded = rewardItem == null || InventoryManager.AddItem(rewardItem, rewardQuantity);
        rewardWasGiven = rewardAdded;
        if (!rewardAdded)
        {
            SetFeedback(inventoryFullMessage, false);
            hasSolved = false;

            if (submitButton != null)
            {
                submitButton.interactable = true;
            }

            if (answerInput != null)
            {
                answerInput.interactable = true;
                answerInput.ActivateInputField();
            }

            return;
        }

        SetFeedback(successMessage, true);

        if (activePlayer != null)
        {
            activePlayer.ShowInteractionMessage(successMessage);
        }

        ObjectiveManager.Instance?.CompleteVendingMachine();

        if (closeRoutine != null)
        {
            StopCoroutine(closeRoutine);
        }

        closeRoutine = StartCoroutine(CloseAfterDelay(successCloseDelay));
    }

    private void HandleInputSubmitted(string _)
    {
        SubmitAnswer();
    }

    private void GenerateChallenge()
    {
        int resolvedMinLoop = Mathf.Min(minLoopCount, maxLoopCount);
        int resolvedMaxLoop = Mathf.Max(minLoopCount, maxLoopCount);
        int resolvedMinStart = Mathf.Min(minStartValue, maxStartValue);
        int resolvedMaxStart = Mathf.Max(minStartValue, maxStartValue);
        int resolvedMinMultiplier = Mathf.Min(minMultiplier, maxMultiplier);
        int resolvedMaxMultiplier = Mathf.Max(minMultiplier, maxMultiplier);
        int resolvedMinAddition = Mathf.Min(minAddition, maxAddition);
        int resolvedMaxAddition = Mathf.Max(minAddition, maxAddition);

        int loopCount = UnityEngine.Random.Range(resolvedMinLoop, resolvedMaxLoop + 1);
        int startValue = UnityEngine.Random.Range(resolvedMinStart, resolvedMaxStart + 1);
        int multiplier = UnityEngine.Random.Range(resolvedMinMultiplier, resolvedMaxMultiplier + 1);
        int addition = UnityEngine.Random.Range(resolvedMinAddition, resolvedMaxAddition + 1);

        long value = startValue;
        for (int i = 0; i < loopCount; i++)
        {
            value = (value * multiplier) + addition;
        }

        expectedAnswer = value;

        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"x <- {startValue}");
        builder.AppendLine($"Pour i de 1 a {loopCount}");
        builder.AppendLine($"    x <- x * {multiplier}");
        builder.AppendLine($"    x <- x + {addition}");
        builder.AppendLine("FinPour");
        builder.Append("Afficher x");

        generatedPseudoCode = builder.ToString();
    }

    private void RenderChallenge()
    {
        if (pseudoCodeText != null)
        {
            pseudoCodeText.text = generatedPseudoCode;
        }
    }

    private void SetFeedback(string message, bool isSuccess)
    {
        if (feedbackText == null)
        {
            return;
        }

        feedbackText.gameObject.SetActive(!string.IsNullOrEmpty(message));
        feedbackText.text = message;
        feedbackText.color = isSuccess ? new Color(0.35f, 0.95f, 0.55f) : Color.white;
    }

    private IEnumerator CloseAfterDelay(float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        CloseMinigame();
    }

    public void CloseMinigame()
    {
        if (isClosing)
        {
            return;
        }

        isClosing = true;
        isActive = false;

        if (closeRoutine != null)
        {
            StopCoroutine(closeRoutine);
            closeRoutine = null;
        }

        onClosed?.Invoke(rewardWasGiven);
        onClosed = null;
        activePlayer = null;

        Scene scene = gameObject.scene;
        if (scene.IsValid() && scene.isLoaded && SceneManager.sceneCount > 1)
        {
            SceneManager.UnloadSceneAsync(scene);
            return;
        }

        gameObject.SetActive(false);
    }

    private static void EnsureEventSystemExists()
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

        Camera fallbackCamera = canvas.worldCamera;
        if (fallbackCamera == null || !fallbackCamera.isActiveAndEnabled)
        {
            fallbackCamera = Camera.main;
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
        }

        if (fallbackCamera != null)
        {
            canvas.worldCamera = fallbackCamera;
        }
        else
        {
            Debug.LogWarning("VendingMachineMinigame: no enabled camera found for world-space canvas event camera.");
        }
    }

    private void AutoBindUIReferences()
    {
        if (pseudoCodeText == null)
        {
            pseudoCodeText = GetComponentInChildren<TMP_Text>(true);
        }

        if (instructionText == null)
        {
            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text currentText = texts[i];
                if (currentText == null || currentText == pseudoCodeText || currentText == feedbackText)
                {
                    continue;
                }

                instructionText = currentText;
                break;
            }
        }

        if (answerInput == null)
        {
            answerInput = GetComponentInChildren<TMP_InputField>(true);
        }

        if (submitButton == null || closeButton == null)
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button currentButton = buttons[i];
                if (currentButton == null)
                {
                    continue;
                }

                if (submitButton == null)
                {
                    submitButton = currentButton;
                    continue;
                }

                if (closeButton == null && currentButton != submitButton)
                {
                    closeButton = currentButton;
                    break;
                }
            }
        }
    }

    private void EnsureCanvasOnTopAndInteractive()
    {
        Canvas canvas = GetComponentInParent<Canvas>(true);
        if (canvas == null)
            return;

        CanvasGroup cg = canvas.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = canvas.gameObject.AddComponent<CanvasGroup>();

        cg.interactable = true;
        cg.blocksRaycasts = true;

        canvas.overrideSorting = true;
        canvas.sortingOrder = 5000;

        if (canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();

        if (canvas.renderMode == RenderMode.WorldSpace && canvas.worldCamera == null)
            EnsureCanvasCanReceiveClicks();

        Canvas.ForceUpdateCanvases();

        Debug.Log($"VendingMachineMinigame: Canvas prepared. renderMode={canvas.renderMode}, worldCamera={(canvas.worldCamera!=null)}, sortingOrder={canvas.sortingOrder}, EventSystem={(EventSystem.current!=null)}");
    }
}