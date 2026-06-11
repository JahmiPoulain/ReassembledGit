using UnityEngine;
using System;

public class GalleryManager : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;

    // Cette méthode vérifie juste si la plateforme actuelle supporte peut avoir l'option de prendre une photo.
    // Si on est sur mobile (iOS ou Android) et pas dans l'éditeur, alors on retourne true, sinon false.
    public bool SupportsTakingPhoto()
    {
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
        return true;
#else
        return false;
#endif
    }

    private void Start()
    {
        if (!CanUseNativeGalleryOnCurrentPlatform())
        {
            Debug.LogWarning("NativeGallery n'est pas pris en charge sur cette plateforme dans cette configuration.");
            return;
        }
    }

    public void PickImage()
    {
        PickImageFromGallery(null);
    }

    public void PickImageFromGallery(Action<Texture2D> onImagePicked)
    {
        if (!CanUseNativeGalleryOnCurrentPlatform())
        {
            Debug.LogWarning("Aucune image importee disponible. Le niveau ne peut pas demarrer sans image.");
            onImagePicked?.Invoke(null);
            return;
        }

        NativeGallery.GetImageFromGallery(path =>
        {
            HandlePickedImagePath(path, onImagePicked, "Aucune image choisie.");
        }, "Choose an image");
    }

    public void TakePhoto(Action<Texture2D> onImagePicked)
    {
        if (!SupportsTakingPhoto())
        {
            Debug.LogWarning("La camera n'est pas disponible sur cette plateforme.");
            onImagePicked?.Invoke(null);
            return;
        }

        if (!NativeCamera.DeviceHasCamera())
        {
            Debug.LogWarning("Aucune camera detectee sur cet appareil.");
            onImagePicked?.Invoke(null);
            return;
        }

        NativeCamera.TakePicture(path =>
        {
            HandlePickedImagePath(path, onImagePicked, "Aucune photo prise.");
        }, 2048, true, NativeCamera.PreferredCamera.Default);
    }

    private void HandlePickedImagePath(string path, Action<Texture2D> onImagePicked, string emptyPathLog)
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.Log(emptyPathLog);
            onImagePicked?.Invoke(null);
            return;
        }

        Texture2D texture = NativeGallery.LoadImageAtPath(path, 2048);

        if (texture == null)
        {
            Debug.LogError("Impossible de charger l'image.");
            onImagePicked?.Invoke(null);
            return;
        }

        ApplyTextureToGame(texture);
        onImagePicked?.Invoke(texture);
        Debug.Log($"Image chargee : {texture.width}x{texture.height}");
    }

    private void ApplyTextureToGame(Texture2D texture)
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        if (gameManager == null)
        {
            Debug.LogError("GameManager introuvable pour appliquer l'image choisie.");
            return;
        }

        gameManager.SetPuzzleTexture(texture);
    }

    private bool CanUseNativeGalleryOnCurrentPlatform()
    {
#if UNITY_EDITOR || UNITY_IOS || UNITY_ANDROID
        return true;
#else
        return false;
#endif
    }
}
