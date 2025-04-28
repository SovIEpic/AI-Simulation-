using UnityEngine;
using UnityEngine.UI;

public class BossHpBar : MonoBehaviour
{
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
}
