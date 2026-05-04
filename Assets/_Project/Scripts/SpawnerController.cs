using UnityEngine;

public class SpawnerController : MonoBehaviour
{
    [SerializeField] private DatabaseAnimalSO animalDatabase;
    [SerializeField] private AnimalController animalPrefab;
    [SerializeField] private Transform spawnPlane;
    [SerializeField] private Camera visibilityCamera;
    [SerializeField, Min(0f)] private float spawnDiameter = 50f;
    [SerializeField] private float spawnHeightOffset = 0.5f;
    [SerializeField] private float collisionCheckRadius = 1.2f;
    [SerializeField] private float minDistanceBetweenAnimals = 5f;
    [SerializeField] private int candidateBatchSize = 12;
    [SerializeField] private float cameraViewportPadding = 0.08f;
    [SerializeField] private int maxSpawnAttempts = 80;
    [SerializeField] private LayerMask blockingLayers = ~0;

    private Collider spawnPlaneCollider;
    private Renderer spawnPlaneRenderer;
    private readonly Collider[] collisionBuffer = new Collider[16];

    private void Awake()
    {
        FindSceneReferencesIfNeeded();
    }

    public AnimalController SpawnAnimal()
    {
        return SpawnAnimal(false);
    }

    public AnimalController SpawnAnimal(bool ignoreCameraVisibility)
    {
        AnimalSO animalData = animalDatabase != null ? animalDatabase.GetRandomAnimal() : null;
        AnimalController selectedPrefab = animalData != null ? animalData.Prefab : animalPrefab;

        if (selectedPrefab == null)
        {
            Debug.LogWarning($"{nameof(SpawnerController)} needs an animal database or fallback animal prefab.", this);
            return null;
        }

        FindSceneReferencesIfNeeded();

        if (TryFindBestSpawnPoint(ignoreCameraVisibility, out Vector3 spawnPosition))
        {
            Quaternion spawnRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            AnimalController animal = Instantiate(selectedPrefab, spawnPosition, spawnRotation);
            animal.name = animalData != null && !string.IsNullOrWhiteSpace(animalData.AnimalName)
                ? animalData.AnimalName
                : selectedPrefab.name;

            return animal;
        }

        Debug.LogWarning($"{nameof(SpawnerController)} could not find a valid spawn point.", this);
        return null;
    }

    private bool TryFindBestSpawnPoint(bool ignoreCameraVisibility, out Vector3 bestPosition)
    {
        bestPosition = Vector3.zero;
        float bestScore = float.NegativeInfinity;
        int attempts = Mathf.Max(1, maxSpawnAttempts);
        int batchSize = Mathf.Max(1, candidateBatchSize);
        float minDistanceSqr = minDistanceBetweenAnimals * minDistanceBetweenAnimals;

        for (int i = 0; i < attempts; i++)
        {
            for (int candidateIndex = 0; candidateIndex < batchSize; candidateIndex++)
            {
                if (!TryGetSpawnPoint(out Vector3 spawnPosition))
                {
                    continue;
                }

                if (!ignoreCameraVisibility && IsPointVisibleToCamera(spawnPosition))
                {
                    continue;
                }

                if (IsPointBlocked(spawnPosition))
                {
                    continue;
                }

                float score = GetAnimalSeparationScore(spawnPosition);

                if (score < minDistanceSqr)
                {
                    continue;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestPosition = spawnPosition;
                }
            }

            if (bestScore > float.NegativeInfinity)
            {
                return true;
            }
        }

        return false;
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
        float radius = spawnDiameter * 0.5f;
        Vector2 randomOffset = Random.insideUnitCircle * radius;

        float x = transform.position.x + randomOffset.x;
        float z = transform.position.z + randomOffset.y;
        float planeRayStartY = GetPlaneRayStartY();
        float y = planeRayStartY + spawnHeightOffset;

        spawnPosition = new Vector3(x, y, z);

        if (spawnPlaneCollider != null)
        {
            Vector3 rayStart = new Vector3(x, planeRayStartY + 50f, z);
            Ray ray = new Ray(rayStart, Vector3.down);

            if (spawnPlaneCollider.Raycast(ray, out RaycastHit hit, 100f))
            {
                spawnPosition = hit.point + Vector3.up * spawnHeightOffset;
                return true;
            }

            return false;
        }

        return true;
    }

    private float GetPlaneRayStartY()
    {
        if (spawnPlaneRenderer != null)
        {
            return spawnPlaneRenderer.bounds.max.y;
        }

        if (spawnPlaneCollider != null)
        {
            return spawnPlaneCollider.bounds.max.y;
        }

        return transform.position.y;
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
        int colliderCount = Physics.OverlapSphereNonAlloc(
            point,
            collisionCheckRadius,
            collisionBuffer,
            blockingLayers,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < colliderCount; i++)
        {
            Collider hitCollider = collisionBuffer[i];

            if (IsSpawnSurface(hitCollider))
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

    private float GetAnimalSeparationScore(Vector3 point)
    {
        var animals = AnimalController.ActiveAnimals;

        if (animals.Count == 0)
        {
            return float.PositiveInfinity;
        }

        float nearestDistanceSqr = float.PositiveInfinity;

        foreach (AnimalController animal in animals)
        {
            if (animal == null || !animal.gameObject.activeInHierarchy)
            {
                continue;
            }

            Vector3 offset = animal.transform.position - point;
            offset.y = 0f;
            nearestDistanceSqr = Mathf.Min(nearestDistanceSqr, offset.sqrMagnitude);
        }

        return nearestDistanceSqr;
    }

    private bool IsSpawnSurface(Collider hitCollider)
    {
        if (hitCollider == null)
        {
            return true;
        }

        if (hitCollider == spawnPlaneCollider)
        {
            return true;
        }

        return hitCollider.transform == spawnPlane || hitCollider.gameObject.name.StartsWith("Plane");
    }
}
