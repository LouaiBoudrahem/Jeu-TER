using UnityEngine;

public class LightSwitchInteractable : MonoBehaviour, IInteractable
{
    [Header("Lights")]
    [SerializeField] private Light[] lights;
    [SerializeField] private float onIntensity = 10f;
    [SerializeField] private float offIntensity = 0f;

    [Header("Switch Visual")]
    [SerializeField] private Transform switchHandle;
    [SerializeField] private float switchOnRotationX = 20f;
    [SerializeField] private bool startOn;

    private Quaternion switchOffLocalRotation;
    private bool isOn;

    private void Awake()
    {
        if (switchHandle != null)
        {
            switchOffLocalRotation = switchHandle.localRotation;
        }

        ApplyState(startOn);
    }

    public void Interact()
    {
        ApplyState(!isOn);
    }

    private void ApplyState(bool turnOn)
    {
        isOn = turnOn;

        float targetIntensity = isOn ? onIntensity : offIntensity;
        if (lights != null)
        {
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null)
                {
                    lights[i].intensity = targetIntensity;
                }
            }
        }

        if (switchHandle != null)
        {
            switchHandle.localRotation = isOn
                ? switchOffLocalRotation * Quaternion.Euler(switchOnRotationX, 0f, 0f)
                : switchOffLocalRotation;
        }
    }
}
