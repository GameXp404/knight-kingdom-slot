// GameGacor WebGL build helpers — Pre-Export method for Unity Cloud Build.
// Applies WebGL player settings (Brotli, template, decompression fallback)
// then returns so Unity Cloud Build's own BuildPipeline.BuildPlayer runs
// with these settings applied.
using UnityEditor;
using UnityEngine;

public static class GameGacorWebGLBuild
{
    // Called by Unity Cloud Build as "Pre-Export method" before the WebGL build.
    public static void SetupWebGLSettings()
    {
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.runInBackground = true;
        PlayerSettings.SplashScreen.showUnityLogo = false;

        // Try the GameGacor template; Unity silently falls back to "APPLICATION:Default" if missing.
        var oldTemplate = PlayerSettings.WebGL.template;
        PlayerSettings.WebGL.template = "PROJECT:GameGacor";
        if (PlayerSettings.WebGL.template != "PROJECT:GameGacor") {
            Debug.LogWarning("[GG-Build] GameGacor template not found, keeping previous: " + oldTemplate);
            PlayerSettings.WebGL.template = oldTemplate;
        } else {
            Debug.Log("[GG-Build] WebGL template set to PROJECT:GameGacor");
        }

        Debug.Log("[GG-Build] WebGL settings applied (Brotli compression, runInBackground, no Unity logo).");
    }
}
