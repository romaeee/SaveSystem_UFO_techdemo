using TMPro;
using UnityEngine;

public class TimerController : MonoBehaviour, ISaveable
{
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private bool startOnEnable = true;
    [SerializeField] private bool useUnscaledTime;

    private float elapsedTime;
    private bool isRunning;
    private int displayedTotalSeconds = -1;

    public float ElapsedTime => elapsedTime;
    public int SaveOrder => 0;

    private void Awake()
    {
        FindTimerTextIfNeeded();
        UpdateTimerText();
    }

    private void OnEnable()
    {
        if (startOnEnable)
        {
            StartTimer();
        }
    }

    private void Update()
    {
        if (!isRunning)
        {
            return;
        }

        elapsedTime += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        UpdateTimerTextIfNeeded();
    }

    public void StartTimer()
    {
        isRunning = true;
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    public void ResetTimer()
    {
        SetElapsedTime(0f);
    }

    public void RestartTimer()
    {
        ResetTimer();
        StartTimer();
    }

    public void SetElapsedTime(float value)
    {
        elapsedTime = Mathf.Max(0f, value);
        UpdateTimerText();
    }

    public void CaptureState(SaveData saveData)
    {
        saveData.timerElapsedSeconds = elapsedTime;
    }

    public void RestoreState(SaveData saveData)
    {
        SetElapsedTime(saveData.timerElapsedSeconds);
    }

    private void FindTimerTextIfNeeded()
    {
        if (timerText == null)
        {
            timerText = GetComponent<TMP_Text>();
        }

        if (timerText == null)
        {
            timerText = GetComponentInChildren<TMP_Text>();
        }
    }

    private void UpdateTimerText()
    {
        if (timerText == null)
        {
            return;
        }

        int totalSeconds = Mathf.FloorToInt(elapsedTime);
        displayedTotalSeconds = totalSeconds;
        int seconds = totalSeconds % 60;
        int minutes = (totalSeconds / 60) % 60;
        int hours = totalSeconds / 3600;

        timerText.text = hours > 0
            ? $"{hours:00}:{minutes:00}:{seconds:00}"
            : $"{minutes:00}:{seconds:00}";
    }

    private void UpdateTimerTextIfNeeded()
    {
        int totalSeconds = Mathf.FloorToInt(elapsedTime);

        if (totalSeconds != displayedTotalSeconds)
        {
            UpdateTimerText();
        }
    }
}
