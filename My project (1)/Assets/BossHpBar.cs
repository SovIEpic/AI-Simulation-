using UnityEngine;
using UnityEngine.UI;

public class BossHpBar : MonoBehaviour
{
    /* Boss Hp was not linked to bossStats, instead it links to a character script in boss object 
     * someone broke it so that this part of code doesnt work no more
     
    public Slider slider;
    public BossStats bossStats;

    void Start()
    {
        slider.maxValue = bossStats.maxHP;
        slider.value = bossStats.currentHP;
    }

    void Update()
    {
        slider.value = bossStats.currentHP; // Live updates
    }
    */

    public Slider slider;
    public CharacterStats bossCharacterStats;

    void Start()
    {
        if (bossCharacterStats != null)
        {
            slider.maxValue = bossCharacterStats.maxHealth;
            slider.value = bossCharacterStats.currentHealth;
        }
    }

    void Update()
    {
        if (bossCharacterStats != null)
        {
            slider.value = bossCharacterStats.currentHealth;
        }
    }

}
