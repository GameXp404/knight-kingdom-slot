using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildScript
{
    private const string OutputFolder = "BUILD";
    private const string ExeName = "KnightKingdomSlot.exe";
    private const string IconAssetPath = "Assets/Resources/Symbols/knight.png";

    public static void BuildWindowsWithSetup()
    {
        EnsureTMPEssentials();
        SetIconInternal();
        ConfigureWindowedMode();
        EnsureShaderIncluded("UI/LuminanceAlpha");
        BuildWindows();
    }

    private static void EnsureShaderIncluded(string shaderName)
    {
        var shader = Shader.Find(shaderName);
        if (shader == null) { Debug.LogWarning($"[Shader] Not found: {shaderName}"); return; }
        var settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
        if (settings == null || settings.Length == 0) { Debug.LogWarning("[Shader] GraphicsSettings not loadable"); return; }
        var so = new SerializedObject(settings[0]);
        var arr = so.FindProperty("m_AlwaysIncludedShaders");
        for (int i = 0; i < arr.arraySize; i++) {
            if (arr.GetArrayElementAtIndex(i).objectReferenceValue == shader) { Debug.Log($"[Shader] Already included: {shaderName}"); return; }
        }
        arr.arraySize++;
        arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = shader;
        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        Debug.Log($"[Shader] Added to AlwaysIncluded: {shaderName}");
    }

    private static void EnsureTMPEssentials()
    {
        if (Directory.Exists(Path.Combine(Application.dataPath, "TextMesh Pro"))) { Debug.Log("[TMP] Already exists."); return; }
        string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string cacheRoot = Path.Combine(root, "Library", "PackageCache");
        if (!Directory.Exists(cacheRoot)) { Debug.LogWarning("[TMP] PackageCache not found"); return; }
        var files = Directory.GetFiles(cacheRoot, "TMP Essential Resources.unitypackage", SearchOption.AllDirectories);
        if (files.Length == 0) { Debug.LogWarning("[TMP] Package not found"); return; }
        Debug.Log($"[TMP] Importing: {files[0]}");
        AssetDatabase.ImportPackage(files[0], false);
        AssetDatabase.Refresh();
    }

    private static void SetIconInternal()
    {
        if (!File.Exists(IconAssetPath)) { Debug.LogWarning($"[Icon] Not found: {IconAssetPath}"); return; }
        var importer = AssetImporter.GetAtPath(IconAssetPath) as TextureImporter;
        if (importer != null && !importer.isReadable) { importer.isReadable = true; importer.SaveAndReimport(); }
        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconAssetPath);
        if (icon != null)
        {
            PlayerSettings.SetIcons(NamedBuildTarget.Standalone, new[] { icon }, IconKind.Application);
            PlayerSettings.companyName = "Coloknet";
            PlayerSettings.productName = "KnightKingdomSlot";
            AssetDatabase.SaveAssets();
            Debug.Log("[Icon] Set from knight.png");
        }
    }

    private static void ConfigureWindowedMode()
    {
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.defaultIsNativeResolution = false;
        PlayerSettings.allowFullscreenSwitch = true;
        PlayerSettings.runInBackground = true;
        Debug.Log("[Window] Windowed 1280x720, resizable.");
    }

    [MenuItem("KnightKingdom/Build Windows %#b")]
    public static void BuildWindowsWithSetupMenu() { BuildWindowsWithSetup(); }

    [MenuItem("KnightKingdom/Build Windows Only")]
    public static void BuildWindows()
    {
        string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string buildDir = Path.Combine(root, OutputFolder);
        if (!Directory.Exists(buildDir)) Directory.CreateDirectory(buildDir);
        string exePath = Path.Combine(buildDir, ExeName);
        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/SampleScene.unity" },
            locationPathName = exePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };
        Debug.Log($"[Build] Building to: {exePath}");
        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[Build] SUCCESS! Size: {(report.summary.totalSize / (1024f * 1024f)):F1} MB");
            EditorUtility.RevealInFinder(exePath);
        }
        else { Debug.LogError($"[Build] FAILED: {report.summary.result}"); }
    }

    [MenuItem("KnightKingdom/Open Build Folder")]
    public static void OpenBuildFolder()
    {
        string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string buildDir = Path.Combine(root, OutputFolder);
        if (!Directory.Exists(buildDir)) Directory.CreateDirectory(buildDir);
        EditorUtility.RevealInFinder(buildDir);
    }
}
