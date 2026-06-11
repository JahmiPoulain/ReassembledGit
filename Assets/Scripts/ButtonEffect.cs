using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

// IPointerDownHandler : pour détecter quand le bouton est pressé
// IPointerUpHandler : pour détecter quand le bouton est relâché
// IPointerExitHandler : pour détecter quand le curseur sort du bouton
public class ButtonEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("Bounce Settings")]
    [SerializeField] private float pressedScale = 0.92f; // Quand on est en train d'appuyer, le bouton est à 92% de sa taille normale
    [SerializeField] private float pressDuration = 0.06f; // La durée pour atteindre la taille pressée
    [SerializeField] private float releaseDuration = 0.12f; // La durée pour atteindre la taille de relâchement
    [SerializeField] private float overshootScale = 1.04f; // La taille maximale atteinte lors du relâchement

    private Coroutine bounceCoroutine;
    private Vector3 initialScale;
    private bool isPressed;

    private void Awake()
    {
        initialScale = transform.localScale;
    }

    private void OnEnable()
    {
        transform.localScale = initialScale;
        isPressed = false;
    }

    // Appelé quand le bouton est désactivé. On s'assure de stopper toute animation en cours et de remettre l'échelle à la normale.
    private void OnDisable()
    {
        if (bounceCoroutine != null)
        {
            StopCoroutine(bounceCoroutine);
            bounceCoroutine = null;
        }

        transform.localScale = initialScale;
        isPressed = false;
    }

    // Appelé quand le bouton est en train d'être pressé.
    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        StartBounce(initialScale * pressedScale, pressDuration);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isPressed)
        {
            return;
        }

        isPressed = false;
        StartReleaseBounce();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isPressed)
        {
            return;
        }

        isPressed = false;
        StartReleaseBounce();
    }

    private void StartReleaseBounce()
    {
        if (bounceCoroutine != null)
        {
            StopCoroutine(bounceCoroutine);
        }

        bounceCoroutine = StartCoroutine(ReleaseBounceRoutine());
    }

    private void StartBounce(Vector3 targetScale, float duration)
    {
        if (bounceCoroutine != null)
        {
            StopCoroutine(bounceCoroutine);
        }

        bounceCoroutine = StartCoroutine(ScaleRoutine(transform.localScale, targetScale, duration));
    }

    private IEnumerator ReleaseBounceRoutine()
    {
        Vector3 overshootTarget = initialScale * overshootScale;
        yield return ScaleRoutine(transform.localScale, overshootTarget, releaseDuration * 0.5f);
        yield return ScaleRoutine(transform.localScale, initialScale, releaseDuration * 0.5f);
        bounceCoroutine = null;
    }

    private IEnumerator ScaleRoutine(Vector3 from, Vector3 to, float duration)
    {
        if (duration <= 0f)
        {
            transform.localScale = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // On utilise unscaledDeltaTime pour que l'animation ne soit pas affectée par le temps de jeu
            float t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t);
            transform.localScale = Vector3.LerpUnclamped(from, to, t);
            yield return null;
        }

        transform.localScale = to;
    }
}
