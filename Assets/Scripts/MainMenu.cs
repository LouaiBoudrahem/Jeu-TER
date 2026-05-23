using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button questionsButton;
    [SerializeField] private Button thirdButton;
    [SerializeField] private Button quitButton;

    [Header("Scenes")]
    [SerializeField] private string gameplaySceneName = "Game";
    [SerializeField] private string questionsSceneName = "Questions";

    [Header("Loading Transition")]
    [SerializeField] private CanvasGroup transitionCanvasGroup;
    [SerializeField] private Slider loadingSlider;
    [SerializeField] private TMP_Text loadingPercentText;
    [SerializeField, Range(0f, 1f)] private float sceneSwitchPoint = 0.75f;
    [SerializeField] private float fadeInDuration = 0.25f;
    [SerializeField] private float fadeOutDuration = 0.2f;

    [Header("Hover Glow")]
    [SerializeField] private bool autoSetupHoverGlow = true;
    [SerializeField, Range(0f, 1f)] private float tmpOutlineWidthOnHover = 0.25f;

    private Coroutine transitionRoutine;

    private void Awake()
    {
        AutoAssignButtonsIfMissing();
        BindButtonActions();
        SetupTransitionUI(false);

        if (playButton == null)
        {
            Debug.LogWarning("MainMenu: playButton is not assigned after AutoAssignButtonsIfMissing(). Button clicks will be ignored.");
        }

        Debug.Log($"MainMenu Awake: transitionCanvasGroup={(transitionCanvasGroup!=null)}, loadingSlider={(loadingSlider!=null)}, loadingPercentText={(loadingPercentText!=null)}");

        if (autoSetupHoverGlow)
        {
            SetupHoverGlow(playButton);
            SetupHoverGlow(questionsButton);
            SetupHoverGlow(thirdButton);
            SetupHoverGlow(quitButton);
        }
    }

    private void OnDestroy()
    {
        UnbindButtonActions();
    }

    private void AutoAssignButtonsIfMissing()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        if (buttons == null || buttons.Length == 0)
        {
            return;
        }

        if (playButton == null && buttons.Length > 0)
        {
            playButton = buttons[0];
        }

        if (questionsButton == null && buttons.Length > 1)
        {
            questionsButton = buttons[1];
        }

        if (thirdButton == null && buttons.Length > 2)
        {
            thirdButton = buttons[2];
        }

        if (quitButton == null && buttons.Length > 3)
        {
            quitButton = buttons[3];
        }
    }

    private void BindButtonActions()
    {
        if (playButton != null)
        {
            playButton.onClick.RemoveListener(LoadGameplaySceneWithTransition);
            playButton.onClick.AddListener(LoadGameplaySceneWithTransition);
            Debug.Log("MainMenu: Bound playButton to LoadGameplaySceneWithTransition");
        }

        if (questionsButton != null)
        {
            questionsButton.onClick.RemoveListener(LoadQuestionsScene);
            questionsButton.onClick.AddListener(LoadQuestionsScene);
        }

        if (thirdButton != null)
        {
            thirdButton.onClick.RemoveListener(ThirdButtonAction);
            thirdButton.onClick.AddListener(ThirdButtonAction);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(QuitGame);
            quitButton.onClick.AddListener(QuitGame);
        }
    }

    private void UnbindButtonActions()
    {
        if (playButton != null)
        {
            playButton.onClick.RemoveListener(LoadGameplaySceneWithTransition);
        }

        if (questionsButton != null)
        {
            questionsButton.onClick.RemoveListener(LoadQuestionsScene);
        }

        if (thirdButton != null)
        {
            thirdButton.onClick.RemoveListener(ThirdButtonAction);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(QuitGame);
        }
    }

    private void LoadGameplaySceneWithTransition()
    {
        Debug.Log("MainMenu: Play button clicked — starting transition");
        StartSceneTransition(gameplaySceneName);
    }

    private void LoadQuestionsScene()
    {
        LoadSceneSafe(questionsSceneName);
    }

    private void ThirdButtonAction()
    {
        
    }

    private void LoadSceneSafe(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("MainMenu: scene name is empty. Set it in the inspector.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning($"MainMenu: scene '{sceneName}' is not in Build Settings.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    private void StartSceneTransition(string sceneName)
    {
        Debug.Log($"MainMenu: StartSceneTransition -> {sceneName}");


        if (transitionCanvasGroup == null || loadingSlider == null)
        {
            Debug.LogWarning("MainMenu: Transition UI not fully assigned. Loading scene directly as fallback.");
            LoadSceneSafe(sceneName);
            return;
        }

        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
        }

        transitionRoutine = StartCoroutine(LoadSceneWithTransition(sceneName));
    }

    private IEnumerator LoadSceneWithTransition(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("MainMenu: scene name is empty. Set it in the inspector.");
            yield break;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning($"MainMenu: scene '{sceneName}' is not in Build Settings.");
            yield break;
        }

        SetButtonsInteractable(false);
        SetupTransitionUI(true);
        DontDestroyOnLoad(gameObject);

        if (transitionCanvasGroup != null)
        {
            transitionCanvasGroup.alpha = 0f;
            yield return FadeTransition(0f, 1f, fadeInDuration);
        }

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);
        loadOperation.allowSceneActivation = false;

        while (loadOperation.progress < 0.9f)
        {
            UpdateLoadingSlider(loadOperation.progress / 0.9f);
            yield return null;
        }

        // record the intended start percent for the game scene overlay
        TransitionState.PendingStartPercent = sceneSwitchPoint;
        TransitionState.HasPending = true;
        UpdateLoadingSlider(sceneSwitchPoint);
        loadOperation.allowSceneActivation = true;

        while (!loadOperation.isDone)
        {
            yield return null;
        }

        if (transitionCanvasGroup != null)
        {
            yield return FadeTransition(1f, 0f, fadeOutDuration);
        }

        Destroy(gameObject);
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (playButton != null)
        {
            playButton.interactable = interactable;
        }

        if (questionsButton != null)
        {
            questionsButton.interactable = interactable;
        }

        if (thirdButton != null)
        {
            thirdButton.interactable = interactable;
        }

        if (quitButton != null)
        {
            quitButton.interactable = interactable;
        }
    }

    private void SetupTransitionUI(bool visible)
    {
        if (transitionCanvasGroup != null)
        {
            transitionCanvasGroup.alpha = visible ? 1f : 0f;
            transitionCanvasGroup.interactable = visible;
            transitionCanvasGroup.blocksRaycasts = visible;
        }

        if (loadingSlider != null)
        {
            loadingSlider.gameObject.SetActive(visible);
            loadingSlider.normalizedValue = 0f;
        }

        UpdateLoadingPercentText(0f);
    }

    private void UpdateLoadingSlider(float normalizedProgress)
    {
        if (loadingSlider == null)
        {
            UpdateLoadingPercentText(0f);
            return;
        }

        float sliderProgress = Mathf.Clamp01(normalizedProgress) * Mathf.Clamp01(sceneSwitchPoint);
        loadingSlider.normalizedValue = sliderProgress;
        UpdateLoadingPercentText(sliderProgress);
    }

    private void UpdateLoadingPercentText(float sliderProgress)
    {
        if (loadingPercentText == null)
        {
            return;
        }

        int percent = Mathf.RoundToInt(Mathf.Clamp01(sliderProgress) * 100f);
        loadingPercentText.text = $"{percent}%";
    }

    private IEnumerator FadeTransition(float fromAlpha, float toAlpha, float duration)
    {
        if (transitionCanvasGroup == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            transitionCanvasGroup.alpha = toAlpha;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            transitionCanvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, elapsed / duration);
            yield return null;
        }

        transitionCanvasGroup.alpha = toAlpha;
    }

    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void SetupHoverGlow(Button button)
    {
        if (button == null)
        {
            return;
        }

        MainMenuButtonHoverGlow glow = button.GetComponent<MainMenuButtonHoverGlow>();
        if (glow == null)
        {
            glow = button.gameObject.AddComponent<MainMenuButtonHoverGlow>();
        }

        glow.Configure(tmpOutlineWidthOnHover);
    }
}

public class MainMenuButtonHoverGlow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private TMP_Text tmpText;
    private Text uiText;
    private Outline uiOutline;

    private float outlineWidthOnHover = 0.25f;
    private float baseTmpOutlineWidth;
    private Color baseTextColor;

    public void Configure(float hoverOutlineWidth)
    {
        outlineWidthOnHover = Mathf.Clamp01(hoverOutlineWidth);
        CacheReferences();
        SetGlow(false);
    }

    private void Awake()
    {
        CacheReferences();
        SetGlow(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetGlow(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetGlow(false);
    }

    private void CacheReferences()
    {
        if (tmpText == null)
        {
            tmpText = GetComponentInChildren<TMP_Text>(true);
            if (tmpText != null)
            {
                baseTmpOutlineWidth = tmpText.outlineWidth;
                baseTextColor = tmpText.color;
            }
        }

        if (uiText == null)
        {
            uiText = GetComponentInChildren<Text>(true);
            if (uiText != null)
            {
                baseTextColor = uiText.color;
                uiOutline = uiText.GetComponent<Outline>();
                if (uiOutline == null)
                {
                    uiOutline = uiText.gameObject.AddComponent<Outline>();
                    uiOutline.effectColor = new Color(1f, 1f, 1f, 0.8f);
                    uiOutline.effectDistance = new Vector2(2f, 2f);
                }
            }
        }
    }

    private void SetGlow(bool enabled)
    {
        if (tmpText != null)
        {
            tmpText.outlineWidth = enabled ? outlineWidthOnHover : baseTmpOutlineWidth;
            tmpText.color = enabled ? Color.white : baseTextColor;
        }

        if (uiText != null)
        {
            uiText.color = enabled ? Color.white : baseTextColor;
        }

        if (uiOutline != null)
        {
            uiOutline.enabled = enabled;
        }
    }
}
