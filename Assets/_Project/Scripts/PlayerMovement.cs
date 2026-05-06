using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour, ISaveable
{
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float acceleration = 12f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float maxTiltAngle = 18f;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private bool lockHeight = true;

    private Rigidbody rb;
    private Vector3 moveInput;
    private float controlMultiplier = 1f;
    private Vector3 centerAssistTarget;
    private float centerAssistStrength;
    private float centerAssistSpeed;
    private bool movementLocked;
    private bool inputLocked;
    private float lockedYPosition;

    public Vector3 MoveInput => moveInput;
    public bool HasMoveInput => moveInput.sqrMagnitude > 0.01f;
    public int SaveOrder => 20;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        lockedYPosition = rb.position.y;
    }

    private void Update()
    {
        Vector2 input = ReadMoveInput();

        moveInput = new Vector3(input.x, 0f, input.y);
        moveInput = Vector3.ClampMagnitude(moveInput, 1f);
    }

    private void FixedUpdate()
    {
        if (movementLocked)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            KeepLockedHeight();
            return;
        }

        Vector3 targetVelocity = inputLocked ? Vector3.zero : moveInput * moveSpeed * controlMultiplier;

        if (centerAssistStrength > 0f)
        {
            Vector3 centerOffset = centerAssistTarget - rb.position;
            centerOffset.y = 0f;

            targetVelocity += centerOffset * centerAssistSpeed * centerAssistStrength;
        }

        Vector3 velocityChange = targetVelocity - rb.linearVelocity;

        velocityChange.y = 0f;
        velocityChange = Vector3.ClampMagnitude(velocityChange, acceleration * Time.fixedDeltaTime);

        rb.linearVelocity += velocityChange;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        KeepLockedHeight();
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

    public void SetControlMultiplier(float multiplier)
    {
        controlMultiplier = Mathf.Clamp01(multiplier);
    }

    public void SetCenterAssist(Vector3 targetPosition, float strength, float speed)
    {
        centerAssistTarget = targetPosition;
        centerAssistStrength = Mathf.Clamp01(strength);
        centerAssistSpeed = Mathf.Max(0f, speed);
    }

    public void ClearCenterAssist()
    {
        centerAssistStrength = 0f;
    }

    public void SetMovementLocked(bool isLocked)
    {
        movementLocked = isLocked;

        if (movementLocked)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void SetInputLocked(bool isLocked)
    {
        inputLocked = isLocked;
    }

    public void Teleport(Vector3 position)
    {
        rb.position = position;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        lockedYPosition = position.y;
    }

    public void CaptureState(SaveData saveData)
    {
        saveData.player = new TransformData(transform);
    }

    public void RestoreState(SaveData saveData)
    {
        if (saveData.player == null)
        {
            return;
        }

        transform.rotation = saveData.player.Rotation;
        Teleport(saveData.player.position);
    }

    private void KeepLockedHeight()
    {
        if (!lockHeight)
        {
            return;
        }

        Vector3 position = rb.position;

        if (!Mathf.Approximately(position.y, lockedYPosition))
        {
            position.y = lockedYPosition;
            rb.position = position;
        }
    }
}
