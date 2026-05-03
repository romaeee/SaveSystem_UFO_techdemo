using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private Vector3 offset = new Vector3(0f, 12f, -6f);
    [SerializeField] private float followSmoothTime = 0.18f;
    [SerializeField] private float rotationSmoothSpeed = 8f;

    private Transform player;
    private Vector3 followVelocity;

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
}
