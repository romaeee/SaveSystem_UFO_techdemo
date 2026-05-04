using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private SpawnerController animalSpawner;
    [SerializeField] private int targetAnimalCount = 10;
    [SerializeField, Range(30, 120)] private int targetFrameRate = 60;

    private readonly HashSet<AnimalController> animals = new HashSet<AnimalController>();

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
        FillAnimalPopulation();
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

        AnimalController[] existingAnimals = FindObjectsByType<AnimalController>(FindObjectsInactive.Exclude);

        foreach (AnimalController animal in existingAnimals)
        {
            if (animal != null && animal.gameObject.activeInHierarchy)
            {
                animals.Add(animal);
            }
        }
    }

    private void FillAnimalPopulation()
    {
        if (animalSpawner == null)
        {
            return;
        }

        animals.RemoveWhere(animal => animal == null);

        int animalsToSpawn = targetAnimalCount - animals.Count;

        for (int i = 0; i < animalsToSpawn; i++)
        {
            AnimalController animal = animalSpawner.SpawnAnimal();

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
        FillAnimalPopulation();
    }
}
