using UnityEngine;
using System;
using System.Collections.Generic;

public class AnimalController : MonoBehaviour
{
    public static event Action<AnimalController> AnimalDestroyed;
    private static readonly List<AnimalController> activeAnimals = new List<AnimalController>();

    public static IReadOnlyList<AnimalController> ActiveAnimals => activeAnimals;

    [SerializeField] private float abductDuration = 2f;
    [SerializeField] private float targetYOffset = -0.4f;
    [SerializeField] private float endScale = 0.05f;
    [SerializeField] private LeanTweenType moveEase = LeanTweenType.easeInOutQuad;
    [SerializeField] private LeanTweenType scaleEase = LeanTweenType.easeInQuad;

    private Collider[] colliders;
    private bool isAbducting;
    private bool isAbducted;
    private bool isRegistered;
    private Action onAbducted;

    public AnimalSO AnimalData { get; private set; }
    public bool CanBeAbducted => !isAbducting && !isAbducted && gameObject.activeInHierarchy;

    private void Awake()
    {
        colliders = GetComponentsInChildren<Collider>();
    }

    private void OnEnable()
    {
        RegisterActiveAnimal();
    }

    private void OnDisable()
    {
        UnregisterActiveAnimal();
    }

    private void OnDestroy()
    {
        UnregisterActiveAnimal();
    }

    public void Abduct(Transform ufo, Action onComplete)
    {
        if (!CanBeAbducted || ufo == null)
        {
            return;
        }

        isAbducting = true;
        onAbducted = onComplete;

        SetCollidersEnabled(false);
        LeanTween.cancel(gameObject);

        Vector3 targetPosition = ufo.position + Vector3.up * targetYOffset;

        LeanTween.move(gameObject, targetPosition, abductDuration)
            .setEase(moveEase);

        LeanTween.scale(gameObject, Vector3.one * endScale, abductDuration)
            .setEase(scaleEase)
            .setOnComplete(CompleteAbduction);
    }

    public void Initialize(AnimalSO animalData)
    {
        AnimalData = animalData;
    }

    private void CompleteAbduction()
    {
        isAbducting = false;
        isAbducted = true;
        onAbducted?.Invoke();
        AnimalDestroyed?.Invoke(this);
        Destroy(gameObject);
    }

    private void SetCollidersEnabled(bool enabled)
    {
        foreach (Collider animalCollider in colliders)
        {
            animalCollider.enabled = enabled;
        }
    }

    private void RegisterActiveAnimal()
    {
        if (isRegistered)
        {
            return;
        }

        activeAnimals.Add(this);
        isRegistered = true;
    }

    private void UnregisterActiveAnimal()
    {
        if (!isRegistered)
        {
            return;
        }

        activeAnimals.Remove(this);
        isRegistered = false;
    }
}
