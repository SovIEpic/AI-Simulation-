using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public List<CharacterStats> aiCharacters = new List<CharacterStats>();
    public CharacterStats boss;

    void Awake()
    {
        Instance = this;
    }

    public void CheckWinCondition()
    {
        if (boss.currentHealth <= 0)
        {
            Debug.Log("AI Team Wins!");
        }
        else if (aiCharacters.Count == 0)
        {
            Debug.Log("Boss Wins!");
        }
    }
}