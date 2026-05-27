using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SpinRecord { public int bet; public int win; public string topSymbol; public string timestamp; }

[System.Serializable]
public class SpinRecordList { public List<SpinRecord> records = new List<SpinRecord>(); }

public static class HistoryTracker
{
    public const int MaxRecords = 10;

    public static void Add(int bet, int win, SymbolType topSymbol)
    {
        var list = Load();
        list.records.Insert(0, new SpinRecord { bet = bet, win = win, topSymbol = topSymbol.ToString(), timestamp = System.DateTime.Now.ToString("HH:mm:ss") });
        while (list.records.Count > MaxRecords) list.records.RemoveAt(list.records.Count - 1);
        SaveSystem.HistoryJson = JsonUtility.ToJson(list);
    }

    public static SpinRecordList Load()
    {
        var json = SaveSystem.HistoryJson;
        if (string.IsNullOrEmpty(json)) return new SpinRecordList();
        return JsonUtility.FromJson<SpinRecordList>(json) ?? new SpinRecordList();
    }
}
