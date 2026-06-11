using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MenuManager : MonoBehaviour
{
    // ── Inspector refs ──────────────────────────────────────────────────
    [Header("Scripts")]
    [SerializeField] private GameManager    gameManager;
    [SerializeField] private GalleryManager galleryManager;

    [Header("Menu UI")]
    [SerializeField] private GameObject difficultyPanel;
    [SerializeField] private GameObject selectionModePanel;
    [SerializeField] private Button     easyButton;
    [SerializeField] private Button     intermediateButton;
    [SerializeField] private Button     hardButton;
    [SerializeField] private Button     chooseFromGalleryButton;
    [SerializeField] private Button     takePhotoButton;

    [Header("In-Game Navigation UI")]
    [SerializeField] private GameObject pauseButtonObject;
    [SerializeField] private Button     pauseButton;
    [SerializeField] private GameObject inGamePanel;
    [SerializeField] private Button     inGameReturnButton;
    [SerializeField] private Button     inGameRestartButton;
    [SerializeField] private Button     inGamebackToMenuButton;

    [Header("Completion UI")]
    [SerializeField] private GameObject finishPanel;
    [SerializeField] private TMP_Text   finishStatusText;
    [SerializeField] private Button     finishRestartButton;
    [SerializeField] private Button     finishBackToMenuButton;

    // ── Runtime-created UI ──────────────────────────────────────────────
    // Finish panel stats
    private TMP_Text finishStarsText;
    private TMP_Text finishTimeText;
    private TMP_Text finishMovesText;
    private TMP_Text finishBestTimeText;
    private Button   finishShareButton;

    // In-game HUD
    private TMP_Text hudMoveCountText;
    private TMP_Text hudProgressText;
    private Button   hudHintButton;
    private TMP_Text hudHintCountText;

    // Difficulty best-time labels
    private TMP_Text easyBestLabel;
    private TMP_Text intermediateBestLabel;
    private TMP_Text hardBestLabel;

    // Relax mode toggle (lives on the difficulty panel)
    private Button   relaxToggleButton;
    private TMP_Text relaxToggleLabel;

    // ── State ───────────────────────────────────────────────────────────
    private bool pickerInitialized = false;
    private int  currentGridLength = 0;
    private int  pendingGridLength = 0;
    private bool isRelaxMode       = false;
    private int  hintsRemaining    = 3;
    private const int MaxHints     = 3;

    // ══════════════════════════════════════════════════════════════════
    //  LIFECYCLE
    // ══════════════════════════════════════════════════════════════════

    private void Awake()
    {
        EnsureManagers();
        galleryManager.enabled = false;
        CreateDynamicUI();
        BindButtons();
        ShowMenu();
    }

    private void OnEnable()
    {
        gameManager.PuzzleCompleted      += HandlePuzzleCompleted;
        gameManager.PiecePlacedCorrectly += HandlePiecePlacedCorrectly;
        gameManager.MoveMade             += OnMoveMade;
    }

    private void OnDisable()
    {
        gameManager.PuzzleCompleted      -= HandlePuzzleCompleted;
        gameManager.PiecePlacedCorrectly -= HandlePiecePlacedCorrectly;
        gameManager.MoveMade             -= OnMoveMade;
    }

    // ══════════════════════════════════════════════════════════════════
    //  MANAGER BOOTSTRAP
    // ══════════════════════════════════════════════════════════════════

    private void EnsureManagers()
    {
        if (ScoreManager.Instance == null)
            new GameObject("ScoreManager").AddComponent<ScoreManager>();

        if (ShareManager.Instance == null)
            new GameObject("ShareManager").AddComponent<ShareManager>();

        if (HapticsManager.Instance == null)
            new GameObject("HapticsManager").AddComponent<HapticsManager>();

        if (WinEffectManager.Instance == null)
        {
            var wem = new GameObject("WinEffectManager").AddComponent<WinEffectManager>();
            Canvas c = finishPanel.GetComponentInParent<Canvas>();
            wem.Init(c);
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  DYNAMIC UI CREATION
    // ══════════════════════════════════════════════════════════════════

    private void CreateDynamicUI()
    {
        Canvas canvas = finishPanel.GetComponentInParent<Canvas>();
        if (canvas == null) return;

        CreateFinishStats();
        CreateHUD(canvas);
        CreateBestTimeLabels();
        CreateRelaxToggle(canvas);
    }

    // ── Finish panel ────────────────────────────────────────────────────

    private void CreateFinishStats()
    {
        GameObject container = new GameObject("FinishStats");
        container.transform.SetParent(finishPanel.transform, false);

        RectTransform rt = container.AddComponent<RectTransform>();
        rt.anchorMin  = new Vector2(0.05f, 0.22f);
        rt.anchorMax  = new Vector2(0.95f, 0.64f);
        rt.offsetMin  = rt.offsetMax = Vector2.zero;

        VerticalLayoutGroup vlg = container.AddComponent<VerticalLayoutGroup>();
        vlg.spacing             = 6f;
        vlg.childAlignment      = TextAnchor.MiddleCenter;
        vlg.childControlWidth   = true;
        vlg.childControlHeight  = false;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(0, 0, 4, 4);

        finishStarsText    = CloneText(finishStatusText, container.transform, "FinishStars",    "★★★", 52, true,  new Color(1f, 0.85f, 0.1f));
        finishTimeText     = CloneText(finishStatusText, container.transform, "FinishTime",     "",    32, false, Color.white);
        finishMovesText    = CloneText(finishStatusText, container.transform, "FinishMoves",    "",    26, false, new Color(0.85f, 0.85f, 0.85f));
        finishBestTimeText = CloneText(finishStatusText, container.transform, "FinishBestTime", "",    24, false, new Color(0.4f, 1f, 0.55f));

        // Share button — clone from existing button to inherit font/style
        GameObject shareGo = Instantiate(finishRestartButton.gameObject, container.transform);
        shareGo.name = "FinishShareButton";
        finishShareButton = shareGo.GetComponent<Button>();
        finishShareButton.onClick.RemoveAllListeners();
        finishShareButton.onClick.AddListener(OnShareButtonClicked);
        TMP_Text shareLbl = shareGo.GetComponentInChildren<TMP_Text>();
        if (shareLbl != null) shareLbl.text = "Share Challenge";
        RectTransform shareRt = shareGo.GetComponent<RectTransform>();
        shareRt.sizeDelta = new Vector2(shareRt.sizeDelta.x, 46f);
        shareGo.SetActive(false);
    }

    // ── In-game HUD ─────────────────────────────────────────────────────

    private void CreateHUD(Canvas canvas)
    {
        hudMoveCountText = CreateHUDText(canvas, "HUD_Moves",    "0 coups", new Vector2(0.02f, 0.03f), new Vector2(0.36f, 0.115f));
        hudProgressText  = CreateHUDText(canvas, "HUD_Progress", "",        new Vector2(0.64f, 0.03f), new Vector2(0.98f, 0.115f));
        CreateHintButton(canvas);
    }

    private void CreateHintButton(Canvas canvas)
    {
        GameObject go = new GameObject("HUD_Hint");
        go.transform.SetParent(canvas.transform, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.36f, 0.03f);
        rt.anchorMax = new Vector2(0.64f, 0.115f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        Image img = go.AddComponent<Image>();
        img.color = new Color(1f, 0.85f, 0.1f, 0.88f);

        hudHintButton = go.AddComponent<Button>();
        hudHintButton.targetGraphic = img;
        hudHintButton.onClick.AddListener(OnHintClicked);
        go.AddComponent<ButtonEffect>();

        GameObject labelGo = new GameObject("Label");
        labelGo.transform.SetParent(go.transform, false);

        hudHintCountText = labelGo.AddComponent<TextMeshProUGUI>();
        hudHintCountText.text      = $"💡 {MaxHints}";
        hudHintCountText.fontSize  = 24;
        hudHintCountText.color     = Color.black;
        hudHintCountText.fontStyle = FontStyles.Bold;
        hudHintCountText.alignment = TextAlignmentOptions.Center;

        RectTransform lrt = labelGo.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;

        go.SetActive(false);
    }

    // ── Best-time labels ────────────────────────────────────────────────

    private void CreateBestTimeLabels()
    {
        easyBestLabel         = AddBestLabelToButton(easyButton);
        intermediateBestLabel = AddBestLabelToButton(intermediateButton);
        hardBestLabel         = AddBestLabelToButton(hardButton);
    }

    private TMP_Text AddBestLabelToButton(Button btn)
    {
        GameObject go = new GameObject("BestTimeLabel");
        go.transform.SetParent(btn.transform, false);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize  = 18;
        tmp.color     = new Color(1f, 1f, 0.5f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.text      = "";

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, -0.48f);
        rt.anchorMax = new Vector2(1f,  0f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        return tmp;
    }

    // ── Relax mode toggle ────────────────────────────────────────────────

    private void CreateRelaxToggle(Canvas canvas)
    {
        GameObject go = new GameObject("RelaxToggle");
        go.transform.SetParent(canvas.transform, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.28f, 0.82f);
        rt.anchorMax = new Vector2(0.72f, 0.91f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        Image img = go.AddComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.15f, 0.75f);

        relaxToggleButton = go.AddComponent<Button>();
        relaxToggleButton.targetGraphic = img;
        relaxToggleButton.onClick.AddListener(ToggleRelaxMode);
        go.AddComponent<ButtonEffect>();

        GameObject labelGo = new GameObject("Label");
        labelGo.transform.SetParent(go.transform, false);

        relaxToggleLabel = labelGo.AddComponent<TextMeshProUGUI>();
        relaxToggleLabel.text      = "⏱  Mode Chrono";
        relaxToggleLabel.fontSize  = 22;
        relaxToggleLabel.color     = Color.white;
        relaxToggleLabel.alignment = TextAlignmentOptions.Center;

        RectTransform lrt = labelGo.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;

        go.SetActive(false); // shown only when difficulty panel is visible
    }

    // ── UI helper factories ──────────────────────────────────────────────

    private TMP_Text CloneText(TMP_Text source, Transform parent, string name, string text, float fontSize, bool bold, Color color)
    {
        GameObject go = Instantiate(source.gameObject, parent);
        go.name = name;

        TMP_Text tmp = go.GetComponent<TMP_Text>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = color;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, fontSize * 1.6f);
        return tmp;
    }

    private TMP_Text CreateHUDText(Canvas canvas, string name, string defaultText, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(canvas.transform, false);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text         = defaultText;
        tmp.fontSize     = 26;
        tmp.color        = Color.white;
        tmp.fontStyle    = FontStyles.Bold;
        tmp.alignment    = TextAlignmentOptions.Center;
        tmp.outlineWidth = 0.2f;
        tmp.outlineColor = new Color32(0, 0, 0, 180);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        go.SetActive(false);
        return tmp;
    }

    // ══════════════════════════════════════════════════════════════════
    //  BUTTON BINDINGS
    // ══════════════════════════════════════════════════════════════════

    private void BindButtons()
    {
        easyButton.onClick.AddListener(()         => StartSelectedMode(3));
        intermediateButton.onClick.AddListener(() => StartSelectedMode(4));
        hardButton.onClick.AddListener(()         => StartSelectedMode(5));
        chooseFromGalleryButton.onClick.AddListener(ChooseImageFromGallery);
        takePhotoButton.onClick.AddListener(TakePhoto);
        pauseButton.onClick.AddListener(ToggleInGameMenu);
        finishRestartButton.onClick.AddListener(RestartCurrentGame);
        finishBackToMenuButton.onClick.AddListener(ReturnToMenu);
        inGameReturnButton.onClick.AddListener(CloseInGameMenu);
        inGameRestartButton?.onClick.AddListener(RestartCurrentGame);
        inGamebackToMenuButton.onClick.AddListener(ReturnToMenu);
    }

    // ══════════════════════════════════════════════════════════════════
    //  GAME FLOW
    // ══════════════════════════════════════════════════════════════════

    private void StartSelectedMode(int gridLength)
    {
        SoundManager.Instance.PlayButtonClick();
        pendingGridLength = gridLength;

        if (galleryManager == null) { ShowMenu(); return; }

        if (!pickerInitialized)
        {
            galleryManager.enabled = true;
            pickerInitialized = true;
        }

        if (galleryManager.SupportsTakingPhoto())
        {
            difficultyPanel.SetActive(false);
            relaxToggleButton?.gameObject.SetActive(false);
            selectionModePanel.SetActive(true);
            return;
        }

        StartGameWithImageSelection(galleryManager.PickImageFromGallery);
    }

    private void ChooseImageFromGallery()
    {
        SoundManager.Instance.PlayButtonClick();
        StartGameWithImageSelection(galleryManager.PickImageFromGallery);
    }

    private void TakePhoto()
    {
        SoundManager.Instance.PlayButtonClick();
        StartGameWithImageSelection(galleryManager.TakePhoto);
    }

    private void StartGameWithImageSelection(Action<Action<Texture2D>> imagePickerAction)
    {
        if (pendingGridLength <= 1) return;

        imagePickerAction(texture =>
        {
            if (texture == null) return;

            currentGridLength = pendingGridLength;
            gameManager.StartGame(currentGridLength, isRelaxMode);

            difficultyPanel.SetActive(false);
            relaxToggleButton?.gameObject.SetActive(false);
            pauseButtonObject.SetActive(true);
            selectionModePanel.SetActive(false);
            finishPanel.SetActive(false);
            inGamePanel.SetActive(false);

            ShowGameHUD();
        });
    }

    private void ToggleInGameMenu()
    {
        SoundManager.Instance.PlayButtonClick();
        OpenInGameMenu();
    }

    private void ReturnToMenu()
    {
        SoundManager.Instance.PlayButtonClick();
        gameManager.ReturnToMenu();
        currentGridLength = 0;
        HideGameHUD();
        ShowMenu();
    }

    private void ShowMenu()
    {
        difficultyPanel.SetActive(true);
        relaxToggleButton?.gameObject.SetActive(true);
        pauseButtonObject.SetActive(false);
        selectionModePanel.SetActive(false);
        inGamePanel.SetActive(false);
        finishPanel.SetActive(false);
        UpdateBestTimeLabels();
    }

    private void RestartCurrentGame()
    {
        SoundManager.Instance.PlayButtonClick();
        if (currentGridLength <= 1) return;

        gameManager.RestartGame();

        difficultyPanel.SetActive(false);
        pauseButtonObject.SetActive(true);
        finishPanel.SetActive(false);
        inGamePanel.SetActive(false);

        gameManager.ResumeGame();
        ShowGameHUD();
    }

    // ══════════════════════════════════════════════════════════════════
    //  IN-GAME HUD
    // ══════════════════════════════════════════════════════════════════

    private void ShowGameHUD()
    {
        hintsRemaining = MaxHints;
        hudMoveCountText?.gameObject.SetActive(true);
        hudProgressText?.gameObject.SetActive(true);
        hudHintButton?.gameObject.SetActive(true);
        RefreshHUD();
    }

    private void HideGameHUD()
    {
        hudMoveCountText?.gameObject.SetActive(false);
        hudProgressText?.gameObject.SetActive(false);
        hudHintButton?.gameObject.SetActive(false);
    }

    private void OnMoveMade() => RefreshHUD();

    private void HandlePiecePlacedCorrectly(int _)
    {
        HapticsManager.Instance?.PieceSnap();
        RefreshHUD();
    }

    private void RefreshHUD()
    {
        if (hudMoveCountText != null)
            hudMoveCountText.text = $"{gameManager.MoveCount} coups";

        if (hudProgressText != null)
        {
            int total   = gameManager.TotalPieceCount;
            int correct = gameManager.CorrectPieceCount;
            hudProgressText.text = total > 0 ? $"{correct}/{total} ✓" : "";
        }

        if (hudHintCountText != null)
            hudHintCountText.text = $"💡 {hintsRemaining}";

        if (hudHintButton != null)
        {
            bool canHint = hintsRemaining > 0;
            hudHintButton.interactable = canHint;
            Image img = hudHintButton.GetComponent<Image>();
            if (img != null)
                img.color = canHint ? new Color(1f, 0.85f, 0.1f, 0.88f)
                                    : new Color(0.45f, 0.45f, 0.45f, 0.5f);
        }
    }

    private void OnHintClicked()
    {
        if (hintsRemaining <= 0) return;
        SoundManager.Instance.PlayButtonClick();
        gameManager.ShowHint();
        hintsRemaining--;
        RefreshHUD();
    }

    // ══════════════════════════════════════════════════════════════════
    //  RELAX MODE TOGGLE
    // ══════════════════════════════════════════════════════════════════

    private void ToggleRelaxMode()
    {
        isRelaxMode = !isRelaxMode;
        SoundManager.Instance.PlayButtonClick();
        if (relaxToggleLabel != null)
            relaxToggleLabel.text = isRelaxMode ? "∞  Mode Relax" : "⏱  Mode Chrono";
    }

    // ══════════════════════════════════════════════════════════════════
    //  PAUSE MENU
    // ══════════════════════════════════════════════════════════════════

    private void OpenInGameMenu()
    {
        inGamePanel.SetActive(true);
        gameManager?.PauseGame();
    }

    private void CloseInGameMenu()
    {
        SoundManager.Instance.PlayButtonClick();
        inGamePanel.SetActive(false);
        gameManager.ResumeGame();
    }

    // ══════════════════════════════════════════════════════════════════
    //  COMPLETION
    // ══════════════════════════════════════════════════════════════════

    private void HandlePuzzleCompleted(bool didWin)
    {
        HideGameHUD();

        if (didWin)
        {
            SoundManager.Instance.PlayVictory();
            HapticsManager.Instance?.Win();
            Vector3 confettiPos = gameManager.GameTransform != null
                ? gameManager.GameTransform.position
                : Vector3.zero;
            WinEffectManager.Instance?.PlayWin(confettiPos);
        }
        else
        {
            SoundManager.Instance.PlayDefeat();
            HapticsManager.Instance?.Lose();
            WinEffectManager.Instance?.PlayLose();
        }

        pauseButtonObject.SetActive(false);
        inGamePanel.SetActive(false);
        finishPanel.SetActive(false);

        ShowCompletionPanel(didWin);
    }

    private void ShowCompletionPanel(bool didWin)
    {
        finishStatusText.text = didWin ? "Bravo ! 🎉" : "Perdu... 😔";

        if (didWin)
        {
            float elapsed = gameManager.ElapsedTime;
            int   moves   = gameManager.MoveCount;
            int   stars   = CalculateStars(gameManager.RemainingTime, gameManager.GameDurationSeconds, gameManager.IsRelaxMode);

            bool isNewBest = !gameManager.IsRelaxMode && ScoreManager.Instance != null
                && ScoreManager.Instance.TrySetBestTime(currentGridLength, elapsed);

            if (finishTimeText  != null) finishTimeText.text  = ScoreManager.FormatTime(elapsed);
            if (finishMovesText != null) finishMovesText.text = $"{moves} moves";

            if (finishBestTimeText != null)
            {
                if (gameManager.IsRelaxMode)
                    finishBestTimeText.text = "Mode Relax ∞";
                else if (isNewBest)
                    finishBestTimeText.text = "🏆 Nouveau record !";
                else if (ScoreManager.Instance != null)
                    finishBestTimeText.text = $"Meilleur : {ScoreManager.FormatTime(ScoreManager.Instance.GetBestTime(currentGridLength))}";
            }

            finishShareButton?.gameObject.SetActive(true);

            finishPanel.SetActive(true);
            StartCoroutine(AnimatePanelIn(finishPanel));
            StartCoroutine(AnimateStarsIn(finishStarsText, stars));
        }
        else
        {
            if (finishStarsText    != null) finishStarsText.text    = "";
            if (finishTimeText     != null) finishTimeText.text     = "";
            if (finishMovesText    != null) finishMovesText.text    = "";
            if (finishBestTimeText != null) finishBestTimeText.text = "";
            finishShareButton?.gameObject.SetActive(false);

            finishPanel.SetActive(true);
            StartCoroutine(AnimatePanelIn(finishPanel));
        }
    }

    private static int CalculateStars(float remaining, float total, bool relax)
    {
        if (relax || total <= 0f) return 3;
        float ratio = remaining / total;
        if (ratio > 0.5f) return 3;
        if (ratio > 0.2f) return 2;
        return 1;
    }

    private void OnShareButtonClicked()
    {
        SoundManager.Instance.PlayButtonClick();
        ShareManager.Instance?.SharePuzzleChallenge(
            gameManager.CurrentPuzzleTexture,
            gameManager.ElapsedTime,
            currentGridLength
        );
    }

    // ══════════════════════════════════════════════════════════════════
    //  BEST TIME LABELS
    // ══════════════════════════════════════════════════════════════════

    private void UpdateBestTimeLabels()
    {
        if (ScoreManager.Instance == null) return;
        UpdateBestLabel(easyBestLabel,         3);
        UpdateBestLabel(intermediateBestLabel, 4);
        UpdateBestLabel(hardBestLabel,         5);
    }

    private static void UpdateBestLabel(TMP_Text label, int gridSize)
    {
        if (label == null || ScoreManager.Instance == null) return;
        label.text = ScoreManager.Instance.HasBestTime(gridSize)
            ? $"Best: {ScoreManager.FormatTime(ScoreManager.Instance.GetBestTime(gridSize))}"
            : "";
    }

    // ══════════════════════════════════════════════════════════════════
    //  ANIMATIONS
    // ══════════════════════════════════════════════════════════════════

    private static IEnumerator AnimatePanelIn(GameObject panel)
    {
        Transform t = panel.transform;
        Vector3 original = t.localScale;
        t.localScale = Vector3.zero;

        float elapsed = 0f, duration = 0.38f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(elapsed / duration);
            // ease to 1.08 then settle to 1.0
            float s = p < 0.82f
                ? Mathf.LerpUnclamped(0f, 1.08f, p / 0.82f)
                : Mathf.LerpUnclamped(1.08f, 1f, (p - 0.82f) / 0.18f);
            t.localScale = original * s;
            yield return null;
        }
        t.localScale = original;
    }

    private static IEnumerator AnimateStarsIn(TMP_Text label, int count)
    {
        if (label == null) yield break;

        string[] stages = { "☆☆☆", "★☆☆", "★★☆", "★★★" };
        label.text = "☆☆☆";
        label.transform.localScale = Vector3.one;

        for (int i = 1; i <= Mathf.Clamp(count, 0, 3); i++)
        {
            yield return new WaitForSecondsRealtime(0.28f);
            label.text = stages[i];

            float t = 0f;
            Vector3 base3 = label.transform.localScale;
            while (t < 0.2f)
            {
                t += Time.unscaledDeltaTime;
                label.transform.localScale = base3 * (1f + 0.38f * Mathf.Sin((t / 0.2f) * Mathf.PI));
                yield return null;
            }
            label.transform.localScale = base3;
        }
    }
}
