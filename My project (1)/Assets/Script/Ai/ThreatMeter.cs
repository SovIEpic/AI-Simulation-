using System.Collections.Generic;
using UnityEngine;

public class ThreatMeter
{
    private Dictionary<Transform, float> threatLevels = new Dictionary<Transform, float>();

    public ThreatMeter(List<Transform> players)
    {
        foreach (var p in players)
            threatLevels[p] = 0f;
    }

    public void AddThreat(Transform target, float amount)
    {
        if (threatLevels.ContainsKey(target))
            threatLevels[target] += amount;
    }

    public float GetThreatValue(Transform target)
    {
        return threatLevels.ContainsKey(target) ? threatLevels[target] : 0f;
    }

    public Transform GetTopThreatTarget()
    {
        float maxThreat = float.MinValue;
        Transform topTarget = null;

        foreach (var kvp in threatLevels)
        {
            if (kvp.Value > maxThreat)
            {
                maxThreat = kvp.Value;
                topTarget = kvp.Key;
            }
        }

        return topTarget;
    }
}
