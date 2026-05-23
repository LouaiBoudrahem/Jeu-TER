using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class AdditiveMinigameController : MonoBehaviour
{
    private Player activePlayer;
    private GameObject activeCamera;
    private string activeSceneName;
    private bool isOpen;
    private bool isClosing;

    public bool IsOpen => isOpen;

    public void OpenMinigame(GameObject computerVirtualCamera, string sceneName)
    {
        if (isOpen)
        {
            return;
        }

        if (computerVirtualCamera == null)
        {
            Debug.LogWarning("AdditiveMinigameController.OpenMinigame: computerVirtualCamera is not assigned.");
            return;
        }

        Player player = FindObjectOfType<Player>();
        if (player == null)
        {
            Debug.LogWarning("AdditiveMinigameController.OpenMinigame: no Player found in scene.");
            return;
        }

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("AdditiveMinigameController.OpenMinigame: sceneName is empty.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning($"AdditiveMinigameController.OpenMinigame: scene '{sceneName}' is not in Build Settings.");
            return;
        }

        activePlayer = player;
        activeCamera = computerVirtualCamera;
        activeSceneName = sceneName;
        isOpen = true;

        activePlayer.BeginComputerInteraction(activeCamera, null);

        if (!SceneManager.GetSceneByName(sceneName).isLoaded)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
        }
        else
        {
            ActivateSceneRoots(SceneManager.GetSceneByName(sceneName));
        }
    }

    private void Update()
    {
        if (!isOpen)
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseMinigame();
        }
    }

    public void CloseMinigame()
    {
        if (isClosing)
        {
            return;
        }

        isClosing = true;

        if (activePlayer != null)
        {
            activePlayer.EndComputerInteraction();
        }

        Scene scene = SceneManager.GetSceneByName(activeSceneName);
        if (scene.IsValid() && scene.isLoaded)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.UnloadSceneAsync(scene.name);
        }

        activePlayer = null;
        activeCamera = null;
        activeSceneName = null;
        isOpen = false;
        isClosing = false;
    }

    private void HandleSceneLoaded(Scene loadedScene, LoadSceneMode loadMode)
    {
        if (!string.Equals(loadedScene.name, activeSceneName, System.StringComparison.Ordinal))
        {
            return;
        }

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        ActivateSceneRoots(loadedScene);
    }

    private static void ActivateSceneRoots(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return;
        }

        foreach (GameObject rootObject in scene.GetRootGameObjects())
        {
            if (rootObject != null)
            {
                rootObject.SetActive(true);
            }
        }
    }
}