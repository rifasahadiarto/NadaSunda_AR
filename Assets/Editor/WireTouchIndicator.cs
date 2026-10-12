using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Wiring otomatis komponen TouchIndicatorEffect ke TouchRaycaster di
/// ARScene, plus mastiin shader "Sprites/Default" masuk Always Included
/// Shaders biar riak bawaan gak ke-strip di build.
/// Jalanin headless: -executeMethod WireTouchIndicator.Run
/// </summary>
public static class WireTouchIndicator
{
    private const string PATH_AR_SCENE = "Assets/Scenes/ARScene.unity";

    public static void Run()
    {
        PastikanShaderRiakIkutBuild();
        SambungkanKeScene();
        AssetDatabase.SaveAssets();
        Debug.Log("[WireTouchIndicator] SELESAI");
        EditorApplication.Exit(0);
    }

    private static void PastikanShaderRiakIkutBuild()
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            Debug.LogWarning("[WireTouchIndicator] Shader 'Sprites/Default' gak ketemu, dilewat.");
            return;
        }

        Object graphicsSettings = AssetDatabase.LoadAllAssetsAtPath(
            "ProjectSettings/GraphicsSettings.asset")[0];
        var so = new SerializedObject(graphicsSettings);
        SerializedProperty daftar = so.FindProperty("m_AlwaysIncludedShaders");

        for (int i = 0; i < daftar.arraySize; i++)
        {
            if (daftar.GetArrayElementAtIndex(i).objectReferenceValue == shader)
            {
                Debug.Log("[WireTouchIndicator] 'Sprites/Default' sudah ada di Always Included Shaders.");
                return;
            }
        }

        daftar.arraySize++;
        daftar.GetArrayElementAtIndex(daftar.arraySize - 1).objectReferenceValue = shader;
        so.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log("[WireTouchIndicator] 'Sprites/Default' ditambahkan ke Always Included Shaders.");
    }

    private static void SambungkanKeScene()
    {
        Scene scene = EditorSceneManager.OpenScene(PATH_AR_SCENE, OpenSceneMode.Single);

        TouchRaycaster raycaster = Object.FindObjectOfType<TouchRaycaster>();
        if (raycaster == null)
        {
            Debug.LogError("[WireTouchIndicator] TouchRaycaster gak ketemu di ARScene.");
            EditorApplication.Exit(1);
            return;
        }

        TouchIndicatorEffect indikator = raycaster.GetComponent<TouchIndicatorEffect>();
        if (indikator == null)
        {
            indikator = raycaster.gameObject.AddComponent<TouchIndicatorEffect>();
            Debug.Log("[WireTouchIndicator] TouchIndicatorEffect ditambahkan ke objek '" +
                      raycaster.gameObject.name + "'.");
        }

        var so = new SerializedObject(raycaster);
        SerializedProperty prop = so.FindProperty("touchIndicator");
        if (prop == null)
        {
            Debug.LogError("[WireTouchIndicator] Field 'touchIndicator' gak ada di TouchRaycaster.");
            EditorApplication.Exit(1);
            return;
        }

        prop.objectReferenceValue = indikator;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[WireTouchIndicator] touchIndicator ke-assign & ARScene disimpan.");
    }
}
