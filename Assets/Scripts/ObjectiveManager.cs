using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;


public class ObjectiveManager : MonoBehaviour
{

    public static ObjectiveManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (startObjectivesOnAwake)
        {
            StartObjectives();
        }
    }


    [Header("UI References")]
    [SerializeField] private CanvasGroup objectivePanelGroup; 
    [SerializeField] private TextMeshProUGUI objectiveLabel;   
    [SerializeField] private TextMeshProUGUI objectiveText;     
    [SerializeField] private TextMeshProUGUI scoreText;

    [SerializeField] private string scorePrefix = "Score: ";

    [Header("Objectives (ordered)")]
    [SerializeField] private List<string> objectives = new List<string>
    {
        "Solve the vending machine minigame.",
        "Reach a score of 400 or more.",
        "Solve the Linux minigame.",
        "Find the hidden flag file."
    };

    [Header("Timing")]
    [Tooltip("Duration of the fade in / fade out in seconds.")]
    [SerializeField] private float fadeDuration = 0.4f;

    [Tooltip("How long the panel stays fully visible before fading out (set 0 to keep it visible).")]
    [SerializeField] private float displayDuration = 3.5f;

    [Tooltip("Gap between fade-out and fade-in when switching objectives.")]
    [SerializeField] private float switchGapDuration = 0.15f;
    
    [Header("Sound Effect")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip objectiveChangeSFX;

    [Header("Startup")]
    [SerializeField] private bool startObjectivesOnAwake = true;

    private int currentIndex = -1;        
    private Coroutine activeRoutine;
    private readonly List<bool> objectiveCompletionState = new List<bool>();


    public int CurrentIndex => currentIndex;

    public bool IsComplete => currentIndex >= objectives.Count;

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            ShowCurrentObjectiveWithScore();
        }
    }

    public void StartObjectives()
    {
        ResetCompletionState();
        currentIndex = -1;
        AdvanceObjective();
    }

    public void AdvanceObjective()
    {
        currentIndex++;

        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        if (currentIndex >= objectives.Count)
        {
            activeRoutine = StartCoroutine(FadeOut());
            return;
        }

        if (audioSource != null && objectiveChangeSFX != null)
        {
            audioSource.PlayOneShot(objectiveChangeSFX);
        }

        activeRoutine = StartCoroutine(TransitionToObjective(objectives[currentIndex]));
    }

    public void SetObjective(int index)
    {
        if (index < 0 || index >= objectives.Count)
        {
            Debug.LogWarning($"[ObjectiveManager] Index {index} is out of range.");
            return;
        }

        currentIndex = index;

        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        activeRoutine = StartCoroutine(TransitionToObjective(objectives[currentIndex]));
    }

    public void SetObjectiveList(List<string> newObjectives)
    {
        objectives = newObjectives;
        ResetCompletionState();
        currentIndex = -1;

        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        activeRoutine = StartCoroutine(FadeOut());
    }

    public void HideImmediate()
    {
        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        objectivePanelGroup.alpha = 0f;
        objectivePanelGroup.interactable = false;
        objectivePanelGroup.blocksRaycasts = false;
    }

    public void SetCanvasVisible(bool visible)
    {
        if (objectivePanelGroup == null)
        {
            return;
        }

        objectivePanelGroup.gameObject.SetActive(visible);

        if (!visible)
        {
            objectivePanelGroup.alpha = 0f;
            objectivePanelGroup.interactable = false;
            objectivePanelGroup.blocksRaycasts = false;
        }
    }

    public void CompleteVendingMachine()
    {
        MarkObjectiveComplete(0);
    }

    public void ReachHighScore()
    {
        MarkObjectiveComplete(1);
    }

    public void CompleteLinuxMinigame()
    {
        MarkObjectiveComplete(2);
    }

    public void FindFlagFile()
    {
        MarkObjectiveComplete(3);
    }

    public void ShowCurrentObjectiveWithScore()
    {
        if (objectivePanelGroup == null)
        {
            return;
        }

        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
        }

        activeRoutine = StartCoroutine(TransitionToObjective(GetCurrentObjectiveText()));
    }

    private void MarkObjectiveComplete(int index)
    {
        if (index < 0 || index >= objectiveCompletionState.Count)
        {
            return;
        }

        if (objectiveCompletionState[index])
        {
            return;
        }

        objectiveCompletionState[index] = true;
        AdvanceCompletedObjectives();
    }

    private void AdvanceCompletedObjectives()
    {
        while (currentIndex + 1 < objectiveCompletionState.Count && objectiveCompletionState[currentIndex + 1])
        {
            AdvanceObjective();
        }
    }

    private void ResetCompletionState()
    {
        objectiveCompletionState.Clear();

        for (int i = 0; i < objectives.Count; i++)
        {
            objectiveCompletionState.Add(false);
        }
    }

    private IEnumerator TransitionToObjective(string text)
    {
        // If already visible, fade out first
        if (objectivePanelGroup.alpha > 0f)
        {
            yield return StartCoroutine(FadeGroup(objectivePanelGroup, objectivePanelGroup.alpha, 0f, fadeDuration));
            yield return new WaitForSeconds(switchGapDuration);
        }

        SetText(text);

        // Fade in
        yield return StartCoroutine(FadeGroup(objectivePanelGroup, 0f, 1f, fadeDuration));

        if (displayDuration > 0f)
        {
            yield return new WaitForSeconds(displayDuration);
            yield return StartCoroutine(FadeGroup(objectivePanelGroup, 1f, 0f, fadeDuration));
        }

        activeRoutine = null;
    }

    private IEnumerator FadeOut()
    {
        if (objectivePanelGroup.alpha > 0f)
            yield return StartCoroutine(FadeGroup(objectivePanelGroup, objectivePanelGroup.alpha, 0f, fadeDuration));

        activeRoutine = null;
    }

    private IEnumerator FadeGroup(CanvasGroup group, float from, float to, float duration)
    {
        group.interactable  = false;
        group.blocksRaycasts = to > 0f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        group.alpha = to;
    }


    private string GetCurrentObjectiveText()
    {
        if (currentIndex < 0)
        {
            return objectives.Count > 0 ? objectives[0] : string.Empty;
        }

        if (currentIndex >= objectives.Count)
        {
            return "All objectives completed.";
        }

        return objectives[currentIndex];
    }

    private void SetText(string text)
    {
        if (objectiveText != null)
        {
            objectiveText.text = text;
        }

        UpdateScoreText();

        if (objectiveLabel != null)
            objectiveLabel.text = $"OBJECTIVE  {currentIndex + 1}/{objectives.Count}";
    }

    private void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = $"{scorePrefix}{QuizController.CurrentScore}";
        }
    }


#if UNITY_EDITOR
    [ContextMenu("Test: Start Objectives")]
    private void EditorTestStart() => StartObjectives();

    [ContextMenu("Test: Advance Objective")]
    private void EditorTestAdvance() => AdvanceObjective();
#endif
}