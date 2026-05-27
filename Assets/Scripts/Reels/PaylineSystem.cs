using System.Collections.Generic;
using UnityEngine;

public static class PaylineSystem
{
    public const int Reels = 5;
    public const int Rows = 4;

    public static readonly int[][] Lines =
    {
        new[]{0,0,0,0,0}, new[]{1,1,1,1,1}, new[]{2,2,2,2,2}, new[]{3,3,3,3,3},
        new[]{0,1,2,1,0}, new[]{1,2,3,2,1}, new[]{3,2,1,2,3}, new[]{2,1,0,1,2},
        new[]{0,0,1,0,0}, new[]{1,1,2,1,1}, new[]{2,2,3,2,2}, new[]{3,3,2,3,3},
        new[]{0,1,1,1,0}, new[]{1,2,2,2,1}, new[]{2,3,3,3,2}, new[]{3,2,2,2,3},
        new[]{0,1,0,1,0}, new[]{1,2,1,2,1}, new[]{2,3,2,3,2}, new[]{3,2,3,2,3},
        new[]{0,0,2,0,0}, new[]{1,1,3,1,1}, new[]{0,2,0,2,0}, new[]{1,3,1,3,1},
        new[]{0,1,2,3,0}, new[]{1,2,3,2,0}, new[]{0,2,3,2,1}, new[]{3,1,0,1,3},
        new[]{2,0,1,0,2}, new[]{3,3,0,3,3}, new[]{0,0,3,0,0}, new[]{1,3,2,3,1},
        new[]{2,0,3,0,2}, new[]{0,1,3,1,0}, new[]{3,2,0,2,3}, new[]{1,0,2,0,1},
        new[]{2,3,1,3,2}, new[]{0,3,0,3,0}, new[]{3,0,3,0,3}, new[]{1,2,0,2,1}
    };

    public static int Count => Lines.Length;

    public struct Win
    {
        public int paylineIndex;
        public SymbolType symbol;
        public int matchCount;
        public int payout;
        public List<Vector2Int> cells;
    }

    public static List<Win> Evaluate(SymbolType[,] grid, int bet)
    {
        var wins = new List<Win>();
        int betUnit = bet / SymbolDatabase.BaseBetUnit;
        if (betUnit < 1) betUnit = 1;

        for (int p = 0; p < Lines.Length; p++)
        {
            var line = Lines[p];

            // Cari target = simbol non-wild pertama dari kiri
            SymbolType target = SymbolDatabase.WildSymbol;
            for (int r = 0; r < Reels; r++)
            {
                var s = grid[r, line[r]];
                if (s != SymbolDatabase.WildSymbol) { target = s; break; }
            }

            if (target == SymbolDatabase.ScatterSymbol) continue;

            // Hitung match chain dari kiri (Wild substitusi target)
            int match = 0;
            var cells = new List<Vector2Int>();
            for (int r = 0; r < Reels; r++)
            {
                var s = grid[r, line[r]];
                bool ok = (target == SymbolDatabase.WildSymbol)
                    ? (s == SymbolDatabase.WildSymbol)
                    : (s == target || s == SymbolDatabase.WildSymbol);
                if (ok) { match++; cells.Add(new Vector2Int(r, line[r])); }
                else break;
            }

            if (match < 3) continue;

            // FIX #1: Ambil MAX antara substitution pay vs wild-only pay
            int payoutSubst = SymbolDatabase.GetPayout(target, match) * betUnit;
            var winSymbol = target;
            var winCells = cells;
            var winMatch = match;
            int winPayout = payoutSubst;

            if (target != SymbolDatabase.WildSymbol)
            {
                int wildOnly = 0;
                var wildCells = new List<Vector2Int>();
                for (int r = 0; r < Reels; r++)
                {
                    if (grid[r, line[r]] == SymbolDatabase.WildSymbol)
                    {
                        wildOnly++;
                        wildCells.Add(new Vector2Int(r, line[r]));
                    }
                    else break;
                }
                if (wildOnly >= 3)
                {
                    int wildPay = SymbolDatabase.GetPayout(SymbolDatabase.WildSymbol, wildOnly) * betUnit;
                    if (wildPay > winPayout)
                    {
                        winPayout = wildPay;
                        winSymbol = SymbolDatabase.WildSymbol;
                        winMatch = wildOnly;
                        winCells = wildCells;
                    }
                }
            }

            wins.Add(new Win
            {
                paylineIndex = p,
                symbol = winSymbol,
                matchCount = winMatch,
                payout = winPayout,
                cells = winCells
            });
        }

        // Scatter anywhere
        int scatterCount = 0;
        var scatterCellList = new List<Vector2Int>();
        for (int r = 0; r < Reels; r++)
            for (int c = 0; c < Rows; c++)
                if (grid[r, c] == SymbolDatabase.ScatterSymbol)
                {
                    scatterCount++;
                    scatterCellList.Add(new Vector2Int(r, c));
                }

        if (scatterCount >= 3)
        {
            int scatterBetMult = scatterCount >= 5 ? 50 : (scatterCount == 4 ? 15 : 5);
            int payout = scatterBetMult * bet;
            wins.Add(new Win
            {
                paylineIndex = -1,
                symbol = SymbolDatabase.ScatterSymbol,
                matchCount = scatterCount,
                payout = payout,
                cells = scatterCellList
            });
        }

        return wins;
    }
}
