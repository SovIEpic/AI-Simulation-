using System.Collections.Generic;
using System.Linq;
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

    public Transform GetTopThreatTarget()
    {
        return threatLevels.OrderByDescending(t => t.Value).First().Key;
    }
}
