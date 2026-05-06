using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AnimalCounterController : MonoBehaviour, ISaveable
{
    private const string CowName = "Cow";
    private const string PigName = "Pig";
    private const string ChickenName = "Chicken";

    [SerializeField] private DatabaseAnimalSO animalDatabase;
    [SerializeField] private GameObject counterPrefab;
    [SerializeField] private Transform countersParent;

    private readonly Dictionary<string, int> counts = new Dictionary<string, int>();
    private readonly Dictionary<string, TMP_Text> counterTexts = new Dictionary<string, TMP_Text>();

    public int SaveOrder => 10;

    private void Awake()
    {
        FindReferencesIfNeeded();
        BuildCountersFromDatabase();
        UpdateAllCounters();
    }

    private void OnEnable()
    {
        AnimalController.AnimalDestroyed += OnAnimalDestroyed;
    }

    private void OnDisable()
    {
        AnimalController.AnimalDestroyed -= OnAnimalDestroyed;
    }

    public void ResetCounters()
    {
        List<string> keys = new List<string>(counts.Keys);

        foreach (string key in keys)
        {
            counts[key] = 0;
        }

        UpdateAllCounters();
    }

    public void CaptureState(SaveData saveData)
    {
        saveData.animalCounterValues.Clear();

        foreach (KeyValuePair<string, int> count in counts)
        {
            saveData.animalCounterValues.Add(new AnimalCountSaveData
            {
                animalType = count.Key,
                count = count.Value
            });
        }

        saveData.animalCounts.cows = GetCount(CowName);
        saveData.animalCounts.pigs = GetCount(PigName);
        saveData.animalCounts.chickens = GetCount(ChickenName);
    }

    public void RestoreState(SaveData saveData)
    {
        ResetCounters();

        if (saveData.animalCounterValues != null && saveData.animalCounterValues.Count > 0)
        {
            foreach (AnimalCountSaveData countData in saveData.animalCounterValues)
            {
                if (countData == null || string.IsNullOrWhiteSpace(countData.animalType))
                {
                    continue;
                }

                SetCount(countData.animalType, countData.count);
            }
        }
        else if (saveData.animalCounts != null)
        {
            SetCount(CowName, saveData.animalCounts.cows);
            SetCount(PigName, saveData.animalCounts.pigs);
            SetCount(ChickenName, saveData.animalCounts.chickens);
        }

        UpdateAllCounters();
    }

    private void OnAnimalDestroyed(AnimalController animal)
    {
        if (animal == null)
        {
            return;
        }

        string animalType = GetAnimalType(animal);

        if (string.IsNullOrWhiteSpace(animalType))
        {
            return;
        }

        SetCount(animalType, GetCount(animalType) + 1);
        UpdateCounter(animalType);
    }

    private void BuildCountersFromDatabase()
    {
        if (animalDatabase == null || counterPrefab == null || countersParent == null)
        {
            return;
        }

        foreach (Transform child in countersParent)
        {
            Destroy(child.gameObject);
        }

        counts.Clear();
        counterTexts.Clear();

        foreach (AnimalSO animal in animalDatabase.Animals)
        {
            if (animal == null || string.IsNullOrWhiteSpace(animal.AnimalName))
            {
                continue;
            }

            string key = animal.AnimalName;
            counts[key] = 0;

            GameObject counterObject = Instantiate(counterPrefab, countersParent);
            counterObject.name = $"{key}Counter";

            Image iconImage = FindFirstImage(counterObject.transform);

            if (iconImage != null)
            {
                if (animal.Icon != null)
                {
                    iconImage.sprite = animal.Icon;
                }

                iconImage.preserveAspect = true;
            }

            TMP_Text counterText = counterObject.GetComponentInChildren<TMP_Text>(true);

            if (counterText != null)
            {
                counterText.text = "0";
                counterTexts[key] = counterText;
            }
        }
    }

    private Image FindFirstImage(Transform root)
    {
        Image[] images = root.GetComponentsInChildren<Image>(true);

        foreach (Image image in images)
        {
            if (image != null && image.gameObject.name.ToLowerInvariant().Contains("image"))
            {
                return image;
            }
        }

        return images.Length > 0 ? images[0] : null;
    }

    private void FindReferencesIfNeeded()
    {
        if (animalDatabase == null)
        {
            SpawnerController spawner = FindAnyObjectByType<SpawnerController>();
            animalDatabase = spawner != null ? spawner.AnimalDatabase : null;
        }

        if (countersParent == null)
        {
            countersParent = transform;
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

    private int GetCount(string animalType)
    {
        string key = FindMatchingKey(animalType);
        return key != null && counts.TryGetValue(key, out int count) ? count : 0;
    }

    private void SetCount(string animalType, int value)
    {
        string key = FindMatchingKey(animalType) ?? animalType;
        counts[key] = Mathf.Max(0, value);
    }

    private string FindMatchingKey(string animalType)
    {
        if (string.IsNullOrWhiteSpace(animalType))
        {
            return null;
        }

        foreach (string key in counts.Keys)
        {
            if (string.Equals(key, animalType, System.StringComparison.OrdinalIgnoreCase))
            {
                return key;
            }
        }

        return null;
    }

    private void UpdateAllCounters()
    {
        foreach (string animalType in counts.Keys)
        {
            UpdateCounter(animalType);
        }
    }

    private void UpdateCounter(string animalType)
    {
        string key = FindMatchingKey(animalType);

        if (key == null || !counterTexts.TryGetValue(key, out TMP_Text counterText) || counterText == null)
        {
            return;
        }

        counterText.text = counts[key].ToString();
    }
}
