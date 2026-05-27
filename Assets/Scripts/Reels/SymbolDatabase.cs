using System.Collections.Generic;
using UnityEngine;

public enum SymbolType
{
    Ten = 0,
    Jack = 1,
    Queen = 2,
    King = 3,
    Ace = 4,
    Potion = 5,
    Skull = 6,
    Treasure = 7,
    Cavalry = 8,
    Knight = 9,
    Dragon = 10,
    Crown = 11
}

public enum Difficulty
{
    Easy = 0,
    Medium = 1,
    Hard = 2
}

public static class SymbolDatabase
{
    public const int Count = 12;
    public const int BaseBetUnit = 10;

    public static readonly SymbolType WildSymbol = SymbolType.Dragon;
    public static readonly SymbolType ScatterSymbol = SymbolType.Crown;

    private static readonly int[] Payout3 = { 0, 0, 0, 0, 0, 1, 1, 2, 3,  5,  8,  3 };
    private static readonly int[] Payout4 = { 1, 1, 1, 1, 2, 3, 4, 6, 12, 25, 40, 12 };
    private static readonly int[] Payout5 = { 2, 2, 3, 4, 6, 10,15,30, 60, 120,180,75 };

    private static readonly Color[] Colors = {
        new Color(0.65f, 0.65f, 0.70f),
        new Color(0.40f, 0.55f, 0.95f),
        new Color(0.95f, 0.45f, 0.85f),
        new Color(0.55f, 0.30f, 0.10f),
        new Color(1.00f, 0.78f, 0.20f),
        new Color(0.60f, 0.20f, 0.80f),
        new Color(0.85f, 0.85f, 0.85f),
        new Color(0.95f, 0.70f, 0.10f),
        new Color(0.40f, 0.30f, 0.15f),
        new Color(0.85f, 0.10f, 0.10f),
        new Color(1.00f, 0.40f, 0.10f),
        new Color(1.00f, 0.85f, 0.30f)
    };

    private static readonly string[] Labels = {
        "10", "J", "Q", "K", "A", "PTN", "SKL", "TRS", "CVL", "KNT", "DRG", "CRN"
    };

    private static readonly string[] DisplayNames = {
        "Ten", "Jack", "Queen", "King", "Ace",
        "Potion", "Skull", "Treasure", "Cavalry",
        "Knight", "Dragon (Wild)", "Crown (Scatter)"
    };

    private static readonly string[] SpriteFileNames = {
        "10", "J", "Q", "K", "A",
        "potion", "skull", "treasure", "cavalry",
        "knight", "dragon", "crown"
    };

    private static Dictionary<SymbolType, Sprite> spriteCache;
    private static bool spriteCacheInitialized;

    // FIX #5: Cache strip biar tidak regenerate tiap call
    private static readonly Dictionary<(int, Difficulty), SymbolType[]> stripCache = new();

    public static int GetPayout(SymbolType s, int matchCount)
    {
        int idx = (int)s;
        return matchCount switch
        {
            3 => Payout3[idx],
            4 => Payout4[idx],
            >= 5 => Payout5[idx],
            _ => 0
        };
    }

    public static Color GetColor(SymbolType s) => Colors[(int)s];
    public static string GetLabel(SymbolType s) => Labels[(int)s];
    public static string GetDisplayName(SymbolType s) => DisplayNames[(int)s];

    public static Sprite GetSprite(SymbolType s)
    {
        if (!spriteCacheInitialized) InitSpriteCache();
        return spriteCache.TryGetValue(s, out var sprite) ? sprite : null;
    }

    private static void InitSpriteCache()
    {
        spriteCache = new Dictionary<SymbolType, Sprite>();
        for (int i = 0; i < Count; i++)
        {
            var loaded = Resources.Load<Sprite>("Symbols/" + SpriteFileNames[i]);
            if (loaded != null) spriteCache[(SymbolType)i] = loaded;
        }
        spriteCacheInitialized = true;
    }

    public static void ReloadSprites()
    {
        spriteCacheInitialized = false;
        InitSpriteCache();
    }

    public static SymbolType[] GetReelStrip(int reelIndex)
    {
        return GetReelStrip(reelIndex, (Difficulty)SaveSystem.DifficultyLevel);
    }

    public static SymbolType[] GetReelStrip(int reelIndex, Difficulty difficulty)
    {
        // FIX #5: Cache check
        var key = (reelIndex, difficulty);
        if (stripCache.TryGetValue(key, out var cached)) return cached;

        var rng = new System.Random(54321 + reelIndex * 31 + (int)difficulty * 1009);
        int len = 24;
        var strip = new SymbolType[len];
        int[] weights = GetWeights(difficulty);
        int total = 0;
        foreach (var w in weights) total += w;
        for (int i = 0; i < len; i++)
        {
            int roll = rng.Next(total);
            int acc = 0;
            for (int s = 0; s < weights.Length; s++)
            {
                acc += weights[s];
                if (roll < acc) { strip[i] = (SymbolType)s; break; }
            }
        }

        // FIX #2: Spacing >= Rows+1 untuk jamin tidak ada 2 scatter/wild dalam window
        EnforceMinSpacing(strip, WildSymbol, PaylineSystem.Rows + 1, rng, weights);
        EnforceMinSpacing(strip, ScatterSymbol, PaylineSystem.Rows + 1, rng, weights);

        // FIX: pastikan tiap strip punya MIN 1 Wild + 1 Scatter (fix Hard 0-trigger issue)
        EnsurePresence(strip, WildSymbol, rng);
        EnsurePresence(strip, ScatterSymbol, rng);

        // Debug log strip pattern
        var sb = new System.Text.StringBuilder();
        sb.Append($"[Strip R{reelIndex} D{difficulty}] ");
        for (int i = 0; i < strip.Length; i++)
        {
            if (strip[i] == WildSymbol) sb.Append("W");
            else if (strip[i] == ScatterSymbol) sb.Append("S");
            else sb.Append(".");
        }
        var scatterPos = new List<int>();
        var wildPos = new List<int>();
        for (int i = 0; i < strip.Length; i++)
        {
            if (strip[i] == ScatterSymbol) scatterPos.Add(i);
            if (strip[i] == WildSymbol) wildPos.Add(i);
        }
        sb.Append($"  S@[{string.Join(",", scatterPos)}]  W@[{string.Join(",", wildPos)}]");
        VerifySpacing(strip, ScatterSymbol, PaylineSystem.Rows, "SCATTER", reelIndex, sb);
        VerifySpacing(strip, WildSymbol, PaylineSystem.Rows, "WILD", reelIndex, sb);
        UnityEngine.Debug.Log(sb.ToString());

        // FIX #5: Cache hasil sebelum return
        stripCache[key] = strip;
        return strip;
    }

    // FIX: jamin strip punya minimal 1 target symbol
    private static void EnsurePresence(SymbolType[] strip, SymbolType target, System.Random rng)
    {
        for (int i = 0; i < strip.Length; i++)
            if (strip[i] == target) return; // udah ada minimal 1
        int pos = rng.Next(strip.Length);
        strip[pos] = target;
    }

    private static void VerifySpacing(SymbolType[] strip, SymbolType target, int minSpacing, string name, int reelIndex, System.Text.StringBuilder sb)
    {
        for (int i = 0; i < strip.Length; i++)
        {
            if (strip[i] != target) continue;
            for (int d = 1; d < minSpacing; d++)
            {
                int j = (i + d) % strip.Length;
                if (strip[j] == target)
                {
                    sb.Append($"  ⚠ {name} CLUSTER R{reelIndex}: pos {i} & {j}");
                    return;
                }
            }
        }
    }

    private static void EnforceMinSpacing(SymbolType[] strip, SymbolType target, int minSpacing, System.Random rng, int[] weights)
    {
        int len = strip.Length;
        for (int pass = 0; pass < 8; pass++)
        {
            bool changed = false;
            for (int i = 0; i < len; i++)
            {
                if (strip[i] != target) continue;
                for (int d = 1; d < minSpacing; d++)
                {
                    int j = (i + d) % len;
                    if (strip[j] == target)
                    {
                        strip[j] = PickReplacement(rng, weights);
                        changed = true;
                    }
                }
            }
            if (!changed) break;
        }
    }

    private static SymbolType PickReplacement(System.Random rng, int[] weights)
    {
        int total = 0;
        for (int s = 0; s < weights.Length; s++)
        {
            if ((SymbolType)s == WildSymbol || (SymbolType)s == ScatterSymbol) continue;
            total += weights[s];
        }
        if (total <= 0) return SymbolType.Ten;
        int roll = rng.Next(total);
        int acc = 0;
        for (int s = 0; s < weights.Length; s++)
        {
            if ((SymbolType)s == WildSymbol || (SymbolType)s == ScatterSymbol) continue;
            acc += weights[s];
            if (roll < acc) return (SymbolType)s;
        }
        return SymbolType.Ten;
    }

    private static int[] GetWeights(Difficulty difficulty)
    {
        // Symbol order: Ten, Jack, Queen, King, Ace, Potion, Skull, Treasure, Cavalry, Knight, Dragon(Wild), Crown(Scatter)
        return difficulty switch
        {
            //                          10, J,  Q,  K,  A, Ptn, Skl, Trs, Cav, Knt, DRG, CRN
            Difficulty.Easy   => new[] { 30, 30, 25, 20, 15, 10,  8,  6,  4,  3,   1,   4 },
            Difficulty.Hard   => new[] { 45, 42, 35, 25, 18, 10,  7,  4,  2,  1,   1,   4 },
            _                 => new[] { 35, 33, 28, 22, 17, 10,  7,  5,  3,  2,   1,   3 }
        };
    }

    public static string DifficultyLabel(Difficulty d) => d switch
    {
        Difficulty.Easy => "EASY (sering menang besar)",
        Difficulty.Hard => "HARD (jarang menang, jackpot susah)",
        _ => "MEDIUM (balanced)"
    };
}
