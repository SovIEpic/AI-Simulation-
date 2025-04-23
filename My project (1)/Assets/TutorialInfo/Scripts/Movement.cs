using UnityEngine;

public class Movement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 120f;
    public bool enableKeyboardTesting = false;

    private float originalMoveSpeed;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        originalMoveSpeed = moveSpeed;
    }

    void Update()
    {
        if (enableKeyboardTesting)
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            Vector3 moveDirection = new Vector3(horizontal, 0, vertical).normalized;
            if (moveDirection != Vector3.zero)
            {
                Move(moveDirection);
            }

            if (Input.GetMouseButton(1))
            {
                float mouseX = Input.GetAxis("Mouse X");
                Rotate(mouseX);
            }
        }
    }

    public void Move(Vector3 direction)
    {
        if (direction != Vector3.zero)
        {
            // Movement
            rb.linearVelocity = direction * moveSpeed;

            // Auto-rotation when not testing
            if (!enableKeyboardTesting)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }
        }
    }

    public void Rotate(float rotationInput)
    {
        transform.Rotate(0, rotationInput * rotationSpeed * Time.deltaTime, 0);
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        moveSpeed = originalMoveSpeed * multiplier;
    }

    public void ResetSpeed()
    {
        moveSpeed = originalMoveSpeed;
    }
}