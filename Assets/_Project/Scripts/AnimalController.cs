using UnityEngine;
using System;

public class AnimalController : MonoBehaviour
{
    [SerializeField] private float abductDuration = 2f;
    [SerializeField] private float targetYOffset = -0.4f;
    [SerializeField] private float endScale = 0.05f;
    [SerializeField] private LeanTweenType moveEase = LeanTweenType.easeInOutQuad;
    [SerializeField] private LeanTweenType scaleEase = LeanTweenType.easeInQuad;

    private Collider[] colliders;
    private bool isAbducting;
    private bool isAbducted;
    private Action onAbducted;

    public bool CanBeAbducted => !isAbducting && !isAbducted && gameObject.activeInHierarchy;

    private void Awake()
    {
        colliders = GetComponentsInChildren<Collider>();
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

    private void CompleteAbduction()
    {
        isAbducting = false;
        isAbducted = true;
        onAbducted?.Invoke();
        Destroy(gameObject);
    }

    private void SetCollidersEnabled(bool enabled)
    {
        foreach (Collider animalCollider in colliders)
        {
            animalCollider.enabled = enabled;
        }
    }
}
