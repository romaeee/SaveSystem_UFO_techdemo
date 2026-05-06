using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour, ISaveable
{
    [SerializeField] private SpawnerController animalSpawner;
    [SerializeField] private int targetAnimalCount = 10;
    [SerializeField, Range(30, 120)] private int targetFrameRate = 60;

    private readonly HashSet<AnimalController> animals = new HashSet<AnimalController>();

    public int SaveOrder => 30;

    private void Awake()
    {
        if (animalSpawner == null)
        {
            animalSpawner = FindAnyObjectByType<SpawnerController>();
        }

        ApplyFrameRateLimit();
    }

    private void OnEnable()
    {
        AnimalController.AnimalDestroyed += OnAnimalDestroyed;
    }

    private void OnDisable()
    {
        AnimalController.AnimalDestroyed -= OnAnimalDestroyed;
    }

    private void Start()
    {
        ApplyFrameRateLimit();
        RegisterExistingAnimals();
        FillAnimalPopulation(true);
    }

    private void OnValidate()
    {
        targetFrameRate = Mathf.Clamp(targetFrameRate, 30, 120);
    }

    private void ApplyFrameRateLimit()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = Mathf.Clamp(targetFrameRate, 30, 120);
    }

    private void RegisterExistingAnimals()
    {
        animals.Clear();

        var existingAnimals = AnimalController.ActiveAnimals;

        foreach (AnimalController animal in existingAnimals)
        {
            if (animal != null && animal.gameObject.activeInHierarchy)
            {
                animals.Add(animal);
            }
        }
    }

    private void FillAnimalPopulation(bool ignoreCameraVisibility)
    {
        if (animalSpawner == null)
        {
            return;
        }

        animals.RemoveWhere(animal => animal == null);

        int animalsToSpawn = targetAnimalCount - animals.Count;

        for (int i = 0; i < animalsToSpawn; i++)
        {
            AnimalController animal = animalSpawner.SpawnAnimal(ignoreCameraVisibility);

            if (animal == null)
            {
                break;
            }

            animals.Add(animal);
        }
    }

    private void OnAnimalDestroyed(AnimalController animal)
    {
        animals.Remove(animal);
        FillAnimalPopulation(false);
    }

    public void RebuildAnimalList()
    {
        RegisterExistingAnimals();
    }

    public void CaptureState(SaveData saveData)
    {
        saveData.animals.Clear();

        foreach (AnimalController animal in AnimalController.ActiveAnimals)
        {
            if (animal == null || !animal.gameObject.activeInHierarchy)
            {
                continue;
            }

            saveData.animals.Add(new AnimalSaveData
            {
                animalType = GetAnimalType(animal),
                transform = new TransformData(animal.transform),
                localScale = animal.transform.localScale,
                isAbducting = animal.IsAbducting
            });
        }
    }

    public void RestoreState(SaveData saveData)
    {
        RestoreAnimals(saveData.animals);
        RegisterExistingAnimals();
    }

    private void RestoreAnimals(List<AnimalSaveData> savedAnimals)
    {
        ClearSceneAnimals();

        if (animalSpawner == null || savedAnimals == null)
        {
            return;
        }

        foreach (AnimalSaveData animalData in savedAnimals)
        {
            if (animalData == null || animalData.transform == null)
            {
                continue;
            }

            AnimalController animal = animalSpawner.SpawnAnimalByName(
                animalData.animalType,
                animalData.transform.position,
                animalData.transform.Rotation
            );

            if (animal != null)
            {
                Vector3 savedScale = animalData.localScale.sqrMagnitude > 0.001f
                    ? animalData.localScale
                    : Vector3.one;

                animal.RestoreSaveState(savedScale, animalData.isAbducting);
            }
        }
    }

    private void ClearSceneAnimals()
    {
        List<AnimalController> activeAnimals = new List<AnimalController>(AnimalController.ActiveAnimals);

        foreach (AnimalController animal in activeAnimals)
        {
            if (animal == null)
            {
                continue;
            }

            animal.gameObject.SetActive(false);
            Destroy(animal.gameObject);
        }
    }

    private string GetAnimalType(AnimalController animal)
    {
        if (animal.AnimalData != null && !string.IsNullOrWhiteSpace(animal.AnimalData.AnimalName))
        {
            return animal.AnimalData.AnimalName;
        }

        return animal.gameObject.name.Replace("(Clone)", string.Empty).Trim();
    }
}
