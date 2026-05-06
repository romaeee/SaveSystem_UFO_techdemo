using UnityEngine;

[CreateAssetMenu(fileName = "AnimalSO", menuName = "UFO/Animals/Animal")]
public class AnimalSO : ScriptableObject
{
    [SerializeField] private string animalName;
    [SerializeField] private Sprite icon;
    [SerializeField] private AnimalController prefab;
    [SerializeField, Min(0f)] private float spawnWeight = 1f;

    public string AnimalName => animalName;
    public Sprite Icon => icon;
    public AnimalController Prefab => prefab;
    public float SpawnWeight => spawnWeight;
}
