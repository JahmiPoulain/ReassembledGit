using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource; // AudioSource 1 : celui pour la musique
    [SerializeField] private AudioSource sfxSource;   // AudioSource 2 : celui pour les SFX

    [Header("Music")]
    [SerializeField] private AudioClip backgroundMusic; // La musique du jeu qui tourne en boucle

    [Header("SFX")]
    [SerializeField] private AudioClip buttonClickSfx; // Le son joué quand on clique sur un bouton
    [SerializeField] private AudioClip swapPieceSound; // Le son joué quand on clique sur un bouton
    [SerializeField] private AudioClip victorySfx;     // Le son joué quand le joueur gagne un niveau
    [SerializeField] private AudioClip defeatSfx;      // Le son joué quand le joueur perd un niveau

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // On joue la musique dès le début du jeu
        PlayMusic(backgroundMusic);
    }

    // Quand on clique sur un bouton, cette fonction est appelée pour jouer le son de clic
    public void PlayButtonClick()
    {
        sfxSource.PlayOneShot(buttonClickSfx);
    }

    // Quand on clique sur une pièce, cette fonction est appelée pour jouer le son de déplacement
    public void PlaySwapPiece()
    {
        sfxSource.PlayOneShot(swapPieceSound);
    }

    // Quand le joueur gagne un niveau, cette fonction est appelée pour jouer le son de victoire
    public void PlayVictory()
    {
        sfxSource.PlayOneShot(victorySfx);
    }

    // Quand le joueur perd un niveau, cette fonction est appelée pour jouer le son de défaite
    public void PlayDefeat()
    {
        sfxSource.PlayOneShot(defeatSfx);
    }

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (musicSource.clip == clip && musicSource.isPlaying)
        {
            return;
        }

        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();
    }
}
