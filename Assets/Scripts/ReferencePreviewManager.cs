using UnityEngine;
using UnityEngine.UI;

public class ReferencePreviewManager : MonoBehaviour
{
    [SerializeField] private Canvas previewCanvas;
    // Le button en haut à gauche pour afficher le preview
    [SerializeField] private Button referencePreviewButton;
    // La row image de ce button
    [SerializeField] private RawImage referencePreviewThumbnail;

    private GameObject referencePreviewPanel;
    private RawImage referencePreviewImage;

    private GameObject gameplayView;
    private Texture2D currentTexture;
    private bool isGameStarted;

    public bool IsPreviewVisible => referencePreviewPanel != null && referencePreviewPanel.activeSelf;

    // Cette fonction est appelée par le MenuManager après que le niveau ait été lancé et que la
    // référence ait été définie. Elle reçoit en paramètre la view de jeu (pour pouvoir la cacher
    // quand le preview est ouvert) et s'assure que le système de preview est prêt à être utilisé.
    public void Initialize(GameObject targetGameplayView)
    {
        gameplayView = targetGameplayView;
        EnsureReferencePreviewUi();
        SetButtonVisible(false);
        SetPreviewVisible(false);
    }

    public void SetTexture(Texture2D texture)
    {
        currentTexture = texture;

        if (referencePreviewThumbnail != null)
        {
            referencePreviewThumbnail.texture = texture;
        }

        if (referencePreviewImage != null)
        {
            referencePreviewImage.texture = texture;
            FitPreviewImageToScreen();
        }

        UpdateUiState();
    }

    public void SetGameStarted(bool started)
    {
        isGameStarted = started;
        UpdateUiState();
    }

    public void HidePreview()
    {
        SetPreviewVisible(false);
    }

    private void EnsureReferencePreviewUi()
    {
        if (referencePreviewButton != null)
        {
            referencePreviewButton.onClick.RemoveListener(OpenPreview);
            referencePreviewButton.onClick.AddListener(OpenPreview);
        }

        if (referencePreviewPanel == null)
        {
            CreateReferencePreviewPanel(previewCanvas);
        }
    }

    // Cette fonction crée elle même le panel de preview et l'image à l'intérieur, et ce panel est un button entier,
    // comme ça le joueur peut cliquer n'importe où dessus pour le fermer.
    private void CreateReferencePreviewPanel(Canvas canvas)
    {
        referencePreviewPanel = new GameObject("ReferencePreviewPanel");
        referencePreviewPanel.transform.SetParent(canvas.transform, false);

        Image panelBackground = referencePreviewPanel.AddComponent<Image>();
        panelBackground.color = Color.clear;

        Button closeButton = referencePreviewPanel.AddComponent<Button>();
        closeButton.transition = Selectable.Transition.None;
        closeButton.onClick.AddListener(ClosePreview);

        RectTransform panelRectTransform = referencePreviewPanel.GetComponent<RectTransform>();
        panelRectTransform.anchorMin = Vector2.zero;
        panelRectTransform.anchorMax = Vector2.one;
        panelRectTransform.offsetMin = Vector2.zero;
        panelRectTransform.offsetMax = Vector2.zero;

        GameObject imageObject = new GameObject("ReferencePreviewImage");
        imageObject.transform.SetParent(referencePreviewPanel.transform, false);
        referencePreviewImage = imageObject.AddComponent<RawImage>();
        referencePreviewImage.raycastTarget = false;

        RectTransform imageRectTransform = referencePreviewImage.rectTransform;
        imageRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        imageRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        imageRectTransform.pivot = new Vector2(0.5f, 0.5f);
        imageRectTransform.sizeDelta = new Vector2(800f, 800f);
    }

    private void OpenPreview()
    {
        if (currentTexture == null || !isGameStarted)
        {
            return;
        }

        SetPreviewVisible(true);
    }

    private void ClosePreview()
    {
        SetPreviewVisible(false);
    }

    private void SetPreviewVisible(bool isVisible)
    {
        if (referencePreviewPanel != null)
        {
            referencePreviewPanel.SetActive(isVisible);
        }

        if (gameplayView != null)
        {
            gameplayView.SetActive(!isVisible);
        }

        SetButtonVisible(!isVisible && currentTexture != null && isGameStarted);

        if (isVisible)
        {
            FitPreviewImageToScreen();
        }
    }

    private void SetButtonVisible(bool isVisible)
    {
        if (referencePreviewButton != null)
        {
            referencePreviewButton.gameObject.SetActive(isVisible);
        }
    }

    private void UpdateUiState()
    {
        if (currentTexture == null || !isGameStarted)
        {
            SetPreviewVisible(false);
            return;
        }

        if (!IsPreviewVisible)
        {
            SetButtonVisible(true);
        }
    }

    private void FitPreviewImageToScreen()
    {
        if (referencePreviewImage == null || currentTexture == null)
        {
            return;
        }

        RectTransform parentRectTransform = referencePreviewImage.transform.parent as RectTransform;
        if (parentRectTransform == null)
        {
            return;
        }

        float maxWidth = parentRectTransform.rect.width * 0.9f;
        float maxHeight = parentRectTransform.rect.height * 0.82f;
        float imageAspect = currentTexture.width / (float)Mathf.Max(1, currentTexture.height);

        float width = maxWidth;
        float height = width / imageAspect;

        if (height > maxHeight)
        {
            height = maxHeight;
            width = height * imageAspect;
        }

        referencePreviewImage.rectTransform.sizeDelta = new Vector2(width, height);
    }

}
