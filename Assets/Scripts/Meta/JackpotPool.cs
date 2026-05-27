using UnityEngine;

public static class JackpotPool
{
    public const float ContributionRate = 0.02f;
    public const int JackpotTriggerOdds = 5000;
    public const int MinJackpot = 10000;

    public static int Current => SaveSystem.Jackpot;
    public static void Contribute(int bet) { int contribution = Mathf.Max(1, Mathf.RoundToInt(bet * ContributionRate)); SaveSystem.Jackpot += contribution; }
    public static bool TryHitJackpot() { return Random.Range(0, JackpotTriggerOdds) == 0; }
    public static int Claim() { int amount = SaveSystem.Jackpot; SaveSystem.Jackpot = MinJackpot; return amount; }
}
