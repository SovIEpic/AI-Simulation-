using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class statsInputManager : MonoBehaviour
{
    public TMP_InputField hpInputField;
    public float hpLevel;
    public TMP_InputField powerInputField;
    public float powerLevel;

    private testUnit selectedUnit;

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

    }

    void UpdateHealthPoint(string input)
    {
        if (float.TryParse(input, out float value))
        {
            Debug.Log("HP input updated to: " + value);

            if (UnitSelectionManager.Instance.unitsSelected.Count > 0)
            {
                foreach (var unit in UnitSelectionManager.Instance.unitsSelected)
                {
                    var characterStats = unit.GetComponent<CharacterStats>();
                    if (characterStats != null)
                    {
                        characterStats.SetHealth(value);  // Only update this unit's health
                    }
                }
            }
            else
            {
                Debug.LogWarning("No unit selected to update HP!");
            }
        }
        else
        {
            Debug.LogWarning("Invalid input for HP!");
        }
    }

    void UpdatePower(string input)
    {
        if (float.TryParse(input, out float value))
        {
            Debug.Log("Power input updated to: " + value);

            if (UnitSelectionManager.Instance.unitsSelected.Count > 0)
            {
                foreach (var unit in UnitSelectionManager.Instance.unitsSelected)
                {
                    var characterStats = unit.GetComponent<CharacterStats>();
                    if (characterStats != null)
                    {
                        characterStats.SetDamage(value);  // Only update this unit's power
                    }
                }
            }
            else
            {
                Debug.LogWarning("No unit selected to update Power!");
            }
        }
        else
        {
            Debug.LogWarning("Invalid input for Power!");
        }
    }

    private testUnit SelectedUnit
    {
        get
        {
            if (UnitSelectionManager.Instance.unitsSelected.Count > 0)
            {
                return UnitSelectionManager.Instance.unitsSelected[0].GetComponent<testUnit>();
            }
            return null;
        }
    }
}
