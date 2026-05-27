// GameGacor WebGL build helpers — two entry points:
//
// 1. SetupWebGLSettings   — called by Unity Cloud Build as Pre-Export method.
//    Sets WebGL player settings (Brotli, template, IL2CPP) then RETURNS so the
//    Cloud Build's own BuildPipeline.BuildPlayer runs with these settings applied.
//
// 2. BuildWebGL           — full batch build (clones + builds + exits). Used when
//    invoking Unity locally from CLI:
//      Unity.exe -batchmode -nographics -projectPath <path> \
//        -executeMethod GameGacorWebGLBuild.BuildWebGL -quit
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class GameGacorWebGLBuild
{
    // ============================================================
    // PRE-EXPORT METHOD (Unity Cloud Build)
    // Sets settings only — does NOT call BuildPipeline.BuildPlayer.
    // ============================================================
    public static void SetupWebGLSettings()
    {
        ApplyWebGLPlayerSettings();
        Debug.Log("[GG-Build] SetupWebGLSettings DONE — settings applied. Cloud Build will run BuildPipeline.BuildPlayer with these.");
    }

    // ============================================================
    // BATCH BUILD (local CLI)
    // Sets settings AND runs the build, then exits.
    // ============================================================
    public static void BuildWebGL()
    {
        var scenes = new System.Collections.Generic.List<string>();
        foreach (var s in EditorBuildSettings.scenes)
            if (s.enabled) scenes.Add(s.path);

        if (scenes.Count == 0 && Directory.Exists("Assets/Scenes")) {
            var found = Directory.GetFiles("Assets/Scenes", "*.unity", SearchOption.AllDirectories);
            scenes.AddRange(found);
            Debug.Log($"[GG-Build] EditorBuildSettings had no scenes; using {found.Length} scenes from Assets/Scenes/");
        }

        var outDir = System.Environment.GetEnvironmentVariable("GG_WEBGL_OUT");
        if (string.IsNullOrEmpty(outDir)) outDir = "WebGL_Build";
        if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

        ApplyWebGLPlayerSettings();

        var opts = new BuildPlayerOptions {
            scenes = scenes.ToArray(),
            locationPathName = outDir,
            target = BuildTarget.WebGL,
            targetGroup = BuildTargetGroup.WebGL,
            options = BuildOptions.None,
        };

        Debug.Log($"[GG-Build] Starting WebGL build → {Path.GetFullPath(outDir)}");
        BuildReport report = BuildPipeline.BuildPlayer(opts);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded) {
            Debug.Log($"[GG-Build] SUCCESS — size={summary.totalSize} bytes, time={summary.totalTime}");
            EditorApplication.Exit(0);
        } else {
            Debug.LogError($"[GG-Build] FAILED — result={summary.result}, errors={summary.totalErrors}");
            EditorApplication.Exit(1);
        }
    }

    // ============================================================
    // SHARED — applied by both entry points.
    // ============================================================
    private static void ApplyWebGLPlayerSettings()
    {
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.runInBackground = true;
        PlayerSettings.SplashScreen.showUnityLogo = false;

        // Try the GameGacor template; fall back to Default if it's missing in the project.
        var templates = new[] { "PROJECT:GameGacor", "APPLICATION:Default" };
        foreach (var t in templates) {
            PlayerSettings.WebGL.template = t;
            Debug.Log($"[GG-Build] tried WebGL template '{t}' → resolved '{PlayerSettings.WebGL.template}'");
            if (PlayerSettings.WebGL.template == t) break;
        }
    }
}
