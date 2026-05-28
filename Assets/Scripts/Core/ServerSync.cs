using System;
using System.Runtime.InteropServices;
using UnityEngine;

// Bridges the Unity game to the GameGacor cloud API when the WebGL build is
// served inside the lobby (same origin as /api/*). When the build is opened
// stand-alone or no token is present, ServerSync stays disabled and the game
// falls back to local PlayerPrefs (SaveSystem) — the existing offline flow.
public class ServerSync : MonoBehaviour
{
    public static ServerSync Instance { get; private set; }

    public bool IsOnline { get; private set; }
    public string Username { get; private set; }

    public event Action<int> OnBalanceSynced; // server-authoritative balance after sync

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern int GG_HasToken();
    [DllImport("__Internal")] private static extern IntPtr GG_GetUsername();
    [DllImport("__Internal")] private static extern void GG_FetchBalance(string goName, string method);
    [DllImport("__Internal")] private static extern void GG_RecordSpin(int bet, int win, string tier, int scatters, int isFree, string goName, string method);
    [DllImport("__Internal")] private static extern void GG_GetDifficulty(string goName, string method);
    [DllImport("__Internal")] private static extern void GG_BackToLobby();
    [DllImport("__Internal")] private static extern void GG_Log(string msg);

    private static string PtrToStr(IntPtr p) {
        if (p == IntPtr.Zero) return "";
        return Marshal.PtrToStringAnsi(p) ?? "";
    }
#endif

    // Auto-bootstrap: spawn a hidden GameObject hosting ServerSync before the first scene loads.
    // Lets the cloud-sync feature ride along without needing the scene file to be edited in the Editor.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("ServerSync");
        DontDestroyOnLoad(go);
        go.AddComponent<ServerSync>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

#if UNITY_WEBGL && !UNITY_EDITOR
        IsOnline = GG_HasToken() == 1;
        if (IsOnline) {
            Username = PtrToStr(GG_GetUsername());
            GG_Log($"ServerSync online as '{Username}' — fetching balance…");
        }
#else
        IsOnline = false;
#endif
    }

    public void FetchBalance()
    {
        if (!IsOnline) return;
#if UNITY_WEBGL && !UNITY_EDITOR
        GG_FetchBalance(gameObject.name, nameof(OnBalanceResponse));
#endif
    }

    // Operator-controlled difficulty (RTP). Fetched from /api/game-config on start;
    // applies to SaveSystem.DifficultyLevel so reels use the admin-set weight table.
    public void FetchDifficulty()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        GG_GetDifficulty(gameObject.name, nameof(OnDifficultyResponse));
#endif
    }

    public void OnDifficultyResponse(string json)
    {
        var r = JsonUtility.FromJson<DifficultyResponse>(json);
        if (r == null) return;
        int level = Mathf.Clamp(r.difficulty, 0, 2);
        SaveSystem.DifficultyLevel = level;
        if (GameManager.Instance != null) GameManager.Instance.RefreshReelStrips();
    }

    public void RecordSpin(int bet, int win, string tier, int scatterCount, bool isFreeSpin)
    {
        if (!IsOnline) return;
#if UNITY_WEBGL && !UNITY_EDITOR
        GG_RecordSpin(bet, win, tier ?? "", scatterCount, isFreeSpin ? 1 : 0, gameObject.name, nameof(OnSpinResponse));
#endif
    }

    public void BackToLobby()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        GG_BackToLobby();
#else
        Application.Quit();
#endif
    }

    // Called from JS bridge (SendMessage) with JSON: { ok, balance, username?, error? }
    public void OnBalanceResponse(string json)
    {
        var r = JsonUtility.FromJson<SyncResponse>(json);
        if (r == null || !r.ok) { Debug.LogWarning($"[ServerSync] balance sync failed: {r?.error}"); return; }
        ApplyServerBalance(r.balance);
    }

    public void OnSpinResponse(string json)
    {
        var r = JsonUtility.FromJson<SyncResponse>(json);
        if (r == null || !r.ok) { Debug.LogWarning($"[ServerSync] spin sync failed: {r?.error}"); return; }
        ApplyServerBalance(r.balance);
    }

    private void ApplyServerBalance(int serverBalance)
    {
        if (serverBalance < 0) return;
        SaveSystem.Currency = serverBalance;
        if (GameManager.Instance != null && GameManager.Instance.ui != null)
            GameManager.Instance.ui.UpdateCurrency(serverBalance);
        OnBalanceSynced?.Invoke(serverBalance);
    }

    [Serializable]
    private class SyncResponse {
        public bool ok;
        public int balance;
        public string username;
        public string error;
    }

    [Serializable]
    private class DifficultyResponse {
        public bool ok;
        public int difficulty;
    }
}
