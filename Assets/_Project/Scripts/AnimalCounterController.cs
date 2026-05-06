using TMPro;
using UnityEngine;

public class AnimalCounterController : MonoBehaviour, ISaveable
{
    private const string CowName = "Cow";
    private const string PigName = "Pig";
    private const string ChickenName = "Chicken";

    [SerializeField] private TMP_Text cowCounterText;
    [SerializeField] private TMP_Text pigCounterText;
    [SerializeField] private TMP_Text chickenCounterText;

    private int cowCount;
    private int pigCount;
    private int chickenCount;

    public int CowCount => cowCount;
    public int PigCount => pigCount;
    public int ChickenCount => chickenCount;
    public int SaveOrder => 10;

    private void Awake()
    {
        FindCounterTextsIfNeeded();
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
        SetCounters(0, 0, 0);
    }

    public void SetCounters(int cows, int pigs, int chickens)
    {
        cowCount = Mathf.Max(0, cows);
        pigCount = Mathf.Max(0, pigs);
        chickenCount = Mathf.Max(0, chickens);
        UpdateAllCounters();
    }

    public void CaptureState(SaveData saveData)
    {
        saveData.animalCounts.cows = cowCount;
        saveData.animalCounts.pigs = pigCount;
        saveData.animalCounts.chickens = chickenCount;
    }

    public void RestoreState(SaveData saveData)
    {
        if (saveData.animalCounts == null)
        {
            ResetCounters();
            return;
        }

        SetCounters(
            saveData.animalCounts.cows,
            saveData.animalCounts.pigs,
            saveData.animalCounts.chickens
        );
    }

    private void OnAnimalDestroyed(AnimalController animal)
    {
        if (animal == null)
        {
            return;
        }

        string animalName = GetAnimalName(animal);

        if (IsAnimalType(animalName, CowName))
        {
            cowCount++;
            UpdateCounter(cowCounterText, cowCount);
            return;
        }

        if (IsAnimalType(animalName, PigName))
        {
            pigCount++;
            UpdateCounter(pigCounterText, pigCount);
            return;
        }

        if (IsAnimalType(animalName, ChickenName))
        {
            chickenCount++;
            UpdateCounter(chickenCounterText, chickenCount);
        }
    }

    private string GetAnimalName(AnimalController animal)
    {
        if (animal.AnimalData != null && !string.IsNullOrWhiteSpace(animal.AnimalData.AnimalName))
        {
            return animal.AnimalData.AnimalName;
        }

        return animal.gameObject.name;
    }

    private bool IsAnimalType(string animalName, string animalType)
    {
        return !string.IsNullOrWhiteSpace(animalName)
            && animalName.Contains(animalType, System.StringComparison.OrdinalIgnoreCase);
    }

    private void FindCounterTextsIfNeeded()
    {
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

        foreach (TMP_Text text in texts)
        {
            if (text == null)
            {
                continue;
            }

            string textName = text.gameObject.name;

            if (cowCounterText == null && IsAnimalType(textName, CowName))
            {
                cowCounterText = text;
            }
            else if (pigCounterText == null && IsAnimalType(textName, PigName))
            {
                pigCounterText = text;
            }
            else if (chickenCounterText == null && IsAnimalType(textName, ChickenName))
            {
                chickenCounterText = text;
            }
        }
    }

    private void UpdateAllCounters()
    {
        UpdateCounter(cowCounterText, cowCount);
        UpdateCounter(pigCounterText, pigCount);
        UpdateCounter(chickenCounterText, chickenCount);
    }

    private void UpdateCounter(TMP_Text counterText, int value)
    {
        if (counterText != null)
        {
            counterText.text = value.ToString();
        }
    }
}
