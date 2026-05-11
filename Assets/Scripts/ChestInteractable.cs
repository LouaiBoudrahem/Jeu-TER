using UnityEngine;

public class ChestInteractable : MonoBehaviour, IInteractable
{
    [Header("Chest Lid")]
    [SerializeField] private Transform[] lidTransforms;
    [SerializeField] private Vector3[] openPositions;
    [SerializeField] private Vector3[] openRotations;
    [SerializeField] private float openDuration = 0.5f;
    
    private Vector3[] closedPositions;
    private Quaternion[] closedRotations;
    private Collider chestCollider;
    private bool isOpen = false;
    private float openProgress = 0f;
    private bool isAnimating = false;

    private void Start()
    {
        if (lidTransforms == null || lidTransforms.Length == 0)
        {
            Debug.LogWarning("ChestInteractable: lidTransforms is not assigned or empty.");
            return;
        }

        chestCollider = GetComponent<Collider>();

        closedPositions = new Vector3[lidTransforms.Length];
        closedRotations = new Quaternion[lidTransforms.Length];
        for (int i = 0; i < lidTransforms.Length; i++)
        {
            if (lidTransforms[i] != null)
            {
                closedPositions[i] = lidTransforms[i].localPosition;
                closedRotations[i] = lidTransforms[i].localRotation;
            }
        }

        
        if (openPositions == null || openPositions.Length != lidTransforms.Length)
        {

        }
        if (openRotations == null || openRotations.Length != lidTransforms.Length)
        {
            
        }
    }

    private void Update()
    {
        if (isAnimating)
        {
            openProgress += Time.deltaTime / openDuration;
            openProgress = Mathf.Clamp01(openProgress);

            for (int i = 0; i < lidTransforms.Length; i++)
            {
                if (lidTransforms[i] != null && i < openPositions.Length && i < openRotations.Length)
                {
                    Quaternion targetRotation = Quaternion.Euler(openRotations[i]);

                    if (isOpen)
                    {
                        lidTransforms[i].localPosition = Vector3.Lerp(closedPositions[i], openPositions[i], openProgress);
                        lidTransforms[i].localRotation = Quaternion.Lerp(closedRotations[i], targetRotation, openProgress);
                    }
                    else
                    {
                        lidTransforms[i].localPosition = Vector3.Lerp(openPositions[i], closedPositions[i], openProgress);
                        lidTransforms[i].localRotation = Quaternion.Lerp(targetRotation, closedRotations[i], openProgress);
                    }
                }
            }

            if (openProgress >= 1f)
            {
                isAnimating = false;
            }
        }
    }

    public void Interact()
    {
        if (isAnimating)
        {
            return;
        }

        isOpen = !isOpen;
        openProgress = 0f;
        isAnimating = true;

        
        if (chestCollider != null)
        {
            chestCollider.enabled = !isOpen;
        }
    }
}
