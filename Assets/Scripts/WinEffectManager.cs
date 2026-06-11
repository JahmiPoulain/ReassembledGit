using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WinEffectManager : MonoBehaviour
{
    public static WinEffectManager Instance { get; private set; }

    private Canvas mainCanvas;
    private Image flashImage;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // Called by MenuManager after creation, passing the scene canvas.
    public void Init(Canvas canvas)
    {
        mainCanvas = canvas;
        CreateFlashOverlay();
    }

    private void CreateFlashOverlay()
    {
        if (mainCanvas == null) return;

        GameObject go = new GameObject("ScreenFlash");
        go.transform.SetParent(mainCanvas.transform, false);

        flashImage = go.AddComponent<Image>();
        flashImage.raycastTarget = false;
        flashImage.color = Color.clear;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.transform.SetAsLastSibling();
    }

    public void PlayWin(Vector3 worldPos)
    {
        StartCoroutine(FlashRoutine(new Color(1f, 1f, 1f, 0.55f)));
        SpawnConfetti(worldPos);
    }

    public void PlayLose()
    {
        StartCoroutine(FlashRoutine(new Color(0f, 0f, 0f, 0.45f)));
    }

    private IEnumerator FlashRoutine(Color peak)
    {
        if (flashImage == null) yield break;

        float t = 0f;
        float duration = 0.35f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Sin(Mathf.Clamp01(t / duration) * Mathf.PI);
            flashImage.color = new Color(peak.r, peak.g, peak.b, peak.a * alpha);
            yield return null;
        }
        flashImage.color = Color.clear;
    }

    private void SpawnConfetti(Vector3 origin)
    {
        GameObject go = new GameObject("WinConfetti");
        go.transform.position = origin + new Vector3(0f, 0.5f, -2f);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();
        psr.material = new Material(Shader.Find("Sprites/Default"));
        psr.sortingOrder = 100;

        var main = ps.main;
        main.duration = 0.5f;
        main.loop = false;
        main.startLifetime  = new ParticleSystem.MinMaxCurve(1.2f, 2.8f);
        main.startSpeed     = new ParticleSystem.MinMaxCurve(1.8f, 5.5f);
        main.startSize      = new ParticleSystem.MinMaxCurve(0.05f, 0.18f);
        main.startRotation  = new ParticleSystem.MinMaxCurve(0f, 2f * Mathf.PI);
        main.gravityModifier = 0.35f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.85f, 0f),   0f),
                new GradientColorKey(new Color(0.2f, 0.8f, 1f),  0.4f),
                new GradientColorKey(new Color(1f, 0.3f, 0.75f), 0.8f),
                new GradientColorKey(Color.white,                 1f),
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 0.65f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLife.color = new ParticleSystem.MinMaxGradient(g);

        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 130) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 55f;
        shape.radius = 0.15f;

        ps.Play();
        Destroy(go, 5f);
    }
}
