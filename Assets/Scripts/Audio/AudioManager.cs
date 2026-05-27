using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private AudioSource sfxSource;
    private AudioSource sfxLayerSource;
    private AudioSource musicSource;

    private AudioClip clipSpin;
    private AudioClip clipStop;
    private AudioClip clipClick;
    private AudioClip clipWinSmall;
    private AudioClip clipWinMedium;
    private AudioClip clipWinJackpot;
    private AudioClip clipMusic;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxLayerSource = gameObject.AddComponent<AudioSource>();
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;

        LoadClips();
        ApplyVolume();

        if (clipMusic != null)
        {
            musicSource.clip = clipMusic;
            musicSource.Play();
        }
    }

    private void LoadClips()
    {
        clipSpin = LoadOrFallback("Audio/wheelSpin", MakeSweepTone(220f, 440f, 0.6f, 0.3f));
        clipStop = LoadOrFallback("Audio/Bleep1", MakeTone(180f, 0.08f, 0.6f));
        clipClick = LoadOrFallback("Audio/Bleep1", MakeTone(880f, 0.04f, 0.5f));
        clipWinSmall = LoadOrFallback("Audio/Beep", MakeChord(new[] { 523f, 659f }, 0.4f, 0.5f));
        clipWinMedium = LoadOrFallback("Audio/Beep", MakeChord(new[] { 523f, 659f, 784f }, 0.7f, 0.6f));
        clipWinJackpot = LoadOrFallback("Audio/Beep", MakeChord(new[] { 523f, 659f, 784f, 1047f }, 1.4f, 0.8f));
        // POLISH #7: Background music — pakai casinoSound kalau ada, fallback ke synth pad
        clipMusic = LoadOrFallback("Audio/casinoSound", MakeLoopableBgMusic());
    }

    private AudioClip LoadOrFallback(string path, AudioClip fallback)
    {
        var loaded = Resources.Load<AudioClip>(path);
        return loaded != null ? loaded : fallback;
    }

    private AudioClip MakeTone(float freq, float duration, float amp)
    {
        int sr = 44100; int n = (int)(sr * duration);
        var clip = AudioClip.Create("tone", n, 1, sr, false);
        var data = new float[n];
        for (int i = 0; i < n; i++) { float t = (float)i / sr; float env = Mathf.Min(1f, t * 20f) * Mathf.Min(1f, (duration - t) * 20f); data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * amp * env; }
        clip.SetData(data, 0); return clip;
    }

    private AudioClip MakeSweepTone(float startFreq, float endFreq, float duration, float amp)
    {
        int sr = 44100; int n = (int)(sr * duration);
        var clip = AudioClip.Create("sweep", n, 1, sr, false);
        var data = new float[n]; float phase = 0f;
        for (int i = 0; i < n; i++) { float t = (float)i / sr; float f = Mathf.Lerp(startFreq, endFreq, t / duration); phase += 2f * Mathf.PI * f / sr; float env = Mathf.Min(1f, t * 10f) * Mathf.Min(1f, (duration - t) * 10f); data[i] = Mathf.Sin(phase) * amp * env; }
        clip.SetData(data, 0); return clip;
    }

    private AudioClip MakeChord(float[] freqs, float duration, float amp)
    {
        int sr = 44100; int n = (int)(sr * duration);
        var clip = AudioClip.Create("chord", n, 1, sr, false);
        var data = new float[n];
        for (int i = 0; i < n; i++) { float t = (float)i / sr; float v = 0f; foreach (var f in freqs) v += Mathf.Sin(2f * Mathf.PI * f * t); v /= freqs.Length; float env = Mathf.Min(1f, t * 5f) * Mathf.Min(1f, (duration - t) * 5f); data[i] = v * amp * env; }
        clip.SetData(data, 0); return clip;
    }

    // POLISH #7: Looping ambient casino pad — 8 detik loop seamless
    private AudioClip MakeLoopableBgMusic()
    {
        int sr = 44100;
        float duration = 8f;
        int n = (int)(sr * duration);
        var clip = AudioClip.Create("bgmusic", n, 1, sr, false);
        var data = new float[n];

        // Cycle-aligned frequencies (integer cycles per 8s loop) untuk seamless loop
        // f harus = N/8 Hz untuk integer N
        // C3=131, E3=164, G3=196, C4=262, E4=329 (semua approx integer cycles per 8s)
        float[] freqs = { 130.5f, 164.0f, 196.0f, 261.5f, 329.5f };

        for (int i = 0; i < n; i++)
        {
            float t = (float)i / sr;
            float v = 0f;
            foreach (var f in freqs) v += Mathf.Sin(2f * Mathf.PI * f * t);
            v /= freqs.Length;

            // Slow LFO 0.25 Hz untuk subtle movement (2 cycle per 8s loop)
            float lfo = 0.7f + 0.3f * Mathf.Sin(2f * Mathf.PI * 0.25f * t);

            // Tambahan low bass drone
            float bass = Mathf.Sin(2f * Mathf.PI * 65.5f * t) * 0.3f;

            data[i] = (v + bass) * 0.12f * lfo;
        }

        clip.SetData(data, 0);
        return clip;
    }

    public void PlaySpin() { if (clipSpin != null) sfxSource.PlayOneShot(clipSpin); }
    public void PlayStop() { if (clipStop != null) sfxSource.PlayOneShot(clipStop, 0.7f); }
    public void PlayClick() { if (clipClick != null) sfxSource.PlayOneShot(clipClick, 0.6f); }
    public void PlayWin(int tier)
    {
        AudioClip c = tier switch { 0 => clipWinSmall, 1 => clipWinMedium, _ => clipWinJackpot };
        if (c != null) sfxSource.PlayOneShot(c);
        // POLISH #7: Ducking music saat win sound play
        if (tier >= 1) StartCoroutine(DuckMusicRoutine(1.5f, 0.35f));
    }

    private System.Collections.IEnumerator DuckMusicRoutine(float duration, float duckedRatio)
    {
        if (musicSource == null) yield break;
        float baseVolume = SaveSystem.Muted ? 0f : SaveSystem.Volume * 0.35f;
        musicSource.volume = baseVolume * duckedRatio;
        yield return new WaitForSeconds(duration);
        float t = 0f, fadeTime = 0.4f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(baseVolume * duckedRatio, baseVolume, t / fadeTime);
            yield return null;
        }
        musicSource.volume = baseVolume;
    }

    public void SetVolume(float v) { SaveSystem.Volume = v; ApplyVolume(); }
    public void SetMuted(bool m) { SaveSystem.Muted = m; ApplyVolume(); }
    public float GetVolume() => SaveSystem.Volume;
    public bool IsMuted() => SaveSystem.Muted;

    private void ApplyVolume()
    {
        float v = SaveSystem.Muted ? 0f : SaveSystem.Volume;
        if (sfxSource != null) sfxSource.volume = v;
        if (sfxLayerSource != null) sfxLayerSource.volume = v * 0.8f;
        if (musicSource != null) musicSource.volume = v * 0.35f;
    }
}
