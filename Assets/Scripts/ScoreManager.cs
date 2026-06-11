using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool HasBestTime(int gridSize) => PlayerPrefs.HasKey(Key(gridSize));

    public float GetBestTime(int gridSize) => PlayerPrefs.GetFloat(Key(gridSize), float.MaxValue);

    // Returns true if this is a new record.
    public bool TrySetBestTime(int gridSize, float elapsedSeconds)
    {
        if (elapsedSeconds < GetBestTime(gridSize))
        {
            PlayerPrefs.SetFloat(Key(gridSize), elapsedSeconds);
            PlayerPrefs.Save();
            return true;
        }
        return false;
    }

    public static string FormatTime(float totalSeconds)
    {
        int s = Mathf.FloorToInt(totalSeconds);
        return $"{s / 60:00}:{s % 60:00}";
    }

    private static string Key(int gridSize) => $"BestTime_{gridSize}";
}
