using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Achievement { public string id; public string title; public string description; public bool unlocked; }

[System.Serializable]
public class AchievementList { public List<Achievement> items = new List<Achievement>(); }

public static class AchievementManager
{
    public static System.Action<Achievement> OnUnlocked;

    private static readonly Achievement[] DefaultAchievements =
    {
        new Achievement { id="first_spin", title="Pemula", description="Putar pertama kalinya" },
        new Achievement { id="first_win", title="Untung Pertama", description="Menang pertama" },
        new Achievement { id="big_win", title="Untung Besar", description="Menang 10x lipat taruhan" },
        new Achievement { id="mega_win", title="Mega Win", description="Menang 50x lipat taruhan" },
        new Achievement { id="jackpot", title="JACKPOT!", description="Hit jackpot progresif" },
        new Achievement { id="million_coins", title="Sultan", description="Saldo capai 1.000.000" },
        new Achievement { id="hundred_spins", title="Setia", description="Putar 100 kali" },
        new Achievement { id="thousand_spins", title="Master Knight", description="Putar 1.000 kali" },
        new Achievement { id="five_crowns", title="Lima Mahkota", description="Dapat 5 Crown sekaligus" },
        new Achievement { id="all_dragons", title="Naga Trio", description="3 Dragon dalam 1 putaran" }
    };

    private static AchievementList cache;

    private static AchievementList Load()
    {
        if (cache != null) return cache;
        var json = SaveSystem.AchievementsJson;
        if (string.IsNullOrEmpty(json))
        {
            cache = new AchievementList();
            foreach (var a in DefaultAchievements) cache.items.Add(new Achievement { id = a.id, title = a.title, description = a.description, unlocked = false });
            Save();
        }
        else
        {
            cache = JsonUtility.FromJson<AchievementList>(json) ?? new AchievementList();
            foreach (var def in DefaultAchievements) if (!cache.items.Exists(x => x.id == def.id)) cache.items.Add(new Achievement { id = def.id, title = def.title, description = def.description, unlocked = false });
        }
        return cache;
    }

    private static void Save() { SaveSystem.AchievementsJson = JsonUtility.ToJson(cache); }
    public static List<Achievement> All() => Load().items;

    public static bool Unlock(string id)
    {
        var list = Load();
        var a = list.items.Find(x => x.id == id);
        if (a == null || a.unlocked) return false;
        a.unlocked = true; Save(); OnUnlocked?.Invoke(a); return true;
    }

    public static void CheckOnSpin()
    {
        Unlock("first_spin");
        if (SaveSystem.TotalSpins >= 100) Unlock("hundred_spins");
        if (SaveSystem.TotalSpins >= 1000) Unlock("thousand_spins");
    }

    public static void CheckOnWin(int winAmount, int bet, int wildCount, int scatterCount, bool isJackpot)
    {
        Unlock("first_win");
        if (winAmount >= bet * 10) Unlock("big_win");
        if (winAmount >= bet * 50) Unlock("mega_win");
        if (isJackpot) Unlock("jackpot");
        if (SaveSystem.Currency >= 1000000) Unlock("million_coins");
        if (scatterCount >= 5) Unlock("five_crowns");
        if (wildCount >= 3) Unlock("all_dragons");
    }
}
