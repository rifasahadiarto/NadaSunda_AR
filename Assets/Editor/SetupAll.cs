using UnityEditor;
using UnityEngine;

/// <summary>
/// Titik masuk otomasi headless: -executeMethod SetupAll.Run
/// Menjalankan konfigurasi project, pembangunan prefab, dan pembangunan scene.
/// </summary>
public static class SetupAll
{
    public static void Run()
    {
        try
        {
            ProjectConfigurator.Configure();
            PrefabBuilder.BuildAll();
            SceneBuilder.BuildAll();
            Debug.Log("[Setup] ===== SELURUH SETUP SELESAI TANPA ERROR =====");
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Setup] GAGAL: " + e);
            EditorApplication.Exit(1);
        }
    }
}
