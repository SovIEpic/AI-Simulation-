using UnityEngine;
using UnityEngine.UI;

public class UnitHUDUI : MonoBehaviour
{
    public GameObject hudPanel;
    public Text nameText;
    public Slider hpSlider;

    private SelectableUnit currentTarget;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Debug.Log("Clicked: " + hit.collider.name);
                var unit = hit.collider.GetComponent<SelectableUnit>();
                if (unit != null)
                {
                    currentTarget = unit;
                    hudPanel.SetActive(true);
                    nameText.text = unit.unitName;
                }
            }
        }

        if (currentTarget != null)
        {
            float hp = currentTarget.GetHP();
            float max = currentTarget.GetMaxHP();
            hpSlider.value = Mathf.Clamp01(hp / max);

            if (hp <= 0f)
            {
                hudPanel.SetActive(false);
                currentTarget = null;
            }
        }
    }
}
