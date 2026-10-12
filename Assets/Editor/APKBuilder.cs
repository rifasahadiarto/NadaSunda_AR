using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Build APK headless: -executeMethod APKBuilder.Build
/// </summary>
public static class APKBuilder
{
    public static void Build()
    {
        BuildPlayerOptions opsi = new BuildPlayerOptions
        {
            scenes = new[]
            {
                "Assets/Scenes/MainMenu.unity",
                "Assets/Scenes/ARScene.unity"
            },
            locationPathName = "Builds/NadaSundaAR.apk",
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        BuildReport laporan = BuildPipeline.BuildPlayer(opsi);
        BuildSummary ringkasan = laporan.summary;

        if (ringkasan.result == BuildResult.Succeeded)
        {
            Debug.Log($"[Build] SUKSES: {ringkasan.outputPath} ({ringkasan.totalSize / (1024 * 1024)} MB, " +
                      $"durasi {ringkasan.totalTime.TotalMinutes:F1} menit)");
            EditorApplication.Exit(0);
        }
        else
        {
            Debug.LogError($"[Build] GAGAL: {ringkasan.result}, error: {ringkasan.totalErrors}");
            EditorApplication.Exit(1);
        }
    }
}
