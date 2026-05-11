using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class DoorLockKeypadButton : MonoBehaviour
{
    [SerializeField] private DoorLockKeypad keypad;
    [SerializeField] private string symbol = "1";

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(HandleClick);

        if (keypad == null)
        {
            keypad = GetComponentInParent<DoorLockKeypad>(true);
        }

        if (string.IsNullOrEmpty(symbol))
        {
            symbol = "0";
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
        }
    }

    public void SetKeypad(DoorLockKeypad keypadController)
    {
        keypad = keypadController;
    }

    public void SetSymbol(string newSymbol)
    {
        symbol = string.IsNullOrEmpty(newSymbol) ? "0" : newSymbol.Substring(0, 1);
    }

    private void HandleClick()
    {
        if (keypad == null)
        {
            keypad = GetComponentInParent<DoorLockKeypad>(true);
        }

        if (keypad == null)
        {
            Debug.LogWarning($"DoorLockKeypadButton on '{name}': no DoorLockKeypad found. Assign keypad or place this button under keypad root.");
            return;
        }

        keypad.PressSymbol(symbol);
    }
}