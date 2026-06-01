using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class ComputerMinigameController : MonoBehaviour
{
    [SerializeField] private string computerSceneName = "ComputerScene";

    private Player activePlayer;
    private GameObject activeCamera;
    private bool isOpen;
    private bool isClosing;

    public bool IsOpen => isOpen;

    public void OpenMinigame(GameObject computerVirtualCamera)
    {
        if (isOpen)
        {
            return;
        }

        if (computerVirtualCamera == null)
        {
            Debug.LogWarning("ComputerMinigameController.OpenMinigame: computerVirtualCamera is not assigned.");
            return;
        }

        Player player = FindObjectOfType<Player>();
        if (player == null)
        {
            Debug.LogWarning("ComputerMinigameController.OpenMinigame: no Player found in scene.");
            return;
        }

        if (string.IsNullOrWhiteSpace(computerSceneName))
        {
            Debug.LogWarning("ComputerMinigameController.OpenMinigame: computerSceneName is empty.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(computerSceneName))
        {
            Debug.LogWarning($"ComputerMinigameController.OpenMinigame: scene '{computerSceneName}' is not in Build Settings.");
            return;
        }

        activePlayer = player;
        activeCamera = computerVirtualCamera;
        isOpen = true;

        activePlayer.BeginComputerInteraction(activeCamera, null);

        if (!SceneManager.GetSceneByName(computerSceneName).isLoaded)
        {
            SceneManager.sceneLoaded -= HandleComputerSceneLoaded;
            SceneManager.sceneLoaded += HandleComputerSceneLoaded;
            SceneManager.LoadScene(computerSceneName, LoadSceneMode.Additive);
        }
        else
        {
            ActivateSceneRoots(SceneManager.GetSceneByName(computerSceneName));
            OpenComputerExplorerUI();
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

        Scene scene = SceneManager.GetSceneByName(computerSceneName);
        if (scene.IsValid() && scene.isLoaded)
        {
            SceneManager.sceneLoaded -= HandleComputerSceneLoaded;
            SceneManager.UnloadSceneAsync(scene.name);
        }

        activePlayer = null;
        activeCamera = null;
        isOpen = false;
        isClosing = false;
    }

    private void HandleComputerSceneLoaded(Scene loadedScene, LoadSceneMode loadMode)
    {
        if (!string.Equals(loadedScene.name, computerSceneName, System.StringComparison.Ordinal))
        {
            return;
        }

        SceneManager.sceneLoaded -= HandleComputerSceneLoaded;
        ActivateSceneRoots(loadedScene);
        OpenComputerExplorerUI();
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

    private void OpenComputerExplorerUI()
    {
        ComputerUIController uiController = FindObjectOfType<ComputerUIController>(true);
        if (uiController != null)
        {
            uiController.OpenExplorer();
        }
    }
}
