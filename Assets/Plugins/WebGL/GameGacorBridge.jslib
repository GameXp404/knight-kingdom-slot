// GameGacor WebGL <-> JavaScript bridge.
// Loaded by Unity WebGL build; lets the Unity side read the lobby session
// (stored in localStorage by /lobby) and call the cloud API for balance + spin sync.
mergeInto(LibraryManager.library, {
  // ============================================================
  // Session helpers (read localStorage written by /lobby)
  // ============================================================
  GG_HasToken: function () {
    try { return localStorage.getItem('calavera_token') ? 1 : 0; } catch (e) { return 0; }
  },

  GG_GetUsername: function () {
    var s = '';
    try { s = localStorage.getItem('calavera_user') || ''; } catch (e) {}
    var len = lengthBytesUTF8(s) + 1;
    var ptr = _malloc(len);
    stringToUTF8(s, ptr, len);
    return ptr;
  },

  GG_GetApiBase: function () {
    // Same-origin: the WebGL build is served from /games/knight/ on the lobby domain,
    // so /api/* hits our serverless functions directly.
    var s = (typeof window !== 'undefined' && window.location && window.location.origin) ? window.location.origin : '';
    var len = lengthBytesUTF8(s) + 1;
    var ptr = _malloc(len);
    stringToUTF8(s, ptr, len);
    return ptr;
  },

  // ============================================================
  // GET /api/me -> Unity callback receives JSON body
  // ============================================================
  GG_FetchBalance: function (goNamePtr, methodPtr) {
    var goName = UTF8ToString(goNamePtr);
    var method = UTF8ToString(methodPtr);
    var token = '';
    try { token = localStorage.getItem('calavera_token') || ''; } catch (e) {}
    if (!token) {
      try { SendMessage(goName, method, JSON.stringify({ ok: false, error: 'no_token' })); } catch (e) {}
      return;
    }
    fetch('/api/me', { headers: { 'Authorization': 'Bearer ' + token } })
      .then(function (r) { return r.json().then(function (j) { return { status: r.status, body: j }; }); })
      .then(function (res) {
        var payload = (res.status >= 200 && res.status < 300)
          ? { ok: true, balance: (res.body && res.body.user && res.body.user.balance) || 0, username: (res.body && res.body.user && res.body.user.username) || '' }
          : { ok: false, error: (res.body && res.body.error) || ('http_' + res.status) };
        try { SendMessage(goName, method, JSON.stringify(payload)); } catch (e) {}
      })
      .catch(function (err) {
        try { SendMessage(goName, method, JSON.stringify({ ok: false, error: String(err) })); } catch (e) {}
      });
  },

  // ============================================================
  // POST /api/player/spin-record -> Unity callback receives new balance
  // ============================================================
  GG_RecordSpin: function (bet, win, tierPtr, scatters, isFree, goNamePtr, methodPtr) {
    var goName = UTF8ToString(goNamePtr);
    var method = UTF8ToString(methodPtr);
    var tier = UTF8ToString(tierPtr);
    var token = '';
    try { token = localStorage.getItem('calavera_token') || ''; } catch (e) {}
    if (!token) {
      try { SendMessage(goName, method, JSON.stringify({ ok: false, error: 'no_token' })); } catch (e) {}
      return;
    }
    var body = {
      bet: bet | 0,
      win: win | 0,
      tier: tier || null,
      scatterCount: scatters | 0,
      isFreeSpinSpin: !!isFree,
      game: 'knight_kingdom',
    };
    fetch('/api/player/spin-record', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'Authorization': 'Bearer ' + token },
      body: JSON.stringify(body),
    })
      .then(function (r) { return r.json().then(function (j) { return { status: r.status, body: j }; }); })
      .then(function (res) {
        var payload = (res.status >= 200 && res.status < 300)
          ? { ok: true, balance: (res.body && res.body.balance) || 0 }
          : { ok: false, error: (res.body && res.body.error) || ('http_' + res.status) };
        try { SendMessage(goName, method, JSON.stringify(payload)); } catch (e) {}
      })
      .catch(function (err) {
        try { SendMessage(goName, method, JSON.stringify({ ok: false, error: String(err) })); } catch (e) {}
      });
  },

  // ============================================================
  // GET /api/game-config?game=knight -> operator-set difficulty (0/1/2)
  // Public endpoint (no auth) — difficulty is global game config, not user data.
  // ============================================================
  GG_GetDifficulty: function (goNamePtr, methodPtr) {
    var goName = UTF8ToString(goNamePtr);
    var method = UTF8ToString(methodPtr);
    fetch('/api/game-config?game=knight')
      .then(function (r) { return r.json(); })
      .then(function (d) {
        var diff = (d && typeof d.difficulty === 'number') ? d.difficulty : 1;
        try { SendMessage(goName, method, JSON.stringify({ ok: true, difficulty: diff })); } catch (e) {}
      })
      .catch(function () {
        try { SendMessage(goName, method, JSON.stringify({ ok: false, difficulty: 1 })); } catch (e) {}
      });
  },

  // ============================================================
  // Navigation: back to lobby
  // ============================================================
  GG_BackToLobby: function () {
    try { window.location.href = '/lobby'; } catch (e) {}
  },

  // Logs to browser console so we can debug from DevTools.
  GG_Log: function (msgPtr) {
    try { console.log('[GG]', UTF8ToString(msgPtr)); } catch (e) {}
  },
});
