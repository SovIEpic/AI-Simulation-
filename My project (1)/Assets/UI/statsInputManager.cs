using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class statsInputManager : MonoBehaviour
{
    public TMP_InputField hpInputField;
    public float hpLevel;
    public TMP_InputField powerInputField;
    public float powerLevel;
    public TMP_InputField amourInputField;
    public float amourLevel;
    public TMP_InputField speedInputField;
    public float speedLevel;

    void Start()
    {
        if (hpInputField != null)
        {
            hpInputField.onEndEdit.AddListener(UpdateHealthPoint);
        }

        if (powerInputField != null)
        {
            powerInputField.onEndEdit.AddListener(UpdatePower);
        }

        if (amourInputField != null)
        {
            amourInputField.onEndEdit.AddListener(UpdateAmour);
        }
        if (speedInputField != null)
        {
            speedInputField.onEndEdit.AddListener(UpdateSpeed);
        }

    }

    void UpdateHealthPoint(string input)
    {
        if (float.TryParse(input, out float value))
        {
            hpLevel = value;
            Debug.Log("HP updated to: " + hpLevel);
        }
        else
        {
            Debug.LogWarning("Invalid input for hp");
        }
    }
    void UpdatePower(string input)
    {
        if (float.TryParse(input, out float value))
        {
            powerLevel = value;
            Debug.Log("Power updated to: " + powerLevel);
        }
        else
        {
            Debug.LogWarning("Invalid input for Power");
        }
    }
    void UpdateAmour(string input)
    {
        if (float.TryParse(input, out float value))
        {
            amourLevel = value;
            Debug.Log("Amour updated to: " + amourLevel);
        }
        else
        {
            Debug.LogWarning("Invalid input for Amour");
        }
    }
    void UpdateSpeed(string input)
    {
        if (float.TryParse(input, out float value))
        {
            speedLevel = value;
            Debug.Log("Speed updated to: " + speedLevel);
        }
        else
        {
            Debug.LogWarning("Invalid input for Speed");
        }
    }
}
