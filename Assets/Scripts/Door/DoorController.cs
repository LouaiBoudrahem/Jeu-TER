using System.Collections;
using UnityEngine;

public class DoorController : MonoBehaviour
{
    [Header("Door")]
    [SerializeField] private Transform doorTransform;
    [SerializeField] private bool useLocalRotation = true;
    [SerializeField] private Vector3 openRotation = new Vector3(0f, 90f, 0f);
    [SerializeField, Min(0f)] private float overshootDegrees = 3f;

    [Header("Animation")]
    [SerializeField, Min(0.01f)] private float openDuration = 1f;
    [SerializeField] private AnimationCurve openCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField, Min(0f)] private float settleDuration = 0.2f;
    [SerializeField] private AnimationCurve settleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Quaternion closedLocalRotation;
    private Quaternion closedWorldRotation;
    private Coroutine openRoutine;
    private bool isOpen;

    private void Awake()
    {
        if (doorTransform == null)
        {
            doorTransform = transform;
        }

        closedLocalRotation = doorTransform.localRotation;
        closedWorldRotation = doorTransform.rotation;
    }

    public void OpenDoor()
    {
        if (isOpen)
        {
            return;
        }

        if (openRoutine != null)
        {
            StopCoroutine(openRoutine);
        }

        openRoutine = StartCoroutine(OpenDoorRoutine());
    }

    public void ResetDoor()
    {
        if (openRoutine != null)
        {
            StopCoroutine(openRoutine);
            openRoutine = null;
        }

        isOpen = false;
        ApplyRotation(0f);
    }

    private IEnumerator OpenDoorRoutine()
    {
        Quaternion closedRotation = GetClosedRotation();
        Quaternion finalRotation = BuildTargetRotation(openRotation);

        bool shouldSettle = overshootDegrees > 0f && settleDuration > 0f && openRotation.sqrMagnitude > 0f;
        Quaternion primaryTarget = shouldSettle ? BuildTargetRotation(GetOvershootRotation()) : finalRotation;

        float elapsed = 0f;
        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;
            float t = openCurve.Evaluate(Mathf.Clamp01(elapsed / openDuration));
            SetDoorRotation(Quaternion.Slerp(closedRotation, primaryTarget, t));
            yield return null;
        }

        SetDoorRotation(primaryTarget);

        if (shouldSettle)
        {
            elapsed = 0f;
            while (elapsed < settleDuration)
            {
                elapsed += Time.deltaTime;
                float t = settleCurve.Evaluate(Mathf.Clamp01(elapsed / settleDuration));
                SetDoorRotation(Quaternion.Slerp(primaryTarget, finalRotation, t));
                yield return null;
            }
        }

        SetDoorRotation(finalRotation);
        isOpen = true;
        openRoutine = null;
    }

    private void ApplyRotation(float t)
    {
        Quaternion targetRotation = BuildTargetRotation(openRotation);

        if (useLocalRotation)
        {
            doorTransform.localRotation = Quaternion.Slerp(closedLocalRotation, targetRotation, t);
        }
        else
        {
            doorTransform.rotation = Quaternion.Slerp(closedWorldRotation, targetRotation, t);
        }
    }

    private Vector3 GetOvershootRotation()
    {
        Vector3 direction = openRotation.normalized;
        return openRotation + (direction * overshootDegrees);
    }

    private Quaternion BuildTargetRotation(Vector3 relativeEuler)
    {
        return useLocalRotation
            ? closedLocalRotation * Quaternion.Euler(relativeEuler)
            : Quaternion.Euler(relativeEuler) * closedWorldRotation;
    }

    private Quaternion GetClosedRotation()
    {
        return useLocalRotation ? closedLocalRotation : closedWorldRotation;
    }

    private void SetDoorRotation(Quaternion rotation)
    {
        if (useLocalRotation)
        {
            doorTransform.localRotation = rotation;
        }
        else
        {
            doorTransform.rotation = rotation;
        }
    }
}