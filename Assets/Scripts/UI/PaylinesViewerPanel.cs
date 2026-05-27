using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PaylinesViewerPanel : MonoBehaviour
{
    public Image[] cellHighlights;
    public UILineRenderer line;
    public TextMeshProUGUI titleText;
    public Button prevButton;
    public Button nextButton;
    public Button closeButton;

    public Vector2 gridOrigin = new Vector2(-276f, 162f);
    public float cellSpacingX = 138f;
    public float cellSpacingY = 108f;

    private int currentIndex;

    private static readonly Color BaseCellColor = new Color(0.16f, 0.16f, 0.22f, 1f);
    private static readonly Color HighlightCellColor = new Color(1f, 0.78f, 0.20f, 0.55f);

    private static readonly Color[] LinePalette =
    {
        new Color(1f, 0.85f, 0.20f), new Color(1f, 0.30f, 0.30f),
        new Color(0.30f, 0.85f, 1f), new Color(0.30f, 1f, 0.55f),
        new Color(1f, 0.55f, 0.20f), new Color(0.80f, 0.40f, 1f),
        new Color(1f, 0.40f, 0.85f), new Color(0.55f, 1f, 0.30f),
        new Color(0.40f, 0.70f, 1f), new Color(1f, 1f, 0.50f)
    };

    void OnEnable()
    {
        if (prevButton) prevButton.onClick.AddListener(Prev);
        if (nextButton) nextButton.onClick.AddListener(Next);
        if (closeButton) closeButton.onClick.AddListener(OnClose);
        Show(currentIndex);
    }

    void OnDisable()
    {
        if (prevButton) prevButton.onClick.RemoveListener(Prev);
        if (nextButton) nextButton.onClick.RemoveListener(Next);
        if (closeButton) closeButton.onClick.RemoveListener(OnClose);
    }

    private void OnClose() { gameObject.SetActive(false); }
    private void Prev() { Show((currentIndex - 1 + PaylineSystem.Count) % PaylineSystem.Count); }
    private void Next() { Show((currentIndex + 1) % PaylineSystem.Count); }

    public void Show(int idx)
    {
        if (idx < 0 || idx >= PaylineSystem.Count) idx = 0;
        currentIndex = idx;
        var ln = PaylineSystem.Lines[idx];

        if (titleText) titleText.text = $"PAYLINE {idx + 1} / {PaylineSystem.Count}";

        if (cellHighlights != null)
            for (int i = 0; i < cellHighlights.Length; i++)
                if (cellHighlights[i]) cellHighlights[i].color = BaseCellColor;

        var pts = new List<Vector2>(PaylineSystem.Reels);
        for (int r = 0; r < PaylineSystem.Reels; r++)
        {
            int row = ln[r];
            int hi = r * PaylineSystem.Rows + row;
            if (cellHighlights != null && hi >= 0 && hi < cellHighlights.Length && cellHighlights[hi])
                cellHighlights[hi].color = HighlightCellColor;
            pts.Add(new Vector2(gridOrigin.x + r * cellSpacingX, gridOrigin.y - row * cellSpacingY));
        }

        if (line)
        {
            line.color = LinePalette[idx % LinePalette.Length];
            line.SetPoints(pts);
        }
    }
}
