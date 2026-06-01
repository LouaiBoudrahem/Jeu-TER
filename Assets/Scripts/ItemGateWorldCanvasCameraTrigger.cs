using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ItemGateWorldCanvasCameraTrigger : MonoBehaviour
{
    [Header("Score Canvas References")]
    [SerializeField] private GameObject worldCanvasRoot;
    [SerializeField] private Canvas worldCanvas;
    [SerializeField] private GameObject scorePanel;
    [SerializeField] private TMP_Text playerScoreText;
    [SerializeField] private TMP_Text totalTimeText;
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private string mainMenuSceneName = "Main Menu";

    [Header("Text Prefixes")]
    [SerializeField] private string scorePrefix = "Score: ";
    [SerializeField] private string timePrefix = "Time: ";
    [SerializeField] private string rankPrefix = "Rank: ";

    [Header("Rank Thresholds")]
    [SerializeField] private int rankAMinScore = 200;
    [SerializeField] private int rankSMinScore = 400;
    [SerializeField] private int rankSPlusMinScore = 600;
    [SerializeField] private int rankSPlusPlusMinScore = 800;
    [SerializeField] private float rankAMaxTimeSeconds = 600f;
    [SerializeField] private float rankSMaxTimeSeconds = 450f;
    [SerializeField] private float rankSPlusMaxTimeSeconds = 300f;
    [SerializeField] private float rankSPlusPlusMaxTimeSeconds = 180f;

    private bool hasOpenedMainMenu;

    private void Awake()
    {
        if (worldCanvas != null)
        {
            worldCanvas.enabled = false;
        }

        if (scorePanel != null)
        {
            scorePanel.SetActive(false);
        }

        EnsureEventSystemExists();
        EnsureCanvasCanReceiveClicks();
        EnsureCanvasRaycaster();
        EnsureCanvasOnTopAndInteractive();
        ResolveMainMenuButton();
        EnsureMainMenuButtonFallback();

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(GoToMainMenu);
            mainMenuButton.onClick.AddListener(TryGoToMainMenu);
        }
    }

    private void OnDestroy()
    {
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(TryGoToMainMenu);
        }
    }

    public void ShowScores()
    {
        EnsureEventSystemExists();
        EnsureCanvasCanReceiveClicks();
        EnsureCanvasRaycaster();
        EnsureCanvasOnTopAndInteractive();
        ResolveMainMenuButton();
        EnsureMainMenuButtonFallback();

        if (worldCanvas != null)
        {
            worldCanvas.enabled = true;
        }
        else if (worldCanvasRoot != null)
        {
            worldCanvasRoot.SetActive(true);
        }

        if (scorePanel != null)
        {
            scorePanel.SetActive(true);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.interactable = true;
            mainMenuButton.onClick.RemoveListener(TryGoToMainMenu);
            mainMenuButton.onClick.AddListener(TryGoToMainMenu);
        }

        // Ensure the canvas uses the active/main camera after camera transitions
        Canvas canvas = ResolveCanvas();
        if (canvas != null)
        {
            if (Camera.main != null)
            {
                canvas.worldCamera = Camera.main;
            }

            // Try set to UI layer if it exists so it's visible to UI cameras
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer != -1)
            {
                canvas.gameObject.layer = uiLayer;
                if (scorePanel != null)
                {
                    scorePanel.layer = uiLayer;
                }
                if (mainMenuButton != null)
                {
                    mainMenuButton.gameObject.layer = uiLayer;
                }
            }

            // Move to front in hierarchy to favor raycast order
            canvas.transform.SetAsLastSibling();
            if (scorePanel != null)
            {
                scorePanel.transform.SetAsLastSibling();
            }
            if (mainMenuButton != null)
            {
                mainMenuButton.transform.SetAsLastSibling();
            }
            // give UI focus to the button so Input System events route correctly
            if (EventSystem.current != null && mainMenuButton != null)
            {
                EventSystem.current.SetSelectedGameObject(mainMenuButton.gameObject);
            }

            // show cursor for UI interaction
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        float elapsed = Time.timeSinceLevelLoad;
        int score = QuizController.CurrentScore;

        WriteText(playerScoreText, $"{scorePrefix}{score}");
        WriteText(totalTimeText,   $"{timePrefix}{FormatTime(elapsed)}");
        WriteText(rankText,        $"{rankPrefix}{GetRankLabel(score, elapsed)}");
    }

    private void WriteText(TMP_Text field, string value)
    {
        if (field == null) return;
        field.text = value;
        field.ForceMeshUpdate();
    }

    private string GetRankLabel(int score, float seconds)
    {
        if (score >= rankSPlusPlusMinScore && seconds <= rankSPlusPlusMaxTimeSeconds) return "S++";
        if (score >= rankSPlusMinScore     && seconds <= rankSPlusMaxTimeSeconds)     return "S+";
        if (score >= rankSMinScore         && seconds <= rankSMaxTimeSeconds)         return "S";
        if (score >= rankAMinScore         && seconds <= rankAMaxTimeSeconds)         return "A";

        if (score >= rankSPlusPlusMinScore) return "S+";
        if (score >= rankSPlusMinScore)     return "S";
        if (score >= rankSMinScore)         return "A";
        if (score >= rankAMinScore)         return "B";

        return "C";
    }

    private static string FormatTime(float totalSeconds)
    {
        int s = Mathf.Max(0, Mathf.FloorToInt(totalSeconds));
        return $"{s / 60:00}:{s % 60:00}";
    }

    private void TryGoToMainMenu()
    {
        if (hasOpenedMainMenu)
        {
            return;
        }

        hasOpenedMainMenu = true;
        GoToMainMenu();
    }

    private void GoToMainMenu()
    {
        if (string.IsNullOrWhiteSpace(mainMenuSceneName))
        {
            Debug.LogWarning($"{nameof(ItemGateWorldCanvasCameraTrigger)} on '{name}': Main Menu scene name is empty.");
            return;
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void ResolveMainMenuButton()
    {
        if (mainMenuButton != null)
        {
            return;
        }

        if (worldCanvasRoot == null)
        {
            return;
        }

        mainMenuButton = worldCanvasRoot.GetComponentInChildren<Button>(true);
    }

    private void EnsureMainMenuButtonFallback()
    {
        if (mainMenuButton == null)
        {
            return;
        }

        RectTransform rectTransform = mainMenuButton.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            return;
        }

        BoxCollider collider = mainMenuButton.GetComponent<BoxCollider>();
        if (collider == null)
        {
            collider = mainMenuButton.gameObject.AddComponent<BoxCollider>();
        }

        Vector2 size = rectTransform.rect.size;
        collider.size = new Vector3(Mathf.Max(0.01f, size.x), Mathf.Max(0.01f, size.y), 0.1f);
        collider.center = new Vector3((0.5f - rectTransform.pivot.x) * size.x, (0.5f - rectTransform.pivot.y) * size.y, 0f);

        MainMenuButtonMouseFallback fallback = mainMenuButton.GetComponent<MainMenuButtonMouseFallback>();
        if (fallback == null)
        {
            fallback = mainMenuButton.gameObject.AddComponent<MainMenuButtonMouseFallback>();
        }

        fallback.Configure(this);
    }

    private void EnsureCanvasRaycaster()
    {
        Canvas canvas = ResolveCanvas();
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

        if (canvas.worldCamera == null)
        {
            Camera chosen = FindBestCamera();
            if (chosen != null)
            {
                canvas.worldCamera = chosen;
            }
        }
    }

    private void EnsureCanvasCanReceiveClicks()
    {
        Canvas canvas = ResolveCanvas();
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
            canvas.worldCamera = fallbackCamera;
        }
    }

    private Camera FindBestCamera()
    {
        Camera cam = GetComponentInChildren<Camera>(true);
        if (cam != null)
        {
            return cam;
        }

        if (Camera.main != null)
        {
            return Camera.main;
        }

        if (transform.parent != null)
        {
            cam = transform.parent.GetComponentInChildren<Camera>(true);
            if (cam != null)
            {
                return cam;
            }
        }

        Camera[] all = Camera.allCameras;
        if (all != null && all.Length > 0)
        {
            return all[0];
        }

        return null;
    }

    private void EnsureCanvasOnTopAndInteractive()
    {
        Canvas canvas = ResolveCanvas();
        if (canvas == null)
        {
            return;
        }

        CanvasGroup canvasGroup = canvas.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        if (scorePanel != null)
        {
            CanvasGroup panelGroup = scorePanel.GetComponent<CanvasGroup>();
            if (panelGroup == null)
            {
                panelGroup = scorePanel.AddComponent<CanvasGroup>();
            }

            panelGroup.alpha = 1f;
            panelGroup.interactable = true;
            panelGroup.blocksRaycasts = true;
        }

        canvas.overrideSorting = true;
        canvas.sortingOrder = 32767;
        try
        {
            canvas.sortingLayerName = "UI";
        }
        catch { }

        GraphicRaycaster gr = canvas.GetComponent<GraphicRaycaster>();
        if (gr == null)
        {
            gr = canvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        gr.blockingObjects = GraphicRaycaster.BlockingObjects.None;

        Canvas.ForceUpdateCanvases();
    }

    private Canvas ResolveCanvas()
    {
        if (worldCanvas != null)
        {
            return worldCanvas;
        }

        if (worldCanvasRoot == null)
        {
            return null;
        }

        worldCanvas = worldCanvasRoot.GetComponentInChildren<Canvas>(true);
        return worldCanvas;
    }

    private void OnValidate()
    {
        if (scorePanel == null && worldCanvasRoot != null)
        {
            Transform panelCandidate = worldCanvasRoot.transform.childCount > 0 ? worldCanvasRoot.transform.GetChild(0) : null;
            if (panelCandidate != null)
            {
                scorePanel = panelCandidate.gameObject;
            }
        }
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

    private sealed class MainMenuButtonMouseFallback : MonoBehaviour
    {
        private ItemGateWorldCanvasCameraTrigger owner;

        public void Configure(ItemGateWorldCanvasCameraTrigger trigger)
        {
            owner = trigger;
        }

        private void OnMouseDown()
        {
            if (owner == null || owner.mainMenuButton == null || !owner.mainMenuButton.interactable)
            {
                return;
            }

            owner.TryGoToMainMenu();
        }
    }
}