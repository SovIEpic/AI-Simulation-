using UnityEngine;
using UnityEngine.UI;

public class UnitHUDUI : MonoBehaviour
{
    public GameObject hudPanel;
    public Text nameText;
    public Text hpText;   // make sure this is set in Inspector!

    private SelectableUnit currentTarget;

    void Update()
    {
        // 1. Check for mouse click to select a new unit
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                var unit = hit.collider.GetComponent<SelectableUnit>();
                if (unit != null)
                {
                    currentTarget = unit;
                    hudPanel.SetActive(true);
                    nameText.text = unit.unitName;
                }
            }
        }

        // 2. Always update the HP text for the current target
        if (currentTarget != null)
        {
            float hp = currentTarget.GetHP();
            float max = currentTarget.GetMaxHP();
            hpText.text = $"HP: {Mathf.FloorToInt(hp)} / {Mathf.FloorToInt(max)}";

            // 3. Hide HUD if the unit is dead
            if (hp <= 0f)
            {
                hudPanel.SetActive(false);
                currentTarget = null;
            }
        }
    }
}
