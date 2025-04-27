using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public float moveSpeed = 10f;

    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal"); // A, D (left/right)
        float vertical = Input.GetAxis("Vertical");     // W, S (forward/back)

        Vector3 move = new Vector3(horizontal, 0, vertical);
        transform.Translate(move * moveSpeed * Time.deltaTime, Space.World);
    }
}