using UnityEngine;

public class WindmillController : MonoBehaviour
{
    [SerializeField] private Transform blades;
    [SerializeField] private float rotationSpeed = 120f;
    [SerializeField] private bool rotateOnStart = true;

    private bool isRotating;

    private void Awake()
    {
        if (blades == null)
        {
            blades = transform;
        }
    }

    private void OnEnable()
    {
        isRotating = rotateOnStart;
    }

    private void Update()
    {
        if (!isRotating || blades == null)
        {
            return;
        }

        blades.Rotate(0f, 0f, rotationSpeed * Time.deltaTime, Space.Self);
    }

    public void StartRotation()
    {
        isRotating = true;
    }

    public void StopRotation()
    {
        isRotating = false;
    }
}
