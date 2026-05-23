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
        LinuxMinigameController linuxController = FindObjectOfType<LinuxMinigameController>(true);
        if (linuxController != null && linuxController.IsOpen)
        {
            return;
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (FindObjectOfType<QuizController>() != null)
            {
                return;
            }

            if (IsLinuxMinigame(SceneManager.GetSceneByName(activeMinigameSceneName)))
            {
                return;
            }

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
        LinuxMinigameController linuxController = FindObjectOfType<LinuxMinigameController>(true);
        if (linuxController != null && linuxController.IsOpen)
        {
            return;
        }

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

    private static bool IsLinuxMinigame(Scene minigameScene)
    {
        if (!string.IsNullOrWhiteSpace(activeMinigameSceneName) &&
            string.Equals(activeMinigameSceneName, "Linux", System.StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return minigameScene.IsValid() &&
               string.Equals(minigameScene.name, "Linux", System.StringComparison.OrdinalIgnoreCase);
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
