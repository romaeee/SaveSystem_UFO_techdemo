using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveLoadController : MonoBehaviour
{
    [SerializeField] private string saveFileName = "savegame.json";
    [SerializeField] private bool autoDiscoverSaveables = true;
    [SerializeField] private MonoBehaviour[] saveHandlers;

    public string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);

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
        SaveData saveData = BuildSaveData();
        string json = JsonUtility.ToJson(saveData, true);

        File.WriteAllText(SavePath, json);
        Debug.Log($"Game saved to {SavePath}", this);
    }

    public void LoadGame()
    {
        if (!File.Exists(SavePath))
        {
            Debug.LogWarning($"Save file not found: {SavePath}", this);
            return;
        }

        string json = File.ReadAllText(SavePath);
        SaveData saveData = JsonUtility.FromJson<SaveData>(json);
        saveData = SaveDataMigrator.Migrate(saveData);

        if (saveData == null)
        {
            Debug.LogWarning("Save file is empty or invalid.", this);
            return;
        }

        ApplySaveData(saveData);
        Debug.Log($"Game loaded from {SavePath}", this);
    }

    private SaveData BuildSaveData()
    {
        SaveData saveData = new SaveData
        {
            schemaVersion = SaveDataMigrator.CurrentSchemaVersion
        };

        foreach (ISaveable saveable in GetSaveables())
        {
            saveable.CaptureState(saveData);
        }

        return saveData;
    }

    private void ApplySaveData(SaveData saveData)
    {
        foreach (ISaveable saveable in GetSaveables())
        {
            saveable.RestoreState(saveData);
        }
    }

    private List<ISaveable> GetSaveables()
    {
        List<ISaveable> saveables = new List<ISaveable>();

        AddAssignedSaveables(saveables);

        if (autoDiscoverSaveables)
        {
            AddSceneSaveables(saveables);
        }

        saveables.Sort((first, second) => first.SaveOrder.CompareTo(second.SaveOrder));
        return saveables;
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
}
