using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Abilities : MonoBehaviour
{
    public enum CharacterType { Tank, Healer, SwordMaster, Assassin } // class selection in unity inspector

    [Header("Character Type")]
    public CharacterType characterType;

    public GameObject abilityUI;
    public Slider healthBarSlider;


    [Header("HP Display")]  // text that shows hp numbers
    public TextMeshProUGUI hpText;
    public bool showCurrentHP = true;
    public bool showMaxHP = true;
    public string format = "{0}/{1}"; //{0} = current HP, {1} = max HP

    [Header("Ability1")]
    public Image abilityImage1;
    public float cooldown1;
    [HideInInspector] public bool isCooldown1 = false; 
    public KeyCode ability1;

    [Header("Ability2")]
    public Image abilityImage2;
    public float cooldown2;
    [HideInInspector] public bool isCooldown2 = false;
    public KeyCode ability2;

    [Header("Ability3")]
    public Image abilityImage3;
    public float cooldown3;
    [HideInInspector] public bool isCooldown3 = false;
    public KeyCode ability3;

    void Reset()
    {
        // Initialize all abilities as ready
        abilityImage1.fillAmount = 0;
        abilityImage2.fillAmount = 0;
        abilityImage3.fillAmount = 0;

        // set cooldowns based on class
        if (characterType == CharacterType.Tank)
        {
            cooldown1 = 15f;
            cooldown2 = 8f;
            cooldown3 = 10f;
        }
        else if (characterType == CharacterType.Healer)
        {
            cooldown1 = 10000f;
            cooldown2 = 20f;
            cooldown3 = 10000f;
        }
        else if (characterType == CharacterType.SwordMaster)
        {
            cooldown1 = 25f;
            cooldown2 = 40f;
            cooldown3 = 30f;
        }
        else if (characterType == CharacterType.Assassin)
        {
            cooldown1 = 140f;
            cooldown2 = 140f;
            cooldown3 = 140f;
        }
    }

    // only show stuff and update if the unit is selected
    void Update()
    {
        bool isSelected = IsThisCharacterSelected();
        SetAbilityUIActive(isSelected);

        if (isSelected)
        {
            UpdateCooldowns();
            CheckPlayerInput();
            UpdateHealthBar();
        }

    }

        void UpdateCooldowns()
        {
        // ability 1 cooldown timer
        if (isCooldown1)
            {
                abilityImage1.fillAmount -= 1 / cooldown1 * Time.deltaTime;
                if (abilityImage1.fillAmount <= 0)
                {
                    abilityImage1.fillAmount = 0;
                    isCooldown1 = false;
                }
            }

        // ability 2 cooldown timer
        if (isCooldown2)
            {
                abilityImage2.fillAmount -= 1 / cooldown2 * Time.deltaTime;
                if (abilityImage2.fillAmount <= 0)
                {
                    abilityImage2.fillAmount = 0;
                    isCooldown2 = false;
                }
            }

        // ability 3 cooldown timer
        if (isCooldown3)
            {
                abilityImage3.fillAmount -= 1 / cooldown3 * Time.deltaTime;
                if (abilityImage3.fillAmount <= 0)
                {
                    abilityImage3.fillAmount = 0;
                    isCooldown3 = false;
                }
            }
        }

    // press the keys to use abilities
    void CheckPlayerInput()
        {
            if (Input.GetKey(ability1)) TryStartAbility(1);
            if (Input.GetKey(ability2)) TryStartAbility(2);
            if (Input.GetKey(ability3)) TryStartAbility(3);
        }

    // starts an ability if it's not on cooldown
    public void TryStartAbility(int abilityIndex)
        {
            switch (abilityIndex)
            {
                case 1:
                    if (!isCooldown1)
                    {
                        isCooldown1 = true;
                        abilityImage1.fillAmount = 1;
                    }
                    break;
                case 2:
                    if (!isCooldown2)
                    {
                        isCooldown2 = true;
                        abilityImage2.fillAmount = 1;
                    }
                    break;
                case 3:
                    if (!isCooldown3)
                    {
                        isCooldown3 = true;
                        abilityImage3.fillAmount = 1;
                    }
                    break;
            }
        }

    /* handling Ability UI differently now
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
    */

    // updates hp bar and text for selected unit
    public void UpdateHealthBar()
        {
            if (!IsThisCharacterSelected()) return;

            var stats = GetComponent<CharacterStats>();
            if (stats == null || hpText == null) return;
            
         
            healthBarSlider.maxValue = stats.GetMaxHP();
            healthBarSlider.value = stats.GetCurrentHP();

        // format: current/max (like 120/200)
        hpText.text = $"{stats.GetCurrentHP():0}/{stats.GetMaxHP():0}";
            
        }

    // show or hide the ability ui
    public void SetAbilityUIActive(bool isActive)
        {
            if (abilityUI != null)
            {
                abilityUI.SetActive(isActive);
            }
        }

    // check if this unit is the only one selected
    private bool IsThisCharacterSelected()
    {
        //check if this character is in the selected units list
        if (UnitSelectionManager.Instance != null &&
            UnitSelectionManager.Instance.unitsSelected.Count == 1)
        {
            return UnitSelectionManager.Instance.unitsSelected[0] == gameObject;
        }
        return false;
    }


}

