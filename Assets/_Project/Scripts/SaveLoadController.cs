using System.Collections.Generic;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaveLoadController : MonoBehaviour
{
    [SerializeField] private string saveFileName = "savegame.json";
    [SerializeField] private string autosaveFileName = "autosave.json";
    [SerializeField, Min(0.1f)] private float autosaveInterval = 10f;
    [SerializeField] private Toggle autosaveToggle;
    [SerializeField] private TMP_Text autosaveStatusText;
    [SerializeField] private bool createAutosaveStatusIfMissing = true;
    [SerializeField, Min(0f)] private float autosaveStatusDuration = 2f;
    [SerializeField] private bool logAutosaves;
    [SerializeField] private bool autoDiscoverSaveables = true;
    [SerializeField] private MonoBehaviour[] saveHandlers;

    public string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);
    public string AutosavePath => Path.Combine(Application.persistentDataPath, autosaveFileName);

    private Coroutine autosaveCoroutine;
    private Coroutine autosaveStatusCoroutine;
    private readonly List<ISaveable> cachedSaveables = new List<ISaveable>();

    private void Awake()
    {
        CreateAutosaveStatusIfNeeded();
        SetAutosaveStatusVisible(false);
        RefreshSaveables();
    }

    private void OnEnable()
    {
        if (autosaveToggle != null)
        {
            autosaveToggle.onValueChanged.AddListener(SetAutosaveEnabled);
            SetAutosaveEnabled(autosaveToggle.isOn);
        }
    }

    private void OnDisable()
    {
        if (autosaveToggle != null)
        {
            autosaveToggle.onValueChanged.RemoveListener(SetAutosaveEnabled);
        }

        StopAutosave();
    }

    public void Save()
    {
        SaveGame();
    }

    public void Load()
    {
        LoadGame();
    }

    public void SaveGame()
    {
        SaveToFile(SavePath);
        Debug.Log($"Game saved to {SavePath}", this);
    }

    public void LoadGame()
    {
        if (LoadFromFile(SavePath))
        {
            Debug.Log($"Game loaded from {SavePath}", this);
        }
    }

    public void LoadAutosave()
    {
        if (LoadFromFile(AutosavePath))
        {
            Debug.Log($"Autosave loaded from {AutosavePath}", this);
        }
    }

    public void SetAutosaveEnabled(bool isEnabled)
    {
        if (isEnabled)
        {
            StartAutosave();
        }
        else
        {
            StopAutosave();
        }
    }

    public void RefreshSaveables()
    {
        cachedSaveables.Clear();
        AddAssignedSaveables(cachedSaveables);

        if (autoDiscoverSaveables)
        {
            AddSceneSaveables(cachedSaveables);
        }

        cachedSaveables.Sort((first, second) => first.SaveOrder.CompareTo(second.SaveOrder));
    }

    public void SaveToFile(string path)
    {
        SaveData previousSaveData = ReadSaveData(path);
        SaveData saveData = CaptureSaveData();
        PreserveUnknownAnimalCounters(previousSaveData, saveData);
        string json = JsonUtility.ToJson(saveData, true);

        string directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, json);
    }

    public bool LoadFromFile(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogWarning($"Save file not found: {path}", this);
            return false;
        }

        SaveData saveData = ReadSaveData(path);

        if (saveData == null)
        {
            Debug.LogWarning("Save file is empty or invalid.", this);
            return false;
        }

        ApplySaveData(saveData);
        return true;
    }

    private SaveData ReadSaveData(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        string json = File.ReadAllText(path);
        SaveData saveData = JsonUtility.FromJson<SaveData>(json);
        return SaveDataMigrator.Migrate(saveData);
    }

    public SaveData CaptureSaveData()
    {
        SaveData saveData = new SaveData
        {
            schemaVersion = SaveDataMigrator.CurrentSchemaVersion
        };

        foreach (ISaveable saveable in cachedSaveables)
        {
            saveable.CaptureState(saveData);
        }

        return saveData;
    }

    private void ApplySaveData(SaveData saveData)
    {
        foreach (ISaveable saveable in cachedSaveables)
        {
            saveable.RestoreState(saveData);
        }
    }

    private void PreserveUnknownAnimalCounters(SaveData previousSaveData, SaveData saveData)
    {
        if (previousSaveData == null
            || previousSaveData.animalCounterValues == null
            || previousSaveData.animalCounterValues.Count == 0)
        {
            return;
        }

        saveData.animalCounterValues ??= new List<AnimalCountSaveData>();

        foreach (AnimalCountSaveData previousCount in previousSaveData.animalCounterValues)
        {
            if (previousCount == null || string.IsNullOrWhiteSpace(previousCount.animalType))
            {
                continue;
            }

            if (HasAnimalCounter(saveData, previousCount.animalType))
            {
                continue;
            }

            saveData.animalCounterValues.Add(new AnimalCountSaveData
            {
                animalType = previousCount.animalType,
                count = previousCount.count
            });
        }
    }

    private bool HasAnimalCounter(SaveData saveData, string animalType)
    {
        if (saveData.animalCounterValues == null)
        {
            return false;
        }

        foreach (AnimalCountSaveData countData in saveData.animalCounterValues)
        {
            if (countData != null
                && string.Equals(countData.animalType, animalType, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private void AddAssignedSaveables(List<ISaveable> saveables)
    {
        if (saveHandlers == null)
        {
            return;
        }

        foreach (MonoBehaviour handler in saveHandlers)
        {
            AddSaveable(saveables, handler);
        }
    }

    private void AddSceneSaveables(List<ISaveable> saveables)
    {
        MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);

        foreach (MonoBehaviour behaviour in behaviours)
        {
            AddSaveable(saveables, behaviour);
        }
    }

    private void AddSaveable(List<ISaveable> saveables, MonoBehaviour behaviour)
    {
        if (behaviour is not ISaveable saveable || saveables.Contains(saveable))
        {
            return;
        }

        saveables.Add(saveable);
    }

    private void StartAutosave()
    {
        StopAutosave();
        autosaveCoroutine = StartCoroutine(AutosaveRoutine());
    }

    private void StopAutosave()
    {
        if (autosaveCoroutine != null)
        {
            StopCoroutine(autosaveCoroutine);
            autosaveCoroutine = null;
        }
    }

    private IEnumerator AutosaveRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(autosaveInterval);
            SaveToFile(AutosavePath);
            ShowAutosaveStatus();

            if (logAutosaves)
            {
                Debug.Log($"Game autosaved to {AutosavePath}", this);
            }
        }
    }

    private void ShowAutosaveStatus()
    {
        if (autosaveStatusText == null)
        {
            return;
        }

        if (autosaveStatusCoroutine != null)
        {
            StopCoroutine(autosaveStatusCoroutine);
        }

        autosaveStatusCoroutine = StartCoroutine(AutosaveStatusRoutine());
    }

    private IEnumerator AutosaveStatusRoutine()
    {
        autosaveStatusText.text = "Autosaving...";
        SetAutosaveStatusVisible(true);

        if (autosaveStatusDuration > 0f)
        {
            yield return new WaitForSeconds(autosaveStatusDuration);
        }

        SetAutosaveStatusVisible(false);
        autosaveStatusCoroutine = null;
    }

    private void SetAutosaveStatusVisible(bool isVisible)
    {
        if (autosaveStatusText != null)
        {
            autosaveStatusText.gameObject.SetActive(isVisible);
        }
    }

    private void CreateAutosaveStatusIfNeeded()
    {
        if (autosaveStatusText != null || !createAutosaveStatusIfMissing)
        {
            return;
        }

        Canvas canvas = FindAnyObjectByType<Canvas>();

        if (canvas == null)
        {
            return;
        }

        GameObject statusObject = new GameObject("AutosaveStatusText", typeof(RectTransform));
        statusObject.transform.SetParent(canvas.transform, false);

        RectTransform rectTransform = statusObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.zero;
        rectTransform.pivot = Vector2.zero;
        rectTransform.anchoredPosition = new Vector2(24f, 24f);
        rectTransform.sizeDelta = new Vector2(260f, 40f);

        autosaveStatusText = statusObject.AddComponent<TextMeshProUGUI>();
        autosaveStatusText.text = "Autosaving...";
        autosaveStatusText.fontSize = 24f;
        autosaveStatusText.alignment = TextAlignmentOptions.Left;
        autosaveStatusText.color = Color.white;
        autosaveStatusText.raycastTarget = false;
    }
}
