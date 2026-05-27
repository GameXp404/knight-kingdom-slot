using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WinPopupController : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public RectTransform popupBox;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI amountText;

    private static readonly string[] Titles = { "NICE WIN!", "BIG WIN!", "MEGA WIN!", "JACKPOT!" };
    private static readonly Color[] TitleColors = {
        new Color(1f, 0.95f, 0.4f),
        new Color(1f, 0.6f, 0.1f),
        new Color(1f, 0.2f, 0.7f),
        new Color(0.4f, 1f, 1f)
    };

    public IEnumerator ShowFreeSpins(int spinsAwarded)
    {
        if (canvasGroup == null) yield break;
        titleText.text = "FREE SPINS!";
        titleText.color = new Color(1f, 0.85f, 0.2f);
        amountText.text = $"{spinsAwarded} PUTARAN GRATIS\nx3 MULTIPLIER";

        canvasGroup.gameObject.SetActive(true);
        canvasGroup.alpha = 0f;
        popupBox.localScale = Vector3.zero;

        float t1 = 0f, dur1 = 0.4f;
        while (t1 < dur1) { t1 += Time.deltaTime; float p = t1 / dur1; float ease = 1f - Mathf.Pow(1f - p, 3f); canvasGroup.alpha = ease; popupBox.localScale = Vector3.one * Mathf.Lerp(0f, 1.2f, ease); yield return null; }
        t1 = 0f;
        while (t1 < 0.2f) { t1 += Time.deltaTime; popupBox.localScale = Vector3.one * Mathf.Lerp(1.2f, 1f, t1 / 0.2f); yield return null; }
        popupBox.localScale = Vector3.one;

        yield return new WaitForSeconds(2.8f);

        t1 = 0f;
        while (t1 < 0.3f) { t1 += Time.deltaTime; canvasGroup.alpha = 1f - t1 / 0.3f; yield return null; }
        canvasGroup.alpha = 0f;
        canvasGroup.gameObject.SetActive(false);
    }

    public IEnumerator Show(int tier, int amount)
    {
        if (canvasGroup == null) yield break;
        tier = Mathf.Clamp(tier, 0, 3);
        titleText.text = Titles[tier];
        titleText.color = TitleColors[tier];
        amountText.text = $"+{amount:N0}";

        canvasGroup.gameObject.SetActive(true);
        canvasGroup.alpha = 0f;
        popupBox.localScale = Vector3.zero;

        float t = 0f, dur = 0.35f;
        while (t < dur) { t += Time.deltaTime; float p = t / dur; float ease = 1f - Mathf.Pow(1f - p, 3f); canvasGroup.alpha = ease; popupBox.localScale = Vector3.one * Mathf.Lerp(0f, 1.15f, ease); yield return null; }
        t = 0f;
        while (t < 0.15f) { t += Time.deltaTime; popupBox.localScale = Vector3.one * Mathf.Lerp(1.15f, 1f, t / 0.15f); yield return null; }
        popupBox.localScale = Vector3.one;

        float hold = tier switch { 0 => 1.5f, 1 => 2.0f, 2 => 2.5f, _ => 3.5f };
        yield return new WaitForSeconds(hold);

        t = 0f;
        while (t < 0.3f) { t += Time.deltaTime; canvasGroup.alpha = 1f - t / 0.3f; yield return null; }
        canvasGroup.alpha = 0f;
        canvasGroup.gameObject.SetActive(false);
    }
}
