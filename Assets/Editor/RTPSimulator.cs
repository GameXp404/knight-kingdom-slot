using UnityEditor;
using UnityEngine;

public static class RTPSimulator
{
    [MenuItem("KnightKingdom/Tools/Simulate RTP — 10k spins (fast)")]
    public static void Simulate10k() => RunSimulation(10_000);

    [MenuItem("KnightKingdom/Tools/Simulate RTP — 100k spins (recommended)")]
    public static void Simulate100k() => RunSimulation(100_000);

    [MenuItem("KnightKingdom/Tools/Simulate RTP — 1M spins (slow, most accurate)")]
    public static void Simulate1M() => RunSimulation(1_000_000);

    private struct Stats
    {
        public long totalBet;
        public long totalPayout;
        public int paidSpins;
        public int winCount;
        public int bigWinCount;
        public int megaWinCount;
        public int scatter3Count;
        public int scatter4Count;
        public int scatter5Count;
        public int freeSpinsTriggered;
        public int freeSpinsPlayed;
        public int maxWin;

        public float RTP => totalBet == 0 ? 0 : (float)totalPayout / totalBet * 100f;
        public float HitFreq(int totalSpins) => totalSpins == 0 ? 0 : (float)winCount / totalSpins * 100f;
        public float BigWinFreq(int totalSpins) => totalSpins == 0 ? 0 : (float)bigWinCount / totalSpins * 100f;
    }

    private static void RunSimulation(int spinCount)
    {
        const int bet = 10;
        const int freeSpinMult = 1; // sesuai GameManager.FreeSpinMultiplier setelah Fix #6

        var report = new System.Text.StringBuilder();
        report.AppendLine($"\n╔════════════════════════════════════════════════╗");
        report.AppendLine($"║   KNIGHT KINGDOM SLOT — RTP SIMULATION         ║");
        report.AppendLine($"║   Spins per difficulty: {spinCount,-22:N0} ║");
        report.AppendLine($"║   Bet per spin:         {bet,-22:N0} ║");
        report.AppendLine($"╚════════════════════════════════════════════════╝");

        try
        {
            int diffIdx = 0;
            foreach (Difficulty diff in System.Enum.GetValues(typeof(Difficulty)))
            {
                EditorUtility.DisplayProgressBar(
                    "RTP Simulation",
                    $"Simulating {diff} ({diffIdx + 1}/3)…",
                    (float)diffIdx / 3f);

                var stats = SimulateOne(diff, spinCount, bet, freeSpinMult);
                AppendReport(report, diff, spinCount, stats, bet);
                diffIdx++;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        Debug.Log(report.ToString());
        EditorUtility.DisplayDialog(
            "RTP Simulation Selesai",
            $"Simulasi selesai untuk semua difficulty.\n\nLihat Console untuk laporan lengkap.",
            "OK");
    }

    private static Stats SimulateOne(Difficulty diff, int paidSpinCount, int bet, int freeSpinMult)
    {
        var stats = new Stats { paidSpins = paidSpinCount };
        var rng = new System.Random();

        var strips = new SymbolType[PaylineSystem.Reels][];
        for (int r = 0; r < PaylineSystem.Reels; r++)
            strips[r] = SymbolDatabase.GetReelStrip(r, diff);

        var grid = new SymbolType[PaylineSystem.Reels, PaylineSystem.Rows];
        int freeSpinsRemaining = 0;
        int paidDone = 0;

        while (paidDone < paidSpinCount || freeSpinsRemaining > 0)
        {
            bool isFreeSpin = freeSpinsRemaining > 0;
            if (!isFreeSpin)
            {
                stats.totalBet += bet;
                paidDone++;
            }
            else
            {
                stats.freeSpinsPlayed++;
            }

            for (int r = 0; r < PaylineSystem.Reels; r++)
            {
                int top = rng.Next(0, strips[r].Length);
                for (int row = 0; row < PaylineSystem.Rows; row++)
                    grid[r, row] = strips[r][(top + row) % strips[r].Length];
            }

            var wins = PaylineSystem.Evaluate(grid, bet);
            int spinWin = 0;
            int scatterCountInGrid = 0;
            foreach (var w in wins)
            {
                spinWin += w.payout;
                if (w.symbol == SymbolDatabase.ScatterSymbol && w.matchCount >= 3)
                    scatterCountInGrid = w.matchCount;
            }
            if (isFreeSpin) spinWin *= freeSpinMult;

            stats.totalPayout += spinWin;

            if (spinWin > 0)
            {
                stats.winCount++;
                if (spinWin >= bet * 50) stats.megaWinCount++;
                if (spinWin >= bet * 10) stats.bigWinCount++;
                if (spinWin > stats.maxWin) stats.maxWin = spinWin;
            }

            if (scatterCountInGrid >= 3 && !isFreeSpin)
            {
                int award = scatterCountInGrid >= 5 ? 150 : (scatterCountInGrid == 4 ? 50 : 10);
                freeSpinsRemaining = System.Math.Min(freeSpinsRemaining + award, 200);
                stats.freeSpinsTriggered += award;

                if (scatterCountInGrid == 3) stats.scatter3Count++;
                else if (scatterCountInGrid == 4) stats.scatter4Count++;
                else stats.scatter5Count++;
            }

            if (isFreeSpin) freeSpinsRemaining--;
        }

        return stats;
    }

    private static void AppendReport(System.Text.StringBuilder sb, Difficulty diff, int spinCount, Stats s, int bet)
    {
        sb.AppendLine();
        sb.AppendLine($"┌─ [{SymbolDatabase.DifficultyLabel(diff)}] ─────────");
        sb.AppendLine($"│  RTP:              {s.RTP,7:F2} %");
        sb.AppendLine($"│  Total bet:        {s.totalBet,12:N0}");
        sb.AppendLine($"│  Total payout:     {s.totalPayout,12:N0}");
        sb.AppendLine($"│  Net to player:    {s.totalPayout - s.totalBet,12:N0}");
        sb.AppendLine($"│");
        sb.AppendLine($"│  Hit frequency:    {s.HitFreq(spinCount),7:F2} %   ({s.winCount:N0} winning spins of {spinCount:N0})");
        sb.AppendLine($"│  Big Win (>=10x):  {s.BigWinFreq(spinCount),7:F2} %   ({s.bigWinCount:N0} spins)");
        sb.AppendLine($"│  Mega Win (>=50x): {(s.megaWinCount * 100f / spinCount),7:F2} %   ({s.megaWinCount:N0} spins)");
        sb.AppendLine($"│  Max single win:   {s.maxWin,12:N0}  ({(float)s.maxWin / bet:F0}x bet)");
        sb.AppendLine($"│");
        sb.AppendLine($"│  Scatter triggers (paid spin only):");
        sb.AppendLine($"│    3 Crown:        {s.scatter3Count,7:N0}");
        sb.AppendLine($"│    4 Crown:        {s.scatter4Count,7:N0}");
        sb.AppendLine($"│    5 Crown:        {s.scatter5Count,7:N0}");
        sb.AppendLine($"│  Free spins awarded: {s.freeSpinsTriggered,10:N0}");
        sb.AppendLine($"│  Free spins played:  {s.freeSpinsPlayed,10:N0}");
        sb.AppendLine($"└──────────────────────────────");
    }
}
