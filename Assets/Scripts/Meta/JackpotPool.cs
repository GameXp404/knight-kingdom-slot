using UnityEngine;

// Tiered progressive jackpot: MINI / MINOR / MAJOR / GRAND.
// Each spin contributes a share of the bet to every tier; each spin also rolls every tier
// (Mini most frequent, Grand rarest). On a hit, that tier is awarded and reset to its seed.
public static class JackpotPool
{
    public enum Tier { None = -1, Mini = 0, Minor = 1, Major = 2, Grand = 3 }

    public static readonly string[] Names = { "MINI", "MINOR", "MAJOR", "GRAND" };
    static readonly int[]   Seeds = { 1000, 5000, 25000, 100000 };  // reset value per tier
    static readonly int[]   Odds  = { 600, 3000, 12000, 60000 };    // 1-in-N trigger per spin
    static readonly float[] Share = { 0.45f, 0.30f, 0.18f, 0.07f }; // how the contribution splits

    public const float ContributionRate = 0.02f;

    // Back-compat: the headline "Current" jackpot is the GRAND tier.
    public static int Current => SaveSystem.JpGrand;

    public static int Get(Tier t) => t switch
    {
        Tier.Mini  => SaveSystem.JpMini,
        Tier.Minor => SaveSystem.JpMinor,
        Tier.Major => SaveSystem.JpMajor,
        Tier.Grand => SaveSystem.JpGrand,
        _ => 0
    };

    static void Set(Tier t, int v)
    {
        switch (t)
        {
            case Tier.Mini:  SaveSystem.JpMini  = v; break;
            case Tier.Minor: SaveSystem.JpMinor = v; break;
            case Tier.Major: SaveSystem.JpMajor = v; break;
            case Tier.Grand: SaveSystem.JpGrand = v; break;
        }
    }

    public static void Contribute(int bet)
    {
        int total = Mathf.Max(4, Mathf.RoundToInt(bet * ContributionRate));
        for (int i = 0; i < 4; i++)
            Set((Tier)i, Get((Tier)i) + Mathf.Max(1, Mathf.RoundToInt(total * Share[i])));
    }

    // Roll Grand -> Mini, return the highest tier that hits this spin (or None).
    public static Tier TryHit()
    {
        for (int i = 3; i >= 0; i--)
            if (Random.Range(0, Odds[i]) == 0) return (Tier)i;
        return Tier.None;
    }

    public static int Claim(Tier t)
    {
        int amount = Get(t);
        Set(t, Seeds[(int)t]); // reset that tier to its seed
        return amount;
    }
}
