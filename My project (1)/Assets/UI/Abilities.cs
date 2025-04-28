using UnityEngine;
using UnityEngine.UI;


public class Abilities : MonoBehaviour
{

    public GameObject abilityUI;
    public Slider healthBarSlider;

    [Header("Ability1")]
    public Image abilityImage1;
    public float cooldown1 = 10;
    bool isCooldown = false;
    public KeyCode ability1;

    [Header("Ability2")]
    public Image abilityImage2;
    public float cooldown2 = 5;
    bool isCooldown2 = false;
    public KeyCode ability2;

    [Header("Ability3")]
    public Image abilityImage3;
    public float cooldown3 = 5;
    bool isCooldown3 = false;
    public KeyCode ability3;

    // start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        abilityImage1.fillAmount = 1;
        abilityImage2.fillAmount = 1;
        abilityImage3.fillAmount = 1;
    }

    // Update is called once per frame
    void Update()
    {   

        UpdateAbilityUI(); 
        //check if the UI is already activated
        if (abilityUI.activeSelf)
        {
            Ability1();
            Ability2();
            Ability3();
        }
    }

    void Ability1()
    {
        if(Input.GetKey(ability1)&& isCooldown == false)
        {
            isCooldown = true;
            abilityImage1.fillAmount = 1;
        }

        if (isCooldown)
        {
            abilityImage1.fillAmount -= 1 / cooldown1 * Time.deltaTime;

            if(abilityImage1.fillAmount <= 0)
            {
                abilityImage1.fillAmount = 1;
                isCooldown=false;
            }
        }
    }
    void Ability2()
    {
        if (Input.GetKey(ability2) && isCooldown2 == false)
        {
            isCooldown2 = true;
            abilityImage2.fillAmount = 1;
        }

        if (isCooldown2)
        {
            abilityImage2.fillAmount -= 1 / cooldown2 * Time.deltaTime;

            if (abilityImage2.fillAmount <= 0)
            {
                abilityImage2.fillAmount = 1;
                isCooldown2 = false;
            }
        }
    }
    void Ability3()
    {
        if (Input.GetKey(ability3) && isCooldown3 == false)
        {
            isCooldown3 = true;
            abilityImage3.fillAmount = 1;
        }

        if (isCooldown3)
        {
            abilityImage3.fillAmount -= 1 / cooldown3 * Time.deltaTime;

            if (abilityImage3.fillAmount <= 0)
            {
                abilityImage3.fillAmount = 1;
                isCooldown3 = false;
            }
        }
    }

    // function to update the ability UI based on selected unit
    public void UpdateAbilityUI()
    {
        if (UnitSelectionManager.Instance.unitsSelected.Count == 1)
        {
            abilityUI.SetActive(true);
            UpdateHealthBar();
        }
        else
        {
            abilityUI.SetActive(false);
        }
    }
    // function to update the health bar based on the selected unit
    public void UpdateHealthBar()
    {
        if (UnitSelectionManager.Instance.unitsSelected.Count == 1)
        {
            var selectedUnit = UnitSelectionManager.Instance.unitsSelected[0];  // get the first selected unit
            var stats = selectedUnit.GetComponent<CharacterStats>(); // get character stats component of the unit
            if (stats != null && healthBarSlider != null)
            {
                healthBarSlider.maxValue = stats.GetMaxHP(); // set the max value of the health bar
                healthBarSlider.value = stats.GetCurrentHP(); // as well as current in case current HP went higher than MaxHP
            }
        }
    }
}
