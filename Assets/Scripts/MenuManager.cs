using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MenuManager : MonoBehaviour
{
    [Header("Scripts References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private GalleryManager galleryManager;

    [Header("Menu UI")]
    [SerializeField] private GameObject difficultyPanel;     // Le panel qui contient les boutons de difficulté (3x3, 4x4, 5x5)
    [SerializeField] private GameObject selectionModePanel;  // Le panel qui contient les boutons de sélection de mode (choisir depuis la galerie ou prendre une photo)

    [SerializeField] private Button easyButton;              // Le bouton pour commencer une partie en Easy
    [SerializeField] private Button intermediateButton;      // Le bouton pour commencer une partie en Intermediate
    [SerializeField] private Button hardButton;              // Le bouton pour commencer une partie en Hard

    [SerializeField] private Button chooseFromGalleryButton; // Le bouton pour choisir une image depuis la galerie
    [SerializeField] private Button takePhotoButton;         // Le bouton pour prendre une photo avec la caméra

    [Header("In-Game Navigation UI")]
    [SerializeField] private GameObject pauseButtonObject;   // L'objet du button pause en haut à droite pour ouvrir le panel d'options pendant la partie
    [SerializeField] private Button pauseButton;             // Le button pause en haut à droite pour ouvrir le panel d'options pendant la partie

    [SerializeField] private GameObject inGamePanel;         // Le panel qui s'affiche quand on appuie sur le bouton pause
    [SerializeField] private Button inGameReturnButton;      // Le button dans le menu de pause pour retourner à la partie      
    [SerializeField] private Button inGameRestartButton;     // Le button dans le menu de pause pour recommencer la partie
    [SerializeField] private Button inGamebackToMenuButton;  // Le button dans le menu de pause pour retourner au menu principal

    [Header("Completion UI")]
    [SerializeField] private GameObject finishPanel;         // Le panel qui s'affiche quand le temps est écoulé (quand la partie est donc finie)
    [SerializeField] private TMP_Text finishStatusText;      // Le texte qui s'affiche dans le panel de fin pour indiquer si le joueur a gagné ou perdu
    [SerializeField] private Button finishRestartButton;     // Le button dans le menu de fin pour recommencer la partie
    [SerializeField] private Button finishBackToMenuButton;  // Le button dans le menu de fin pour retourner au menu principal

    private bool pickerInitialized = false;
    private int currentGridLength = 0;
    private int pendingGridLength = 0;

    private void Awake()
    {
        galleryManager.enabled = false;

        BindButtons();
        ShowMenu();
    }

    private void OnEnable()
    {
        gameManager.PuzzleCompleted += HandlePuzzleCompleted;
    }

    private void OnDisable()
    {
        gameManager.PuzzleCompleted -= HandlePuzzleCompleted;
    }

    // On bind directement dans le script les fonctions à appeler quand les différents boutons sont cliqués.
    private void BindButtons()
    {
        easyButton.onClick.AddListener(() => StartSelectedMode(3));
        intermediateButton.onClick.AddListener(() => StartSelectedMode(4));
        hardButton.onClick.AddListener(() => StartSelectedMode(5));
        chooseFromGalleryButton.onClick.AddListener(ChooseImageFromGallery);
        takePhotoButton.onClick.AddListener(TakePhoto);
        pauseButton.onClick.AddListener(ToggleInGameMenu);
        finishRestartButton.onClick.AddListener(RestartCurrentGame);
        finishBackToMenuButton.onClick.AddListener(ReturnToMenu);
        finishRestartButton.onClick.AddListener(RestartCurrentGame);
        inGameReturnButton.onClick.AddListener(CloseInGameMenu);
        inGamebackToMenuButton.onClick.AddListener(ReturnToMenu);
    }

    private void StartSelectedMode(int gridLength)
    {
        SoundManager.Instance.PlayButtonClick();

        pendingGridLength = gridLength;

        if (galleryManager == null)
        {
            Debug.LogError("galleryManager n'est pas disponible. Le niveau ne peut pas demarrer.");
            ShowMenu();
            return;
        }

        if (!pickerInitialized)
        {
            galleryManager.enabled = true;
            pickerInitialized = true;
        }

        // On vérifie si la plateforme supporte la prise de photo. Si c'est le cas, on affiche d'abord le panel de sélection de mode
        // pour laisser le choix au joueur entre choisir une image depuis la galerie ou prendre une photo.
        if (galleryManager.SupportsTakingPhoto())
        {
            difficultyPanel.SetActive(false); // On masque donc le panel de difficulté
            selectionModePanel.SetActive(true); // Pour afficher le panel de sélection de mode
            return; // Et on retourne pour ne pas lancer directement le processus de sélection d'image depuis la galerie ci dessous
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
        if (pendingGridLength <= 1)
        {
            Debug.LogError("No difficulty is currently selected.");
            return;
        }

        imagePickerAction(texture =>
        {
            if (texture == null)
            {
                return;
            }

            currentGridLength = pendingGridLength;
            gameManager.StartGame(currentGridLength);

            difficultyPanel.SetActive(false);
            pauseButtonObject.SetActive(true);
            selectionModePanel.SetActive(false);
            finishPanel.SetActive(false);
            inGamePanel.SetActive(false);
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
        ShowMenu();
    }

    // Le menu est juste le panel de sélection de difficulté. Tout le reste est desactivé.
    private void ShowMenu()
    {
        difficultyPanel.SetActive(true);
        pauseButtonObject.SetActive(false);
        selectionModePanel.SetActive(false);
        inGamePanel.SetActive(false);
        finishPanel.SetActive(false);
    }

    private void HandlePuzzleCompleted(bool didWin)
    {
        if (didWin)
        {
            SoundManager.Instance.PlayVictory();
        }
        else
        {
            SoundManager.Instance.PlayDefeat();
        }

        pauseButtonObject.SetActive(false);
        inGamePanel.SetActive(false);
        finishPanel.SetActive(false);

        ShowCompletionPanel(didWin);
    }

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

    private void ShowCompletionPanel(bool didWin)
    {
        SetCompletionStatusText(didWin ? "You win" : "You lose");

        finishPanel.SetActive(true);
    }

    private void SetCompletionStatusText(string value)
    {
        finishStatusText.text = value;
    }

    private void RestartCurrentGame()
    {
        SoundManager.Instance.PlayButtonClick();

        if (currentGridLength <= 1)
        {
            Debug.LogError("No difficulty is currently selected.");
            return;
        }

        gameManager.RestartGame();

        difficultyPanel.SetActive(false);
        pauseButtonObject.SetActive(true);
        finishPanel.SetActive(false);
        inGamePanel.SetActive(false);

        gameManager.ResumeGame();
    }
}
