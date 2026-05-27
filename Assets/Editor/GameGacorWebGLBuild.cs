// Batch-mode WebGL build entry point for GameGacor lobby integration.
// Invoke via:
//   "Unity.exe" -batchmode -nographics -projectPath <path> \
//      -executeMethod GameGacorWebGLBuild.BuildWebGL \
//      -logFile build_webgl.log -quit
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class GameGacorWebGLBuild
{
    public static void BuildWebGL()
    {
        // Resolve scenes from EditorBuildSettings; fall back to scanning Assets/Scenes if empty.
        var scenes = new System.Collections.Generic.List<string>();
        foreach (var s in EditorBuildSettings.scenes)
            if (s.enabled) scenes.Add(s.path);

        if (scenes.Count == 0) {
            var found = Directory.GetFiles("Assets/Scenes", "*.unity", SearchOption.AllDirectories);
            scenes.AddRange(found);
            Debug.Log($"[GG-Build] EditorBuildSettings had no scenes; using {found.Length} scenes from Assets/Scenes/");
        }

        var outDir = System.Environment.GetEnvironmentVariable("GG_WEBGL_OUT");
        if (string.IsNullOrEmpty(outDir)) outDir = "WebGL_Build";
        if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

        // WebGL-specific player settings — keep them explicit so the batch build is reproducible.
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.runInBackground = true;
        PlayerSettings.SplashScreen.showUnityLogo = false;

        // Pick the GameGacor template if it's been imported into the project.
        var templates = new[] { "PROJECT:GameGacor", "APPLICATION:Default" };
        foreach (var t in templates) {
            PlayerSettings.WebGL.template = t;
            Debug.Log($"[GG-Build] tried WebGL template '{t}' → resolved '{PlayerSettings.WebGL.template}'");
            if (PlayerSettings.WebGL.template == t) break;
        }

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
}
