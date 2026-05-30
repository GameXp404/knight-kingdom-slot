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

    public bool IsBusy { get; private set; }
    public SymbolType[] visibleSymbols { get; private set; } = new SymbolType[4];

    private float currentOffset;
    private bool spinning;
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
        currentOffset -= spinSymbolsPerSec * Time.deltaTime;
        Render();
    }

    private IEnumerator DecelerateRoutine(int targetTopIndex)
    {
        float startOffset = currentOffset;
        float current = ((currentOffset % strip.Length) + strip.Length) % strip.Length;
        float distance = ((current - targetTopIndex) + strip.Length) % strip.Length;
        if (distance < 1f) distance += strip.Length;
        float endOffset = startOffset - distance - strip.Length * 2f;

        float elapsed = 0f;
        while (elapsed < decelDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / decelDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            currentOffset = Mathf.Lerp(startOffset, endOffset, eased);
            Render();
            yield return null;
        }

        currentOffset = Mathf.Round(endOffset);
        Render();

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
