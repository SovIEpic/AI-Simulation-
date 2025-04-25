using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class statsInputManager : MonoBehaviour
{
    public TMP_InputField hpInputField;
    public float hpLevel;

    void Start()
    {
        hpInputField.onEndEdit.AddListener(UpdateHealthPoint);
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
}
