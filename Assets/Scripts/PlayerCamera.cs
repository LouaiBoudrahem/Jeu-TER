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
    [SerializeField] private float crouchPositionSmoothTime = 0.08f;
    private float verticalSmoothVelocity;

    public void Initialize(Transform target)
    {
        this.transform.position = target.position;
        this.transform.eulerAngles = eulerAngles = target.eulerAngles;
        verticalSmoothVelocity = 0f;

    }

    public void UpdateRotation(CameraInput input)
    {
        eulerAngles += new Vector3(-input.Look.y, input.Look.x, 0f) * sensitivity;
        this.transform.eulerAngles = eulerAngles;
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
