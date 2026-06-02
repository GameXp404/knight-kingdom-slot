using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CoinParticleEffect : MonoBehaviour
{
    public RectTransform spawnArea;
    public int coinCountSmall = 25;
    public int coinCountMedium = 60;
    public int coinCountLarge = 140;
    public int coinCountHuge = 220;
    public int maxActiveCoins = 240;   // ceiling koin aktif: jaga performa WebGL biar gak spike pas Grand

    private List<RectTransform> pool = new List<RectTransform>();
    private Sprite coinSprite;

    void Awake() { coinSprite = MakeCoinSprite(); }

    public void Burst(int tier)
    {
        int count = tier switch { 0 => coinCountSmall, 1 => coinCountMedium, 2 => coinCountLarge, _ => coinCountHuge };
        StartCoroutine(BurstRoutine(count));
    }

    private IEnumerator BurstRoutine(int count)
    {
        if (spawnArea == null) yield break;
        float w = spawnArea.rect.width, h = spawnArea.rect.height;
        for (int i = 0; i < count; i++)
        {
            if (CountActive() >= maxActiveCoins) yield break;   // ceiling tercapai — stop spawn burst ini
            var coin = GetCoin();
            coin.gameObject.SetActive(true);
            float startX = Random.Range(-w * 0.5f, w * 0.5f);
            coin.anchoredPosition = new Vector2(startX, h * 0.5f + Random.Range(0f, 100f));
            coin.localScale = Vector3.one * Random.Range(0.7f, 1.3f);
            StartCoroutine(AnimateCoin(coin));
            if (i % 6 == 0) yield return new WaitForSeconds(0.04f);
        }
    }

    private IEnumerator AnimateCoin(RectTransform coin)
    {
        float fallSpeed = Random.Range(450f, 900f);
        float spinSpeed = Random.Range(180f, 720f);
        float horizontalDrift = Random.Range(-80f, 80f);
        float gravity = 1200f;
        float vy = -fallSpeed * 0.3f;
        float rot = Random.Range(0f, 360f);
        float elapsed = 0f, lifetime = 2.8f;
        while (elapsed < lifetime)
        {
            elapsed += Time.deltaTime;
            vy -= gravity * Time.deltaTime;
            coin.anchoredPosition += new Vector2(horizontalDrift * Time.deltaTime, vy * Time.deltaTime);
            rot += spinSpeed * Time.deltaTime;
            coin.localEulerAngles = new Vector3(0, 0, rot);
            yield return null;
        }
        coin.gameObject.SetActive(false);
    }

    private int CountActive()
    {
        int n = 0;
        for (int i = 0; i < pool.Count; i++) if (pool[i].gameObject.activeSelf) n++;
        return n;
    }

    private RectTransform GetCoin()
    {
        foreach (var c in pool) if (!c.gameObject.activeSelf) return c;
        var go = new GameObject("Coin", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(spawnArea, false);
        var img = go.GetComponent<Image>();
        img.sprite = coinSprite; img.color = Color.white;
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(48, 48);
        pool.Add(rt);
        return rt;
    }

    private Sprite MakeCoinSprite()
    {
        int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color[size * size];
        float center = (size - 1) * 0.5f, outerR = size * 0.46f, innerR = size * 0.32f;
        Color goldRim = new Color(0.65f, 0.45f, 0.08f, 1f), goldMid = new Color(0.95f, 0.78f, 0.20f, 1f), goldHi = new Color(1f, 0.97f, 0.55f, 1f);
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) {
            float dx = x - center, dy = y - center, dist = Mathf.Sqrt(dx*dx + dy*dy);
            if (dist > outerR) { pixels[y*size+x] = new Color(0,0,0,0); continue; }
            float edgeFade = Mathf.Clamp01((outerR - dist) / 1.5f);
            Color baseColor;
            if (dist > innerR) { float t = (dist - innerR) / (outerR - innerR); baseColor = Color.Lerp(goldMid, goldRim, t); }
            else { float dxh = (x-center) - size*0.12f, dyh = (y-center) + size*0.12f; float distHi = Mathf.Sqrt(dxh*dxh + dyh*dyh); float hi = Mathf.Clamp01(1f - distHi/(size*0.25f)); baseColor = Color.Lerp(goldMid, goldHi, hi*0.7f); }
            pixels[y*size+x] = new Color(baseColor.r, baseColor.g, baseColor.b, edgeFade);
        }
        tex.SetPixels(pixels); tex.filterMode = FilterMode.Bilinear; tex.wrapMode = TextureWrapMode.Clamp; tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
    }
}
