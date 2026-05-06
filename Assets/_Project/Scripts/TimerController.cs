using TMPro;
using UnityEngine;

public class TimerController : MonoBehaviour
{
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private bool startOnEnable = true;
    [SerializeField] private bool useUnscaledTime;

    private float elapsedTime;
    private bool isRunning;

    public float ElapsedTime => elapsedTime;

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
        UpdateTimerText();
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
        elapsedTime = 0f;
        UpdateTimerText();
    }

    public void RestartTimer()
    {
        ResetTimer();
        StartTimer();
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
        int seconds = totalSeconds % 60;
        int minutes = (totalSeconds / 60) % 60;
        int hours = totalSeconds / 3600;

        timerText.text = hours > 0
            ? $"{hours:00}:{minutes:00}:{seconds:00}"
            : $"{minutes:00}:{seconds:00}";
    }
}
