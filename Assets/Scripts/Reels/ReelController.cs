using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ReelController : MonoBehaviour
{
    public SymbolType[] strip;
    public RectTransform stripContainer;
    public Image[] symbolImages;
    public TMPro.TextMeshProUGUI[] symbolLabels;

    public float symbolHeight = 160f;
    public float spinSymbolsPerSec = 14f;
    public float decelDuration = 1.5f;

    // Mechanical-reel feel (kosmetik only — titik berhenti tetap outcome-controlled)
    public float accelDuration = 0.11f;     // ramp-up pendek biar reel-1 & turbo sempet ngebut sebelum decel
    public float overshootAmount = 0.2f;    // lewat target sekian fraksi simbol sebelum mental balik
    public float settleDuration = 0.13f;    // durasi mental-balik ke posisi seat pas
    public float blurStretch = 0.3f;        // trail vertikal ke bawah (searah scroll) — jangan kegedean biar gak numpuk
    public float blurFade = 0.28f;          // turunin alpha pas muter kenceng — jangan kebanyakan biar gak washed-out

    public bool IsBusy { get; private set; }
    public SymbolType[] visibleSymbols { get; private set; } = new SymbolType[4];

    private float currentOffset;
    private bool spinning;
    private float spinElapsed;
    private Coroutine stopRoutine;
    private System.Action onStopped;

    public void Initialize(SymbolType[] reelStrip, RectTransform container, Image[] images, TMPro.TextMeshProUGUI[] labels, float symHeight)
    {
        strip = reelStrip;
        stripContainer = container;
        symbolImages = images;
        symbolLabels = labels;
        symbolHeight = symHeight;
        currentOffset = 0;
        visibleSymbols = new SymbolType[PaylineSystem.Rows];
        Render();
    }

    public void StartSpin()
    {
        if (IsBusy) return;
        IsBusy = true;
        spinning = true;
        spinElapsed = 0f;
        // JUICE: clear any leftover land-pop / win-pulse scale from the previous round
        for (int i = 0; i < symbolImages.Length; i++)
            if (symbolImages[i] != null) symbolImages[i].rectTransform.localScale = Vector3.one;
    }

    public void StopAt(int targetTopIndex, System.Action callback)
    {
        if (!spinning && stopRoutine != null) return;
        spinning = false;
        onStopped = callback;
        if (stopRoutine != null) StopCoroutine(stopRoutine);
        stopRoutine = StartCoroutine(DecelerateRoutine(targetTopIndex));
    }

    void Update()
    {
        if (!spinning) return;
        spinElapsed += Time.deltaTime;
        float ramp = accelDuration > 0f ? Mathf.Clamp01(spinElapsed / accelDuration) : 1f;
        float eased = ramp * ramp;                                  // ease-in: pelan dulu baru ngebut
        float speed = spinSymbolsPerSec * (0.15f + 0.85f * eased);  // mulai 15% speed -> 100%
        currentOffset -= speed * Time.deltaTime;
        Render();
        ApplyMotionBlur(speed);
    }

    private IEnumerator DecelerateRoutine(int targetTopIndex)
    {
        float startOffset = currentOffset;
        float current = ((currentOffset % strip.Length) + strip.Length) % strip.Length;
        float distance = ((current - targetTopIndex) + strip.Length) % strip.Length;
        if (distance < 1f) distance += strip.Length;
        float endOffset = startOffset - distance - strip.Length * 2f;
        float finalOffset = Mathf.Round(endOffset);          // posisi seat pas (integer)
        float pastOffset = finalOffset - overshootAmount;    // lewat dikit (mental ke bawah)

        // FASE 1: ngerem ke titik overshoot (ease-out cubic) + motion blur ngikut kecepatan
        float prev = currentOffset;
        float elapsed = 0f;
        while (elapsed < decelDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / decelDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            currentOffset = Mathf.Lerp(startOffset, pastOffset, eased);
            float spd = Mathf.Abs(currentOffset - prev) / dt;
            prev = currentOffset;
            Render();
            ApplyMotionBlur(spd);
            yield return null;
        }

        // FASE 2: mental balik dari overshoot ke posisi seat pas (ease-out, snappy)
        float settleStart = currentOffset;
        float settleDur = Mathf.Max(0.01f, settleDuration);
        elapsed = 0f;
        while (elapsed < settleDur)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / settleDur);
            float eased = 1f - Mathf.Pow(1f - t, 2f);
            currentOffset = Mathf.Lerp(settleStart, finalOffset, eased);
            Render();
            ClearMotionBlur();                               // udah pelan: gak ada blur
            yield return null;
        }

        currentOffset = finalOffset;
        Render();
        ClearMotionBlur();

        int top = ((int)currentOffset) % strip.Length;
        if (top < 0) top += strip.Length;
        for (int i = 0; i < visibleSymbols.Length; i++)
        {
            visibleSymbols[i] = strip[(top + i) % strip.Length];
        }

        IsBusy = false;
        stopRoutine = null;
        StartCoroutine(LandBounce());
        onStopped?.Invoke();
    }

    // JUICE: quick scale "pop" when a reel lands — tactile landing feedback.
    private IEnumerator LandBounce()
    {
        float dur = 0.16f;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / dur);
            float s = 1f + Mathf.Sin(p * Mathf.PI) * 0.07f; // 1 → 1.07 → 1
            for (int i = 0; i < symbolImages.Length; i++)
                if (symbolImages[i] != null) symbolImages[i].rectTransform.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        for (int i = 0; i < symbolImages.Length; i++)
            if (symbolImages[i] != null) symbolImages[i].rectTransform.localScale = Vector3.one;
    }

    // Fake motion blur murah: pas muter kenceng, simbol ditarik vertikal + alpha turun.
    // Gak butuh aset/shader baru, aman WebGL. Di-clear pas reel mau berhenti.
    private void ApplyMotionBlur(float speedSymbolsPerSec)
    {
        float norm = spinSymbolsPerSec > 0f ? Mathf.Clamp01(speedSymbolsPerSec / spinSymbolsPerSec) : 0f;
        float stretch = 1f + norm * blurStretch;
        float alpha = 1f - norm * blurFade;
        for (int i = 0; i < symbolImages.Length; i++)
        {
            if (symbolImages[i] == null) continue;
            symbolImages[i].rectTransform.localScale = new Vector3(1f, stretch, 1f);
            var c = symbolImages[i].color; c.a = alpha; symbolImages[i].color = c;
        }
    }

    private void ClearMotionBlur()
    {
        for (int i = 0; i < symbolImages.Length; i++)
        {
            if (symbolImages[i] == null) continue;
            symbolImages[i].rectTransform.localScale = Vector3.one;
            var c = symbolImages[i].color; c.a = 1f; symbolImages[i].color = c;
        }
    }

    private void Render()
    {
        int displayCount = symbolImages.Length;
        int floorOffset = Mathf.FloorToInt(currentOffset);
        float frac = currentOffset - floorOffset;

        for (int i = 0; i < displayCount; i++)
        {
            int stripIdx = (floorOffset + i) % strip.Length;
            if (stripIdx < 0) stripIdx += strip.Length;
            var sym = strip[stripIdx];
            var sprite = SymbolDatabase.GetSprite(sym);
            if (sprite != null)
            {
                symbolImages[i].sprite = sprite;
                symbolImages[i].color = Color.white;
                symbolLabels[i].text = "";
            }
            else
            {
                symbolImages[i].sprite = null;
                symbolImages[i].color = SymbolDatabase.GetColor(sym);
                symbolLabels[i].text = SymbolDatabase.GetLabel(sym);
            }
            float y = -(i - frac) * symbolHeight;
            (symbolImages[i].rectTransform).anchoredPosition = new Vector2(0, y);
        }
    }
}
