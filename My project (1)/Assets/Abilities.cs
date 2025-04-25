using UnityEngine;
using UnityEngine.UI;
public class Abilities : MonoBehaviour
{
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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        abilityImage1.fillAmount = 1;
        abilityImage2.fillAmount = 1;
        abilityImage3.fillAmount = 1;
    }

    // Update is called once per frame
    void Update()
    {
        Ability1();
        Ability2();
        Ability3();
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
}
