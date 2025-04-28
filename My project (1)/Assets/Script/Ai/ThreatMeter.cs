using System.Collections.Generic;
using UnityEngine;

public class ThreatMeter
{
    private Dictionary<Transform, float> threatLevels = new Dictionary<Transform, float>();
    private float decayRate;

    public ThreatMeter(List<Transform> players, float decayRate = 1f)
    {
        foreach (var player in players)
        {
            if (player != null)
                threatLevels[player] = 0f;
        }
        this.decayRate = decayRate;
    }
    public void CleanupDeadPlayers()
    {
        List<Transform> deadPlayers = new List<Transform>();

        foreach (var pair in threatLevels)
        {
            if (pair.Key == null || !pair.Key.gameObject.activeInHierarchy)
            {
                deadPlayers.Add(pair.Key);
            }
        }

        foreach (var dead in deadPlayers)
        {
            threatLevels.Remove(dead);
        }
    }

    public void AddThreat(Transform player, float amount)
    {
        if (player == null) return;
        if (!threatLevels.ContainsKey(player))
            threatLevels[player] = 0f;

        threatLevels[player] += amount;
    }

    public float GetThreatValue(Transform player)
    {
        if (player == null || !threatLevels.ContainsKey(player)) return 0f;
        return threatLevels[player];
    }

    public Transform GetHighestThreatTarget()
    {
        Transform bestTarget = null;
        float highestThreat = float.MinValue;

        foreach (var kvp in threatLevels)
        {
            if (kvp.Value > highestThreat)
            {
                highestThreat = kvp.Value;
                bestTarget = kvp.Key;
            }
        }
        return bestTarget;
    }

    public void AddDamageThreat(Transform player, float damage)
    {
        AddThreat(player, damage);
    }

    public void AddHealingThreat(Transform player, float healAmount)
    {
        AddThreat(player, healAmount * 0.5f); // Healing generates 50% threat compared to damage
    }

    public void AddCCThreat(Transform player, float ccAmount)
    {
        AddThreat(player, ccAmount * 2f); // CC abilities generate double threat
    }

    public void DecayThreat(float deltaTime)
    {
        List<Transform> keys = new List<Transform>(threatLevels.Keys);
        foreach (var key in keys)
        {
            threatLevels[key] = Mathf.Max(0, threatLevels[key] - decayRate * deltaTime);
        }
    }

    public void CleanupInactiveTargets()
    {
        List<Transform> toRemove = new List<Transform>();
        foreach (var kvp in threatLevels)
        {
            if (kvp.Key == null || !kvp.Key.gameObject.activeInHierarchy)
                toRemove.Add(kvp.Key);
        }

        foreach (var key in toRemove)
        {
            threatLevels.Remove(key);
        }
    }
}