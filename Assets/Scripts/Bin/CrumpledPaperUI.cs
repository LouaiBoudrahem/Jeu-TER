using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;

public class CrumpledPaperUI : MonoBehaviour
{
    [Header("Canvas & Root")]
    [SerializeField] private GameObject paperPanel;
    [SerializeField] private Canvas worldCanvas;

    [Header("References")]
    [SerializeField] private Player player;

    [Header("Shared")]
    [SerializeField] private TextMeshProUGUI codeDisplay;
    [SerializeField] private TextMeshProUGUI questionText;
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private Button closeButton;

    [Header("Output Answer (Short Answer)")]
    [SerializeField] private GameObject shortAnswerGroup;
    [SerializeField] private TMP_InputField answerField;
    [SerializeField] private Button submitButton;

    [Header("Crash Line (Button Grid)")]
    [SerializeField] private GameObject crashLineGroup;
    [SerializeField] private Transform lineButtonContainer;
    [SerializeField] private Button lineButtonPrefab;

    [Header("Audio")]
    [SerializeField] private AudioClip correctAnswerClip;
    [SerializeField] private AudioClip wrongAnswerClip;
    [SerializeField] private AudioSource audioSource;

    private BinPuzzleData puzzle;
    private bool useOutput;
    private int attempts;
    private Action onFinished;
    private bool solved;
    private bool isOpen;

    void Awake()
    {
        EnsureEventSystemExists();
        EnsureCanvasCanReceiveClicks();
        EnsureCanvasOnTopAndInteractive();

        paperPanel.SetActive(false);
        submitButton.onClick.AddListener(OnSubmitShortAnswer);
        closeButton.onClick.AddListener(RequestClose);
    }

    void Update()
    {
        if (!isOpen)
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            RequestClose();
        }
    }

    public void Open(BinPuzzleData data, bool outputMode, Action finishedCallback)
    {
        puzzle     = data;
        useOutput  = outputMode;
        attempts   = 0;
        solved     = false;
        onFinished = finishedCallback;

        feedbackText.text = "";
        BuildCodeDisplay();
        BuildQuestionArea();

        EnsureEventSystemExists();
        EnsureCanvasCanReceiveClicks();
        EnsureCanvasOnTopAndInteractive();

        paperPanel.SetActive(true);
        worldCanvas.enabled = true;
        isOpen = true;

        EnsurePlayerReference();
        player?.SetInputLocked(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void EnsurePlayerReference()
    {
        if (player == null)
        {
            player = FindObjectOfType<Player>();
        }
    }

    private void BuildCodeDisplay()
    {
        var lines = puzzle.AlgorithmCode.Split('\n');
        var sb    = new System.Text.StringBuilder();

        for (int i = 0; i < lines.Length; i++)
            sb.AppendLine($"<color=#888888>{i + 1:D2}</color>  {lines[i]}");

        codeDisplay.text = sb.ToString();
    }

    private void BuildQuestionArea()
    {
        if (useOutput)
        {
            questionText.text = "Qu'affiche cet algorithme ?";
            shortAnswerGroup.SetActive(true);
            crashLineGroup.SetActive(false);
            answerField.text = "";
            answerField.ActivateInputField();
        }
        else
        {
            questionText.text = "À quelle ligne se produit l'erreur ?";
            shortAnswerGroup.SetActive(false);
            crashLineGroup.SetActive(true);
            BuildLineButtons();
        }
    }

    private void BuildLineButtons()
    {
        foreach (Transform child in lineButtonContainer)
            Destroy(child.gameObject);

        var lines = puzzle.AlgorithmCode.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            int lineNumber = i + 1;
            var btn = Instantiate(lineButtonPrefab, lineButtonContainer);
            btn.GetComponentInChildren<TextMeshProUGUI>().text = lineNumber.ToString();
            btn.onClick.AddListener(() => OnLineButtonClicked(lineNumber));
        }
    }

    private void OnSubmitShortAnswer()
    {
        if (solved) return;
        EvaluateAnswer(answerField.text.Trim());
    }

    private void OnLineButtonClicked(int lineNumber)
    {
        if (solved) return;
        EvaluateAnswer(lineNumber.ToString());
    }

    private void EvaluateAnswer(string raw)
    {
        string given   = raw.ToLower();
        string correct = useOutput
            ? puzzle.OutputAnswer.Trim().ToLower()
            : puzzle.CrashLine.ToString();

        if (given == correct)
        {
            PlayAnswerSound(correctAnswerClip);

            int awardedPoints = CalculateAwardedPoints();
            if (awardedPoints > 0)
            {
                QuizController.AddScore(awardedPoints);
            }

            feedbackText.text = awardedPoints > 0
                ? $"<color=#00cc66>Correct ! +{awardedPoints} points.</color>"
                : "<color=#00cc66>Correct !</color>";
            solved = true;
            StartCoroutine(CloseAfterDelay(2f));
        }
        else
        {
            PlayAnswerSound(wrongAnswerClip);

            attempts++;
            if (attempts >= 3)
            {
                feedbackText.text = "<color=#cc3333>Trop de tentatives.</color>";
                solved = true;
                StartCoroutine(CloseAfterDelay(2f));
            }
            else
            {
                feedbackText.text = $"<color=#cc3333>Mauvaise réponse — tentative {attempts}/3</color>";
            }
        }
    }

    private int CalculateAwardedPoints()
    {
        if (puzzle == null)
        {
            return 0;
        }

        int maxPoints = Mathf.Max(0, puzzle.MaxPoints);
        int penaltyPerAttempt = Mathf.Max(0, puzzle.PenaltyPerAttempt);
        int awardedPoints = maxPoints - (attempts * penaltyPerAttempt);

        return Mathf.Max(0, awardedPoints);
    }

    private void Close()
    {
        if (!isOpen)
        {
            return;
        }

        isOpen = false;
        paperPanel.SetActive(false);
        worldCanvas.enabled = false;

        EnsurePlayerReference();
        player?.SetInputLocked(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        onFinished?.Invoke();
    }

    private void RequestClose()
    {
        Close();
    }

    private void PlayAnswerSound(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        EnsureAudioSource();
        if (audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void EnsureAudioSource()
    {
        if (audioSource != null)
        {
            return;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private IEnumerator CloseAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Close();
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
        if (worldCanvas == null)
        {
            return;
        }

        if (worldCanvas.GetComponent<GraphicRaycaster>() == null)
        {
            worldCanvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        if (worldCanvas.renderMode != RenderMode.WorldSpace)
        {
            return;
        }

        Camera fallbackCamera = worldCanvas.worldCamera;
        if (fallbackCamera == null || !fallbackCamera.isActiveAndEnabled)
        {
            fallbackCamera = Camera.main;
        }

        if (fallbackCamera == null || !fallbackCamera.isActiveAndEnabled)
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
            worldCanvas.worldCamera = fallbackCamera;
        }
    }

    private void EnsureCanvasOnTopAndInteractive()
    {
        if (worldCanvas == null)
        {
            return;
        }

        CanvasGroup canvasGroup = worldCanvas.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = worldCanvas.gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        worldCanvas.overrideSorting = true;
        worldCanvas.sortingOrder = 5000;

        if (worldCanvas.GetComponent<GraphicRaycaster>() == null)
        {
            worldCanvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        Canvas.ForceUpdateCanvases();
    }
}