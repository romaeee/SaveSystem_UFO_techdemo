using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private SpawnerController animalSpawner;
    [SerializeField] private int targetAnimalCount = 10;

    private readonly HashSet<AnimalController> animals = new HashSet<AnimalController>();

    private void Awake()
    {
        if (animalSpawner == null)
        {
            animalSpawner = FindAnyObjectByType<SpawnerController>();
        }
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
        RegisterExistingAnimals();
        FillAnimalPopulation();
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
