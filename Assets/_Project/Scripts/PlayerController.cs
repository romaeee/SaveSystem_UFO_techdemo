using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private enum AbductionState
    {
        Idle,
        Centering,
        ReadyToAbduct,
        Abducting
    }

    [SerializeField] private string animalTag = "Animal";
    [SerializeField] private float detectionRadius = 3f;
    [SerializeField] private float centeredDistance = 0.15f;
    [SerializeField] private float minControlMultiplierNearAnimal = 0.35f;
    [SerializeField] private float outwardInputDotThreshold = 0.25f;
    [SerializeField] private float centerDuration = 0.45f;
    [SerializeField] private float centerAssistSpeed = 4f;
    [SerializeField] private LeanTweenType centerEase = LeanTweenType.easeOutQuad;
    [SerializeField] private GameObject abductionBeam;
    [SerializeField] private Material abductionBeamMaterial;

    private PlayerMovement playerMovement;
    private AnimalController targetAnimal;
    private AbductionState state;
    private bool isCenterTweenActive;
    private bool isAbductionQueued;
    private float centerAssistStrength;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        FindAbductionBeamIfNeeded();
        LoadAbductionBeamMaterialIfNeeded();
        ApplyAbductionBeamMaterial();
        SetAbductionBeamActive(false);
    }

    private void Update()
    {
        if (state != AbductionState.Abducting)
        {
            UpdateNearbyAnimal();
        }

        switch (state)
        {
            case AbductionState.Idle:
                if (IsTargetValid() && IsMovingAwayFromAnimal(targetAnimal))
                {
                    ClearCenterAssist();
                    break;
                }

                TryStartCentering();
                break;

            case AbductionState.Centering:
                if (WasAbductPressed())
                {
                    QueueAbductionAfterCentering();
                }

                UpdateCenterAssist();

                if (IsTargetValid() && GetHorizontalDistance(targetAnimal.transform.position) <= centeredDistance)
                {
                    SetAbductionBeamActive(true);
                    FinishCentering();
                }
                break;

            case AbductionState.ReadyToAbduct:
                if (!IsTargetValid())
                {
                    ResetTarget();
                    return;
                }

                SetAbductionBeamActive(true);

                if (playerMovement != null && playerMovement.HasMoveInput)
                {
                    if (IsMovingAwayFromAnimal(targetAnimal))
                    {
                        ClearCenterAssist();
                    }
                    else
                    {
                        UpdateCenterAssist();
                    }

                    state = AbductionState.Idle;
                    return;
                }

                if (WasAbductPressed())
                {
                    StartAnimalAbduction();
                }
                break;
        }
    }

    private void UpdateNearbyAnimal()
    {
        if (IsTargetValid())
        {
            ApplyControlWeight(targetAnimal);

            if (GetHorizontalDistance(targetAnimal.transform.position) > detectionRadius)
            {
                ResetTarget();
            }

            return;
        }

        targetAnimal = FindNearestAnimal();

        if (targetAnimal == null)
        {
            ResetTarget();
            return;
        }

        ApplyControlWeight(targetAnimal);

        if (GetHorizontalDistance(targetAnimal.transform.position) > detectionRadius)
        {
            ResetTarget();
        }
    }

    private AnimalController FindNearestAnimal()
    {
        GameObject[] animals = GameObject.FindGameObjectsWithTag(animalTag);
        AnimalController nearestAnimal = null;
        float nearestDistanceSqr = detectionRadius * detectionRadius;

        foreach (GameObject animalObject in animals)
        {
            AnimalController animal = animalObject.GetComponent<AnimalController>();

            if (animal == null || !animal.CanBeAbducted)
            {
                continue;
            }

            Vector3 offset = animal.transform.position - transform.position;
            offset.y = 0f;
            float distanceSqr = offset.sqrMagnitude;

            if (distanceSqr <= nearestDistanceSqr)
            {
                nearestDistanceSqr = distanceSqr;
                nearestAnimal = animal;
            }
        }

        return nearestAnimal;
    }

    private void TryStartCentering()
    {
        if (!IsTargetValid() || playerMovement == null)
        {
            return;
        }

        if (playerMovement != null && playerMovement.HasMoveInput)
        {
            return;
        }

        if (GetHorizontalDistance(targetAnimal.transform.position) <= centeredDistance)
        {
            SetAbductionBeamActive(true);
            state = AbductionState.ReadyToAbduct;
            return;
        }

        state = AbductionState.Centering;
        isCenterTweenActive = true;
        isAbductionQueued = false;
        centerAssistStrength = 0f;
        UpdateCenterAssist();
        SetAbductionBeamActive(true);

        LeanTween.cancel(gameObject);
        LeanTween.value(gameObject, 0f, 1f, centerDuration)
            .setEase(centerEase)
            .setOnUpdate(SetCenterAssistStrength)
            .setOnComplete(OnCenteredOnAnimal);
    }

    private void OnCenteredOnAnimal()
    {
        isCenterTweenActive = false;
        centerAssistStrength = 1f;
        UpdateCenterAssist();
        SetAbductionBeamActive(IsTargetValid());

        FinishCentering();
    }

    private void StartAnimalAbduction()
    {
        CancelCentering();
        isAbductionQueued = false;
        state = AbductionState.Abducting;
        SetAbductionBeamActive(true);

        if (playerMovement != null)
        {
            playerMovement.SetInputLocked(false);
            playerMovement.SetControlMultiplier(1f);
            playerMovement.ClearCenterAssist();
            playerMovement.SetMovementLocked(true);
        }

        targetAnimal.Abduct(transform, OnAnimalAbducted);
    }

    private void OnAnimalAbducted()
    {
        ResetTarget();
    }

    private void CancelCentering()
    {
        if (isCenterTweenActive)
        {
            LeanTween.cancel(gameObject);
            isCenterTweenActive = false;
        }

        ClearCenterAssist();
        SetAbductionBeamActive(false);
        isAbductionQueued = false;

        state = AbductionState.Idle;
    }

    private void ResetTarget()
    {
        if (isCenterTweenActive)
        {
            CancelCentering();
        }

        targetAnimal = null;
        state = AbductionState.Idle;
        isAbductionQueued = false;
        SetAbductionBeamActive(false);

        if (playerMovement != null)
        {
            playerMovement.SetInputLocked(false);
            playerMovement.SetControlMultiplier(1f);
            playerMovement.ClearCenterAssist();
            playerMovement.SetMovementLocked(false);
        }
    }

    private void ApplyControlWeight(AnimalController animal)
    {
        if (playerMovement != null)
        {
            float distance = GetHorizontalDistance(animal.transform.position);
            float proximity = 1f - Mathf.Clamp01(distance / detectionRadius);
            float multiplier = Mathf.Lerp(1f, minControlMultiplierNearAnimal, proximity);

            if (IsMovingAwayFromAnimal(animal))
            {
                multiplier = 1f;
            }

            playerMovement.SetControlMultiplier(multiplier);
        }
    }

    private void SetCenterAssistStrength(float strength)
    {
        centerAssistStrength = strength;
        UpdateCenterAssist();
    }

    private void UpdateCenterAssist()
    {
        if (!IsTargetValid() || playerMovement == null)
        {
            ClearCenterAssist();
            return;
        }

        Vector3 targetPosition = transform.position;
        targetPosition.x = targetAnimal.transform.position.x;
        targetPosition.z = targetAnimal.transform.position.z;

        float assistStrength = centerAssistStrength;

        if (!isAbductionQueued && IsMovingAwayFromAnimal(targetAnimal))
        {
            assistStrength = 0f;
        }

        playerMovement.SetCenterAssist(targetPosition, assistStrength, centerAssistSpeed);
    }

    private void QueueAbductionAfterCentering()
    {
        if (!IsTargetValid())
        {
            return;
        }

        isAbductionQueued = true;
        centerAssistStrength = 1f;
        SetAbductionBeamActive(true);

        if (playerMovement != null)
        {
            playerMovement.SetInputLocked(true);
            playerMovement.SetControlMultiplier(1f);
        }
    }

    private void FinishCentering()
    {
        if (!IsTargetValid())
        {
            ResetTarget();
            return;
        }

        if (GetHorizontalDistance(targetAnimal.transform.position) > centeredDistance)
        {
            state = AbductionState.Centering;
            return;
        }

        if (isAbductionQueued)
        {
            StartAnimalAbduction();
            return;
        }

        state = AbductionState.ReadyToAbduct;
    }

    private void ClearCenterAssist()
    {
        centerAssistStrength = 0f;

        if (playerMovement != null)
        {
            playerMovement.ClearCenterAssist();
        }
    }

    private bool IsTargetValid()
    {
        return targetAnimal != null && targetAnimal.CanBeAbducted;
    }

    private float GetHorizontalDistance(Vector3 targetPosition)
    {
        Vector3 offset = targetPosition - transform.position;
        offset.y = 0f;

        return offset.magnitude;
    }

    private bool IsMovingAwayFromAnimal(AnimalController animal)
    {
        if (playerMovement == null || animal == null || !playerMovement.HasMoveInput)
        {
            return false;
        }

        Vector3 awayFromAnimal = transform.position - animal.transform.position;
        awayFromAnimal.y = 0f;

        if (awayFromAnimal.sqrMagnitude <= 0.001f)
        {
            awayFromAnimal = playerMovement.MoveInput;
        }

        awayFromAnimal.Normalize();

        return Vector3.Dot(playerMovement.MoveInput.normalized, awayFromAnimal) >= outwardInputDotThreshold;
    }

    private bool WasAbductPressed()
    {
        bool keyboardPressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        bool gamepadPressed = Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;

        return keyboardPressed || gamepadPressed;
    }

    private void FindAbductionBeamIfNeeded()
    {
        if (abductionBeam != null)
        {
            return;
        }

        Transform beamTransform = transform.Find("AbductionBeamCone");

        if (beamTransform != null)
        {
            abductionBeam = beamTransform.gameObject;
        }
    }

    private void SetAbductionBeamActive(bool isActive)
    {
        if (abductionBeam != null && abductionBeam.activeSelf != isActive)
        {
            abductionBeam.SetActive(isActive);
        }
    }

    private void LoadAbductionBeamMaterialIfNeeded()
    {
        if (abductionBeamMaterial == null)
        {
            abductionBeamMaterial = Resources.Load<Material>("Materials/AbductionBeamGlow");
        }
    }

    private void ApplyAbductionBeamMaterial()
    {
        if (abductionBeam == null || abductionBeamMaterial == null)
        {
            return;
        }

        Renderer[] beamRenderers = abductionBeam.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer beamRenderer in beamRenderers)
        {
            beamRenderer.sharedMaterial = abductionBeamMaterial;
        }
    }
}
