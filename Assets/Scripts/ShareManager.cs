using System.IO;
using UnityEngine;

public class ShareManager : MonoBehaviour
{
    public static ShareManager Instance { get; private set; }

#if UNITY_IOS && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void _ShareTextAndImage(string text, string imagePath);
#endif

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

    // Shares the puzzle image with a challenge message.
    // Call after the player wins so we can include their time.
    public void SharePuzzleChallenge(Texture2D puzzleTexture, float elapsedSeconds, int gridSize)
    {
        int minutes = Mathf.FloorToInt(elapsedSeconds / 60);
        int seconds = Mathf.FloorToInt(elapsedSeconds % 60);
        string message = $"I solved a {gridSize}x{gridSize} puzzle in {minutes:00}:{seconds:00}! Can you beat my score? 🧩";

#if UNITY_IOS && !UNITY_EDITOR
        string imagePath = null;
        if (puzzleTexture != null)
        {
            imagePath = Path.Combine(Application.temporaryCachePath, "puzzle_challenge.jpg");
            File.WriteAllBytes(imagePath, puzzleTexture.EncodeToJPG(85));
        }
        _ShareTextAndImage(message, imagePath);
#else
        Debug.Log($"[ShareManager] {message}");
#endif
    }
}
