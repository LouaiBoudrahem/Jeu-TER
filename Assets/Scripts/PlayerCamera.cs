using Unity.VisualScripting;
using UnityEngine;



public struct CameraInput
{
    public Vector2 Look;
}

public class PlayerCamera : MonoBehaviour
{

    private Vector3 eulerAngles;
    [SerializeField] private float sensitivity = 0.1f;
    [SerializeField] private float maxPitch = 89f;
    [SerializeField] private float crouchPositionSmoothTime = 0.08f;
    private float verticalSmoothVelocity;

    public void Initialize(Transform target)
    {
        this.transform.position = target.position;
        this.transform.eulerAngles = eulerAngles = target.eulerAngles;
        verticalSmoothVelocity = 0f;

    }

    public void ResetAfterRespawn(Transform target)
    {
        if (target == null)
            return;

        transform.position = target.position;
        eulerAngles = target.eulerAngles;
        eulerAngles = ClampEulerAngles(eulerAngles);
        transform.eulerAngles = eulerAngles;
        verticalSmoothVelocity = 0f;
    }

    public void UpdateRotation(CameraInput input)
    {
        float pitch = eulerAngles.x;
        if (pitch > 180f) pitch -= 360f;

        float yaw = eulerAngles.y;

        pitch += -input.Look.y * sensitivity;
        yaw += input.Look.x * sensitivity;

        pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);

        if (pitch < 0f) pitch += 360f;
        yaw = Mathf.Repeat(yaw, 360f);

        eulerAngles = new Vector3(pitch, yaw, 0f);
        this.transform.eulerAngles = eulerAngles;
    }

    private Vector3 ClampEulerAngles(Vector3 angles)
    {
        float pitch = angles.x;
        if (pitch > 180f) pitch -= 360f;
        pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);
        if (pitch < 0f) pitch += 360f;

        float yaw = Mathf.Repeat(angles.y, 360f);
        return new Vector3(pitch, yaw, 0f);
    }

    public void UpdatePosition(Transform target)
    {
        Vector3 currentPosition = this.transform.position;
        float smoothedY = Mathf.SmoothDamp(
            currentPosition.y,
            target.position.y,
            ref verticalSmoothVelocity,
            crouchPositionSmoothTime);

        this.transform.position = new Vector3(target.position.x, smoothedY, target.position.z);
    }   

}
