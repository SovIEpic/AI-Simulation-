using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class statsInputManager : MonoBehaviour
{
    public TMP_InputField hpInputField;
    public float hpLevel; 
    public TMP_InputField powerInputField;
    public float powerLevel;

    private testUnit selectedUnit; //referencing selected units(not used yet)

    void Start()
    {
        // add listeners for when player finishes editing HP and Power(Attack)
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
                        characterStats.SetHealth(value);  // only update this unit health
                    }
                }
            }
            else
            {
                Debug.LogWarning("no unit selected");
            }
        }
        else
        {
            Debug.LogWarning("Invalid input");
        }
    }

    void UpdatePower(string input)
    {
        if (float.TryParse(input, out float value))
        {

            if (UnitSelectionManager.Instance.unitsSelected.Count > 0) // check if any unit is selected
            {
                foreach (var unit in UnitSelectionManager.Instance.unitsSelected) // loop through all selected units
                {
                    var characterStats = unit.GetComponent<CharacterStats>();
                    if (characterStats != null)
                    {
                        characterStats.SetDamage(value);  // only update this units power
                    }
                }
            }
            else
            {
                Debug.LogWarning("no unit selected");
            }
        }
        else
        {
            Debug.LogWarning("Invalid input");
        }
    }

    // get the first selected unit
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
