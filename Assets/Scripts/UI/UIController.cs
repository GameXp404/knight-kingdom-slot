using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIController : MonoBehaviour
{
    public TextMeshProUGUI currencyText;
    public TextMeshProUGUI betText;
    public TextMeshProUGUI winText;
    public TextMeshProUGUI jackpotText;
    public TextMeshProUGUI autoSpinText;
    public TextMeshProUGUI freeSpinText;
    public TextMeshProUGUI historyText;
    public TextMeshProUGUI achievementToastText;
    public CanvasGroup achievementToastGroup;
    public Button spinButton;
    public Button betUpButton;
    public Button betDownButton;
    public Button autoSpinButton;
    public Button stopAutoButton;
    public Button settingsButton;
    public Button bonusButton;
    public Button historyButton;
    public Button achievementButton;
    public GameObject historyPanel;
    public GameObject achievementPanel;
    public GameObject settingsPanel;

    private int displayedCurrency = -1;
    private Coroutine currencyTicker;
    private Coroutine winTextAnim;

    void Start()
    {
        Debug.Log("[Polish] UIController.Start()");
        if (spinButton != null)
        {
            var pulse = spinButton.GetComponent<PulseGlow>();
            if (pulse != null)
            {
                pulse.scaleAmplitude = 0.18f;
                pulse.speed = 2.5f;
                Debug.Log("[Polish] SPIN button PulseGlow boost ke 18%");
            }
            else Debug.LogWarning("[Polish] SPIN button TIDAK punya PulseGlow component!");
        }
    }

    public void UpdateCurrency(int v)
    {
        if (currencyText == null) { displayedCurrency = v; return; }
        if (displayedCurrency < 0) { displayedCurrency = v; currencyText.text = $"$ {v:N0}"; return; }
        if (v <= displayedCurrency)
        {
            if (currencyTicker != null) { StopCoroutine(currencyTicker); currencyTicker = null; }
            displayedCurrency = v;
            currencyText.text = $"$ {v:N0}";
            return;
        }
        if (currencyTicker != null) StopCoroutine(currencyTicker);
        Debug.Log($"[Polish] CURRENCY ANIMATE: {displayedCurrency} -> {v}");
        currencyTicker = StartCoroutine(TickCurrencyTo(v));
    }

    public void UpdateCurrencyAnimated(int v) => UpdateCurrency(v);

    private IEnumerator TickCurrencyTo(int target)
    {
        int start = displayedCurrency;
        int diff = target - start;
        float duration = Mathf.Clamp(Mathf.Abs(diff) / 800f, 1.0f, 2.5f);
        Debug.Log($"[Polish] Ticker duration = {duration}s");

        var rt = currencyText.rectTransform;
        Vector3 baseScale = rt.localScale;
        Color baseColor = currencyText.color;
        Color flashColor = new Color(0.3f, 1f, 0.4f);
        currencyText.enableVertexGradient = false;

        float t = 0f;
        float tickAccum = 0f;
        float tickInterval = duration / 20f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            float ease = 1f - Mathf.Pow(1f - p, 3f);
            displayedCurrency = start + Mathf.RoundToInt(diff * ease);
            currencyText.text = $"$ {displayedCurrency:N0}";

            float scalePulse = 1f + Mathf.Sin(p * Mathf.PI) * 0.35f;
            rt.localScale = baseScale * scalePulse;

            currencyText.color = Color.Lerp(flashColor, Color.white, p);

            tickAccum += Time.deltaTime;
            if (tickAccum >= tickInterval)
            {
                tickAccum = 0f;
                if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
            }
            yield return null;
        }
        displayedCurrency = target;
        currencyText.text = $"$ {target:N0}";
        rt.localScale = baseScale;
        currencyText.enableVertexGradient = true;
        currencyText.color = baseColor;
        currencyTicker = null;
    }

    public void UpdateBet(int v) { if (betText) betText.text = $"BET: {v:N0}"; }

    public void UpdateWin(int v)
    {
        if (winText == null) return;
        if (v > 0)
        {
            winText.text = $"WIN: +{v:N0}";
            Debug.Log($"[Polish] WIN ANIMATE: +{v}");
            if (winTextAnim != null) StopCoroutine(winTextAnim);
            winTextAnim = StartCoroutine(WinTextScaleAnim());
        }
        else
        {
            winText.text = "";
            if (winTextAnim != null) { StopCoroutine(winTextAnim); winTextAnim = null; }
            var rt = winText.rectTransform;
            if (rt != null) rt.localScale = Vector3.one;
        }
    }

    private IEnumerator WinTextScaleAnim()
    {
        var rt = winText.rectTransform;
        if (rt == null) yield break;
        winText.enableVertexGradient = false;
        Color goldColor = new Color(1f, 0.85f, 0.1f);
        Color whiteColor = Color.white;

        float t = 0f, dur1 = 0.4f;
        while (t < dur1)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / dur1);
            float ease = 1f - Mathf.Pow(1f - p, 3f);
            rt.localScale = Vector3.one * Mathf.Lerp(0.2f, 2.2f, ease);
            winText.color = Color.Lerp(whiteColor, goldColor, p);
            yield return null;
        }
        t = 0f;
        float dur2 = 0.3f;
        while (t < dur2)
        {
            t += Time.deltaTime;
            float p = t / dur2;
            float bounce = Mathf.Lerp(2.2f, 1.0f, p);
            rt.localScale = Vector3.one * bounce;
            yield return null;
        }
        rt.localScale = Vector3.one;

        float pulseTime = 0f;
        while (winText != null && !string.IsNullOrEmpty(winText.text))
        {
            pulseTime += Time.deltaTime;
            float pulse = 1f + Mathf.Sin(pulseTime * 4f) * 0.12f;
            rt.localScale = Vector3.one * pulse;
            float colorFlash = (Mathf.Sin(pulseTime * 5f) + 1f) * 0.5f;
            winText.color = Color.Lerp(whiteColor, goldColor, colorFlash);
            yield return null;
        }
        rt.localScale = Vector3.one;
        winText.enableVertexGradient = true;
        winText.color = whiteColor;
        winTextAnim = null;
    }

    public void UpdateJackpot(int v) { if (jackpotText) jackpotText.text = $"JACKPOT: {v:N0}"; }
    public void UpdateAutoSpin(int v) { if (autoSpinText) autoSpinText.text = v > 0 ? $"AUTO: {v}" : ""; }
    public void UpdateFreeSpins(int v) { if (freeSpinText) freeSpinText.text = v > 0 ? $"FREE SPINS: {v}" : ""; }
    // BATCH 2: Emperor's Free Spins — show the climbing multiplier next to the free-spin count.
    public void UpdateFreeSpins(int v, int mult)
    {
        if (freeSpinText == null) return;
        if (v > 0) freeSpinText.text = mult > 1 ? $"FREE SPINS: {v}   <color=#FFD700>×{mult}</color>" : $"FREE SPINS: {v}";
        else freeSpinText.text = "";
    }

    public void ShowAchievementToast(Achievement a)
    {
        if (achievementToastText == null || achievementToastGroup == null) return;
        achievementToastText.text = $"<color=#ffd700>★ {a.title}</color>\n<size=80%>{a.description}</size>";
        StartCoroutine(FadeToast());
    }

    public System.Action<string> ShowAchievementToastRaw => ShowToastRaw;
    private void ShowToastRaw(string formattedText)
    {
        if (achievementToastText == null || achievementToastGroup == null) return;
        achievementToastText.text = formattedText;
        StartCoroutine(FadeToast());
    }

    private IEnumerator FadeToast()
    {
        achievementToastGroup.alpha = 0f;
        achievementToastGroup.gameObject.SetActive(true);
        float t = 0f;
        while (t < 0.4f) { t += Time.deltaTime; achievementToastGroup.alpha = t / 0.4f; yield return null; }
        achievementToastGroup.alpha = 1f;
        yield return new WaitForSeconds(2.5f);
        t = 0f;
        while (t < 0.4f) { t += Time.deltaTime; achievementToastGroup.alpha = 1f - t / 0.4f; yield return null; }
        achievementToastGroup.alpha = 0f;
        achievementToastGroup.gameObject.SetActive(false);
    }

    public void RefreshHistoryPanel()
    {
        if (historyText == null) return;
        var list = HistoryTracker.Load();
        if (list.records.Count == 0) { historyText.text = "Belum ada riwayat."; return; }
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < list.records.Count; i++)
        {
            var r = list.records[i];
            string winStr = r.win > 0 ? $"<color=#00ff66>+{r.win:N0}</color>" : "<color=#ff5555>kalah</color>";
            sb.AppendLine($"{r.timestamp}  bet {r.bet:N0}  →  {winStr}");
        }
        historyText.text = sb.ToString();
    }
}
