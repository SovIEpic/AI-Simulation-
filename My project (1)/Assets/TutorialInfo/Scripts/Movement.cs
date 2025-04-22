using UnityEngine;

public class Movement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotationSpeed = 120f;
    public bool enableKeyboardTesting = true;

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
        transform.Translate(direction * moveSpeed * Time.deltaTime, Space.World);
    }

    public void Rotate(float rotationInput)
    {
        transform.Rotate(0, rotationInput * rotationSpeed * Time.deltaTime, 0);
    }
}