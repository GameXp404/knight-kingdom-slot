using UnityEngine;
using UnityEngine.UI;

public class PulseGlow : MonoBehaviour
{
    public Color baseColor = Color.white;
    public Color highlightColor = Color.white;
    public float speed = 2f;
    public float scaleAmplitude = 0.04f;

    private Image img;
    private RectTransform rt;
    private Vector3 baseScale;

    void Start() { img = GetComponent<Image>(); rt = GetComponent<RectTransform>(); if (img) baseColor = img.color; if (rt) baseScale = rt.localScale; }
    void Update() { float t = (Mathf.Sin(Time.time * speed) + 1f) * 0.5f; if (img) img.color = Color.Lerp(baseColor, highlightColor, t * 0.6f); if (rt) rt.localScale = baseScale * (1f + scaleAmplitude * t); }
}

public class ScreenShake : MonoBehaviour
{
    public static ScreenShake Instance { get; private set; }
    private RectTransform rt; private Vector2 originalPos; private Coroutine current;
    void Awake() { if (Instance != null && Instance != this) { Destroy(gameObject); return; } Instance = this; rt = GetComponent<RectTransform>(); if (rt) originalPos = rt.anchoredPosition; }
    public void Shake(float duration = 0.5f, float magnitude = 15f) { if (rt == null) return; if (current != null) StopCoroutine(current); current = StartCoroutine(ShakeRoutine(duration, magnitude)); }
    private System.Collections.IEnumerator ShakeRoutine(float duration, float magnitude) { float elapsed = 0f; while (elapsed < duration) { float decay = 1f - (elapsed / duration); rt.anchoredPosition = originalPos + new Vector2(Random.Range(-1f,1f) * magnitude * decay, Random.Range(-1f,1f) * magnitude * decay); elapsed += Time.deltaTime; yield return null; } rt.anchoredPosition = originalPos; current = null; }
}

public class ScreenFlash : MonoBehaviour
{
    public static ScreenFlash Instance { get; private set; }
    private Image flashImage;
    void Awake() { if (Instance != null && Instance != this) { Destroy(gameObject); return; } Instance = this; flashImage = GetComponent<Image>(); if (flashImage) { flashImage.color = new Color(1f,1f,1f,0f); flashImage.raycastTarget = false; } }
    public void Flash(Color color, float duration = 0.4f, float maxAlpha = 0.85f) { StartCoroutine(FlashRoutine(color, duration, maxAlpha)); }
    private System.Collections.IEnumerator FlashRoutine(Color color, float duration, float maxAlpha) { if (flashImage == null) yield break; float t = 0f, fadeIn = 0.08f; while (t < fadeIn) { t += Time.deltaTime; flashImage.color = new Color(color.r, color.g, color.b, Mathf.Lerp(0f, maxAlpha, t/fadeIn)); yield return null; } t = 0f; float fadeOut = duration - fadeIn; while (t < fadeOut) { t += Time.deltaTime; flashImage.color = new Color(color.r, color.g, color.b, Mathf.Lerp(maxAlpha, 0f, t/fadeOut)); yield return null; } flashImage.color = new Color(color.r, color.g, color.b, 0f); }
}
