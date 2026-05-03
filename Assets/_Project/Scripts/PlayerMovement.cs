using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float acceleration = 12f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float maxTiltAngle = 18f;
    [SerializeField] private Transform visualRoot;

    private Rigidbody rb;
    private Vector3 moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    private void Update()
    {
        Vector2 input = ReadMoveInput();

        moveInput = new Vector3(input.x, 0f, input.y);
        moveInput = Vector3.ClampMagnitude(moveInput, 1f);
    }

    private void FixedUpdate()
    {
        Vector3 targetVelocity = moveInput * moveSpeed;
        Vector3 velocityChange = targetVelocity - rb.linearVelocity;

        velocityChange.y = 0f;
        velocityChange = Vector3.ClampMagnitude(velocityChange, acceleration * Time.fixedDeltaTime);

        rb.linearVelocity += velocityChange;
    }

    private void LateUpdate()
    {
        if (visualRoot == null)
        {
            return;
        }

        float zTilt = -moveInput.x * maxTiltAngle;
        float xTilt = moveInput.z * maxTiltAngle;
        Quaternion targetRotation = Quaternion.Euler(xTilt, 0f, zTilt);

        visualRoot.localRotation = Quaternion.Slerp(
            visualRoot.localRotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private Vector2 ReadMoveInput()
    {
        Vector2 input = Vector2.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            {
                input.x -= 1f;
            }

            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            {
                input.x += 1f;
            }

            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
            {
                input.y -= 1f;
            }

            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
            {
                input.y += 1f;
            }
        }

        if (Gamepad.current != null)
        {
            Vector2 stickInput = Gamepad.current.leftStick.ReadValue();

            if (stickInput.sqrMagnitude > input.sqrMagnitude)
            {
                input = stickInput;
            }
        }

        return Vector2.ClampMagnitude(input, 1f);
    }
}
