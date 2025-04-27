using UnityEngine;
using System.Collections.Generic;

public class ThreatMeter
{
    private Dictionary<Transform, float> threatTable = new Dictionary<Transform, float>();

    public ThreatMeter(List<Transform> players)
    {
        foreach (var player in players)
        {
            threatTable[player] = 0f;
        }
    }

    public void AddThreat(Transform target, float amount)
    {
        if (threatTable.ContainsKey(target))
        {
            threatTable[target] += amount;
        }
    }

    public float GetThreat(Transform target)
    {
        return threatTable.ContainsKey(target) ? threatTable[target] : 0f;
    }

    public Transform GetHighestThreatTarget()
    {
        Transform highestTarget = null;
        float highestThreat = float.MinValue;

        foreach (var entry in threatTable)
        {
            if (entry.Value > highestThreat)
            {
                highestThreat = entry.Value;
                highestTarget = entry.Key;
            }
        }

        return highestTarget;
    }

    public void CleanupInactiveTargets()
    {
        var toRemove = new List<Transform>();
        foreach (var entry in threatTable)
        {
            if (entry.Key == null || !entry.Key.gameObject.activeInHierarchy)
            {
                toRemove.Add(entry.Key);
            }
        }

        foreach (var target in toRemove)
        {
            threatTable.Remove(target);
        }
    }

    public float GetThreatValue(Transform target)
    {
        return threatTable.ContainsKey(target) ? threatTable[target] : 0f;
    }
}