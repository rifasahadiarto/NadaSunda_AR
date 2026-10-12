using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Menerapkan ikon dari asset pack "Game GUI Vol1/WoodenBlocks" ke tombol-
/// tombol overlay/navigasi tertentu di MainMenu.unity dan ARScene.unity.
///
/// Tombol target diubah jadi gaya ikon-only: sprite WoodenBlocks dipasang
/// sebagai Image tombol, dan child "Label" (teks lama) dinonaktifkan.
/// Tombol besar berlabel teks di daftar menu (Mulai AR/Panduan/Tentang/
/// Keluar, kartu instrumen) sengaja TIDAK disentuh.
///
/// Catatan: SceneBuilder.cs membangun ulang kedua scene dari nol. Kalau
/// SceneBuilder dijalankan lagi setelah ini, perubahan ikon di sini akan
/// tertimpa dan perlu dijalankan ulang.
/// </summary>
public static class WoodenIconApplier
{
    private const string IconFolder = "Assets/Game GUI Vol1/WoodenBlocks/PNG/";

    private struct TargetTombol
    {
        public readonly string path;
        public readonly string iconName;

        public TargetTombol(string path, string iconName)
        {
            this.path = path;
            this.iconName = iconName;
        }
    }

    private static readonly TargetTombol[] TargetMainMenu =
    {
        new TargetTombol("PanelMenuUtama (T-01)/TombolPanduan", "QuestionMark"),
        new TargetTombol("PanelPanduan (T-03)/TombolKembali", "Backward"),
        new TargetTombol("PanelTentang (T-04)/TombolKembali", "Backward"),
    };

    private static readonly TargetTombol[] TargetARScene =
    {
        new TargetTombol("PanelOverlayInstrumen (T-02D)/TombolInfo", "Info"),
        new TargetTombol("PanelOverlayInstrumen (T-02D)/TombolTambah", "Plus"),
        new TargetTombol("PanelOverlayInstrumen (T-02D)/TombolHapus", "Minus"),
        new TargetTombol("TombolKembali", "Backward"),
        new TargetTombol("PopupInfo/Kartu/TombolTutup", "Cancel"),
        new TargetTombol("PanelErrorE02 (E-02)/Kartu/Tombol1", "Settings"),
        new TargetTombol("PanelErrorE02 (E-02)/Kartu/Tombol2", "Restart"),
        new TargetTombol("PanelErrorE03 (E-03)/Kartu/Tombol1", "Restart"),
    };

    [MenuItem("NadaSunda/Terapkan Ikon WoodenBlocks")]
    public static void Terapkan()
    {
        ProsesScene("Assets/Scenes/MainMenu.unity", TargetMainMenu);
        ProsesScene("Assets/Scenes/ARScene.unity", TargetARScene);
        Debug.Log("Selesai menerapkan ikon WoodenBlocks ke kedua scene.");
    }

    private static void ProsesScene(string scenePath, TargetTombol[] daftar)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        GameObject canvas = CariRootBernama(scene, "Canvas");
        if (canvas == null)
        {
            Debug.LogError($"Canvas tidak ditemukan di {scenePath}, dilewati.");
            return;
        }

        int berhasil = 0;
        foreach (TargetTombol target in daftar)
        {
            Transform tombol = canvas.transform.Find(target.path);
            if (tombol == null)
            {
                Debug.LogWarning($"[{scenePath}] Tombol tidak ditemukan: {target.path}");
                continue;
            }

            if (TerapkanIkon(tombol, target.iconName)) berhasil++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[{scenePath}] {berhasil}/{daftar.Length} tombol berhasil diterapkan.");
    }

    private static bool TerapkanIkon(Transform tombol, string iconName)
    {
        Image image = tombol.GetComponent<Image>();
        if (image == null)
        {
            Debug.LogWarning($"Komponen Image tidak ditemukan pada {tombol.name}.");
            return false;
        }

        string spritePath = $"{IconFolder}{iconName}.png";
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (sprite == null)
        {
            Debug.LogWarning($"Sprite tidak ditemukan: {spritePath}");
            return false;
        }

        image.sprite = sprite;
        image.type = Image.Type.Simple;
        // Sprite ikon sudah punya warnanya sendiri; warna tint lama (mis.
        // aksen emas) direset ke putih supaya ikon tidak ikut ternoda tint.
        image.color = Color.white;

        Transform label = tombol.Find("Label");
        if (label != null) label.gameObject.SetActive(false);

        return true;
    }

    private static GameObject CariRootBernama(Scene scene, string nama)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == nama) return root;
        }
        return null;
    }
}
