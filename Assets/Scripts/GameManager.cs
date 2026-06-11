using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    public event Action<bool> PuzzleCompleted;

    // Références aux éléments de la scène
    [SerializeField] private Transform gameTransform;
    [SerializeField] private Transform piecePrefab;
    [SerializeField] private Camera gameCamera;

    // Paramètres de configuration du puzzle
    [SerializeField] private int gridSize = 4;
    [SerializeField] private float gapThickness = 0.01f;

    // La durée du slide (en seconde) quand on glisse une pièce
    [SerializeField] private float slideDuration = 0.15f;

    [SerializeField, Range(0.1f, 1f)] private float screenWidthUsage = 0.88f;
    [SerializeField, Range(0.1f, 1f)] private float screenHeightUsage = 0.78f;
    [SerializeField] private Vector2 screenCenterOffset = Vector2.zero;
    [SerializeField] private float maxAspectTileMultiplier = 2f;

    [SerializeField] private TimerManager timerManager;
    [SerializeField] private ReferencePreviewManager referencePreviewManager;
    [SerializeField] private float completionPanelDelaySeconds = 2f;

    private List<Transform> pieces;
    private int emptyLocation;
    private int rows = 4;
    private int columns = 4;
    private float boardWidth = 2f;
    private float boardHeight = 2f;
    private Material puzzleMaterialInstance;
    private Texture2D currentPuzzleTexture;
    private bool shuffling = false;
    private bool isAnimatingMove = false;
    private bool isGameStarted = false;
    private bool isGamePaused = false;

    private List<int> savedArrangement;
    private int savedEmptyLocation = -1;
    private int savedRows = 4;
    private int savedColumns = 4;

    private void Awake()
    {
        pieces = new List<Transform>();

        InitializePuzzleMaterial();
        referencePreviewManager.Initialize(gameTransform != null ? gameTransform.gameObject : null);
        timerManager.SetVisible(false);
        referencePreviewManager.HidePreview();
        referencePreviewManager.SetGameStarted(false);
    }

    private void CreateGamePieces(float gapThickness)
    {
        UpdatePuzzleLayoutFromImageAndScreen();
        CenterGameTransformOnScreen();

        float tileWidth = boardWidth / columns;
        float tileHeight = boardHeight / rows;
        float boardStartX = -boardWidth * 0.5f;
        float boardStartY = boardHeight * 0.5f;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                Transform piece = Instantiate(piecePrefab, gameTransform);
                pieces.Add(piece);
                MeshRenderer pieceRenderer = piece.GetComponent<MeshRenderer>();
                if (pieceRenderer != null && puzzleMaterialInstance != null)
                {
                    pieceRenderer.material = puzzleMaterialInstance;
                }
                piece.localPosition = new Vector3(
                    boardStartX + (tileWidth * col) + (tileWidth * 0.5f),
                    boardStartY - (tileHeight * row) - (tileHeight * 0.5f),
                    0
                );
                piece.localScale = new Vector3(
                    Mathf.Max(0.01f, tileWidth - gapThickness),
                    Mathf.Max(0.01f, tileHeight - gapThickness),
                    1f
                );
                piece.name = $"{(row * columns) + col}";

                if ((row == rows - 1) && (col == columns - 1))
                {
                    emptyLocation = (rows * columns) - 1;
                    piece.gameObject.SetActive(false);
                }
                else
                {
                    float gapX = (gapThickness * 0.5f) / Mathf.Max(boardWidth, 0.0001f);
                    float gapY = (gapThickness * 0.5f) / Mathf.Max(boardHeight, 0.0001f);
                    Mesh mesh = piece.GetComponent<MeshFilter>().mesh;
                    Vector2[] uv = new Vector2[4];
                    uv[0] = new Vector2(((float)col / columns) + gapX, 1 - (((float)(row + 1) / rows) - gapY));
                    uv[1] = new Vector2(((float)(col + 1) / columns) - gapX, 1 - (((float)(row + 1) / rows) - gapY));
                    uv[2] = new Vector2(((float)col / columns) + gapX, 1 - (((float)row / rows) + gapY));
                    uv[3] = new Vector2(((float)(col + 1) / columns) - gapX, 1 - (((float)row / rows) + gapY));
                    mesh.uv = uv;
                }
            }
        }
    }

    void Update()
    {
        if (!isGameStarted)
        {
            return;
        }

        if (!shuffling && CheckCompletion())
        {
            HandlePuzzleCompleted(true);
            return;
        }

        if (isGamePaused)
        {
            return;
        }

        if (referencePreviewManager.IsPreviewVisible)
        {
            return;
        }

        if (!isAnimatingMove && Input.GetMouseButtonDown(0))
        {
            RaycastHit2D hit = Physics2D.Raycast(gameCamera.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            int pieceIndex = hit ? pieces.IndexOf(hit.transform) : -1;
            if (pieceIndex >= 0)
            {
                if (SwapIfValid(pieceIndex, -columns, columns)) { return; }
                if (SwapIfValid(pieceIndex, +columns, columns)) { return; }
                if (SwapIfValid(pieceIndex, -1, 0)) { return; }
                SwapIfValid(pieceIndex, +1, columns - 1);
            }
        }
    }

    private bool SwapIfValid(int i, int offset, int colCheck)
    {
        SoundManager.Instance.PlaySwapPiece();
        return SwapIfValid(i, offset, colCheck, true);
    }

    private bool SwapIfValid(int i, int offset, int colCheck, bool animate)
    {
        if (((i % columns) != colCheck) && ((i + offset) == emptyLocation))
        {
            int targetIndex = i + offset;
            Transform movingPiece = pieces[i];
            Transform emptyPiece = pieces[targetIndex];
            Vector3 movingPieceTargetPosition = emptyPiece.localPosition;
            Vector3 emptyPieceTargetPosition = movingPiece.localPosition;

            (pieces[i], pieces[targetIndex]) = (pieces[targetIndex], pieces[i]);

            if (animate && slideDuration > 0f && Application.isPlaying)
            {
                StartCoroutine(AnimateSwap(emptyPiece, movingPiece, emptyPieceTargetPosition, movingPieceTargetPosition));
            }
            else
            {
                emptyPiece.localPosition = emptyPieceTargetPosition;
                movingPiece.localPosition = movingPieceTargetPosition;
            }

            emptyLocation = i;
            return true;
        }
        return false;
    }

    private bool CheckCompletion()
    {
        for (int i = 0; i < pieces.Count; i++)
        {
            if (pieces[i].name != $"{i}")
            {
                return false;
            }
        }
        return true;
    }

    private IEnumerator AnimateSwap(Transform emptyPiece, Transform movingPiece, Vector3 emptyPieceTargetPosition, Vector3 movingPieceTargetPosition)
    {
        isAnimatingMove = true;

        if (emptyPiece == null || movingPiece == null)
        {
            isAnimatingMove = false;
            yield break;
        }

        Vector3 emptyPieceStartPosition = emptyPiece.localPosition;
        Vector3 movingPieceStartPosition = movingPiece.localPosition;
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            if (emptyPiece == null || movingPiece == null)
            {
                isAnimatingMove = false;
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            emptyPiece.localPosition = Vector3.Lerp(emptyPieceStartPosition, emptyPieceTargetPosition, t);
            movingPiece.localPosition = Vector3.Lerp(movingPieceStartPosition, movingPieceTargetPosition, t);

            yield return null;
        }

        if (emptyPiece != null)
        {
            emptyPiece.localPosition = emptyPieceTargetPosition;
        }

        if (movingPiece != null)
        {
            movingPiece.localPosition = movingPieceTargetPosition;
        }

        isAnimatingMove = false;
    }

    private void InitializePuzzleMaterial()
    {
        MeshRenderer prefabRenderer = piecePrefab.GetComponent<MeshRenderer>();
        
        if (prefabRenderer == null)
        {
            Debug.LogError("Le prefab de piece n'a pas de MeshRenderer.");
            return;
        }

        if (prefabRenderer.sharedMaterial == null)
        {
            Debug.LogError("Le prefab de piece n'a pas de material assigne.");
            return;
        }

        puzzleMaterialInstance = new Material(prefabRenderer.sharedMaterial);
    }

    public void StartGame(int newGridSize)
    {
        if (newGridSize <= 1)
        {
            Debug.LogError("La taille de grille demandee est invalide.");
            return;
        }

        gridSize = newGridSize;
        PrepareNewBoard();
        referencePreviewManager.HidePreview();
        CreateGamePieces(gapThickness);
        ShuffleBoard();
        SaveCurrentArrangement();
        StartTimer();
    }

    public void RestartGame()
    {
        if (gridSize <= 1)
        {
            Debug.LogError("Aucune partie precedente a redemarrer.");
            return;
        }

        PrepareNewBoard();
        referencePreviewManager.HidePreview();
        CreateGamePieces(gapThickness);

        if (savedArrangement != null && savedArrangement.Count == pieces.Count && savedRows == rows && savedColumns == columns)
        {
            ApplySavedArrangement();
        }
        else
        {
            ShuffleBoard();
            SaveCurrentArrangement();
        }

        StartTimer();
    }

    public void ReturnToMenu()
    {
        ResetBoardState(false);
        timerManager.StopTimer();
        timerManager.SetVisible(false);
        referencePreviewManager.SetGameStarted(false);
        referencePreviewManager.HidePreview();
    }

    public void PauseGame()
    {
        if (!isGameStarted)
        {
            return;
        }

        isGamePaused = true;
        timerManager.PauseTimer();
    }

    public void ResumeGame()
    {
        if (!isGameStarted)
        {
            return;
        }

        isGamePaused = false;
        timerManager.ResumeTimer();
    }

    private void HandlePuzzleCompleted(bool didWin)
    {
        shuffling = false;
        isAnimatingMove = false;
        isGameStarted = false;
        timerManager.StopTimer();
        StartCoroutine(ShowCompletionPanelAfterDelay(didWin));
    }

    private IEnumerator ShowCompletionPanelAfterDelay(bool didWin)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, completionPanelDelaySeconds));
        PuzzleCompleted?.Invoke(didWin);
    }

    private void ShuffleBoard()
    {
        int count = 0;
        int last = 0;
        int tileCount = rows * columns;
        int shuffleTarget = tileCount * Mathf.Max(rows, columns);
        while (count < shuffleTarget)
        {
            int rnd = UnityEngine.Random.Range(0, tileCount);
            if (rnd == last) { continue; }
            last = emptyLocation;

            if (SwapIfValid(rnd, -columns, columns, false))
            {
                count++;
            }
            else if (SwapIfValid(rnd, +columns, columns, false))
            {
                count++;
            }
            else if (SwapIfValid(rnd, -1, 0, false))
            {
                count++;
            }
            else if (SwapIfValid(rnd, +1, columns - 1, false))
            {
                count++;
            }
        }
    }

    private void SaveCurrentArrangement()
    {
        savedArrangement = new List<int>(pieces.Count);
        for (int i = 0; i < pieces.Count; i++)
        {
            savedArrangement.Add(int.Parse(pieces[i].name));
        }

        savedEmptyLocation = emptyLocation;
        savedRows = rows;
        savedColumns = columns;
    }

    private void ApplySavedArrangement()
    {
        Vector3[] solvedPositions = new Vector3[pieces.Count];
        Dictionary<int, Transform> piecesById = new(pieces.Count);

        for (int i = 0; i < pieces.Count; i++)
        {
            solvedPositions[i] = pieces[i].localPosition;
            piecesById[int.Parse(pieces[i].name)] = pieces[i];
        }

        List<Transform> arrangedPieces = new(pieces.Count);
        
        for (int i = 0; i < savedArrangement.Count; i++)
        {
            Transform piece = piecesById[savedArrangement[i]];
            piece.localPosition = solvedPositions[i];
            arrangedPieces.Add(piece);
        }

        pieces = arrangedPieces;
        emptyLocation = savedEmptyLocation;
    }

    private void ClearExistingPieces()
    {
        for (int i = 0; i < pieces.Count; i++)
        {
            if (pieces[i] != null)
            {
                Destroy(pieces[i].gameObject);
            }
        }

        pieces.Clear();
    }

    public void SetPuzzleTexture(Texture2D texture)
    {
        if (texture == null)
        {
            Debug.LogError("Aucune texture a appliquer au puzzle.");
            return;
        }

        currentPuzzleTexture = texture;

        if (puzzleMaterialInstance == null)
        {
            InitializePuzzleMaterial();
        }

        if (puzzleMaterialInstance == null)
        {
            return;
        }

        puzzleMaterialInstance.mainTexture = texture;
        referencePreviewManager.SetTexture(texture);

        foreach (Transform piece in pieces)
        {
            if (piece == null)
            {
                continue;
            }

            MeshRenderer pieceRenderer = piece.GetComponent<MeshRenderer>();

            if (pieceRenderer != null)
            {
                pieceRenderer.material = puzzleMaterialInstance;
            }
        }

        if (isGameStarted)
        {
            RebuildCurrentGameForImage();
        }
    }

    private void UpdatePuzzleLayoutFromImageAndScreen()
    {
        UpdateGridDimensionsFromImage();

        if (!gameCamera.orthographic)
        {
            boardWidth = 2f;
            boardHeight = columns / (float)rows >= 1f ? 2f / (columns / (float)rows) : 2f;
            return;
        }

        float visibleHeight = gameCamera.orthographicSize * 2f;
        float visibleWidth = visibleHeight * gameCamera.aspect;
        float availableWidth = visibleWidth * screenWidthUsage;
        float availableHeight = visibleHeight * screenHeightUsage;
        float boardAspect = columns / (float)rows;

        boardWidth = availableWidth;
        boardHeight = boardWidth / boardAspect;

        if (boardHeight > availableHeight)
        {
            boardHeight = availableHeight;
            boardWidth = boardHeight * boardAspect;
        }
    }

    private void UpdateGridDimensionsFromImage()
    {
        float imageAspect = 1f;
        if (currentPuzzleTexture != null && currentPuzzleTexture.height > 0)
        {
            imageAspect = currentPuzzleTexture.width / (float)currentPuzzleTexture.height;
        }

        int maxLongSide = Mathf.Max(gridSize, Mathf.RoundToInt(gridSize * Mathf.Max(1f, maxAspectTileMultiplier)));

        if (imageAspect >= 1f)
        {
            rows = gridSize;
            columns = Mathf.Clamp(Mathf.RoundToInt(gridSize * imageAspect), gridSize, maxLongSide);
        }
        else
        {
            columns = gridSize;
            rows = Mathf.Clamp(Mathf.RoundToInt(gridSize / Mathf.Max(imageAspect, 0.01f)), gridSize, maxLongSide);
        }
    }

    private void RebuildCurrentGameForImage()
    {
        PrepareNewBoard();
        referencePreviewManager.HidePreview();
        CreateGamePieces(gapThickness);
        ShuffleBoard();
        SaveCurrentArrangement();
        StartTimer();
    }

    // Permet simplement de recentrer le transform du jeu au centre de l'écran par rapport a la position de la caméra.
    private void CenterGameTransformOnScreen()
    {
        Vector3 cameraPosition = gameCamera.transform.position;
        gameTransform.position = new Vector3(
            cameraPosition.x + screenCenterOffset.x,
            cameraPosition.y + screenCenterOffset.y,
            gameTransform.position.z
        );
    }

    private void PrepareNewBoard()
    {
        ResetBoardState(true);
        referencePreviewManager.SetGameStarted(true);
    }

    private void ResetBoardState(bool startGame)
    {
        StopAllCoroutines();
        ClearExistingPieces();
        emptyLocation = 0;
        shuffling = false;
        isAnimatingMove = false;
        isGameStarted = startGame;
        isGamePaused = false;
        referencePreviewManager.SetGameStarted(startGame);
    }

    private void StartTimer()
    {
        timerManager.StartTimer(HandleTimerCompleted);
    }

    private void HandleTimerCompleted()
    {
        if (!isGameStarted)
        {
            return;
        }

        HandlePuzzleCompleted(false);
    }
}
