using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;

public class RoomQuizEventManager : MonoBehaviour
{
    [Header("Room Trigger")]
    [SerializeField] private Collider roomTrigger;
    [SerializeField] private Light[] roomLights;
    [SerializeField] private LightSwitchInteractable lightSwitchInteractable;

    [Header("Quiz Configuration")]
    [SerializeField] private QuestionData[] quizQuestions = new QuestionData[5];
    [SerializeField] private int[] answerIndexOverrides;
    [SerializeField] private float questionDuration = 8f;

    [Header("Audio")]
    [SerializeField] private AudioClip lightsOnClip;
    [SerializeField] private AudioClip quizStartClip;
    [SerializeField] private AudioClip tickingSoundClip;
    [SerializeField] private AudioClip satisfiedClip;
    [SerializeField] private AudioClip unsatisfiedClip;
    [SerializeField] private AudioSource audioSource;

    [Header("UI - Quiz Canvas")]
    [SerializeField] private GameObject quizCanvasRoot;
    [SerializeField] private TextMeshProUGUI questionText;
    [SerializeField] private Button[] choiceButtons = new Button[4];
    [SerializeField] private Image timerDisplay;
    [SerializeField] private Slider timerSlider;

    [Header("Door")]
    [SerializeField] private Transform doorTransform;
    [SerializeField] private Vector3 lockedPosition;
    [SerializeField] private Vector3 lockedRotation;

    private Vector3 unlockedPosition;
    private Vector3 unlockedRotation;
    private DoorController doorController;
    private Coroutine doorEnforceCoroutine;

    [Header("Supervisor")]
    [SerializeField] private Transform supervisorTransform;
    [SerializeField] private Transform supervisorSpawnPoint;
    [SerializeField] private float supervisorChaseDuration = 15f;
    private EnemyAI enemyAI;
    private bool cachedAgentWasEnabled = false;
    private bool cachedAnimatorWasEnabled = false;
    private bool quizStarted = false;
    private bool quizCompleted = false;
    private int currentQuestionIndex = 0;
    private int correctAnswerCount = 0;
    private Player playerReference;
    private Coroutine quizCoroutine;
    private bool hasAnsweredCurrentQuestion = false;

    private void Start()
    {
        EnsureEventSystemExists();
        EnsureQuizCanvasCanReceiveClicks();
    }

    private void Awake()
    {
        Debug.Log("RoomQuizEventManager: Awake() called");
        
        if (roomTrigger == null)
        {
            roomTrigger = GetComponent<Collider>();
            Debug.Log($"RoomQuizEventManager: roomTrigger auto-assigned from GetComponent: {roomTrigger}");
        }
        else
        {
            Debug.Log($"RoomQuizEventManager: roomTrigger already assigned: {roomTrigger.name}");
        }

        if (roomTrigger != null)
        {
            Debug.Log($"RoomQuizEventManager: roomTrigger.isTrigger = {roomTrigger.isTrigger}");
            if (!roomTrigger.isTrigger)
            {
                Debug.LogError("RoomQuizEventManager: CRITICAL - Collider must be set as trigger! Setting it now.");
                roomTrigger.isTrigger = true;
            }
        }
        else
        {
            Debug.LogError("RoomQuizEventManager: CRITICAL - No collider found on this GameObject!");
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezeAll;
            Debug.Log("RoomQuizEventManager: Added Kinematic Rigidbody to trigger for physics detection");
        }
        else if (!rb.isKinematic)
        {
            rb.isKinematic = true;
            Debug.Log("RoomQuizEventManager: Set existing Rigidbody to Kinematic");
        }

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (quizCanvasRoot != null)
        {
            quizCanvasRoot.SetActive(false);
            Debug.Log("RoomQuizEventManager: Canvas disabled at start");
        }

        if (doorTransform != null)
        {
            unlockedPosition = doorTransform.position;
            unlockedRotation = doorTransform.eulerAngles;
            Debug.Log($"RoomQuizEventManager: Door unlocked pose cached: pos={unlockedPosition}, rot={unlockedRotation}");
        }
        else
        {
            Debug.LogWarning("RoomQuizEventManager: doorTransform not assigned! Attempting to find DoorController in scene...");
            DoorController dcFound = FindObjectOfType<DoorController>();
            if (dcFound != null)
            {
                doorController = dcFound;
                doorTransform = dcFound.transform;
                unlockedPosition = doorTransform.position;
                unlockedRotation = doorTransform.eulerAngles;
                Debug.Log($"RoomQuizEventManager: Found DoorController on {doorTransform.name}, assigned as doorTransform");
            }
            else
            {
                Debug.LogWarning("RoomQuizEventManager: No DoorController found in scene.");
            }
        }
        if (doorTransform != null && doorController == null)
        {
            doorController = doorTransform.GetComponent<DoorController>();
            if (doorController != null)
                Debug.Log($"RoomQuizEventManager: doorController attached to {doorTransform.name}");
        }
        
        Debug.Log("RoomQuizEventManager: Awake() complete");
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"RoomQuizEventManager: OnTriggerEnter called with {other.name}");
        Debug.Log($"RoomQuizEventManager: other.gameObject = {other.gameObject.name}");
        Debug.Log($"RoomQuizEventManager: other.gameObject.transform.parent = {other.gameObject.transform.parent}");
        
        if (quizStarted || quizCompleted)
        {
            Debug.Log($"RoomQuizEventManager: Quiz already started or completed. Ignoring.");
            return;
        }

        Debug.Log($"RoomQuizEventManager: Checking for Player component on {other.gameObject.name}...");
        Player player = other.GetComponent<Player>();
        if (player == null)
        {
            Debug.Log($"RoomQuizEventManager: No Player on {other.gameObject.name}, checking parent...");
            player = other.GetComponentInParent<Player>();
        }
        if (player == null)
        {
            Debug.Log($"RoomQuizEventManager: Still no Player, trying owner...");
            player = other.gameObject.GetComponentInParent<Player>();
        }

        if (player != null)
        {
            Debug.Log($"RoomQuizEventManager: Player found on {player.gameObject.name}! Starting quiz event.");
            playerReference = player;
            StartQuizEvent();
        }
        else
        {
            Debug.Log($"RoomQuizEventManager: No Player component found anywhere for {other.gameObject.name}");
        }
    }

    private void StartQuizEvent()
    {
        quizStarted = true;
        Debug.Log("RoomQuizEventManager: Quiz event started!");

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        ObjectiveManager.Instance?.SetObjective(4);
        if (lightSwitchInteractable != null)
        {
            lightSwitchInteractable.SetState(true);
        }
        else if (roomLights != null)
        {
            for (int i = 0; i < roomLights.Length; i++)
            {
                if (roomLights[i] != null)
                {
                    roomLights[i].intensity = 1f;
                }
            }
        }

        if (lightsOnClip != null)
            audioSource.PlayOneShot(lightsOnClip);

        if (doorTransform != null)
        {
            Debug.Log($"RoomQuizEventManager: Locking door. doorTransform={doorTransform.name}, currentPos={doorTransform.position}, currentRot={doorTransform.eulerAngles}");
            DoorController dc = doorTransform.GetComponent<DoorController>();
            if (dc == null)
                dc = doorTransform.GetComponentInParent<DoorController>();

            if (dc != null)
            {
                dc.ResetDoor();
                Debug.Log("RoomQuizEventManager: DoorController.ResetDoor() called to lock the door");
            }
            else
            {
                doorTransform.position = lockedPosition;
                doorTransform.eulerAngles = lockedRotation;
                Debug.Log($"RoomQuizEventManager: Door moved to locked pose: pos={lockedPosition}, rot={lockedRotation}");
            }
            Debug.Log($"RoomQuizEventManager: After lock attempt currentPos={doorTransform.position}, currentRot={doorTransform.eulerAngles}");

            if (doorEnforceCoroutine != null)
                StopCoroutine(doorEnforceCoroutine);
            doorEnforceCoroutine = StartCoroutine(ForceDoorPose(lockedPosition, lockedRotation, 0.5f));
        }

        PrepareSupervisorForQuiz();

        quizCoroutine = StartCoroutine(RunQuizSequence());
    }

    private IEnumerator RunQuizSequence()
    {
        correctAnswerCount = 0;

        if (quizStartClip != null)
        {
            audioSource.PlayOneShot(quizStartClip);
            yield return new WaitForSeconds(quizStartClip.length);
        }

        for (int i = 0; i < quizQuestions.Length; i++)
        {
            if (quizQuestions[i] == null)
            {
                Debug.LogWarning($"RoomQuizEventManager: Question {i} is null!");
                continue;
            }

            currentQuestionIndex = i;
            hasAnsweredCurrentQuestion = false;

            yield return StartCoroutine(RunSingleQuestion(quizQuestions[i]));
        }

        yield return StartCoroutine(EvaluateQuizResults());
    }

    private IEnumerator RunSingleQuestion(QuestionData question)
    {
        if (quizCanvasRoot != null)
        {
            quizCanvasRoot.SetActive(true);
            EnsureQuizCanvasCanReceiveClicks();
            Debug.Log("RoomQuizEventManager: Quiz canvas activated");

            if (EventSystem.current != null)
            {
                for (int i = 0; i < choiceButtons.Length; i++)
                {
                    if (choiceButtons[i] != null && choiceButtons[i].gameObject.activeInHierarchy)
                    {
                        EventSystem.current.SetSelectedGameObject(choiceButtons[i].gameObject);
                        break;
                    }
                }
            }

            if (timerDisplay != null)
                timerDisplay.fillAmount = 1f;
            if (timerSlider != null)
            {
                timerSlider.minValue = 0f;
                timerSlider.maxValue = 1f;
                timerSlider.value = 0f;
            }
        }
        else
        {
            Debug.LogWarning("RoomQuizEventManager: quizCanvasRoot is not assigned!");
        }

        if (questionText != null)
            questionText.text = question.question;

        if (question.audioClip != null)
        {
            audioSource.PlayOneShot(question.audioClip);
            yield return new WaitForSeconds(question.audioClip.length);
        }

        SetupChoiceButtons(question);

        float timeRemaining = questionDuration;
        AudioSource tickingAudioSource = gameObject.AddComponent<AudioSource>();
        tickingAudioSource.clip = tickingSoundClip;
        tickingAudioSource.loop = true;

        if (tickingSoundClip != null)
        {
            tickingAudioSource.Play();
        }

        float elapsedTime = 0f;
        while (elapsedTime < questionDuration && !hasAnsweredCurrentQuestion)
        {
            elapsedTime += Time.deltaTime;
            timeRemaining = questionDuration - elapsedTime;

            if (timerDisplay != null)
            {
                timerDisplay.fillAmount = timeRemaining / questionDuration;
            }

            if (timerSlider != null)
            {
                timerSlider.value = Mathf.Clamp01(elapsedTime / questionDuration);
            }

            yield return null;
        }

        if (timerSlider != null)
            timerSlider.value = 1f;

        if (tickingAudioSource != null)
        {
            tickingAudioSource.Stop();
            Destroy(tickingAudioSource);
        }

        yield return new WaitForSeconds(0.5f);
    }

    private void SetupChoiceButtons(QuestionData question)
    {
        if (question.questionType != QuestionData.QuestionType.MultipleChoice)
            return;
        int correctAnswerIndex = -1;

        if (question.options != null && question.options.Length > 0)
        {
            if (question.correctOptionIndex >= 0 && question.correctOptionIndex < question.options.Length)
            {
                correctAnswerIndex = question.correctOptionIndex;
            }
            else if (!string.IsNullOrEmpty(question.answer))
            {
                for (int k = 0; k < question.options.Length; k++)
                {
                    if (question.options[k] == question.answer)
                    {
                        correctAnswerIndex = k;
                        break;
                    }
                }
            }
            else if (question.answerIndex >= 0f)
            {
                int idx = Mathf.RoundToInt(question.answerIndex);
                idx = Mathf.Clamp(idx, 0, question.options.Length - 1);
                correctAnswerIndex = idx;
            }

            if (correctAnswerIndex == -1)
                correctAnswerIndex = 0;
        }

        int resolvedFromOverride = int.MinValue;
        if (answerIndexOverrides != null && currentQuestionIndex >= 0 && currentQuestionIndex < answerIndexOverrides.Length)
            resolvedFromOverride = answerIndexOverrides[currentQuestionIndex];

        if (resolvedFromOverride >= 0 && question.options != null && resolvedFromOverride < question.options.Length)
        {
            correctAnswerIndex = resolvedFromOverride;
            Debug.Log($"RoomQuizEventManager: Using inspector override for question {currentQuestionIndex}: {correctAnswerIndex}");
        }

        Debug.Log($"RoomQuizEventManager: Setting up buttons for question index {currentQuestionIndex}, resolvedCorrectIndex={correctAnswerIndex}");

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            Button button = choiceButtons[i];
            int choiceIndex = i;

            bool active = question.options != null && i < question.options.Length;
            if (button != null)
            {
                button.gameObject.SetActive(active);
                button.interactable = active;

                TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null && active)
                    buttonText.text = question.options[i];

                button.onClick.RemoveAllListeners();
                int resolvedCorrect = correctAnswerIndex;
                button.onClick.AddListener(() => OnChoiceSelected(choiceIndex, resolvedCorrect));
            }
        }
    }

    private void OnChoiceSelected(int selectedIndex, int correctIndex)
    {
        if (hasAnsweredCurrentQuestion)
            return;

        hasAnsweredCurrentQuestion = true;
        Debug.Log($"RoomQuizEventManager: Choice selected {selectedIndex}, correct={correctIndex}");

        if (selectedIndex == correctIndex)
        {
            correctAnswerCount++;
            Debug.Log($"RoomQuizEventManager: Correct answer! total correct={correctAnswerCount}");
        }
    }

    private IEnumerator EvaluateQuizResults()
    {
        yield return new WaitForSeconds(1f);

        bool passed = correctAnswerCount >= 3;

        if (passed)
        {
            if (satisfiedClip != null)
            {
                audioSource.PlayOneShot(satisfiedClip);
                yield return new WaitForSeconds(satisfiedClip.length);
            }
        }
        else
        {
            if (unsatisfiedClip != null)
            {
                audioSource.PlayOneShot(unsatisfiedClip);
                yield return new WaitForSeconds(unsatisfiedClip.length);
            }

            yield return StartCoroutine(SpawnSupervisor());
        }

        if (correctAnswerCount > 0)
        {
            int pointsToAdd = correctAnswerCount * 100;
            QuizController.AddScore(pointsToAdd);
            Debug.Log($"RoomQuiz: Added {pointsToAdd} points to global score ({correctAnswerCount} correct answers). Total: {QuizController.CurrentScore}");
        }

        if (doorTransform != null)
        {
            DoorController doorController = doorTransform.GetComponent<DoorController>();
            if (doorController != null)
            {
                doorController.ResetDoor();
                Debug.Log("RoomQuizEventManager: DoorController.ResetDoor() called to restore initial pose");
            }
            else
            {
                doorTransform.position = unlockedPosition;
                doorTransform.eulerAngles = unlockedRotation;
                Debug.Log($"RoomQuizEventManager: Door restored to initial pose: pos={unlockedPosition}, rot={unlockedRotation}");
            }
        }

        yield return new WaitForSeconds(1f);
        if (quizCanvasRoot != null)
            quizCanvasRoot.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        ObjectiveManager.Instance?.AdvanceObjective();
        quizCompleted = true;
    }

    private IEnumerator SpawnSupervisor()
    {
        if (supervisorTransform == null)
        {
            Debug.LogWarning("RoomQuizEventManager: supervisorTransform not assigned!");
            yield break;
        }

        UnityEngine.AI.NavMeshAgent parentAgent = supervisorTransform.GetComponentInParent<UnityEngine.AI.NavMeshAgent>();
        Transform supervisorRoot = parentAgent != null ? parentAgent.transform : supervisorTransform;

        supervisorRoot.gameObject.SetActive(true);

        if (enemyAI == null)
        {
            enemyAI = supervisorRoot.GetComponent<EnemyAI>();
            if (enemyAI == null)
                enemyAI = supervisorRoot.GetComponentInChildren<EnemyAI>();
        }

        if (supervisorSpawnPoint != null)
        {
            if (enemyAI != null)
            {
                enemyAI.TeleportTo(supervisorSpawnPoint);
            }
            else
            {
                supervisorRoot.position = supervisorSpawnPoint.position;
                supervisorRoot.rotation = supervisorSpawnPoint.rotation;
            }

            Debug.Log($"RoomQuizEventManager: Supervisor teleported to spawn point {supervisorSpawnPoint.name}");
        }

        Debug.Log("RoomQuizEventManager: Waiting 2 seconds for supervisor to change states...");
        yield return new WaitForSeconds(2f);

        if (enemyAI != null && playerReference != null)
        {
            enemyAI.SetToChase(playerReference.transform);
            Debug.Log($"RoomQuizEventManager: Supervisor set to chase player after 2 second delay");
        }
    }

    public int GetCorrectAnswerCount()
    {
        return correctAnswerCount;
    }

    private void EnsureEventSystemExists()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem (Auto)");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
    }

    private void EnsureQuizCanvasCanReceiveClicks()
    {
        if (quizCanvasRoot == null)
        {
            return;
        }

        Canvas canvas = quizCanvasRoot.GetComponentInChildren<Canvas>(true);
        if (canvas != null && canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        CanvasGroup canvasGroup = quizCanvasRoot.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }

    public int GetTotalQuestions()
    {
        return quizQuestions.Length;
    }

    private IEnumerator ForceDoorPose(Vector3 targetPos, Vector3 targetEuler, float duration)
    {
        if (doorTransform == null)
            yield break;

        Animator animator = doorTransform.GetComponentInParent<Animator>();
        bool hadAnimator = animator != null && animator.enabled;
        if (animator != null && animator.enabled)
            animator.enabled = false;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            doorTransform.position = targetPos;
            doorTransform.eulerAngles = targetEuler;
            yield return null;
        }

        if (animator != null && hadAnimator)
            animator.enabled = true;

        doorEnforceCoroutine = null;
    }

    private void PrepareSupervisorForQuiz()
    {
        if (supervisorTransform == null)
            return;

        if (enemyAI == null)
        {
            enemyAI = supervisorTransform.GetComponent<EnemyAI>();
            if (enemyAI == null)
                enemyAI = supervisorTransform.GetComponentInParent<EnemyAI>();
        }

        if (enemyAI != null)
        {
            enemyAI.SetToIdle();
            Debug.Log("RoomQuizEventManager: Supervisor set to idle for quiz");
        }
    }
}
