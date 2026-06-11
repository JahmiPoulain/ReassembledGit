using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class TimerManager : MonoBehaviour
{
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private float gameDurationSeconds = 120f;

    [Header("Urgency Colors")]
    [SerializeField] private Color normalColor  = Color.white;
    [SerializeField] private Color warningColor = new Color(1f, 0.65f, 0f);
    [SerializeField] private Color dangerColor  = Color.red;
    [SerializeField] private Color relaxColor   = new Color(0.6f, 0.9f, 1f);
    [SerializeField] private float warningThreshold = 30f;
    [SerializeField] private float dangerThreshold  = 10f;

    // ── Public properties ──────────────────────────────────────────────
    public float RemainingTime     => isCountUp ? 0f : remainingTime;
    public float GameDurationSeconds => gameDurationSeconds;
    public float ElapsedTime       => isCountUp ? remainingTime : Mathf.Max(0f, gameDurationSeconds - remainingTime);

    // ── Private state ──────────────────────────────────────────────────
    private Action   timerCompleted;
    private float    remainingTime;
    private bool     isTimerRunning;
    private bool     isTimerPaused;
    private bool     isCountUp;
    private Vector3  originalScale;
    private Coroutine pulseCoroutine;

    private void Awake()
    {
        originalScale = timerText.transform.localScale;
        SetVisible(false);
    }

    private void Update()
    {
        if (!isTimerRunning || isTimerPaused) return;

        if (isCountUp)
        {
            remainingTime += Time.deltaTime;
        }
        else
        {
            remainingTime = Mathf.Max(0f, remainingTime - Time.deltaTime);
            if (remainingTime <= 0f)
            {
                isTimerRunning = false;
                RefreshTimerText();
                timerCompleted?.Invoke();
                return;
            }
        }

        RefreshTimerText();
    }

    // ── Public API ──────────────────────────────────────────────────────

    public void StartTimer(Action onCompleted, bool relaxMode = false)
    {
        isCountUp        = relaxMode;
        timerCompleted   = relaxMode ? null : onCompleted;
        remainingTime    = relaxMode ? 0f : Mathf.Max(0f, gameDurationSeconds);
        isTimerRunning   = true;
        isTimerPaused    = false;
        timerText.color  = relaxMode ? relaxColor : normalColor;
        timerText.transform.localScale = originalScale;

        if (pulseCoroutine != null) { StopCoroutine(pulseCoroutine); pulseCoroutine = null; }

        SetVisible(true);
        RefreshTimerText();
    }

    public void StopTimer()
    {
        isTimerRunning = false;
        isTimerPaused  = false;
        if (pulseCoroutine != null) { StopCoroutine(pulseCoroutine); pulseCoroutine = null; }
        if (timerText != null) timerText.transform.localScale = originalScale;
    }

    public void PauseTimer()
    {
        if (!isTimerRunning) return;
        isTimerPaused = true;
    }

    public void ResumeTimer()
    {
        if (!isTimerRunning) return;
        isTimerPaused = false;
    }

    public void SetVisible(bool isVisible)
    {
        timerText.gameObject.SetActive(isVisible);
    }

    // ── Private ────────────────────────────────────────────────────────

    private void RefreshTimerText()
    {
        float display = isCountUp ? remainingTime : remainingTime;
        int seconds = isCountUp ? Mathf.FloorToInt(display) : Mathf.CeilToInt(display);
        timerText.text = $"{seconds / 60:00}:{seconds % 60:00}";

        if (isCountUp)
        {
            timerText.color = relaxColor;
            return;
        }

        if (remainingTime <= dangerThreshold)
        {
            timerText.color = dangerColor;
            if (pulseCoroutine == null)
                pulseCoroutine = StartCoroutine(PulseRoutine());
        }
        else if (remainingTime <= warningThreshold)
        {
            timerText.color = warningColor;
        }
        else
        {
            timerText.color = normalColor;
        }
    }

    private IEnumerator PulseRoutine()
    {
        while (isTimerRunning && !isTimerPaused && !isCountUp && remainingTime <= dangerThreshold)
        {
            float t = 0f;
            while (t < 0.14f) { t += Time.deltaTime; timerText.transform.localScale = originalScale * Mathf.Lerp(1f, 1.28f, t / 0.14f); yield return null; }
            t = 0f;
            while (t < 0.14f) { t += Time.deltaTime; timerText.transform.localScale = originalScale * Mathf.Lerp(1.28f, 1f, t / 0.14f); yield return null; }
            timerText.transform.localScale = originalScale;
            yield return new WaitForSeconds(0.45f);
        }
        timerText.transform.localScale = originalScale;
        pulseCoroutine = null;
    }
}
