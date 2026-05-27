using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PromoPopupController : MonoBehaviour
{
    public RectTransform container;
    public CanvasGroup canvasGroup;
    public Button closeButton;
    public Button bgButton;
    public float firstDelay = 15f;
    public float intervalSeconds = 300f;
    public Vector2 hiddenPos = new Vector2(-500, 60);
    public Vector2 shownPos = new Vector2(20, 60);

    private bool isShowing;

    void Start()
    {
        if (container != null) container.anchoredPosition = hiddenPos;
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
        if (bgButton != null) bgButton.onClick.AddListener(Hide);
        StartCoroutine(PromoLoop());
    }

    IEnumerator PromoLoop()
    {
        yield return new WaitForSeconds(firstDelay);
        while (true)
        {
            yield return ShowAnim();
            yield return new WaitForSeconds(intervalSeconds);
        }
    }

    IEnumerator ShowAnim()
    {
        if (isShowing) yield break;
        isShowing = true;
        canvasGroup.alpha = 1f;
        float t = 0f, dur = 0.55f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = t / dur;
            float ease = 1f - Mathf.Pow(1f - p, 3f);
            container.anchoredPosition = Vector2.Lerp(hiddenPos, shownPos, ease);
            yield return null;
        }
        container.anchoredPosition = shownPos;
    }

    public void Hide()
    {
        if (!isShowing) return;
        StartCoroutine(HideAnim());
    }

    IEnumerator HideAnim()
    {
        float t = 0f, dur = 0.35f;
        Vector2 start = container.anchoredPosition;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = t / dur;
            container.anchoredPosition = Vector2.Lerp(start, hiddenPos, p);
            yield return null;
        }
        container.anchoredPosition = hiddenPos;
        canvasGroup.alpha = 0f;
        isShowing = false;
    }
}
