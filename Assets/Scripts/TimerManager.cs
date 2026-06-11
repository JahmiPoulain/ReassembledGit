using System;
using TMPro;
using UnityEngine;

public class TimerManager : MonoBehaviour
{
    // Le texte qui affiche le timer
    [SerializeField] private TMP_Text timerText;
    // Le nombre de secondes pour la durée d'une partie, réglable depuis l'inspecteur
    [SerializeField] private float gameDurationSeconds = 120f;

    private Action timerCompleted;
    private float remainingTime;
    private bool isTimerRunning;
    private bool isTimerPaused;

    private void Awake()
    {
        SetVisible(false);
    }

    private void Update()
    {
        if (!isTimerRunning || isTimerPaused)
        {
            return;
        }

        remainingTime = Mathf.Max(0f, remainingTime - Time.deltaTime);
        RefreshTimerText();

        if (remainingTime > 0f)
        {
            return;
        }

        isTimerRunning = false;
        timerCompleted?.Invoke();
    }

    // Quand cette méthode est appelée, on initialise le timer avec la durée définie, on l'affiche et on le démarre.
    public void StartTimer(Action onCompleted)
    {
        timerCompleted = onCompleted;
        remainingTime = Mathf.Max(0f, gameDurationSeconds);
        isTimerRunning = true;
        isTimerPaused = false;
        SetVisible(true);
        RefreshTimerText();
    }

    public void StopTimer()
    {
        isTimerRunning = false;
        isTimerPaused = false;
    }

    public void PauseTimer()
    {
        if (!isTimerRunning)
        {
            return;
        }

        isTimerPaused = true;
    }

    public void ResumeTimer()
    {
        if (!isTimerRunning)
        {
            return;
        }

        isTimerPaused = false;
    }

    public void SetVisible(bool isVisible)
    {
        timerText.gameObject.SetActive(isVisible);
    }

    private void RefreshTimerText()
    {
        int seconds = Mathf.CeilToInt(remainingTime);
        int minutesPart = seconds / 60;
        int secondsPart = seconds % 60;
        timerText.text = $"{minutesPart:00}:{secondsPart:00}";
    }
}
