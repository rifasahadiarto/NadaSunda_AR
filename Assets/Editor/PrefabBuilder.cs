using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Membangun prefab instrumen dari FBX secara otomatis:
/// normalisasi skala ke ukuran dunia nyata, pivot di dasar objek,
/// collider untuk Physics.Raycast, komponen BilahSaron pada 14 bilah,
/// InstrumenRotator pada root, material PBR Saron, dan prefab
/// visualisasi bidang datar untuk ARPlaneManager.
/// </summary>
public static class PrefabBuilder
{
    // Urutan nada naik dalam laras degung: la, ti, na, mi, da (da tertinggi).
    private static readonly string[] NamaNadaDegung =
    {
        "La (rendah)", "Ti (rendah)", "Na (rendah)", "Mi (rendah)", "Da (rendah)",
        "La (tengah)", "Ti (tengah)", "Na (tengah)", "Mi (tengah)", "Da (tengah)",
        "La (tinggi)", "Ti (tinggi)", "Na (tinggi)", "Mi (tinggi)"
    };

    public static void BuildAll()
    {
        Material materialSaron = BuatMaterialSaron();

        // Goong logam mengkilap (metallic diturunkan dari 0.85 karena scene
        // AR tidak punya refleksi lingkungan — permukaan full-metal tanpa
        // refleksi justru tampak hitam); Angklung/Kendang/Kacapi bahan
        // bambu-kayu (non-logam, agak doff).
        Material materialGoong = BuatMaterialPBR("Goong",
            "Assets/Models/Goong/Textures/goong_", 0.5f, 0.6f);
        Material materialKacapi = BuatMaterialPBR("Kacapi",
            "Assets/Models/Kacapi/Textures/kacapi_", 0.1f, 0.35f);
        Material materialAngklung = BuatMaterialPBR("Angklung",
            "Assets/Models/Angklung/Textures/angklung_", 0f, 0.3f);
        Material materialKendang = BuatMaterialPBR("Kendang",
            "Assets/Models/Kendang/Textures/kendang_", 0f, 0.35f);

        BuatPrefabSederhana("Angklung", "Assets/Models/Angklung/Angklung.fbx", 0.6f, true, materialAngklung);
        BuatPrefabKendang(materialKendang);
        BuatPrefabSaron(materialSaron);
        BuatPrefabGoong(materialGoong);
        BuatPrefabKacapi(materialKacapi);
        BuatPrefabBidangDatar();

        AssetDatabase.SaveAssets();
        Debug.Log("[Setup] Semua prefab selesai dibangun.");
    }

    // ---------- Instrumen ----------

    /// <param name="model">
    /// Root objek FBX yang di-instantiate (child tunggal dari root prefab).
    /// Diekspos lewat out (bukan diasumsikan GetChild(0) di pemanggil)
    /// karena beberapa pemanggil (efek visual Angklung/Goong) perlu
    /// menganimasikan mesh visual TANPA menyentuh transform root — root
    /// dipakai InstrumenRotator (rotasi manual) dan InstrumenController
    /// (skala formasi), jadi transform terpisah ini mencegah dua sistem
    /// menulis field yang sama.
    /// </param>
    private static GameObject SiapkanRootInstrumen(string nama, string pathFbx, float ukuranTarget, bool pakaiTinggi,
        out GameObject model)
    {
        GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(pathFbx);
        if (fbx == null)
        {
            throw new FileNotFoundException("FBX tidak ditemukan: " + pathFbx);
        }

        GameObject root = new GameObject(nama);
        model = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
        model.transform.SetParent(root.transform, false);
        model.transform.localPosition = Vector3.zero;

        // Normalisasi skala ke ukuran dunia nyata (meter).
        Bounds b = HitungBounds(root);
        float dimensiSekarang = pakaiTinggi ? b.size.y : Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
        if (dimensiSekarang > 0.0001f)
        {
            model.transform.localScale = model.transform.localScale * (ukuranTarget / dimensiSekarang);
        }

        // Geser sehingga pivot root tepat di dasar-tengah objek (duduk di bidang datar).
        b = HitungBounds(root);
        model.transform.position -= new Vector3(b.center.x, b.min.y, b.center.z);

        // InstrumenRotator TIDAK dipasang lagi: eksplorasi bentuk sekarang
        // dilakukan dengan menggerakkan device fisik mengelilingi objek
        // (sifat asli AR), bukan drag-to-rotate di layar.
        Debug.Log($"[Setup] {nama}: dimensi akhir {HitungBounds(root).size}");
        return root;
    }

    private static void BuatPrefabSederhana(string nama, string pathFbx, float ukuranTarget, bool pakaiTinggi,
        Material material = null)
    {
        GameObject root = SiapkanRootInstrumen(nama, pathFbx, ukuranTarget, pakaiTinggi, out GameObject model);
        TerapkanMaterial(root, material);

        // Satu BoxCollider seluruh badan; sentuhan di mana pun memainkan suara utama.
        Bounds b = HitungBounds(root);
        BoxCollider box = root.AddComponent<BoxCollider>();
        box.center = b.center;
        box.size = b.size;

        if (nama == "Angklung")
        {
            // Efek goyang murni visual (lihat AngklungShakeEffect), dipicu
            // TouchRaycaster bersamaan audio — menyasar mesh visual (model),
            // bukan root, agar tidak bentrok dengan InstrumenRotator.
            AngklungShakeEffect shake = root.AddComponent<AngklungShakeEffect>();
            SerializedObject soShake = new SerializedObject(shake);
            soShake.FindProperty("targetVisual").objectReferenceValue = model.transform;
            soShake.ApplyModifiedPropertiesWithoutUndo();
        }

        SimpanPrefab(root, $"Assets/Prefabs/{nama}.prefab");
    }

    private static void BuatPrefabSaron(Material materialSaron)
    {
        GameObject root = SiapkanRootInstrumen("Saron", "Assets/Models/Saron/Saron_14Bilah.fbx", 0.8f, false, out _);

        if (materialSaron != null)
        {
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>())
            {
                Material[] mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = materialSaron;
                r.sharedMaterials = mats;
            }
        }

        int jumlahBilah = 0;
        foreach (Transform t in root.GetComponentsInChildren<Transform>())
        {
            if (!t.name.StartsWith("Bilah_")) continue;

            string angka = t.name.Substring("Bilah_".Length);
            if (!int.TryParse(angka, out int index)) continue;

            BilahSaron bilah = t.gameObject.AddComponent<BilahSaron>();
            bilah.indexNada = index;
            bilah.namaNada = index < NamaNadaDegung.Length ? NamaNadaDegung[index] : "Nada " + index;
            bilah.audioClipNada = AssetDatabase.LoadAssetAtPath<AudioClip>(
                $"Assets/Audio/Saron/Nada_{index:00}.wav");
            if (bilah.audioClipNada == null)
            {
                Debug.LogWarning($"[Setup] AudioClip Nada_{index:00}.wav tidak ditemukan.");
            }

            // Efek pantul murni visual (lihat SaronBounceEffect), dipicu
            // TouchRaycaster bersamaan audio nada bilah ini. Ditempel di
            // bilah itu sendiri (bukan root) karena tiap bilah bergerak
            // independen; targetVisual menyasar transform-nya sendiri.
            SaronBounceEffect pantul = t.gameObject.AddComponent<SaronBounceEffect>();
            SerializedObject soPantul = new SerializedObject(pantul);
            soPantul.FindProperty("targetVisual").objectReferenceValue = t;
            soPantul.ApplyModifiedPropertiesWithoutUndo();

            TambahBoxColliderDariMesh(t.gameObject);
            jumlahBilah++;
        }

        if (jumlahBilah != 14)
        {
            Debug.LogWarning($"[Setup] Saron: ditemukan {jumlahBilah} bilah (diharapkan 14). " +
                             "Periksa penamaan child object Bilah_00..Bilah_13 pada FBX.");
        }

        // Bagian rangka (non-bilah) SENGAJA tidak diberi collider apa pun:
        // menyentuh rangka tidak boleh membunyikan apa-apa, hanya tiap
        // bilah yang boleh. Pemilihan instrumen ini saat IDLE (menyentuh
        // di mana pun pada badannya) ditangani terpisah lewat pengecekan
        // bounding box di InstrumenController.CariInstrumenPadaRay, jadi
        // rangka tidak perlu collider untuk itu.

        SimpanPrefab(root, "Assets/Prefabs/Saron.prefab");
    }

    /// <summary>
    /// Kendang: DUA zona sentuh sesuai cara main asli — gedug (ujung
    /// membran besar, nada bass) dan kempyang (ujung kecil, nada tinggi).
    /// Membran kedua ujung sudah dipisah jadi mesh sendiri oleh user di
    /// Blender (child "Kendang_Head_Kanan" / "Kendang_Head_Kiri"), sehingga
    /// collider dibentuk pas mengikuti mesh tersebut (bukan tebakan
    /// bounding-box), lalu diberi komponen BilahSaron sebagai label nada —
    /// pola yang sama dengan bilah Saron/dawai Kacapi.
    /// </summary>
    private static void BuatPrefabKendang(Material materialKendang)
    {
        GameObject root = SiapkanRootInstrumen("Kendang", "Assets/Models/Kendang/Kendang.fbx", 0.6f, false, out _);
        TerapkanMaterial(root, materialKendang);

        // Audio rekaman asli, diikat LANGSUNG ke sisi fisiknya (kanan/kiri)
        // — bukan lagi ditebak lewat ukuran membran seperti sebelumnya,
        // karena rekamannya sendiri sudah dibedakan per sisi saat direkam.
        AudioClip clipKanan = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Kendang_Kanan.wav");
        AudioClip clipKiri = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Kendang_Kiri.wav");

        // Ukuran fisik dua membran dipakai HANYA untuk label nama tampilan
        // (Gedug = membran besar/bass, Kempyang = membran kecil/tinggi),
        // audionya sendiri tetap ikut sisi kanan/kiri masing-masing.
        Transform headA = CariAnakBernama(root.transform, "Kendang_Head_Kanan");
        Transform headB = CariAnakBernama(root.transform, "Kendang_Head_Kiri");
        bool aLebihBesar = LuasPenampang(headA) >= LuasPenampang(headB);

        LabeliZonaKendang(headA, 0, aLebihBesar ? "Gedug (bass)" : "Kempyang (tinggi)", clipKanan);
        LabeliZonaKendang(headB, 1, aLebihBesar ? "Kempyang (tinggi)" : "Gedug (bass)", clipKiri);

        SimpanPrefab(root, "Assets/Prefabs/Kendang.prefab");
    }

    /// <summary>Luas bounds renderer sebuah child (perkiraan ukuran membran).</summary>
    private static float LuasPenampang(Transform t)
    {
        if (t == null) return 0f;
        Renderer r = t.GetComponent<Renderer>();
        if (r == null) return 0f;
        Vector3 s = r.bounds.size;
        return s.x * s.y + s.y * s.z + s.x * s.z;
    }

    /// <summary>
    /// Memasang BoxCollider (mengikuti bounds mesh membran, pola yang sama
    /// dengan bilah Saron) + BilahSaron pada satu child zona kendang yang
    /// sudah dipisah di Blender.
    /// </summary>
    private static void LabeliZonaKendang(Transform zona, int index, string namaNada, AudioClip clip)
    {
        if (zona == null)
        {
            Debug.LogWarning($"[Setup] Kendang: child zona untuk '{namaNada}' tidak ditemukan. " +
                             "Periksa nama object Kendang_Head_Kanan/Kiri pada FBX.");
            return;
        }

        TambahBoxColliderDariMesh(zona.gameObject);
        PertebalCollider(zona.gameObject);

        BilahSaron label = zona.gameObject.AddComponent<BilahSaron>();
        label.indexNada = index;
        label.namaNada = namaNada;
        label.audioClipNada = clip;
        if (clip == null)
        {
            Debug.LogWarning($"[Setup] Kendang: audio zona {namaNada} tidak ditemukan.");
        }
    }

    /// <summary>
    /// Goong: sentuh tunggal secara akustik (satu nada), tetapi collider
    /// hanya dipasang pada "Gong_Pencu" (boss/titik pukul di tengah) — sesuai
    /// cara main asli, goong dipukul tepat di pencu-nya, bukan di badan
    /// mana pun. "Gong_Badan" sengaja TIDAK diberi collider (tidak
    /// menghasilkan bunyi bila disentuh), dua object ini sudah dipisah
    /// user di Blender.
    /// </summary>
    private static void BuatPrefabGoong(Material materialGoong)
    {
        GameObject root = SiapkanRootInstrumen("Goong", "Assets/Models/Goong/Goong.fbx", 0.9f, true, out _);
        TerapkanMaterial(root, materialGoong);

        Transform pencu = CariAnakBernama(root.transform, "Gong_Pencu");
        if (pencu == null)
        {
            Debug.LogWarning("[Setup] Goong: child 'Gong_Pencu' tidak ditemukan. " +
                             "Periksa nama object pada FBX.");
        }
        else
        {
            TambahBoxColliderDariMesh(pencu.gameObject);

            // Efek mengangguk murni visual (lihat GoongPulseEffect), dipicu
            // TouchRaycaster bersamaan audio — menyasar HANYA "Gong_Pencu",
            // bukan seluruh badan gong, karena secara akustik & visual
            // yang bergetar akibat pukulan adalah bagian yang dipukul itu
            // sendiri, bukan seluruh badan. Root tidak dipakai (dipakai
            // InstrumenController untuk skala formasi & InstrumenRotator
            // untuk rotasi manual), jadi pencu ini transform terpisah yang
            // aman dari bentrok.
            GoongPulseEffect pulse = root.AddComponent<GoongPulseEffect>();
            SerializedObject soPulse = new SerializedObject(pulse);
            soPulse.FindProperty("targetVisual").objectReferenceValue = pencu;
            soPulse.ApplyModifiedPropertiesWithoutUndo();
        }

        SimpanPrefab(root, "Assets/Prefabs/Goong.prefab");
    }

    /// <summary>
    /// Kacapi: granular per dawai — child object Dawai_00..Dawai_NN masing-
    /// masing diberi collider dan komponen BilahSaron (komponen yang sama
    /// dengan bilah Saron, karena keduanya sama-sama "bagian bernada":
    /// index + nama + clip — tidak perlu kelas baru di luar Class Diagram).
    /// </summary>
    private static void BuatPrefabKacapi(Material materialKacapi)
    {
        GameObject root = SiapkanRootInstrumen("Kacapi", "Assets/Models/Kacapi/Kacapi.fbx", 1.0f, false, out _);
        TerapkanMaterial(root, materialKacapi);

        int jumlahDawai = 0;
        foreach (Transform t in root.GetComponentsInChildren<Transform>())
        {
            if (!t.name.StartsWith("Dawai_")) continue;

            string angka = t.name.Substring("Dawai_".Length);
            if (!int.TryParse(angka, out int index)) continue;

            BilahSaron dawai = t.gameObject.AddComponent<BilahSaron>();
            dawai.indexNada = index;
            dawai.namaNada = "Dawai " + (index + 1);

            // Audio petikan kacapi asli, satu clip per dawai (20 dawai =
            // 20 clip, Nada_00..Nada_19).
            dawai.audioClipNada = AssetDatabase.LoadAssetAtPath<AudioClip>(
                $"Assets/Audio/Kacapi/Nada_{index:00}.wav");

            TambahBoxColliderDariMesh(t.gameObject);
            PertebalCollider(t.gameObject);
            jumlahDawai++;
        }

        if (jumlahDawai == 0)
        {
            Debug.LogWarning("[Setup] Kacapi: tidak ada child Dawai_XX ditemukan. " +
                             "Periksa penamaan child object pada FBX.");
        }

        // Badan dan bagian non-dawai SENGAJA tidak diberi collider apa pun
        // — sama seperti rangka Saron, menyentuhnya tidak boleh
        // membunyikan apa-apa, hanya tiap dawai yang boleh. Pemilihan
        // instrumen saat IDLE ditangani terpisah lewat pengecekan bounding
        // box (InstrumenController.CariInstrumenPadaRay).

        SimpanPrefab(root, "Assets/Prefabs/Kacapi.prefab");
        Debug.Log($"[Setup] Kacapi: {jumlahDawai} dawai diberi collider + BilahSaron.");
    }

    private static void TerapkanMaterial(GameObject root, Material material)
    {
        if (material == null) return;

        foreach (Renderer r in root.GetComponentsInChildren<Renderer>())
        {
            Material[] mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = material;
            r.sharedMaterials = mats;
        }
    }

    /// <summary>
    /// Menebalkan BoxCollider yang terlalu tipis hingga minimum 3 cm per
    /// sumbu. Dawai kacapi hanya berdiameter beberapa milimeter — collider
    /// setipis itu hampir mustahil dikenai ketukan jari di layar ponsel,
    /// jadi area sentuhnya diperluas tanpa mengubah tampilan visual.
    /// </summary>
    private static void PertebalCollider(GameObject go)
    {
        BoxCollider box = go.GetComponentInChildren<BoxCollider>();
        if (box == null) return;

        const float tebalMinimum = 0.03f; // meter, pada skala lokal mesh

        // Ukuran minimum dikonversi ke ruang lokal collider karena
        // box.size dinyatakan pada skala lokal, sedangkan 3 cm adalah
        // ukuran dunia; lossyScale menjembatani keduanya per sumbu.
        Vector3 skala = box.transform.lossyScale;
        Vector3 ukuran = box.size;
        if (skala.x > 0.0001f) ukuran.x = Mathf.Max(ukuran.x, tebalMinimum / skala.x);
        if (skala.y > 0.0001f) ukuran.y = Mathf.Max(ukuran.y, tebalMinimum / skala.y);
        if (skala.z > 0.0001f) ukuran.z = Mathf.Max(ukuran.z, tebalMinimum / skala.z);
        box.size = ukuran;
    }

    private static void TambahBoxColliderDariMesh(GameObject go)
    {
        if (go.GetComponent<Collider>() != null) return;

        MeshFilter mf = go.GetComponent<MeshFilter>();
        if (mf == null) mf = go.GetComponentInChildren<MeshFilter>();

        if (mf != null && mf.gameObject != go)
        {
            // Mesh ada di child: pasang collider di child (BilahSaron tetap
            // ditemukan lewat GetComponentInParent saat raycast).
            go = mf.gameObject;
            if (go.GetComponent<Collider>() != null) return;
        }

        BoxCollider box = go.AddComponent<BoxCollider>();
        if (mf != null && mf.sharedMesh != null)
        {
            box.center = mf.sharedMesh.bounds.center;
            box.size = mf.sharedMesh.bounds.size;
        }
    }

    // ---------- Material & bidang datar ----------

    private static Material BuatMaterialSaron()
    {
        Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(
            "Assets/Models/Saron/Textures/saron_Base_color.png");
        Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(
            "Assets/Models/Saron/Textures/saron_Normal_OpenGL.png");

        string pathNormal = "Assets/Models/Saron/Textures/saron_Normal_OpenGL.png";
        TextureImporter tiNormal = AssetImporter.GetAtPath(pathNormal) as TextureImporter;
        if (tiNormal != null && tiNormal.textureType != TextureImporterType.NormalMap)
        {
            tiNormal.textureType = TextureImporterType.NormalMap;
            tiNormal.SaveAndReimport();
        }

        Material mat = new Material(Shader.Find("Standard"));
        mat.name = "Saron";
        if (albedo != null) mat.mainTexture = albedo;
        if (normal != null)
        {
            mat.SetTexture("_BumpMap", normal);
            mat.EnableKeyword("_NORMALMAP");
        }
        mat.SetFloat("_Metallic", 0.75f);
        mat.SetFloat("_Glossiness", 0.45f);

        AssetDatabase.DeleteAsset("Assets/Materials/Saron.mat");
        AssetDatabase.CreateAsset(mat, "Assets/Materials/Saron.mat");
        return mat;
    }

    /// <summary>
    /// Membuat material Standard dari set tekstur berprefix seragam
    /// (prefix + "Base_color", prefix + "Normal"), pola yang sama dengan
    /// material Saron: metallic/smoothness skalar, bukan peta terpisah.
    /// </summary>
    private static Material BuatMaterialPBR(string nama, string prefixTekstur,
        float metallic, float smoothness)
    {
        string ekstensiAlbedo = File.Exists(prefixTekstur + "Base_color.jpg") ? ".jpg" : ".png";
        Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(
            prefixTekstur + "Base_color" + ekstensiAlbedo);

        string pathNormal = File.Exists(prefixTekstur + "Normal.jpg")
            ? prefixTekstur + "Normal.jpg"
            : prefixTekstur + "Normal.png";
        Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(pathNormal);

        TextureImporter tiNormal = AssetImporter.GetAtPath(pathNormal) as TextureImporter;
        if (tiNormal != null && tiNormal.textureType != TextureImporterType.NormalMap)
        {
            tiNormal.textureType = TextureImporterType.NormalMap;
            tiNormal.SaveAndReimport();
        }

        Material mat = new Material(Shader.Find("Standard"));
        mat.name = nama;
        if (albedo != null) mat.mainTexture = albedo;
        else Debug.LogWarning($"[Setup] Tekstur albedo {nama} tidak ditemukan: {prefixTekstur}Base_color");
        if (normal != null)
        {
            mat.SetTexture("_BumpMap", normal);
            mat.EnableKeyword("_NORMALMAP");
        }
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Glossiness", smoothness);

        AssetDatabase.DeleteAsset($"Assets/Materials/{nama}.mat");
        AssetDatabase.CreateAsset(mat, $"Assets/Materials/{nama}.mat");
        return mat;
    }

    private static void BuatPrefabBidangDatar()
    {
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.name = "BidangDatar";
        mat.color = new Color(1f, 1f, 1f, 0.30f);
        AssetDatabase.DeleteAsset("Assets/Materials/BidangDatar.mat");
        AssetDatabase.CreateAsset(mat, "Assets/Materials/BidangDatar.mat");

        GameObject plane = new GameObject(
            "ARPlaneVisual",
            typeof(ARPlane),
            typeof(ARPlaneMeshVisualizer),
            typeof(MeshFilter),
            typeof(MeshRenderer));
        plane.GetComponent<MeshRenderer>().sharedMaterial = mat;

        SimpanPrefab(plane, "Assets/Prefabs/ARPlaneVisual.prefab");
    }

    // ---------- Util ----------

    /// <summary>
    /// Mencari descendant (anak di kedalaman berapa pun) berdasarkan nama
    /// persis. Transform.Find bawaan Unity hanya menelusuri path eksplisit
    /// satu tingkat (atau path lengkap yang harus diketahui), sedangkan di
    /// sini kedalaman objek target dalam hierarki hasil impor FBX tidak
    /// pasti — PrefabUtility.InstantiatePrefab menyisipkan root model FBX
    /// sebagai satu tingkat ekstra di antara root prefab kita dan objek
    /// yang dinamai user di Blender.
    /// </summary>
    private static Transform CariAnakBernama(Transform akar, string nama)
    {
        foreach (Transform t in akar.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == nama) return t;
        }
        return null;
    }

    private static Bounds HitungBounds(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.zero);

        Bounds b = renderers[0].bounds;
        foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
        return b;
    }

    private static void SimpanPrefab(GameObject root, string path)
    {
        AssetDatabase.DeleteAsset(path);
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log("[Setup] Prefab tersimpan: " + path);
    }
}
