using UnityEditor;
using UnityEditor.XR.ARCore;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;

/// <summary>
/// Konfigurasi otomatis Player Settings dan XR Plug-in Management
/// sesuai spesifikasi: Android API 29+, IL2CPP, ARM64, ARCore required.
/// </summary>
public static class ProjectConfigurator
{
    public static void Configure()
    {
        PlayerSettings.companyName = "NadaSunda";
        PlayerSettings.productName = "NadaSunda AR";
        PlayerSettings.bundleVersion = "1.0";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.nadasunda.ar");
        PlayerSettings.Android.bundleVersionCode = 1;

        PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)29;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

        // ARCore paling stabil di OpenGLES3.
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });

        KonfigurasiXR();
        KonfigurasiIkonAplikasi();

        AssetDatabase.SaveAssets();
        Debug.Log("[Setup] Konfigurasi Player Settings & XR selesai.");
    }

    /// <summary>
    /// Memasang logo aplikasi (desain final dari user, Assets/UI/Icons/AppIcon.png)
    /// sebagai ikon peluncur Android di seluruh ukuran yang diminta Unity —
    /// satu sumber 1024x1024 diperkecil otomatis oleh Unity ke tiap ukuran.
    /// </summary>
    private static void KonfigurasiIkonAplikasi()
    {
        const string pathIkon = "Assets/UI/Icons/AppIcon.png";
        Texture2D ikon = AssetDatabase.LoadAssetAtPath<Texture2D>(pathIkon);
        if (ikon == null)
        {
            Debug.LogWarning($"[Setup] Ikon aplikasi tidak ditemukan: {pathIkon}");
            return;
        }

        int[] ukuran = PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.Android);
        Texture2D[] daftarIkon = new Texture2D[ukuran.Length];
        for (int i = 0; i < ukuran.Length; i++) daftarIkon[i] = ikon;
        PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, daftarIkon);
    }

    /// <summary>Mengaktifkan ARCore loader pada XR Plug-in Management untuk Android.</summary>
    private static void KonfigurasiXR()
    {
        if (!AssetDatabase.IsValidFolder("Assets/XR"))
        {
            AssetDatabase.CreateFolder("Assets", "XR");
        }

        XRGeneralSettingsPerBuildTarget perBuildTarget;
        EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out perBuildTarget);
        if (perBuildTarget == null)
        {
            perBuildTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            AssetDatabase.CreateAsset(perBuildTarget, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perBuildTarget, true);
        }

        XRGeneralSettings androidSettings = perBuildTarget.SettingsForBuildTarget(BuildTargetGroup.Android);
        if (androidSettings == null)
        {
            androidSettings = ScriptableObject.CreateInstance<XRGeneralSettings>();
            androidSettings.name = "Android Settings";
            AssetDatabase.AddObjectToAsset(androidSettings, perBuildTarget);
            perBuildTarget.SetSettingsForBuildTarget(BuildTargetGroup.Android, androidSettings);
        }

        if (androidSettings.Manager == null)
        {
            XRManagerSettings manager = ScriptableObject.CreateInstance<XRManagerSettings>();
            manager.name = "Android Providers";
            AssetDatabase.AddObjectToAsset(manager, perBuildTarget);
            androidSettings.Manager = manager;
        }

        bool sukses = XRPackageMetadataStore.AssignLoader(
            androidSettings.Manager,
            "UnityEngine.XR.ARCore.ARCoreLoader",
            BuildTargetGroup.Android);
        if (!sukses)
        {
            Debug.LogWarning("[Setup] AssignLoader ARCore mengembalikan false.");
        }

        // ARCore wajib ada di perangkat (Google Play hanya menawarkan ke perangkat yang didukung).
        ARCoreSettings arcoreSettings = ARCoreSettings.currentSettings;
        if (arcoreSettings == null)
        {
            arcoreSettings = ScriptableObject.CreateInstance<ARCoreSettings>();
            AssetDatabase.CreateAsset(arcoreSettings, "Assets/XR/ARCoreSettings.asset");
            ARCoreSettings.currentSettings = arcoreSettings;
        }
        arcoreSettings.requirement = ARCoreSettings.Requirement.Required;
        EditorUtility.SetDirty(arcoreSettings);
        EditorUtility.SetDirty(perBuildTarget);
    }
}
