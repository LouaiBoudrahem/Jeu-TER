using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using Unity.Cinemachine;
using TMPro;
using UnityEngine.Serialization;

public class Player : MonoBehaviour
{
    [SerializeField] private PlayerCharacter playerCharacter;
    [SerializeField] private PlayerCamera playerCamera;
    [SerializeField] private Camera playerMainCamera;  
    [SerializeField] private Camera cinemachineOutputCamera; 
    [SerializeField] private CinemachineBrain cinemachineBrain;
    [SerializeField] private GameObject playerPressUI;
    [SerializeField] private GameObject inventoryUIRoot;
    [SerializeField] private TMP_Text interactionMessageText;
    [SerializeField] private float interactionMessageDuration = 2f;
    [FormerlySerializedAs("walkingAudioSource")]
    [SerializeField] private AudioSource walkingSolidAudioSource;
    [SerializeField] private AudioSource walkingLeavesAudioSource;
    [SerializeField] private LayerMask leavesGroundLayers;
    [SerializeField] private string leavesGroundTag = "Leaves";
    [SerializeField] private float triggerDetectionRadius = 0.5f;

    private PlayerInputActions playerInputActions;
    private bool inventoryOpen;
    private bool computerInteractionLocked;
    private bool raycastHitThisFrame;
    private GameObject activeComputerCamera;
    private Coroutine interactionMessageRoutine;
    private Component activeVirtualCameraComponent;
    private int activeVirtualCameraOriginalPriority;
    private AudioSource activeWalkingAudioSource;
    private Rigidbody playerRigidbody;
    private GameObject activeCinemachineCamera;
    private GameObject fallbackVirtualCamera;
    private Coroutine cameraTransitionCoroutine;
    private readonly List<Behaviour> temporarilyDisabledInteractionCameras = new List<Behaviour>();

    void Start()
    {
        playerInputActions = new PlayerInputActions();
        playerInputActions.Enable();

        playerCharacter.Initialize();
        playerCamera.Initialize(playerCharacter.GetCameraTarget());
        playerCharacter.Player = this;

        if (walkingSolidAudioSource == null)
        {
            walkingSolidAudioSource = GetComponent<AudioSource>();
        }

        ValidateColliderSetup();

        if (inventoryUIRoot != null)
            inventoryUIRoot.SetActive(false);

        cinemachineOutputCamera.enabled = false;
    }

    private void ValidateColliderSetup()
    {
        Collider playerCollider = GetComponent<Collider>();
        playerRigidbody = GetComponent<Rigidbody>();

        if (playerCollider == null)
            Debug.LogWarning("Player: No Collider found. Trigger detection will not work.");

        if (playerRigidbody == null)
            Debug.LogWarning("Player: No Rigidbody found. Trigger events may not fire. Consider adding a Rigidbody with Body Type=Kinematic or Dynamic.");
    }

    void OnDestroy()
    {
        playerInputActions.Dispose();
    }

    void Update()
    {
        var playerInput = playerInputActions.Player;

        if (playerInput.Inventory.WasPressedThisFrame())
        {
            if (inventoryOpen)
            {
                CloseInventory();
            }
            else if (!computerInteractionLocked)
            {
                OpenInventory();
            }

            UpdateWalkingAudio(false);
            return;
        }

        if (inventoryOpen || computerInteractionLocked)
        {
            UpdateWalkingAudio(false);
            return;
        }

        Vector2 moveInput = playerInput.Move.ReadValue<Vector2>();

        playerCamera.UpdateRotation(new CameraInput
        {
            Look = playerInput.Look.ReadValue<Vector2>()
        });

        playerCharacter.UpdateInput(new CharacterInput
        {
            Rotation = playerCamera.transform.rotation,
            Move     = moveInput,
            Jump     = playerInput.Jump.triggered,
            Crouch   = playerInput.Crouch.IsPressed()
        });

        UpdateWalkingAudio(playerCharacter.IsWalking() && !playerCharacter.IsCrouching());

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        IInteractable interactable = playerCharacter.PlayerRaycast(ray, out raycastHitThisFrame);

        if(interactable != null)
            playerPressUI.SetActive(true);

        if (interactable != null && playerInput.Interact.WasPressedThisFrame())
        {
            interactable.Interact();
            playerPressUI.SetActive(false);
        }
        if (interactable == null)
        {
            playerPressUI.SetActive(false);
        }
    }

    void LateUpdate()
    {
        if (inventoryOpen || computerInteractionLocked) return;
        playerCamera.UpdatePosition(playerCharacter.GetCameraTarget());
    }

    public void BeginComputerInteraction(GameObject cinemachineCamera, GameObject interactionUI)
    {
        if (cinemachineCamera == null)
        {
            Debug.LogWarning("Player.BeginComputerInteraction: missing computer virtual camera.");
            return;
        }

        computerInteractionLocked = true;
        UpdateInputLockState();

        BeginCinemachineCameraTransition(cinemachineCamera);

        if (interactionUI != null)
            interactionUI.SetActive(true);
    }

    public void EndComputerInteraction()
    {
        EndCinemachineCameraTransition();

        computerInteractionLocked = false;
        UpdateInputLockState();
    }

    public void ShowInteractionMessage(string message)
    {
        if (interactionMessageText == null)
        {
            Debug.Log(message);
            return;
        }

        if (interactionMessageRoutine != null)
        {
            StopCoroutine(interactionMessageRoutine);
        }

        interactionMessageRoutine = StartCoroutine(ShowInteractionMessageRoutine(message));
    }

    private IEnumerator WaitForBlendThenShowUI(GameObject interactionUI)
    {
        if (cinemachineBrain != null)
        {
            yield return null;
            while (cinemachineBrain.IsBlending)
                yield return null;
        }

        interactionUI.SetActive(true);
    }

    private IEnumerator ShowInteractionMessageRoutine(string message)
    {
        interactionMessageText.text = message;
        interactionMessageText.gameObject.SetActive(true);

        yield return new WaitForSeconds(interactionMessageDuration);

        if (interactionMessageText != null)
        {
            interactionMessageText.gameObject.SetActive(false);
            interactionMessageText.text = string.Empty;
        }

        interactionMessageRoutine = null;
    }

    private void OpenInventory()
    {
        Debug.Log("OpenInventory called");
        inventoryOpen = true;

        if (inventoryUIRoot != null)
        {
            Debug.Log($"OpenInventory: Setting inventoryUIRoot active. Root: {inventoryUIRoot.name}");
            inventoryUIRoot.SetActive(true);
            inventoryUIRoot.transform.SetAsLastSibling();
            ConfigureInventoryRaycasts(true);
        }
        else
        {
            Debug.LogWarning("OpenInventory: inventoryUIRoot is not assigned!");
        }

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        UpdateInputLockState();
        UpdateWalkingAudio(false);
        if (playerPressUI != null)
            playerPressUI.SetActive(false);
    }

    private void CloseInventory()
    {
        inventoryOpen = false;

        if (inventoryUIRoot != null)
        {
            inventoryUIRoot.SetActive(false);
            ConfigureInventoryRaycasts(false);
        }

        UpdateInputLockState();
        UpdateWalkingAudio(false);
    }

    public void SetInputLocked(bool locked)
    {
        computerInteractionLocked = locked;
        UpdateInputLockState();
    }

    public void BeginCinemachineCameraTransition(GameObject cinemachineCamera)
    {
        if (cinemachineCamera == null)
            return;
        if (activeCinemachineCamera != null && activeCinemachineCamera != cinemachineCamera)
        {
            if (cameraTransitionCoroutine != null)
            {
                StopCoroutine(cameraTransitionCoroutine);
                cameraTransitionCoroutine = null;
            }

            RestorePreviousVirtualCameraPriority();
            activeCinemachineCamera.SetActive(false);

            if (playerMainCamera != null && cinemachineCamera != null)
            {
                Transform t = cinemachineCamera.transform;
                playerMainCamera.transform.SetPositionAndRotation(t.position, t.rotation);
                playerMainCamera.enabled = true;
                if (cinemachineOutputCamera != null)
                    cinemachineOutputCamera.enabled = false;
            }

            activeCinemachineCamera = cinemachineCamera;
            cinemachineCamera.SetActive(true);
            IsolateInteractionCamera(cinemachineCamera);

            return;
        }

        if (cameraTransitionCoroutine != null)
        {
            StopCoroutine(cameraTransitionCoroutine);
            cameraTransitionCoroutine = null;
        }

        if (cinemachineOutputCamera != null)
            cinemachineOutputCamera.enabled = true;

        if (playerMainCamera != null)
            playerMainCamera.enabled = false;

        activeCinemachineCamera = cinemachineCamera;
        cinemachineCamera.SetActive(true);
        PromoteVirtualCameraPriority(cinemachineCamera);
        IsolateInteractionCamera(cinemachineCamera);
    }

    public void EndCinemachineCameraTransition()
    {
        if (activeCinemachineCamera != null)
        {
            RestorePreviousVirtualCameraPriority();
            activeCinemachineCamera.SetActive(false);
            activeCinemachineCamera = null;
        }

        RestoreIsolatedInteractionCameras();

        if (playerMainCamera != null)
            playerMainCamera.enabled = true;

        if (cinemachineOutputCamera != null)
            cinemachineOutputCamera.enabled = false;

        if (fallbackVirtualCamera != null)
        {
            Destroy(fallbackVirtualCamera);
            fallbackVirtualCamera = null;
        }

        if (cameraTransitionCoroutine != null)
        {
            StopCoroutine(cameraTransitionCoroutine);
            cameraTransitionCoroutine = null;
        }
    }

    private IEnumerator WaitForCinemachineBlendThenDisable(GameObject cinemachineCamera, bool restorePriority)
    {
        if (restorePriority)
        {
            RestorePreviousVirtualCameraPriority();
        }

        if (cinemachineBrain != null)
        {
            yield return null;
            while (cinemachineBrain.IsBlending)
                yield return null;
        }

            cinemachineCamera.SetActive(false);

        bool wasActive = activeCinemachineCamera == cinemachineCamera;
        if (wasActive)
            activeCinemachineCamera = null;

        if (wasActive)
        {
            RestoreIsolatedInteractionCameras();

            if (playerMainCamera != null)
                playerMainCamera.enabled = true;

            if (cinemachineOutputCamera != null)
                cinemachineOutputCamera.enabled = false;
        }

        if (fallbackVirtualCamera != null)
        {
            Destroy(fallbackVirtualCamera.gameObject);
            fallbackVirtualCamera = null;
        }

        cameraTransitionCoroutine = null;
    }

    public void RespawnAt(Transform spawnPoint)
    {
        if (playerCharacter == null)
        {
            Debug.LogWarning("Player.RespawnAt: playerCharacter is not assigned.");
            return;
        }

        if (spawnPoint == null)
        {
            Debug.LogWarning("Player.RespawnAt: spawnPoint is null.");
            return;
        }

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
            playerRigidbody.position = spawnPoint.position;
            playerRigidbody.rotation = spawnPoint.rotation;
        }
        else
        {
            transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
        }

        Physics.SyncTransforms();
        playerCharacter.ResetAfterRespawn(spawnPoint.position, spawnPoint.rotation);

        if (playerCamera != null)
        {
            playerCamera.ResetAfterRespawn(playerCharacter.GetCameraTarget());
            playerCamera.UpdatePosition(playerCharacter.GetCameraTarget());
        }
    }

    private void UpdateInputLockState()
    {
        bool inputLocked = inventoryOpen || computerInteractionLocked;

        if (playerCharacter != null)
            playerCharacter.SetMovementLocked(inputLocked);

        if (inputLocked)
            UpdateWalkingAudio(false);

        Cursor.lockState = inputLocked ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible   = inputLocked;
    }

    private void ConfigureInventoryRaycasts(bool enabled)
    {
        if (inventoryUIRoot == null)
            return;

        CanvasGroup canvasGroup = inventoryUIRoot.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = inventoryUIRoot.AddComponent<CanvasGroup>();

        canvasGroup.alpha = enabled ? 1f : 0f;
        canvasGroup.interactable = enabled;
        canvasGroup.blocksRaycasts = enabled;
        canvasGroup.ignoreParentGroups = true;
    }

    private void UpdateWalkingAudio(bool shouldPlay)
    {
        if (!shouldPlay)
        {
            StopWalkingAudio();
            return;
        }

        AudioSource targetAudioSource = ResolveWalkingAudioSource();
        if (targetAudioSource == null)
        {
            StopWalkingAudio();
            return;
        }

        if (activeWalkingAudioSource != null && activeWalkingAudioSource != targetAudioSource && activeWalkingAudioSource.isPlaying)
        {
            activeWalkingAudioSource.Stop();
        }

        activeWalkingAudioSource = targetAudioSource;

        if (!activeWalkingAudioSource.isPlaying)
            activeWalkingAudioSource.Play();
    }

    private AudioSource ResolveWalkingAudioSource()
    {
        if (IsOnLeavesSurface())
        {
            if (walkingLeavesAudioSource != null)
                return walkingLeavesAudioSource;

            return walkingSolidAudioSource;
        }

        if (walkingSolidAudioSource != null)
            return walkingSolidAudioSource;

        return walkingLeavesAudioSource;
    }

    private bool IsOnLeavesSurface()
    {
        if (playerCharacter == null)
            return false;

        Vector3 playerPos = playerCharacter.transform.position;
        Collider[] overlaps = Physics.OverlapSphere(playerPos, triggerDetectionRadius, ~0, QueryTriggerInteraction.Collide);

        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider collider = overlaps[i];
            if (collider != null && collider.isTrigger && IsLeavesTrigger(collider))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsLeavesTrigger(Collider collider)
    {
        if (collider == null)
            return false;

        bool isTagMatch = !string.IsNullOrEmpty(leavesGroundTag) && collider.CompareTag(leavesGroundTag);
        bool isLayerMatch = (leavesGroundLayers.value & (1 << collider.gameObject.layer)) != 0;

        return isTagMatch || isLayerMatch;
    }

    private void StopWalkingAudio()
    {
        if (activeWalkingAudioSource != null && activeWalkingAudioSource.isPlaying)
        {
            activeWalkingAudioSource.Stop();
        }

        if (walkingSolidAudioSource != null && walkingSolidAudioSource != activeWalkingAudioSource && walkingSolidAudioSource.isPlaying)
        {
            walkingSolidAudioSource.Stop();
        }

        if (walkingLeavesAudioSource != null && walkingLeavesAudioSource != activeWalkingAudioSource && walkingLeavesAudioSource.isPlaying)
        {
            walkingLeavesAudioSource.Stop();
        }

        activeWalkingAudioSource = null;
    }

    private void OnDrawGizmos()
    {
        if (playerCamera == null) return;
        Gizmos.color = raycastHitThisFrame ? Color.green : Color.red;
        //Physics.SphereCastAll(ray, interactionRadius, 2f, ~0, QueryTriggerInteraction.Ignore);
        //Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        Gizmos.DrawLine(playerCamera.transform.position, playerCamera.transform.position + playerCamera.transform.forward * 2f);
        Gizmos.DrawWireSphere(playerCamera.transform.position + playerCamera.transform.forward * 2f, 0.5f);
    }

    private void PromoteVirtualCameraPriority(GameObject cameraObject)
    {
        RestorePreviousVirtualCameraPriority();

        if (cameraObject == null)
            return;

        Component cameraComponent = FindVirtualCameraComponent(cameraObject);
        if (cameraComponent == null)
            return;

        if (!TryGetPriority(cameraComponent, out int originalPriority))
            return;

        activeVirtualCameraComponent = cameraComponent;
        activeVirtualCameraOriginalPriority = originalPriority;
        SetPriority(cameraComponent, 1000);
    }

    private void RestorePreviousVirtualCameraPriority()
    {
        if (activeVirtualCameraComponent == null)
            return;

        SetPriority(activeVirtualCameraComponent, activeVirtualCameraOriginalPriority);
        activeVirtualCameraComponent = null;
    }

    private static Component FindVirtualCameraComponent(GameObject cameraObject)
    {
        if (cameraObject == null)
            return null;

        Component[] components = cameraObject.GetComponents<Component>();
        for (int i = 0; i < components.Length; i++)
        {
            Component component = components[i];
            if (component == null)
                continue;

            string typeName = component.GetType().Name;
            if (typeName.Contains("Cinemachine") && typeName.Contains("Camera"))
            {
                return component;
            }
        }

        return null;
    }

    private static bool TryGetPriority(Component cameraComponent, out int priority)
    {
        priority = 0;
        if (cameraComponent == null)
            return false;

        PropertyInfo property = cameraComponent.GetType().GetProperty("Priority", BindingFlags.Public | BindingFlags.Instance);
        if (property == null || property.PropertyType != typeof(int) || !property.CanRead)
            return false;

        priority = (int)property.GetValue(cameraComponent);
        return true;
    }

    private static bool SetPriority(Component cameraComponent, int priority)
    {
        if (cameraComponent == null)
            return false;

        PropertyInfo property = cameraComponent.GetType().GetProperty("Priority", BindingFlags.Public | BindingFlags.Instance);
        if (property == null || property.PropertyType != typeof(int) || !property.CanWrite)
            return false;

        property.SetValue(cameraComponent, priority);
        return true;
    }

    private void IsolateInteractionCamera(GameObject selectedCamera)
    {
        RestoreIsolatedInteractionCameras();

        if (selectedCamera == null)
            return;

        Behaviour[] allBehaviours = FindObjectsOfType<Behaviour>(true);
        for (int i = 0; i < allBehaviours.Length; i++)
        {
            Behaviour behaviour = allBehaviours[i];
            if (behaviour == null || !behaviour.enabled)
                continue;

            if (!IsCinemachineCameraBehaviour(behaviour))
                continue;

            if (behaviour.transform.IsChildOf(selectedCamera.transform))
                continue;

            behaviour.enabled = false;
            temporarilyDisabledInteractionCameras.Add(behaviour);
        }
    }

    private void RestoreIsolatedInteractionCameras()
    {
        for (int i = 0; i < temporarilyDisabledInteractionCameras.Count; i++)
        {
            Behaviour behaviour = temporarilyDisabledInteractionCameras[i];
            if (behaviour != null)
            {
                behaviour.enabled = true;
            }
        }

        temporarilyDisabledInteractionCameras.Clear();
    }

    private static bool IsCinemachineCameraBehaviour(Behaviour behaviour)
    {
        System.Type type = behaviour.GetType();
        string namespaceName = type.Namespace ?? string.Empty;
        if (!namespaceName.Contains("Cinemachine"))
            return false;

        string typeName = type.Name;
        return typeName.Contains("Cinemachine") && typeName.Contains("Camera");
    }
}