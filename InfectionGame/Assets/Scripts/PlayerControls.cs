using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControls : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform cameraTransform;

    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float mouseSensitivity = 0.15f;

    private Vector2 moveInput;
    private bool isGrounded;
    private bool jumpRequested;

    private float yaw;
    private float pitch;

    private void Start()
    {
        rb.freezeRotation = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        moveInput = Vector2.zero;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed) moveInput.y += 1f;
            if (keyboard.sKey.isPressed) moveInput.y -= 1f;
            if (keyboard.dKey.isPressed) moveInput.x += 1f;
            if (keyboard.aKey.isPressed) moveInput.x -= 1f;
        }

        moveInput = Vector2.ClampMagnitude(moveInput, 1f);

        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame && isGrounded)
            jumpRequested = true;

        Mouse mouse = Mouse.current;

        if (mouse != null)
        {
            Vector2 delta = mouse.delta.ReadValue();
            yaw += delta.x * mouseSensitivity;
            pitch -= delta.y * mouseSensitivity;
        }

        pitch = Mathf.Clamp(pitch, -80f, 80f);

        cameraTransform.localRotation =
            Quaternion.Euler(pitch, yaw, 0f);
    }

    private void FixedUpdate()
    {
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 direction =
            forward * moveInput.y +
            right * moveInput.x;

        Vector3 velocity = rb.linearVelocity;

        velocity.x = direction.x * moveSpeed;
        velocity.z = direction.z * moveSpeed;

        rb.linearVelocity = velocity;

        if (jumpRequested)
        {
            rb.AddForce(
                Vector3.up * jumpForce,
                ForceMode.VelocityChange
            );

            isGrounded = false;
            jumpRequested = false;
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        // GetContact вместо collision.contacts: тот аллоцирует массив каждый вызов
        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contact = collision.GetContact(i);
            if (contact.normal.y > 0.5f)
            {
                isGrounded = true;
                break;
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        isGrounded = false;
    }
}