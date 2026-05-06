using UnityEngine;

public class CameraController : MonoBehaviour, ISaveable
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private Vector3 offset = new Vector3(0f, 12f, -6f);
    [SerializeField] private float followSmoothTime = 0.18f;
    [SerializeField] private float rotationSmoothSpeed = 8f;

    private Transform player;
    private Vector3 followVelocity;

    public int SaveOrder => 25;

    private void Start()
    {
        FindPlayer();
    }

    private void LateUpdate()
    {
        if (player == null)
        {
            FindPlayer();
            return;
        }

        Vector3 playerPosition = player.position;
        Vector3 targetPosition = playerPosition + offset;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref followVelocity,
            followSmoothTime
        );

        Quaternion targetRotation = Quaternion.LookRotation(playerPosition - transform.position, Vector3.up);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSmoothSpeed * Time.deltaTime
        );
    }

    private void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);

        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    public void CaptureState(SaveData saveData)
    {
        saveData.hasCamera = true;
        saveData.camera = new TransformData(transform);
    }

    public void RestoreState(SaveData saveData)
    {
        if (!saveData.hasCamera || saveData.camera == null)
        {
            return;
        }

        followVelocity = Vector3.zero;
        transform.SetPositionAndRotation(saveData.camera.position, saveData.camera.Rotation);
        FindPlayer();
    }
}
