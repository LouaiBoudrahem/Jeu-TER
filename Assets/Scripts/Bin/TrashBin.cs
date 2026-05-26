using UnityEngine;

public class TrashBin : MonoBehaviour, IInteractable
{
    [Header("Puzzle Pool")]
    [SerializeField] private BinPuzzleData[] puzzlePool;

    [Header("Minigame Setup")]
    [SerializeField] private GameObject trashBinVirtualCamera;

    [Header("Audio")]
    [SerializeField] private AudioClip interactSound;
    [SerializeField] private AudioSource audioSource;

    [Header("References")]
    [SerializeField] private CrumpledPaperUI paperUI;
    [SerializeField] private GameObject paperMesh;      
    [SerializeField] private GameObject inspectPrompt;  

    private BinPuzzleData selectedPuzzle;
    private bool useOutputQuestion;   
    private bool isInUse;
    private bool isCompleted;

    void Start()
    {
        EnsureAudioSource();

        if (inspectPrompt != null) inspectPrompt.SetActive(false);
        if (paperMesh != null)     paperMesh.SetActive(false);
    }

    public void Interact()
    {
        if (isCompleted || isInUse)
        {
            return;
        }

        if (paperUI == null)
        {
            Debug.LogWarning("TrashBin.Interact: paperUI is not assigned.");
            return;
        }

        if (puzzlePool == null || puzzlePool.Length == 0)
        {
            Debug.LogWarning("TrashBin.Interact: puzzlePool is empty.");
            return;
        }

        Player player = FindObjectOfType<Player>();
        if (player == null)
        {
            Debug.LogWarning("TrashBin.Interact: no Player found in scene.");
            return;
        }

        if (trashBinVirtualCamera == null)
        {
            Debug.LogWarning("TrashBin.Interact: trashBinVirtualCamera is not assigned.");
            player.ShowInteractionMessage("Trash bin camera is not assigned.");
            return;
        }

        if (selectedPuzzle == null)
        {
            selectedPuzzle = puzzlePool[Random.Range(0, puzzlePool.Length)];
            useOutputQuestion = Random.value > 0.5f;
        }

        isInUse = true;

        PlayInteractSound();

        if (inspectPrompt != null) inspectPrompt.SetActive(false);
        if (paperMesh != null)     paperMesh.SetActive(true);

        player.BeginComputerInteraction(trashBinVirtualCamera, null);

        paperUI.Open(selectedPuzzle, useOutputQuestion, () => OnPuzzleFinished(player));
    }

    private void OnPuzzleFinished(Player player)
    {
        player.EndComputerInteraction();
        isInUse = false;
        isCompleted = true;
        if (paperMesh != null) paperMesh.SetActive(false);
    }

    private void PlayInteractSound()
    {
        if (interactSound == null)
        {
            return;
        }

        EnsureAudioSource();
        if (audioSource != null)
        {
            audioSource.PlayOneShot(interactSound);
        }
    }

    private void EnsureAudioSource()
    {
        if (audioSource != null)
        {
            return;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }
}