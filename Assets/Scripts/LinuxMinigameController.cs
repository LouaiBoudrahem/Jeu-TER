using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class LinuxMinigameController : MonoBehaviour
{
    [SerializeField] private string linuxSceneName = "Linux";

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
            Debug.LogWarning("LinuxMinigameController.OpenMinigame: computerVirtualCamera is not assigned.");
            return;
        }

        Player player = FindObjectOfType<Player>();
        if (player == null)
        {
            Debug.LogWarning("LinuxMinigameController.OpenMinigame: no Player found in scene.");
            return;
        }

        if (string.IsNullOrWhiteSpace(linuxSceneName))
        {
            Debug.LogWarning("LinuxMinigameController.OpenMinigame: linuxSceneName is empty.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(linuxSceneName))
        {
            Debug.LogWarning($"LinuxMinigameController.OpenMinigame: scene '{linuxSceneName}' is not in Build Settings.");
            return;
        }

        activePlayer = player;
        activeCamera = computerVirtualCamera;
        isOpen = true;

        activePlayer.BeginComputerInteraction(activeCamera, null);

        if (!SceneManager.GetSceneByName(linuxSceneName).isLoaded)
        {
            SceneManager.sceneLoaded -= HandleLinuxSceneLoaded;
            SceneManager.sceneLoaded += HandleLinuxSceneLoaded;
            SceneManager.LoadScene(linuxSceneName, LoadSceneMode.Additive);
        }
        else
        {
            ActivateSceneRoots(SceneManager.GetSceneByName(linuxSceneName));
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

        Scene scene = SceneManager.GetSceneByName(linuxSceneName);
        if (scene.IsValid() && scene.isLoaded)
        {
            SceneManager.sceneLoaded -= HandleLinuxSceneLoaded;
            SceneManager.UnloadSceneAsync(scene.name);
        }

        activePlayer = null;
        activeCamera = null;
        isOpen = false;
        isClosing = false;
    }

    private void HandleLinuxSceneLoaded(Scene loadedScene, LoadSceneMode loadMode)
    {
        if (!string.Equals(loadedScene.name, linuxSceneName, System.StringComparison.Ordinal))
        {
            return;
        }

        SceneManager.sceneLoaded -= HandleLinuxSceneLoaded;
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