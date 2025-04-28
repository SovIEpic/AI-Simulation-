using UnityEngine;

public class Movement : MonoBehaviour
{
    public float speed = 3.5f;
    public float rotationSpeed = 90f;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // Only freeze rotations, NOT Y position
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    public void Move(Vector3 direction)
    {

        if (direction.magnitude > 0.1f)
        {
            // Only modify X/Z velocity (preserve Y for gravity)
            Vector3 newVelocity = new Vector3(
                direction.x * speed,
                rb.linearVelocity.y, // Keep existing Y velocity (gravity)
                direction.z * speed
            );

            rb.linearVelocity = Vector3.Lerp(
                rb.linearVelocity,
                newVelocity,
                Time.deltaTime * 5f
            );

            // Rotation
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
        else
        {
            // Only zero X/Z velocity (preserve Y for gravity)
            rb.linearVelocity = new Vector3(
                Mathf.Lerp(rb.linearVelocity.x, 0, Time.deltaTime * 5f),
                rb.linearVelocity.y,
                Mathf.Lerp(rb.linearVelocity.z, 0, Time.deltaTime * 5f)
            );
        }
    }
}