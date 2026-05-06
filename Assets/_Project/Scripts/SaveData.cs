using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveData
{
    public int schemaVersion;
    public float timerElapsedSeconds;
    public AnimalCountsData animalCounts = new AnimalCountsData();
    public TransformData player = new TransformData();
    public bool hasCamera;
    public TransformData camera = new TransformData();
    public List<AnimalSaveData> animals = new List<AnimalSaveData>();
}

[Serializable]
public class AnimalCountsData
{
    public int cows;
    public int pigs;
    public int chickens;
}

[Serializable]
public class AnimalSaveData
{
    public string animalType;
    public TransformData transform = new TransformData();
}

[Serializable]
public class TransformData
{
    public Vector3 position;
    public Vector3 eulerAngles;

    public TransformData()
    {
    }

    public TransformData(Transform transform)
    {
        position = transform.position;
        eulerAngles = transform.eulerAngles;
    }

    public Quaternion Rotation => Quaternion.Euler(eulerAngles);
}
