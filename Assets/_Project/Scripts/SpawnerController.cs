using UnityEngine;

public class SpawnerController : MonoBehaviour
{
    [SerializeField] private AnimalController animalPrefab;
    [SerializeField] private Transform spawnPlane;
    [SerializeField] private Camera visibilityCamera;
    [SerializeField] private Vector2 spawnAreaSize = new Vector2(80f, 80f);
    [SerializeField] private Vector3 spawnAreaCenter;
    [SerializeField] private float spawnHeightOffset = 0.5f;
    [SerializeField] private float collisionCheckRadius = 1.2f;
    [SerializeField] private float cameraViewportPadding = 0.08f;
    [SerializeField] private int maxSpawnAttempts = 80;
    [SerializeField] private LayerMask blockingLayers = ~0;

    private Collider spawnPlaneCollider;
    private Renderer spawnPlaneRenderer;

    private void Awake()
    {
        FindSceneReferencesIfNeeded();
    }

    public AnimalController SpawnAnimal()
    {
        if (animalPrefab == null)
        {
            Debug.LogWarning($"{nameof(SpawnerController)} needs an animal prefab.", this);
            return null;
        }

        FindSceneReferencesIfNeeded();

        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            if (!TryGetSpawnPoint(out Vector3 spawnPosition))
            {
                continue;
            }

            if (!IsPointVisibleToCamera(spawnPosition) && !IsPointBlocked(spawnPosition))
            {
                Quaternion spawnRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                AnimalController animal = Instantiate(animalPrefab, spawnPosition, spawnRotation);
                animal.name = animalPrefab.name;

                return animal;
            }
        }

        Debug.LogWarning($"{nameof(SpawnerController)} could not find a valid spawn point.", this);
        return null;
    }

    private void FindSceneReferencesIfNeeded()
    {
        if (spawnPlane == null)
        {
            GameObject planeObject = GameObject.Find("Plane");

            if (planeObject != null)
            {
                spawnPlane = planeObject.transform;
            }
        }

        if (visibilityCamera == null)
        {
            visibilityCamera = Camera.main;
        }

        spawnPlaneCollider = spawnPlane != null ? spawnPlane.GetComponent<Collider>() : null;
        spawnPlaneRenderer = spawnPlane != null ? spawnPlane.GetComponent<Renderer>() : null;
    }

    private bool TryGetSpawnPoint(out Vector3 spawnPosition)
    {
        Bounds spawnBounds = GetSpawnBounds();

        float x = Random.Range(spawnBounds.min.x, spawnBounds.max.x);
        float z = Random.Range(spawnBounds.min.z, spawnBounds.max.z);
        float y = spawnBounds.max.y + spawnHeightOffset;

        spawnPosition = new Vector3(x, y, z);

        if (spawnPlaneCollider != null)
        {
            Vector3 rayStart = new Vector3(x, spawnBounds.max.y + 50f, z);

            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 100f, blockingLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider == spawnPlaneCollider)
                {
                    spawnPosition = hit.point + Vector3.up * spawnHeightOffset;
                    return true;
                }

                return false;
            }
        }

        return true;
    }

    private Bounds GetSpawnBounds()
    {
        if (spawnAreaSize.x > 0f && spawnAreaSize.y > 0f)
        {
            Vector3 center = spawnAreaCenter;

            if (center == Vector3.zero && spawnPlane != null)
            {
                center = spawnPlane.position;
            }

            return new Bounds(center, new Vector3(spawnAreaSize.x, 0f, spawnAreaSize.y));
        }

        if (spawnPlaneRenderer != null)
        {
            return spawnPlaneRenderer.bounds;
        }

        if (spawnPlaneCollider != null)
        {
            return spawnPlaneCollider.bounds;
        }

        return new Bounds(transform.position, new Vector3(30f, 0f, 30f));
    }

    private bool IsPointVisibleToCamera(Vector3 point)
    {
        if (visibilityCamera == null)
        {
            return false;
        }

        Vector3 viewportPoint = visibilityCamera.WorldToViewportPoint(point);

        return viewportPoint.z > 0f
            && viewportPoint.x > -cameraViewportPadding
            && viewportPoint.x < 1f + cameraViewportPadding
            && viewportPoint.y > -cameraViewportPadding
            && viewportPoint.y < 1f + cameraViewportPadding;
    }

    private bool IsPointBlocked(Vector3 point)
    {
        Collider[] colliders = Physics.OverlapSphere(
            point,
            collisionCheckRadius,
            blockingLayers,
            QueryTriggerInteraction.Ignore
        );

        foreach (Collider hitCollider in colliders)
        {
            if (hitCollider == spawnPlaneCollider)
            {
                continue;
            }

            if (hitCollider.GetComponentInParent<AnimalController>() != null)
            {
                return true;
            }

            return true;
        }

        return false;
    }
}
