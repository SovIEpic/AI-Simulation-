using UnityEngine;

public class DebugFlyCamera : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float lookSpeed = 2f;
    public float sprintMultiplier = 3f;

    [Header("Keybindings")]
    public KeyCode lookKey = KeyCode.Mouse1;
    public KeyCode moveUpKey = KeyCode.Space;
    public KeyCode moveDownKey = KeyCode.LeftControl;
    public KeyCode sprintKey = KeyCode.LeftShift;

    private float _rotationX = 0f;
    private float _rotationY = 0f;

    void Update()
    {
        if (Input.GetKey(lookKey))
        {
            _rotationX += Input.GetAxis("Mouse X") * lookSpeed;
            _rotationY -= Input.GetAxis("Mouse Y") * lookSpeed;
            _rotationY = Mathf.Clamp(_rotationY, -90f, 90f);
            transform.rotation = Quaternion.Euler(_rotationY, _rotationX, 0);
        }

        float speed = moveSpeed * (Input.GetKey(sprintKey) ? sprintMultiplier : 1f);
        Vector3 move = new Vector3(
            Input.GetAxis("Horizontal"), 
            (Input.GetKey(moveDownKey) ? -1f : 0f) + (Input.GetKey(moveUpKey) ? 1f : 0f),
            Input.GetAxis("Vertical") 
        );

        transform.Translate(move * speed * Time.deltaTime);
    }
}