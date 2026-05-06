using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DatabaseAnimalSO", menuName = "UFO/Animals/Database")]
public class DatabaseAnimalSO : ScriptableObject
{
    [SerializeField] private List<AnimalSO> animals = new List<AnimalSO>();

    public IReadOnlyList<AnimalSO> Animals => animals;

    public AnimalSO GetRandomAnimal()
    {
        float totalWeight = 0f;

        foreach (AnimalSO animal in animals)
        {
            if (animal == null || animal.Prefab == null)
            {
                continue;
            }

            totalWeight += animal.SpawnWeight;
        }

        if (totalWeight <= 0f)
        {
            return null;
        }

        float roll = Random.Range(0f, totalWeight);

        foreach (AnimalSO animal in animals)
        {
            if (animal == null || animal.Prefab == null)
            {
                continue;
            }

            roll -= animal.SpawnWeight;

            if (roll <= 0f)
            {
                return animal;
            }
        }

        return null;
    }

    public AnimalSO GetAnimalByName(string animalName)
    {
        if (string.IsNullOrWhiteSpace(animalName))
        {
            return null;
        }

        foreach (AnimalSO animal in animals)
        {
            if (animal == null)
            {
                continue;
            }

            if (string.Equals(animal.AnimalName, animalName, System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(animal.name, animalName, System.StringComparison.OrdinalIgnoreCase))
            {
                return animal;
            }
        }

        return null;
    }
}
