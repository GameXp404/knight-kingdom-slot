using System;

public static class DailyBonusManager
{
    public const int BonusAmount = 500;

    public static bool IsAvailable()
    {
        var last = SaveSystem.LastBonusDate;
        if (string.IsNullOrEmpty(last)) return true;
        if (!DateTime.TryParse(last, out var lastDate)) return true;
        return lastDate.Date < DateTime.Now.Date;
    }

    public static int Claim()
    {
        if (!IsAvailable()) return 0;
        SaveSystem.LastBonusDate = DateTime.Now.ToString("yyyy-MM-dd");
        SaveSystem.Currency += BonusAmount;
        return BonusAmount;
    }

    public static string TimeUntilNext()
    {
        var tomorrow = DateTime.Now.Date.AddDays(1);
        var diff = tomorrow - DateTime.Now;
        return $"{diff.Hours:D2}:{diff.Minutes:D2}:{diff.Seconds:D2}";
    }
}
