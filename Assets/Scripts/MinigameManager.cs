using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class MinigameManager : MonoBehaviour
{
    private static string activeMinigameSceneName;
    private PlayerInputActions playerInputActions;

    void Start()
    {
        playerInputActions = new PlayerInputActions();
        playerInputActions.Enable();
    }

    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ExitMinigame();
        }
    }

    void OnDestroy()
    {
        playerInputActions?.Dispose();
    }

    public static void SetActiveMinigameSceneName(string sceneName)
    {
        activeMinigameSceneName = sceneName;
    }

    public void ExitMinigame()
    {
        ForceCloseComputerUI();

        Player player = FindObjectOfType<Player>();
        if (player != null)
        {
            player.EndComputerInteraction();
        }

        Scene minigameScene = string.IsNullOrWhiteSpace(activeMinigameSceneName)
            ? gameObject.scene
            : SceneManager.GetSceneByName(activeMinigameSceneName);

        if (minigameScene.IsValid() && minigameScene.isLoaded)
        {
            SceneManager.UnloadSceneAsync(minigameScene.name);
        }

        activeMinigameSceneName = null;

        ForceCloseComputerUI();
    }

    private void ForceCloseComputerUI()
    {
        ComputerUIController[] controllers = Resources.FindObjectsOfTypeAll<ComputerUIController>();
        if (controllers == null)
        {
            return;
        }

        foreach (ComputerUIController controller in controllers)
        {
            if (controller == null)
            {
                continue;
            }

            controller.HideComputerUIForExit();
        }
    }

}
