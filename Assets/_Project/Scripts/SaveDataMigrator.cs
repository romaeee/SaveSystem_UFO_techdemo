using UnityEngine;

public static class SaveDataMigrator
{
    public const int CurrentSchemaVersion = 4;

    public static SaveData Migrate(SaveData saveData)
    {
        if (saveData == null)
        {
            return null;
        }

        if (saveData.schemaVersion <= 0)
        {
            saveData.schemaVersion = 1;
        }

        while (saveData.schemaVersion < CurrentSchemaVersion)
        {
            switch (saveData.schemaVersion)
            {
                case 1:
                    MigrateFrom1To2(saveData);
                    break;

                case 2:
                    MigrateFrom2To3(saveData);
                    break;

                case 3:
                    MigrateFrom3To4(saveData);
                    break;

                default:
                    Debug.LogWarning($"No migration path for save version {saveData.schemaVersion}.");
                    saveData.schemaVersion = CurrentSchemaVersion;
                    break;
            }
        }

        EnsureDefaults(saveData);
        return saveData;
    }

    private static void EnsureDefaults(SaveData saveData)
    {
        saveData.animalCounts ??= new AnimalCountsData();
        saveData.animalCounterValues ??= new System.Collections.Generic.List<AnimalCountSaveData>();
        saveData.player ??= new TransformData();
        saveData.camera ??= new TransformData();
        saveData.animals ??= new System.Collections.Generic.List<AnimalSaveData>();
    }

    private static void MigrateFrom1To2(SaveData saveData)
    {
        saveData.hasCamera = false;
        saveData.camera ??= new TransformData();
        saveData.schemaVersion = 2;
    }

    private static void MigrateFrom2To3(SaveData saveData)
    {
        if (saveData.animals != null)
        {
            foreach (AnimalSaveData animal in saveData.animals)
            {
                if (animal != null)
                {
                    animal.localScale = Vector3.one;
                    animal.isAbducting = false;
                }
            }
        }

        saveData.schemaVersion = 3;
    }

    private static void MigrateFrom3To4(SaveData saveData)
    {
        saveData.animalCounterValues ??= new System.Collections.Generic.List<AnimalCountSaveData>();
        saveData.schemaVersion = 4;
    }
}
