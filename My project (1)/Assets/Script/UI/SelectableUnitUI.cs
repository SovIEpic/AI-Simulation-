using UnityEngine;

public class SelectableUnit : MonoBehaviour
{
    public string unitName;

    private CharacterStats characterStats;

    void Awake()
    {
        characterStats = GetComponent<CharacterStats>();
    }

    public float GetHP()
    {
        return characterStats != null ? characterStats.currentHealth : 0;
    }

    public float GetMaxHP()
    {
        return characterStats != null ? characterStats.maxHealth : 0;
    }
}
