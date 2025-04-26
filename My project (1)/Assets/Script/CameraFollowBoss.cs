using UnityEngine;

public class CameraFollowBoss : MonoBehaviour
{
    public Transform bossTarget;
    private Vector3 offset;

    void Start()
    {
        if (bossTarget == null) return;

        // Calculate and store the offset from the initial camera-to-boss position
        offset = transform.position - bossTarget.position;
    }

    void LateUpdate()
    {
        if (bossTarget == null) return;

        // Maintain the initial offset position relative to the boss
        transform.position = bossTarget.position + offset;
    }
}
