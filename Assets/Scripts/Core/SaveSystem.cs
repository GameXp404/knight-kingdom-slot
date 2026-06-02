using UnityEngine;

public static class SaveSystem
{
    const string K_CURRENCY = "kk_currency";
    const string K_BET = "kk_bet";
    const string K_JACKPOT = "kk_jackpot";
    const string K_LAST_BONUS = "kk_lastBonus";
    const string K_VOLUME = "kk_volume";
    const string K_MUTED = "kk_muted";
    const string K_HISTORY = "kk_history";
    const string K_ACHIEVEMENTS = "kk_achievements";
    const string K_TOTAL_SPINS = "kk_totalSpins";
    const string K_TOTAL_WINS = "kk_totalWins";
    const string K_BIGGEST_WIN = "kk_biggestWin";
    const string K_DIFFICULTY = "kk_difficulty";
    const string K_TURBO = "kk_turbo";

    // FIX #4: Setter hanya update memory (no PlayerPrefs.Save())
    // Disk write hanya saat Flush() dipanggil — di OnApplicationPause/Quit/OnDestroy
    public static int Currency { get => PlayerPrefs.GetInt(K_CURRENCY, 1000); set => PlayerPrefs.SetInt(K_CURRENCY, value); }
    public static int Bet { get => PlayerPrefs.GetInt(K_BET, 10); set => PlayerPrefs.SetInt(K_BET, value); }
    public static int Jackpot { get => PlayerPrefs.GetInt(K_JACKPOT, 10000); set => PlayerPrefs.SetInt(K_JACKPOT, value); }
    // Tiered jackpots: Mini / Minor / Major / Grand
    public static int JpMini  { get => PlayerPrefs.GetInt("kk_jpMini",   1000);   set => PlayerPrefs.SetInt("kk_jpMini",   value); }
    public static int JpMinor { get => PlayerPrefs.GetInt("kk_jpMinor",  5000);   set => PlayerPrefs.SetInt("kk_jpMinor",  value); }
    public static int JpMajor { get => PlayerPrefs.GetInt("kk_jpMajor",  25000);  set => PlayerPrefs.SetInt("kk_jpMajor",  value); }
    public static int JpGrand { get => PlayerPrefs.GetInt("kk_jpGrand",  100000); set => PlayerPrefs.SetInt("kk_jpGrand",  value); }
    public static string LastBonusDate { get => PlayerPrefs.GetString(K_LAST_BONUS, ""); set => PlayerPrefs.SetString(K_LAST_BONUS, value); }
    public static float Volume { get => PlayerPrefs.GetFloat(K_VOLUME, 0.7f); set => PlayerPrefs.SetFloat(K_VOLUME, Mathf.Clamp01(value)); }
    public static bool Muted { get => PlayerPrefs.GetInt(K_MUTED, 0) == 1; set => PlayerPrefs.SetInt(K_MUTED, value ? 1 : 0); }
    public static string HistoryJson { get => PlayerPrefs.GetString(K_HISTORY, ""); set => PlayerPrefs.SetString(K_HISTORY, value); }
    public static string AchievementsJson { get => PlayerPrefs.GetString(K_ACHIEVEMENTS, ""); set => PlayerPrefs.SetString(K_ACHIEVEMENTS, value); }
    public static int TotalSpins { get => PlayerPrefs.GetInt(K_TOTAL_SPINS, 0); set => PlayerPrefs.SetInt(K_TOTAL_SPINS, value); }
    public static int TotalWins { get => PlayerPrefs.GetInt(K_TOTAL_WINS, 0); set => PlayerPrefs.SetInt(K_TOTAL_WINS, value); }
    public static int BiggestWin { get => PlayerPrefs.GetInt(K_BIGGEST_WIN, 0); set => PlayerPrefs.SetInt(K_BIGGEST_WIN, value); }
    public static int DifficultyLevel { get => PlayerPrefs.GetInt(K_DIFFICULTY, 1); set => PlayerPrefs.SetInt(K_DIFFICULTY, Mathf.Clamp(value, 0, 2)); }
    public static bool TurboMode { get => PlayerPrefs.GetInt(K_TURBO, 0) == 1; set => PlayerPrefs.SetInt(K_TURBO, value ? 1 : 0); }

    public static void Flush() { PlayerPrefs.Save(); }
    public static void ResetAll() { PlayerPrefs.DeleteAll(); PlayerPrefs.Save(); }
}
