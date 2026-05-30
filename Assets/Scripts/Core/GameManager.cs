using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public ReelController[] reels;
    public UIController ui;
    public WinPopupController winPopup;
    public CoinParticleEffect coinParticles;
    public AutoSpinController autoSpin;

    public float preSpinDelay = 0.15f;
    public float reelStaggerDelay = 0.18f;
    public float postWinDelay = 0.7f;
    public float autoSpinInterval = 0.3f;
    public float reelSpinSymbolsPerSec = 18f;
    public float reelDecelDuration = 1.0f;

    public static readonly int[] BetTiers = { 10, 50, 100, 500, 1000 };

    private bool isSpinning;
    private int freeSpinsRemaining;
    private int fsMultiplier = 1; // Emperor's Free Spins: climbing win multiplier (+1 per winning FS, cap x10)
    private SymbolType[,] grid = new SymbolType[PaylineSystem.Reels, PaylineSystem.Rows];

    public const int FreeSpinsPerScatter = 10;
    public const int FreeSpinMultiplier = 1;

    void Awake() { if (Instance != null && Instance != this) { Destroy(gameObject); return; } Instance = this; }

    void Start()
    {
        if (ui != null) { ui.UpdateCurrency(SaveSystem.Currency); ui.UpdateBet(SaveSystem.Bet); ui.UpdateJackpot(JackpotPool.Current); ui.UpdateWin(0); ui.UpdateAutoSpin(0); }
        AchievementManager.OnUnlocked += OnAchievementUnlocked;
        if (autoSpin != null) autoSpin.OnRemainingChanged += OnAutoSpinChanged;
        // Pull server-authoritative balance (lobby session) — overrides local PlayerPrefs when online.
        // Always fetch operator-set difficulty (public game config, even for guests).
        if (ServerSync.Instance != null) {
            if (ServerSync.Instance.IsOnline) ServerSync.Instance.FetchBalance();
            ServerSync.Instance.FetchDifficulty();
        }
    }

    void OnDestroy() { AchievementManager.OnUnlocked -= OnAchievementUnlocked; if (autoSpin != null) autoSpin.OnRemainingChanged -= OnAutoSpinChanged; SaveSystem.Flush(); }
    void OnApplicationPause(bool paused) { if (paused) SaveSystem.Flush(); }
    void OnApplicationQuit() { SaveSystem.Flush(); }

    public void TrySpin()
    {
        if (isSpinning) return;
        if (SaveSystem.Currency < SaveSystem.Bet) { HandleNoCoins(); return; }
        StartCoroutine(SpinCycle());
    }

    public void IncreaseBet()
    {
        int idx = System.Array.IndexOf(BetTiers, SaveSystem.Bet);
        if (idx < 0) idx = 0;
        SaveSystem.Bet = (idx >= BetTiers.Length - 1) ? BetTiers[BetTiers.Length - 1] : BetTiers[idx + 1];
        if (ui != null) ui.UpdateBet(SaveSystem.Bet);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
    }

    public void DecreaseBet()
    {
        int idx = System.Array.IndexOf(BetTiers, SaveSystem.Bet);
        SaveSystem.Bet = (idx > 0) ? BetTiers[idx - 1] : BetTiers[0];
        if (ui != null) ui.UpdateBet(SaveSystem.Bet);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
    }

    public void StartAutoSpin(int count) { if (autoSpin == null) return; autoSpin.Begin(count); TrySpin(); }
    public void StopAutoSpin() {
        if (autoSpin != null) autoSpin.Cancel();
        freeSpinsRemaining = 0;
        if (ui != null) ui.UpdateFreeSpins(0);
    }

    public void RefreshReelStrips()
    {
        if (reels == null) return;
        for (int r = 0; r < reels.Length; r++) reels[r].strip = SymbolDatabase.GetReelStrip(r);
    }

    public void ClaimDailyBonus()
    {
        if (!DailyBonusManager.IsAvailable()) return;
        int bonus = DailyBonusManager.Claim();
        if (ui != null) ui.UpdateCurrency(SaveSystem.Currency);
        if (winPopup != null) StartCoroutine(winPopup.Show(0, bonus));
        if (coinParticles != null) coinParticles.Burst(0);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayWin(0);
    }

    private IEnumerator SpinCycle()
    {
        isSpinning = true;
        if (ui != null) ui.UpdateWin(0);

        int bet = SaveSystem.Bet;
        bool isFreeSpin = freeSpinsRemaining > 0;
        if (!isFreeSpin) {
            SaveSystem.Currency -= bet;
            JackpotPool.Contribute(bet);
        }
        SaveSystem.TotalSpins++;
        if (ui != null) { ui.UpdateCurrency(SaveSystem.Currency); ui.UpdateJackpot(JackpotPool.Current); }
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySpin();

        bool turbo = SaveSystem.TurboMode;
        float effPreDelay = turbo ? 0.05f : preSpinDelay;
        float effStagger = turbo ? 0.07f : reelStaggerDelay;
        float effSpinSpeed = turbo ? 30f : reelSpinSymbolsPerSec;
        float effDecel = turbo ? 0.45f : reelDecelDuration;

        yield return new WaitForSeconds(effPreDelay);
        foreach (var r in reels) { r.spinSymbolsPerSec = effSpinSpeed; r.decelDuration = effDecel; r.StartSpin(); }

        for (int i = 0; i < reels.Length; i++)
        {
            yield return new WaitForSeconds(effStagger);
            int target = Random.Range(0, reels[i].strip.Length);

            // POLISH #4: ANTICIPATION — slow down reel 3-5 jika 2+ scatter sudah landed
            if (i >= 2 && !turbo) {
                int scattersSoFar = 0;
                for (int r = 0; r < i; r++)
                    for (int row = 0; row < PaylineSystem.Rows; row++)
                        if (reels[r].visibleSymbols[row] == SymbolDatabase.ScatterSymbol) scattersSoFar++;
                if (scattersSoFar >= 2) {
                    reels[i].spinSymbolsPerSec = effSpinSpeed * 0.25f;
                    reels[i].decelDuration = effDecel * 2.5f;
                    Debug.Log($"[Anticipation] Reel {i+1} SLOWS DOWN — {scattersSoFar} scatter landed!");
                    if (AudioManager.Instance != null) AudioManager.Instance.PlaySpin();
                    if (ScreenShake.Instance != null) ScreenShake.Instance.Shake(0.3f, 6f);
                    // JUICE: golden flash to spotlight the scatter tension moment
                    if (ScreenFlash.Instance != null) ScreenFlash.Instance.Flash(new Color(1f, 0.85f, 0.3f), 0.45f, 0.4f);
                }
            }

            bool done = false;
            reels[i].StopAt(target, () => done = true);
            while (!done) yield return null;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayStopPitched(i);
        }

        for (int r = 0; r < reels.Length; r++)
            for (int row = 0; row < PaylineSystem.Rows; row++)
                grid[r, row] = reels[r].visibleSymbols[row];

        EnforceMaxOnePerReel(SymbolDatabase.ScatterSymbol);
        EnforceMaxOnePerReel(SymbolDatabase.WildSymbol);

        // BATCH 2: DRAGON WILD EXPAND — any reel showing a Wild erupts to a full Wild column (+ fire)
        yield return ExpandDragonWilds();

        var wins = PaylineSystem.Evaluate(grid, bet);
        int totalWin = 0;
        foreach (var w in wins) totalWin += w.payout;
        if (isFreeSpin) totalWin *= fsMultiplier;

        bool isJackpot = false;
        if (JackpotPool.TryHitJackpot()) { totalWin += JackpotPool.Claim(); isJackpot = true; }

        int wildCount = CountSymbol(SymbolDatabase.WildSymbol);
        int scatterCount = CountSymbol(SymbolDatabase.ScatterSymbol);
        int scatterPay = 0;
        foreach (var w in wins) if (w.symbol == SymbolDatabase.ScatterSymbol) scatterPay = w.payout;

        Debug.Log($"[Spin] scatterCount={scatterCount} scatterPay={scatterPay} totalWin={totalWin} freeSpins(before)={freeSpinsRemaining}");

        if (scatterCount >= 3 && !isFreeSpin) {
            int award = scatterCount >= 5 ? 150 : (scatterCount == 4 ? 50 : 10);
            freeSpinsRemaining = Mathf.Min(freeSpinsRemaining + award, 200);
            fsMultiplier = 1; // start the Emperor multiplier ladder fresh
            if (ui != null) {
                ui.UpdateFreeSpins(freeSpinsRemaining, fsMultiplier);
                ui.ShowAchievementToastRaw($"<color=#ffd700>★ {scatterCount} SCATTER!</color>\n<size=80%>+{scatterPay:N0} koin & +{award} free spin — perkalian NAIK tiap menang!</size>");
            }
            if (AudioManager.Instance != null) AudioManager.Instance.PlayWin(2);
            if (ScreenFlash.Instance != null) ScreenFlash.Instance.Flash(new Color(1f, 0.8f, 0.2f), 0.6f, 0.85f);
            if (ScreenShake.Instance != null) ScreenShake.Instance.Shake(0.6f, 18f);
            if (winPopup != null) yield return winPopup.ShowFreeSpins(award);
        }
        else if (scatterCount >= 3 && isFreeSpin) {
            if (ui != null) ui.ShowAchievementToastRaw($"<color=#ffd700>★ {scatterCount} SCATTER!</color>\n<size=80%>perkalian ×{fsMultiplier}!</size>");
            if (AudioManager.Instance != null) AudioManager.Instance.PlayWin(1);
        }

        if (totalWin > 0)
        {
            SaveSystem.Currency += totalWin;
            SaveSystem.TotalWins += totalWin;
            if (totalWin > SaveSystem.BiggestWin) SaveSystem.BiggestWin = totalWin;
            if (ui != null) { ui.UpdateCurrency(SaveSystem.Currency); ui.UpdateWin(totalWin); ui.UpdateJackpot(JackpotPool.Current); }

            int tier = isJackpot ? 3 : (totalWin >= bet * 50 ? 2 : (totalWin >= bet * 10 ? 1 : 0));
            if (AudioManager.Instance != null) AudioManager.Instance.PlayWin(Mathf.Clamp(tier, 0, 2));

            // POLISH #5: COIN SHOWER BOOST — Mega/Jackpot dapat large burst + multi-wave
            if (coinParticles != null && tier > 0) {
                int particleTier = (tier >= 2) ? 2 : 0;
                coinParticles.Burst(particleTier);
                if (tier == 3) StartCoroutine(JackpotCoinWaves());
            }

            if (tier >= 2 && ScreenShake.Instance != null) ScreenShake.Instance.Shake(tier == 3 ? 1.2f : 0.7f, tier == 3 ? 32f : 22f);
            if (tier >= 2 && ScreenFlash.Instance != null) { Color c = tier == 3 ? new Color(1f,1f,0.4f) : new Color(1f,0.6f,0.2f); ScreenFlash.Instance.Flash(c, tier == 3 ? 0.6f : 0.4f, tier == 3 ? 0.85f : 0.6f); }

            // POLISH #6: WIN LINE WAVE — winning cells nyala kolom per kolom dari kiri ke kanan
            yield return FlashWinCellsSequential(wins);

            if (winPopup != null && tier > 0) yield return winPopup.Show(tier, totalWin);
            else yield return new WaitForSeconds(turbo ? 0.2f : postWinDelay);

            RestoreCellColors();
        }

        var topSymbol = grid[0, 1];
        HistoryTracker.Add(bet, totalWin, topSymbol);

        AchievementManager.CheckOnSpin();
        if (totalWin > 0) AchievementManager.CheckOnWin(totalWin, bet, wildCount, scatterCount, isJackpot);

        // Cloud sync: server is the source of truth for balance when running inside the lobby.
        if (ServerSync.Instance != null && ServerSync.Instance.IsOnline) {
            int tierNum = isJackpot ? 3 : (totalWin >= bet * 50 ? 2 : (totalWin >= bet * 10 ? 1 : 0));
            string tierStr = tierNum == 3 ? "JACKPOT" : tierNum == 2 ? "MEGA" : tierNum == 1 ? "BIG" : "";
            ServerSync.Instance.RecordSpin(bet, totalWin, tierStr, scatterCount, isFreeSpin);
        }

        isSpinning = false;

        if (isFreeSpin) {
            freeSpinsRemaining--;
            if (totalWin > 0) fsMultiplier = Mathf.Min(fsMultiplier + 1, 10); // Emperor ladder climbs on every winning free spin
            if (ui != null) ui.UpdateFreeSpins(freeSpinsRemaining, fsMultiplier);
        }

        if (freeSpinsRemaining > 0) {
            yield return new WaitForSeconds(turbo ? 0.1f : 0.4f);
            StartCoroutine(SpinCycle());
        } else if (autoSpin != null && autoSpin.IsActive) {
            autoSpin.Decrement();
            if (autoSpin.IsActive && SaveSystem.Currency >= SaveSystem.Bet) { yield return new WaitForSeconds(turbo ? 0.1f : autoSpinInterval); TrySpin(); }
        }
    }

    // BATCH 2: DRAGON WILD EXPAND — every reel that shows a Wild becomes a full Wild column (+ fire eruption).
    private IEnumerator ExpandDragonWilds()
    {
        bool any = false;
        for (int r = 0; r < reels.Length; r++)
        {
            bool hasWild = false;
            for (int row = 0; row < PaylineSystem.Rows; row++)
                if (grid[r, row] == SymbolDatabase.WildSymbol) { hasWild = true; break; }
            if (hasWild)
            {
                for (int row = 0; row < PaylineSystem.Rows; row++) grid[r, row] = SymbolDatabase.WildSymbol;
                if (reels[r] != null) reels[r].ExpandToWild();
                any = true;
            }
        }
        if (any)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayWin(1);
            if (ScreenShake.Instance != null) ScreenShake.Instance.Shake(0.4f, 12f);
            if (ScreenFlash.Instance != null) ScreenFlash.Instance.Flash(new Color(1f, 0.5f, 0.1f), 0.4f, 0.5f);
            yield return new WaitForSeconds(0.55f);
        }
    }

    private IEnumerator JackpotCoinWaves()
    {
        for (int wave = 0; wave < 3; wave++)
        {
            yield return new WaitForSeconds(0.5f);
            if (coinParticles != null) coinParticles.Burst(2);
            if (ScreenShake.Instance != null) ScreenShake.Instance.Shake(0.4f, 18f);
        }
    }

    private IEnumerator FlashWinCellsSequential(List<PaylineSystem.Win> wins)
    {
        Color flashColor = new Color(1.5f, 1.3f, 0.5f, 1f);
        Color dimColor = new Color(0.30f, 0.30f, 0.30f, 1f);
        Color normalColor = Color.white;

        var cellsByCol = new HashSet<int>[reels.Length];
        for (int r = 0; r < reels.Length; r++) cellsByCol[r] = new HashSet<int>();
        foreach (var w in wins)
        {
            if (w.cells == null) continue;
            foreach (var c in w.cells)
                if (c.x >= 0 && c.x < reels.Length) cellsByCol[c.x].Add(c.y);
        }

        for (int r = 0; r < reels.Length; r++)
            for (int row = 0; row < PaylineSystem.Rows; row++) {
                if (reels[r].symbolImages == null || row >= reels[r].symbolImages.Length) continue;
                reels[r].symbolImages[row].color = dimColor;
            }

        for (int col = 0; col < reels.Length; col++)
        {
            foreach (var row in cellsByCol[col])
            {
                if (reels[col].symbolImages != null && row < reels[col].symbolImages.Length)
                {
                    reels[col].symbolImages[row].color = flashColor;
                    var rt = reels[col].symbolImages[row].rectTransform;
                    StartCoroutine(QuickPulse(rt));
                }
            }
            yield return new WaitForSeconds(0.13f);
        }

        var allWinCells = new HashSet<(int, int)>();
        foreach (var w in wins) if (w.cells != null) foreach (var c in w.cells) allWinCells.Add((c.x, c.y));
        StartCoroutine(BounceWinCells(allWinCells));

        for (int pulse = 0; pulse < 5; pulse++)
        {
            yield return new WaitForSeconds(0.16f);
            foreach (var c in allWinCells)
                if (c.Item1 < reels.Length && c.Item2 < reels[c.Item1].symbolImages.Length)
                    reels[c.Item1].symbolImages[c.Item2].color = normalColor;
            yield return new WaitForSeconds(0.16f);
            foreach (var c in allWinCells)
                if (c.Item1 < reels.Length && c.Item2 < reels[c.Item1].symbolImages.Length)
                    reels[c.Item1].symbolImages[c.Item2].color = flashColor;
        }
    }

    private IEnumerator QuickPulse(RectTransform rt)
    {
        if (rt == null) yield break;
        Vector3 baseScale = rt.localScale;
        float t = 0f, dur = 0.25f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = t / dur;
            float pulse = 1f + Mathf.Sin(p * Mathf.PI) * 0.35f;
            rt.localScale = baseScale * pulse;
            yield return null;
        }
        rt.localScale = baseScale;
    }

    private IEnumerator FlashWinCells(HashSet<(int, int)> cells)
    {
        Color flashColor = new Color(1.5f, 1.3f, 0.5f, 1f);
        Color dimColor = new Color(0.30f, 0.30f, 0.30f, 1f);
        Color normalColor = Color.white;

        for (int r = 0; r < reels.Length; r++) for (int row = 0; row < PaylineSystem.Rows; row++) {
            if (reels[r].symbolImages == null || row >= reels[r].symbolImages.Length) continue;
            bool isWinner = cells.Contains((r, row));
            reels[r].symbolImages[row].color = isWinner ? flashColor : dimColor;
        }

        StartCoroutine(BounceWinCells(cells));

        for (int pulse = 0; pulse < 6; pulse++)
        {
            yield return new WaitForSeconds(0.15f);
            foreach (var c in cells) if (c.Item1 < reels.Length && c.Item2 < reels[c.Item1].symbolImages.Length) reels[c.Item1].symbolImages[c.Item2].color = normalColor;
            yield return new WaitForSeconds(0.15f);
            foreach (var c in cells) if (c.Item1 < reels.Length && c.Item2 < reels[c.Item1].symbolImages.Length) reels[c.Item1].symbolImages[c.Item2].color = flashColor;
        }
    }

    private IEnumerator BounceWinCells(HashSet<(int, int)> cells)
    {
        var rects = new List<RectTransform>();
        var origScales = new List<Vector3>();
        foreach (var c in cells) if (c.Item1 < reels.Length && c.Item2 < reels[c.Item1].symbolImages.Length) {
            var rt = reels[c.Item1].symbolImages[c.Item2].rectTransform;
            rects.Add(rt); origScales.Add(rt.localScale);
        }

        for (int wave = 0; wave < 3; wave++)
        {
            float t = 0f, dur = 0.4f;
            while (t < dur) { t += Time.deltaTime; float p = t/dur; float scale = 1f + Mathf.Sin(p * Mathf.PI) * 0.20f; for (int i = 0; i < rects.Count; i++) rects[i].localScale = origScales[i] * scale; yield return null; }
            for (int i = 0; i < rects.Count; i++) rects[i].localScale = origScales[i];
            yield return new WaitForSeconds(0.1f);
        }
    }

    private void RestoreCellColors()
    {
        for (int r = 0; r < reels.Length; r++) {
            if (reels[r].symbolImages == null) continue;
            for (int row = 0; row < reels[r].symbolImages.Length; row++) {
                reels[r].symbolImages[row].color = Color.white;
                reels[r].symbolImages[row].rectTransform.localScale = Vector3.one;
            }
        }
    }

    private int CountSymbol(SymbolType s)
    {
        int count = 0;
        for (int r = 0; r < reels.Length; r++) for (int row = 0; row < PaylineSystem.Rows; row++) if (grid[r, row] == s) count++;
        return count;
    }

    private void EnforceMaxOnePerReel(SymbolType target)
    {
        var rng = new System.Random();
        for (int r = 0; r < reels.Length; r++)
        {
            bool seen = false;
            for (int row = 0; row < PaylineSystem.Rows; row++)
            {
                if (grid[r, row] != target) continue;
                if (!seen) { seen = true; continue; }

                SymbolType replacement = (SymbolType)rng.Next(0, (int)SymbolDatabase.WildSymbol);
                grid[r, row] = replacement;
                reels[r].visibleSymbols[row] = replacement;

                if (row < reels[r].symbolImages.Length)
                {
                    var img = reels[r].symbolImages[row];
                    var lbl = reels[r].symbolLabels[row];
                    var sprite = SymbolDatabase.GetSprite(replacement);
                    if (sprite != null)
                    {
                        img.sprite = sprite;
                        img.color = Color.white;
                        if (lbl) lbl.text = "";
                    }
                    else
                    {
                        img.sprite = null;
                        img.color = SymbolDatabase.GetColor(replacement);
                        if (lbl) lbl.text = SymbolDatabase.GetLabel(replacement);
                    }
                }
            }
        }
    }

    private void OnAchievementUnlocked(Achievement a) { if (ui != null) ui.ShowAchievementToast(a); }
    private void OnAutoSpinChanged(int remaining) { if (ui != null) ui.UpdateAutoSpin(remaining); }
    private void HandleNoCoins() { SaveSystem.Currency += 1000; if (ui != null) ui.UpdateCurrency(SaveSystem.Currency); if (winPopup != null) StartCoroutine(winPopup.Show(0, 1000)); }
}
