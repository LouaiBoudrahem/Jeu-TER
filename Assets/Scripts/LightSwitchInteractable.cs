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

    [Header("Audio")]
    [SerializeField] private AudioClip toggleClip;
    [SerializeField] private AudioSource audioSource;

    private Quaternion switchOffLocalRotation;
    private bool isOn;

    private void Awake()
    {
        if (switchHandle != null)
        {
            switchOffLocalRotation = switchHandle.localRotation;
        }

        ApplyState(startOn, false);
    }

    public void Interact()
    {
        ApplyState(!isOn, true);
    }

    public void SetState(bool turnOn)
    {
        ApplyState(turnOn, true);
    }

    private void ApplyState(bool turnOn, bool playSound)
    {
        isOn = turnOn;

        if (playSound)
        {
            PlayToggleSound();
        }

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

    private void PlayToggleSound()
    {
        if (toggleClip == null)
        {
            return;
        }

        EnsureAudioSource();
        if (audioSource != null)
        {
            audioSource.PlayOneShot(toggleClip);
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
