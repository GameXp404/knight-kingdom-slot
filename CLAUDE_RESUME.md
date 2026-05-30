# 🎮 CLAUDE RESUME — GameGacor / Knight Kingdom Slot

> **Portable handoff doc.** Kalau kamu Claude sesi baru (PC lain / login lain / abis crash) dan memory lokal gak ada di mesin ini — **baca file ini dari atas ke bawah** buat lanjut projek. Ini konteks portabel-nya.
>
> Cara user lanjut: di PC utama cukup bilang **"lanjut Knight Kingdom"** (memory lokal lengkap). Di PC lain: arahkan Claude ke file ini (`github.com/GameXp404/knight-kingdom-slot/blob/main/CLAUDE_RESUME.md`).
>
> 🔒 File ini PUBLIC — tanpa token/password/email. Rahasia ada di memory lokal PC user.

---

## 👤 User
- Bahasa: **Indonesia** (balas pakai Bahasa Indonesia, panggil "beb" santai).
- PC utama: Windows Server 2025 **managed, NO admin**. Prefer solusi non-admin / web / cloud.
- Standar kualitas: **AAA Premium**, BUKAN prototype. Jangan tawarkan slot low-fidelity (PIXI/DOM/emoji) — user nolak konsisten.
- Selalu pakai **MCP Playwright** buat screenshot/navigate/upload/download/automation.

## 🗂️ Projek (saling terhubung)
1. **GameGacor lobby** — social-casino lobby di `gamegacor-id.vercel.app/lobby`. Coin sync via Supabase. Login di kanan atas. Admin panel terpisah (`/admin.html`). Isi: Calavera Riches, Knight Kingdom Slot, MiniSlots.
2. **Knight Kingdom Slot** — Unity 6 WebGL slot (5×3, 40 line). Live di `gamegacor-id.vercel.app/games/knight`.
3. **Calavera Riches** — slot Mexican Day of the Dead 1024-ways (di repo calavera-riches `public/`).

## 📦 Repo & lokasi
- **Unity project (lokal):** `D:\Users\user22\Documents\KnightKingdomSlot` (PC utama)
- **Knight repo:** `github.com/GameXp404/knight-kingdom-slot`
- **Deploy repo:** `github.com/GameXp404/calavera-riches` → **Vercel auto-deploy** tiap push ke `main`
- **Path deploy game knight:** `public/games/knight/` (file Build dinamai `knight.*`)
- `git push` ke kedua repo BISA (kredensial GCM GameXp404 punya akses push). Tapi REST API token = read-only.

---

## ✅ STATUS TERAKHIR (2026-05-29)
- **Knight Kingdom Build #12 = DEPLOYED & LIVE & JALAN.** SETTINGS (layar login) + SET (in-game pojok kanan atas) **HILANG**. Volume pindah ke **MENU → tombol SOUND** (buka panel audio lama). Coin sync OK.
- **SEDANG:** install **Unity 6000.4.9f1 + module "Web Build Support"** di PC utama, biar bisa **build WebGL lokal** (gantiin Unity Cloud Build yang lama ~13 mnt). Unity Hub udah keinstall; Editor lagi/baru didownload. Risiko: install Editor bisa kena UAC/admin (PC managed).

## 🔁 WORKFLOW BUILD & DEPLOY

### A. Build LOKAL (target, kalau Unity keinstall) — CEPAT
1. Edit kode C# di `Assets/Scripts/...`
2. Build: `Unity.exe -quit -batchmode -projectPath "D:\Users\user22\Documents\KnightKingdomSlot" -executeMethod GameGacorWebGLBuild.BuildWebGL -logFile build.log` (project punya method `BuildWebGL()` di `Assets/Editor/GameGacorWebGLBuild.cs`).
3. Copy hasil Build ke `calavera-riches/public/games/knight/` (rename `Knight Kingdom WebGL.*` → `knight.*`), JANGAN timpa `index.html`.
4. `git -C calavera-riches add public/games/knight && commit && push` → Vercel.

### B. Build via Unity Cloud Build (fallback, lambat ~8-13 mnt)
- Org `18968427281584`, project `8c4e10ab-3893-4ebd-91c5-e4d95ae1916f`, build target `knight-kingdom-webgl`.
- Trigger via API (POST `build-automation.services.api.unity.com/v2/orgs/{org}/projects/{proj}/buildtargets/knight-kingdom-webgl/builds` body `{"clean":true,"delay":0}`). Auth: Bearer token diambil dari header request SPA di browser yg udah login (Playwright → network request → request-headers → `authorization`). **Pakai `clean:true` kalau abis ganti kode.**
- Download artifact: GET `.../builds/{N}?internalUrls=true` → `links.download_primary.href` (URL plasticscm, token JWT exp 5 menit) → trigger download via `browser_evaluate` bikin `<a download>` + click (di browser yg udah login Unity; curl/PowerShell token-in-URL DITOLAK).
- Deploy: sama kayak A langkah 3-4.

## 🚨 PELAJARAN KRITIS (JANGAN DIULANG)
1. **JANGAN nambah komponen Unity `Slider` atau kontrol volume custom baru** di `GameBootstrap.cs` → bikin WebGL CRASH saat load (`RangeError: Maximum call stack size exceeded` / `RuntimeError: null function`). Terbukti dari saga build #8-#12. Pakai konstruksi yg udah ada di build yg jalan, atau buka panel settings lama (`settingsObj`).
2. **JANGAN timpa** `public/games/knight/index.html` dengan index.html dari hasil build. Yg live udah ada **token-mirror fix** (`calavera_lobby_token` → `calavera_token`) + ref `knight.*`.
3. Lobby simpan JWT di key **`calavera_lobby_token`** (bukan `calavera_token`). Username: `calavera_user`.
4. WebGL butuh ≥1 scene enabled di `EditorBuildSettings.asset`. Pre-Export method `GameGacorWebGLBuild.SetupWebGLSettings` set Brotli + template GameGacor + decompressionFallback.
5. Verify deploy: HEAD `gamegacor-id.vercel.app/games/knight/Build/knight.wasm.unityweb`, bandingin Content-Length sama file lokal.

## ▶️ NEXT STEPS
1. Cek Unity Editor udah keinstall (`Get-ChildItem -Recurse -Filter Unity.exe "C:\Program Files\*","D:\*"`). Kalau ada → setup build lokal (workflow A).
2. Mulai **poles game BORONGAN** — user prefer kumpulin banyak perubahan → 1x build (rebuild lama). Tanya user daftar perubahan yg diinginkan.
3. PC utama Chrome WASM diblok policy → test game di HP/device lain.
