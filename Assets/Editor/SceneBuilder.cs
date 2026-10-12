using System.IO;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.SpatialTracking;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Membangun scene MainMenu (T-00, T-01, T-03, T-04, E-01) dan
/// ARScene (T-02A/B/C/D, E-02, pop-up info) secara otomatis, termasuk
/// seluruh hierarki UI, komponen AR Foundation, dan wiring referensi
/// antar komponen sesuai Class Diagram.
/// </summary>
public static class SceneBuilder
{
    // ---------- Tema warna ----------
    private static readonly Color WarnaLatar = Hex("#3E2723");     // coklat tua
    private static readonly Color WarnaKartu = Hex("#4E342E");
    private static readonly Color WarnaAksen = Hex("#FFB300");     // emas
    private static readonly Color WarnaTeks = Hex("#FFF8E1");      // krem
    private static readonly Color WarnaTeksGelap = Hex("#3E2723");
    private static readonly Color WarnaBanner = new Color(0f, 0f, 0f, 0.55f);
    private static readonly Color WarnaDim = new Color(0f, 0f, 0f, 0.7f);
    private static readonly Color WarnaError = Hex("#EF5350");

    // ---------- Konten teks ----------
    private const string DeskripsiAngklung =
        "Angklung adalah alat musik tradisional Sunda yang terbuat dari tabung-tabung bambu " +
        "yang dirangkai pada sebuah bingkai. Angklung dibunyikan dengan cara digoyangkan " +
        "sehingga tabung bambu berbenturan dan menghasilkan nada tertentu. Satu angklung " +
        "umumnya menghasilkan satu nada, sehingga permainan angklung dilakukan secara " +
        "berkelompok dan menumbuhkan nilai kerja sama. Pada 18 November 2010, UNESCO " +
        "menetapkan angklung sebagai Warisan Budaya Takbenda Kemanusiaan (Intangible " +
        "Cultural Heritage of Humanity) asal Indonesia.";

    private const string DeskripsiSaron =
        "Saron adalah alat musik pukul (idiofon) dalam gamelan degung Sunda yang terdiri " +
        "atas bilah-bilah logam yang disusun di atas kotak resonator kayu. Saron dimainkan " +
        "dengan cara dipukul menggunakan alat pemukul khusus. Model ini memiliki 14 bilah " +
        "dengan laras degung; bilah terpanjang menghasilkan nada terendah dan bilah " +
        "terpendek menghasilkan nada tertinggi. Dalam gamelan, saron berperan membawakan " +
        "melodi pokok (balungan). Sentuh tiap bilah untuk memainkan nadanya.";

    private const string DeskripsiKendang =
        "Kendang (gendang) adalah alat musik membranofon Sunda berupa tabung kayu yang " +
        "kedua sisinya ditutup membran kulit. Kendang dimainkan dengan dipukul menggunakan " +
        "telapak tangan dan berperan sebagai pengatur irama serta tempo dalam ansambel " +
        "gamelan. Dalam kesenian Sunda, kendang menjadi nyawa iringan tari jaipongan dan " +
        "pencak silat, dengan pola tabuhan yang dinamis dan ekspresif.";

    private const string TeksPanduan =
        "1. Tekan tombol \"Mulai AR\" pada Menu Utama untuk membuka kamera AR.\n\n" +
        "2. Gerakkan perangkat secara perlahan ke arah permukaan datar (lantai atau meja) " +
        "hingga muncul visualisasi bidang datar.\n\n" +
        "3. Pilih satu instrumen dari daftar, lalu sentuh titik pada bidang datar untuk " +
        "menempatkannya.\n\n" +
        "4. Sentuh instrumen untuk membunyikan suaranya. Khusus Saron dan Kacapi, sentuh " +
        "tiap bilah/dawai untuk memainkan nada yang berbeda.\n\n" +
        "5. Tekan \"+ Tambah Instrumen\" untuk menambah instrumen lain di titik berbeda " +
        "(instrumen yang sudah ada tetap berdiri), atau \"Hapus\" untuk menghapus instrumen " +
        "yang sedang aktif.\n\n" +
        "6. Bila ada lebih dari satu instrumen, sentuh badan instrumen lain untuk " +
        "menjadikannya aktif.\n\n" +
        "7. Tekan \"Info\" untuk membaca penjelasan instrumen aktif.";

    private const string TeksTentang =
        "NadaSunda AR v1.0\n\n" +
        "Aplikasi multimedia interaktif pengenalan alat musik tradisional Sunda " +
        "(Angklung, Saron, Kendang, Kacapi, dan Goong) menggunakan teknologi " +
        "Markerless Augmented Reality berbasis Android.\n\n" +
        "Dikembangkan sebagai bagian dari skripsi Program Studi Teknik Informatika.\n\n" +
        "Teknologi: Unity, AR Foundation, Google ARCore.\n" +
        "Aset model 3D: Sketchfab, Envato Elements, dan Meshy AI (berlisensi).";

    private const string DeskripsiKacapi =
        "Kacapi adalah alat musik petik (kordofon) khas Sunda berupa kotak resonator " +
        "kayu dengan dawai-dawai yang direntangkan di atasnya. Kacapi dimainkan dengan " +
        "cara dipetik menggunakan jari dan menjadi instrumen utama dalam kesenian " +
        "tembang Sunda dan kacapi suling. Dalam pertunjukan, kacapi indung membuka " +
        "lagu dan mengatur tempo, sementara kacapi rincik mengiringi dengan pola " +
        "ritmis yang lebih rapat. Sentuh tiap dawai untuk memainkan nadanya.";

    private const string DeskripsiGoong =
        "Goong (gong besar) adalah alat musik pukul (idiofon) berupa lempengan logam " +
        "bundar berpencu yang digantung pada bingkai, lalu dipukul dengan pemukul " +
        "berlapis kain. Dalam gamelan Sunda, goong berperan sebagai penanda akhir " +
        "kalimat lagu (goongan) dengan suara rendah yang bergema panjang. Karena " +
        "perannya itu, goong dianggap sebagai instrumen yang dihormati dalam " +
        "ansambel. Sentuh badan goong untuk membunyikannya.";

    private const string PesanE01 =
        "Perangkat Anda tidak mendukung Google ARCore sehingga fitur Augmented Reality " +
        "tidak dapat dijalankan. Silakan gunakan perangkat lain yang mendukung ARCore.";

    private const string PesanE02 =
        "Aplikasi memerlukan izin kamera untuk menjalankan fitur Augmented Reality. Buka " +
        "Pengaturan Aplikasi, aktifkan izin Kamera, lalu kembali ke aplikasi ini dan tekan " +
        "\"Coba Lagi\".";

    private const string PesanE03 =
        "Bidang datar tidak ditemukan. Pastikan pencahayaan ruangan cukup terang dan " +
        "arahkan kamera ke permukaan datar yang bertekstur (lantai, meja, atau karpet), " +
        "lalu coba lagi.";

    public static void BuildAll()
    {
        BuatSceneMainMenu();
        BuatSceneAR();

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/ARScene.unity", true)
        };
        AssetDatabase.SaveAssets();
        Debug.Log("[Setup] Kedua scene selesai dibangun dan didaftarkan ke Build Settings.");
    }

    // =====================================================================
    //  SCENE 1: MainMenu
    // =====================================================================
    private static void BuatSceneMainMenu()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject kameraGO = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        kameraGO.tag = "MainCamera";
        Camera kamera = kameraGO.GetComponent<Camera>();
        kamera.clearFlags = CameraClearFlags.SolidColor;
        kamera.backgroundColor = WarnaLatar;

        GameObject kanvas = BuatCanvas();
        BuatEventSystem();

        GameObject managers = new GameObject("Managers");
        ARManager arManager = managers.AddComponent<ARManager>();
        UIManager uiManager = managers.AddComponent<UIManager>();

        // Musik latar Menu Utama — otomatis diputar berulang sejak splash
        // (playOnAwake) supaya sudah terdengar begitu Menu Utama tampil,
        // tombol SuaraOn/SuaraOff di T-01 hanya mengatur mute-nya.
        AudioSource musikLatar = managers.AddComponent<AudioSource>();
        musikLatar.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/MusikMenu.mp3");
        musikLatar.loop = true;
        musikLatar.playOnAwake = true;
        musikLatar.volume = 0.5f;

        // ----- T-00 Splash -----
        GameObject panelSplash = BuatPanel(kanvas, "PanelSplash (T-00)", WarnaLatar, true);
        Text judulSplash = BuatTeks(panelSplash, "Judul", "NadaSunda AR", 110, WarnaAksen, TextAnchor.MiddleCenter, FontStyle.Bold);
        Regangkan(judulSplash.rectTransform, new Vector2(0f, 0.45f), new Vector2(1f, 0.75f));
        Text subJudul = BuatTeks(panelSplash, "SubJudul",
            "Pengenalan Alat Musik Tradisional Sunda\ndengan Markerless Augmented Reality",
            40, WarnaTeks, TextAnchor.UpperCenter, FontStyle.Normal);
        Regangkan(subJudul.rectTransform, new Vector2(0.05f, 0.3f), new Vector2(0.95f, 0.48f));
        Text teksCek = BuatTeks(panelSplash, "TeksMemeriksa", "Memeriksa dukungan ARCore...",
            32, WarnaTeks, TextAnchor.MiddleCenter, FontStyle.Italic);
        Regangkan(teksCek.rectTransform, new Vector2(0f, 0.05f), new Vector2(1f, 0.12f));

        // ----- T-01 Menu Utama -----
        // Latar T-01 memakai mockup revisi final (disalin dari
        // "Revisi apk/mainmenu (1).png" ke Assets/UI/Backgrounds/
        // MainMenuBG.png — BUKAN "Revisi apk/contoh ui/mainmenu.png" yang
        // merupakan draft lama bertombol teks) dengan tombol TRANSPARAN
        // (alpha 0) di atas 3 ikon bulat kayu. Tidak ada tombol Keluar di
        // revisi ini (disengaja — keluar aplikasi lewat tombol/gesture back
        // sistem Android). 2 ikon suara di bawahnya (SoundOn/SoundOff)
        // mengatur mute musikLatar lewat UIManager.NyalakanMusik/MatikanMusik.
        //
        // Anchor tiap tombol adalah pecahan (bukan piksel absolut), diukur
        // dari pemindaian warna piksel langsung pada mockup asli (390x845):
        // ketiga ikon sama-sama y=400..455, x berbeda per ikon (info
        // 103-157, play 171-225, tanda-tanya 242-296). Anchor pecahan
        // dipilih (bukan anchoredPosition+sizeDelta) supaya area tombol
        // tetap menempel presisi pada gambar berapa pun resolusi akhirnya,
        // karena Image latar juga diregangkan penuh ke rect yang sama.
        GameObject panelMenu = BuatPanelMockup(kanvas, "PanelMenuUtama (T-01)",
            "Revisi apk/mainmenu (1).png", "Assets/UI/Backgrounds/MainMenuBG.png");

        (string label, UnityAction aksi, float xMin, float xMax)[] tombolMenu =
        {
            ("Tentang", uiManager.BukaTentang, 0.264103f, 0.402564f),  // ikon info (i)
            ("MulaiAR", uiManager.MulaiAR, 0.438462f, 0.576923f),      // ikon play
            ("Panduan", uiManager.BukaPanduan, 0.620513f, 0.758974f), // ikon tanda tanya (?)
        };
        foreach (var t in tombolMenu)
        {
            BuatTombolTransparan(panelMenu, "Tombol" + t.label,
                new Vector2(t.xMin, 0.461538f), new Vector2(t.xMax, 0.526627f), t.aksi);
        }

        // Ikon suara (y=528..577 pada mockup): SoundOn x=140-189, SoundOff x=210-259.
        BuatTombolTransparan(panelMenu, "TombolSuaraOn",
            new Vector2(0.358974f, 0.317160f), new Vector2(0.484615f, 0.375148f), uiManager.NyalakanMusik);
        BuatTombolTransparan(panelMenu, "TombolSuaraOff",
            new Vector2(0.538462f, 0.317160f), new Vector2(0.664103f, 0.375148f), uiManager.MatikanMusik);

        // ----- T-03 Panduan -----
        GameObject panelPanduan = BuatHalamanKonten(kanvas, "PanelPanduan (T-03)",
            "Panduan Penggunaan", TeksPanduan, uiManager.KembaliKeMenuUtama);

        // ----- T-04 Tentang -----
        GameObject panelTentang = BuatHalamanKonten(kanvas, "PanelTentang (T-04)",
            "Tentang Aplikasi", TeksTentang, uiManager.KembaliKeMenuUtama);

        // ----- E-01 -----
        GameObject panelE01 = BuatPanel(kanvas, "PanelErrorE01 (E-01)", WarnaLatar, true);
        BuatKartuError(panelE01, "E-01", "Perangkat Tidak Mendukung", PesanE01,
            "Keluar Aplikasi", uiManager.KeluarAplikasi, null, null);

        // Status awal: hanya splash yang aktif.
        panelMenu.SetActive(false);
        panelPanduan.SetActive(false);
        panelTentang.SetActive(false);
        panelE01.SetActive(false);

        // Wiring referensi UIManager & ARManager lewat SerializedObject.
        SerializedObject soUI = new SerializedObject(uiManager);
        soUI.FindProperty("arManager").objectReferenceValue = arManager;
        soUI.FindProperty("panelSplash").objectReferenceValue = panelSplash;
        soUI.FindProperty("panelMenuUtama").objectReferenceValue = panelMenu;
        soUI.FindProperty("panelPanduan").objectReferenceValue = panelPanduan;
        soUI.FindProperty("panelTentang").objectReferenceValue = panelTentang;
        soUI.FindProperty("panelErrorE01").objectReferenceValue = panelE01;
        soUI.FindProperty("musikLatar").objectReferenceValue = musikLatar;
        soUI.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenu.unity");
        Debug.Log("[Setup] Scene MainMenu tersimpan.");
    }

    // =====================================================================
    //  SCENE 2: ARScene
    // =====================================================================
    private static void BuatSceneAR()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ----- AR Session -----
        GameObject sessionGO = new GameObject("AR Session", typeof(ARSession), typeof(ARInputManager));
        ARSession arSession = sessionGO.GetComponent<ARSession>();

        // ----- XR Origin + kamera AR -----
        GameObject originGO = new GameObject("XR Origin", typeof(XROrigin));
        XROrigin xrOrigin = originGO.GetComponent<XROrigin>();

        GameObject offsetGO = new GameObject("Camera Offset");
        offsetGO.transform.SetParent(originGO.transform, false);

        GameObject kameraGO = new GameObject("Main Camera",
            typeof(Camera), typeof(AudioListener),
            typeof(ARCameraManager), typeof(ARCameraBackground), typeof(TrackedPoseDriver));
        kameraGO.tag = "MainCamera";
        kameraGO.transform.SetParent(offsetGO.transform, false);

        Camera kameraAR = kameraGO.GetComponent<Camera>();
        kameraAR.clearFlags = CameraClearFlags.SolidColor;
        kameraAR.backgroundColor = Color.black;
        kameraAR.nearClipPlane = 0.05f;
        kameraAR.farClipPlane = 20f;

        TrackedPoseDriver tpd = kameraGO.GetComponent<TrackedPoseDriver>();
        tpd.SetPoseSource(TrackedPoseDriver.DeviceType.GenericXRDevice, TrackedPoseDriver.TrackedPose.ColorCamera);
        tpd.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
        tpd.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;

        xrOrigin.Camera = kameraAR;
        xrOrigin.CameraFloorOffsetObject = offsetGO;

        ARPlaneManager planeManager = originGO.AddComponent<ARPlaneManager>();
        planeManager.planePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ARPlaneVisual.prefab");
        planeManager.requestedDetectionMode = UnityEngine.XR.ARSubsystems.PlaneDetectionMode.Horizontal;
        ARRaycastManager raycastManager = originGO.AddComponent<ARRaycastManager>();

        // ----- Cahaya -----
        GameObject lampuGO = new GameObject("Directional Light", typeof(Light));
        Light lampu = lampuGO.GetComponent<Light>();
        lampu.type = LightType.Directional;
        lampu.intensity = 1.3f;
        lampuGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // Scene dibuat dari EmptyScene yang TIDAK punya skybox, sehingga
        // ambient bawaan nyaris hitam — sisi objek yang membelakangi lampu
        // menjadi sangat gelap dan warna tekstur tampak kusam. Ambient
        // diset eksplisit ke mode Flat dengan warna terang netral agar
        // seluruh permukaan menerima cahaya dasar yang merata, mendekati
        // pencahayaan ruangan nyata tempat AR digunakan.
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.65f, 0.64f, 0.62f);

        // ----- Managers -----
        GameObject managers = new GameObject("Managers");
        ARManager arManager = managers.AddComponent<ARManager>();
        InstrumenController instrumenController = managers.AddComponent<InstrumenController>();
        AudioPlayer audioPlayer = managers.AddComponent<AudioPlayer>();
        TouchRaycaster touchRaycaster = managers.AddComponent<TouchRaycaster>();
        UIManager uiManager = managers.AddComponent<UIManager>();
        AudioSource audioSource = managers.GetComponent<AudioSource>();
        audioSource.playOnAwake = false;

        // ----- UI -----
        GameObject kanvas = BuatCanvas();
        BuatEventSystem();

        // Path ikon WoodenBlocks dipakai di beberapa panel ARScene (semua
        // panel di scene ini menumpuk di atas kamera AR, jadi tombolnya
        // digambar sebagai ikon asli, bukan hitbox transparan di atas
        // gambar mockup penuh layar — beda dengan MainMenu yang kameranya
        // warna solid sehingga aman pakai mockup sebagai background).
        const string PathIkonWB = "Assets/Game GUI Vol1/WoodenBlocks/PNG/";

        // T-02A: mencari bidang datar — panel TETAP transparan (BUKAN
        // gambar mockup penuh layar) supaya kamera AR & visualisasi bidang
        // datar tetap terlihat; cuma banner teks + ikon back yang digambar
        // di atasnya, posisinya mengikuti tata letak mockup.
        GameObject panelScanning = BuatBannerBawah(kanvas, "PanelScanning (T-02A)",
            "Gerakkan perangkat perlahan untuk mencari permukaan datar...");
        BuatTombolIkonTitik(panelScanning, "TombolKembali", PathIkonWB + "Backward.png",
            new Vector2(0.120513f, 0.951479f), new Vector2(150f, 150f), uiManager.KembaliKeSceneMenu);

        // Banner hint pencahayaan — muncul di tengah proses scanning lewat
        // ARManager.OnPetunjukPencahayaan (bukan pengukuran cahaya asli,
        // murni pengingat dini berbasis waktu). Tetap digambar manual di
        // atas background mockup karena sifatnya dinamis (bukan bagian
        // gambar statis). Anak dari panelScanning supaya otomatis
        // tersembunyi saat panel diganti, mulai nonaktif.
        GameObject bannerHint = new GameObject("BannerHintPencahayaan", typeof(Image));
        bannerHint.transform.SetParent(panelScanning.transform, false);
        Image imgHint = bannerHint.GetComponent<Image>();
        imgHint.color = WarnaBanner;
        imgHint.raycastTarget = false;
        Regangkan(bannerHint.GetComponent<RectTransform>(), new Vector2(0.1f, 0.19f), new Vector2(0.9f, 0.29f));
        Text teksHint = BuatTeks(bannerHint, "Pesan",
            "Belum ketemu? Coba cari tempat yang lebih terang.",
            30, WarnaAksen, TextAnchor.MiddleCenter, FontStyle.Normal);
        Regangkan(teksHint.rectTransform, new Vector2(0.05f, 0f), new Vector2(0.95f, 1f));
        bannerHint.SetActive(false);

        // T-02B: list pemilihan instrumen — muncul begitu bidang datar
        // ditemukan. User memilih SATU card, lalu menempatkannya sendiri
        // dengan menyentuh bidang datar (lihat panelPlacement di bawah).
        // Panel HANYA diredupkan (WarnaDim, tembus pandang), BUKAN gambar
        // mockup penuh layar — kamera AR & bidang datar yang terdeteksi
        // harus tetap terlihat di baliknya. Tiap kartu instrumen digambar
        // dengan ikon asli dari Assets/UI/Icons (bukan hitbox transparan di
        // atas PNG), urutan sama dengan daftarInstrumen: Angklung, Saron,
        // Kendang, Kacapi, Goong.
        GameObject panelList = BuatPanel(kanvas, "PanelListInstrumen (T-02B)", WarnaDim, true);
        BuatTombolIkonTitik(panelList, "TombolKembali", PathIkonWB + "Backward.png",
            new Vector2(0.115385f, 0.946746f), new Vector2(150f, 150f), uiManager.KembaliKeSceneMenu);

        Text judulList = BuatTeks(panelList, "Judul", "Pilih Instrumen untuk Ditempatkan", 42, WarnaAksen,
            TextAnchor.MiddleCenter, FontStyle.Bold);
        Regangkan(judulList.rectTransform, new Vector2(0f, 0.88f), new Vector2(1f, 0.97f));

        (string nama, string ikon)[] daftarKartu =
        {
            ("Angklung", "angklung"),
            ("Saron", "saron"),
            ("Kendang", "kendang"),
            ("Kacapi", "kecapi"),
            ("Goong", "gong"),
        };
        Button[] kartuInstrumen = new Button[daftarKartu.Length];
        for (int i = 0; i < daftarKartu.Length; i++)
        {
            GameObject kartu = new GameObject("Kartu" + daftarKartu[i].nama, typeof(Image), typeof(Button));
            kartu.transform.SetParent(panelList.transform, false);

            Image imgKartu = kartu.GetComponent<Image>();
            imgKartu.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            imgKartu.type = Image.Type.Sliced;
            imgKartu.color = WarnaAksen;

            Button tombolKartu = kartu.GetComponent<Button>();
            UnityEventTools.AddIntPersistentListener(tombolKartu.onClick, uiManager.PilihKartuInstrumen, i);
            AturWarnaTombol(tombolKartu, imgKartu, WarnaAksen);

            RectTransform rtKartu = kartu.GetComponent<RectTransform>();
            rtKartu.anchorMin = rtKartu.anchorMax = new Vector2(0.5f, 0.74f - i * 0.13f);
            rtKartu.sizeDelta = new Vector2(700f, 130f);
            rtKartu.anchoredPosition = Vector2.zero;

            Sprite ikonSprite = MuatSprite($"Assets/UI/Icons/{daftarKartu[i].ikon}.png");
            if (ikonSprite != null)
            {
                GameObject ikonGO = new GameObject("Ikon", typeof(Image));
                ikonGO.transform.SetParent(kartu.transform, false);
                Image imgIkon = ikonGO.GetComponent<Image>();
                imgIkon.sprite = ikonSprite;
                imgIkon.preserveAspect = true;
                imgIkon.raycastTarget = false;
                RectTransform rtIkon = ikonGO.GetComponent<RectTransform>();
                rtIkon.anchorMin = new Vector2(0f, 0f);
                rtIkon.anchorMax = new Vector2(0f, 1f);
                rtIkon.pivot = new Vector2(0f, 0.5f);
                rtIkon.sizeDelta = new Vector2(100f, -20f);
                rtIkon.anchoredPosition = new Vector2(15f, 0f);
            }

            Text teksKartu = BuatTeks(kartu, "Label", daftarKartu[i].nama, 40, WarnaTeksGelap,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            Regangkan(teksKartu.rectTransform, Vector2.zero, Vector2.one);

            kartuInstrumen[i] = tombolKartu;
        }

        // T-02C: arahan menempatkan instrumen yang baru dipilih — teks
        // instruksi diperbarui runtime oleh UIManager.PilihKartuInstrumen
        // sesuai nama instrumen yang dipilih.
        GameObject panelPlacement = BuatBannerBawah(kanvas, "PanelPlacement (T-02C)",
            "Sentuh area pada bidang datar untuk menempatkan instrumen.");
        Text teksInstruksiPlacement = panelPlacement.transform.Find("Banner/Pesan").GetComponent<Text>();

        // T-02D: overlay instrumen yang sudah ditempatkan — banner nama di
        // atas, tombol Info / "+ Tambah" / "Hapus" di bawah. Tidak ada lagi
        // tombol Kembali/Back: tidak ada state IDLE untuk dikembalikan —
        // instrumen yang tidak aktif tetap berdiri terlihat normal.
        GameObject panelOverlay = new GameObject("PanelOverlayInstrumen (T-02D)", typeof(RectTransform));
        panelOverlay.transform.SetParent(kanvas.transform, false);
        Regangkan(panelOverlay.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);

        GameObject bannerNama = new GameObject("BannerNama", typeof(Image));
        bannerNama.transform.SetParent(panelOverlay.transform, false);
        Image imgBanner = bannerNama.GetComponent<Image>();
        imgBanner.color = WarnaBanner;
        imgBanner.raycastTarget = false;
        Regangkan(bannerNama.GetComponent<RectTransform>(), new Vector2(0.2f, 0.9f), new Vector2(0.8f, 0.97f));
        Text teksNama = BuatTeks(bannerNama, "TeksNamaInstrumen", "Angklung", 48, WarnaAksen,
            TextAnchor.MiddleCenter, FontStyle.Bold);
        Regangkan(teksNama.rectTransform, Vector2.zero, Vector2.one);

        // 4 ikon di baris bawah (Kembali/Info/Hapus/Tambah) mengikuti
        // mockup "informasi alat musik (1).png": posisi diukur dari
        // pemindaian piksel langsung (390x845). Ikonnya dari paket
        // WoodenBlocks yang sudah ada di Assets/Game GUI Vol1 — panel ini
        // menumpuk di atas kamera AR (bukan gambar statis), jadi tombolnya
        // digambar sebagai ikon asli, bukan hitbox transparan di atas PNG.
        // Ikon "Kembali" di sini MENGGANTIKAN tombol "Menu ✕" bergaya lama
        // untuk panel Overlay (lihat TambahTombolMenuKembali di bawah,
        // yang sekarang hanya dipakai untuk panel yang belum ada mockup-nya).
        BuatTombolIkon(panelOverlay, "TombolKembali", PathIkonWB + "Backward.png",
            new Vector2(0.053846f, 0.039053f), new Vector2(0.194872f, 0.102959f), uiManager.KembaliKeSceneMenu);
        BuatTombolIkon(panelOverlay, "TombolInfo", PathIkonWB + "QuestionMark.png",
            new Vector2(0.279487f, 0.039053f), new Vector2(0.420513f, 0.102959f), uiManager.BukaModeInfo);
        BuatTombolIkon(panelOverlay, "TombolHapus", PathIkonWB + "Cancel.png",
            new Vector2(0.633333f, 0.039053f), new Vector2(0.774359f, 0.102959f), uiManager.HapusInstrumenAktif);
        Button tombolTambah = BuatTombolIkon(panelOverlay, "TombolTambah", PathIkonWB + "Plus.png",
            new Vector2(0.823077f, 0.039053f), new Vector2(0.964103f, 0.102959f), uiManager.TambahInstrumenLagi);

        // Pop-up info instrumen — tombol Tutup keluar dari mode Info
        // (menyalakan lagi audio pada instrumen aktif), bukan sekadar
        // menutup panel.
        GameObject popupInfo = BuatPanel(kanvas, "PopupInfo", WarnaDim, true);
        GameObject kartuInfo = BuatKartu(popupInfo, new Vector2(0.07f, 0.18f), new Vector2(0.93f, 0.82f));
        Text judulPopup = BuatTeks(kartuInfo, "TeksJudulPopup", "Nama Instrumen", 56, WarnaAksen,
            TextAnchor.MiddleCenter, FontStyle.Bold);
        Regangkan(judulPopup.rectTransform, new Vector2(0.05f, 0.86f), new Vector2(0.95f, 0.98f));
        Text deskripsiPopup = BuatTeks(kartuInfo, "TeksDeskripsiPopup", "Deskripsi instrumen.", 34, WarnaTeks,
            TextAnchor.UpperLeft, FontStyle.Normal);
        Regangkan(deskripsiPopup.rectTransform, new Vector2(0.06f, 0.16f), new Vector2(0.94f, 0.84f));
        // Ikon X kecil di pojok kanan-atas kartu (mengikuti mockup),
        // menggantikan tombol teks "Tutup" penuh lebar yang lama.
        // Diparenting ke popupInfo (bukan kartuInfo) supaya anchor-nya
        // relatif ke layar penuh, konsisten dengan koordinat hasil
        // pemindaian piksel mockup.
        BuatTombolIkon(popupInfo, "TombolTutup", PathIkonWB + "Cancel.png",
            new Vector2(0.753846f, 0.788166f), new Vector2(0.889744f, 0.849704f), uiManager.TutupModeInfo);

        // E-02: izin kamera ditolak. "Buka Pengaturan" karena setelah
        // penolakan kedua Android tidak lagi menampilkan dialog izin
        // bawaan — satu-satunya jalan mengaktifkan izin adalah lewat
        // Pengaturan sistem.
        GameObject panelE02 = BuatPanel(kanvas, "PanelErrorE02 (E-02)", WarnaDim, true);
        BuatKartuError(panelE02, "E-02", "Izin Kamera Ditolak", PesanE02,
            "Buka Pengaturan", uiManager.BukaPengaturanAplikasi,
            "Coba Lagi", uiManager.CobaIzinKameraLagi);

        // E-03: bidang datar tidak ditemukan dalam batas waktu. "Coba
        // Lagi"/"Kembali ke Menu" dipakai sebagai ikon (Restart/Cancel dari
        // WoodenBlocks) alih-alih tombol teks, atas permintaan langsung —
        // ukurannya disamakan dengan ikon Menu Utama/Overlay lainnya.
        GameObject panelE03 = BuatPanel(kanvas, "PanelErrorE03 (E-03)", WarnaDim, true);
        BuatKartuError(panelE03, "E-03", "Bidang Datar Tidak Ditemukan", PesanE03,
            "Coba Lagi", uiManager.CobaLagiDeteksi,
            "Kembali ke Menu", uiManager.KembaliKeSceneMenu,
            PathIkonWB + "Restart.png", PathIkonWB + "Cancel.png");

        // Tombol "Menu ✕" (kanan atas) sekarang per-panel, BUKAN sibling
        // global di kanvas. Hanya dipasang di panel yang BELUM punya
        // mockup/ikon back sendiri (Placement, Error E-02/E-03) — Scanning
        // dan List Instrumen sudah punya ikon back dari mockup, dan Overlay
        // sudah punya ikon "Backward" WoodenBlocks di baris ikon bawahnya
        // (lihat blok 4-ikon di atas).
        TambahTombolMenuKembali(panelPlacement, uiManager.KembaliKeSceneMenu);
        TambahTombolMenuKembali(panelE02, uiManager.KembaliKeSceneMenu);
        TambahTombolMenuKembali(panelE03, uiManager.KembaliKeSceneMenu);

        // Status awal.
        panelScanning.SetActive(true);
        panelList.SetActive(false);
        panelPlacement.SetActive(false);
        panelOverlay.SetActive(false);
        popupInfo.SetActive(false);
        panelE02.SetActive(false);
        panelE03.SetActive(false);

        // ----- Wiring referensi antar komponen -----
        GameObject prefabAngklung = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Angklung.prefab");
        GameObject prefabSaron = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Saron.prefab");
        GameObject prefabKendang = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Kendang.prefab");
        GameObject prefabKacapi = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Kacapi.prefab");
        GameObject prefabGoong = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Goong.prefab");
        AudioClip clipAngklung = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Angklung.mp3");
        AudioClip clipSaron = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Saron/Nada_00.wav");
        AudioClip clipKendang = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Kendang.wav");

        // Kacapi & Goong sekarang punya audio asli (Kacapi granular per
        // dawai sudah dipasang di PrefabBuilder.BuatPrefabKacapi; clip di
        // sini cuma "clip utama" InstrumenModel yang dipakai sebagai
        // fallback badan-tunggal — praktiknya tidak pernah terpanggil untuk
        // Kacapi karena setiap dawainya selalu terdeteksi lewat BilahSaron
        // lebih dulu). Goong SEBALIKNYA badan-tunggal sungguhan, jadi
        // clip di sini adalah audio yang benar-benar berbunyi saat dipukul.
        AudioClip clipKacapi = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Kacapi/Nada_00.wav");
        AudioClip clipGoong = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Goong.mp3");

        SerializedObject soAR = new SerializedObject(arManager);
        soAR.FindProperty("arSession").objectReferenceValue = arSession;
        soAR.FindProperty("planeManager").objectReferenceValue = planeManager;
        soAR.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject soCtrl = new SerializedObject(instrumenController);
        SerializedProperty daftar = soCtrl.FindProperty("daftarInstrumen");
        daftar.arraySize = 5;
        IsiInstrumen(daftar.GetArrayElementAtIndex(0), "Angklung", prefabAngklung, clipAngklung, DeskripsiAngklung);
        IsiInstrumen(daftar.GetArrayElementAtIndex(1), "Saron", prefabSaron, clipSaron, DeskripsiSaron);
        IsiInstrumen(daftar.GetArrayElementAtIndex(2), "Kendang", prefabKendang, clipKendang, DeskripsiKendang);
        IsiInstrumen(daftar.GetArrayElementAtIndex(3), "Kacapi", prefabKacapi, clipKacapi, DeskripsiKacapi);
        IsiInstrumen(daftar.GetArrayElementAtIndex(4), "Goong", prefabGoong, clipGoong, DeskripsiGoong);
        soCtrl.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject soTouch = new SerializedObject(touchRaycaster);
        soTouch.FindProperty("kameraAR").objectReferenceValue = kameraAR;
        soTouch.FindProperty("arRaycastManager").objectReferenceValue = raycastManager;
        soTouch.FindProperty("instrumenController").objectReferenceValue = instrumenController;
        soTouch.FindProperty("audioPlayer").objectReferenceValue = audioPlayer;
        soTouch.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject soUI = new SerializedObject(uiManager);
        soUI.FindProperty("arManager").objectReferenceValue = arManager;
        soUI.FindProperty("instrumenController").objectReferenceValue = instrumenController;
        soUI.FindProperty("touchRaycaster").objectReferenceValue = touchRaycaster;
        soUI.FindProperty("panelScanning").objectReferenceValue = panelScanning;
        soUI.FindProperty("panelListInstrumen").objectReferenceValue = panelList;
        soUI.FindProperty("panelPlacement").objectReferenceValue = panelPlacement;
        soUI.FindProperty("panelOverlayInstrumen").objectReferenceValue = panelOverlay;
        soUI.FindProperty("panelErrorE02").objectReferenceValue = panelE02;
        soUI.FindProperty("panelErrorE03").objectReferenceValue = panelE03;
        soUI.FindProperty("popupInfo").objectReferenceValue = popupInfo;
        soUI.FindProperty("teksNamaInstrumen").objectReferenceValue = teksNama;
        soUI.FindProperty("teksJudulPopup").objectReferenceValue = judulPopup;
        soUI.FindProperty("teksDeskripsiPopup").objectReferenceValue = deskripsiPopup;
        soUI.FindProperty("tombolTambahInstrumen").objectReferenceValue = tombolTambah.gameObject;
        soUI.FindProperty("teksInstruksiPlacement").objectReferenceValue = teksInstruksiPlacement;
        soUI.FindProperty("bannerHintPencahayaan").objectReferenceValue = bannerHint;
        SerializedProperty spKartu = soUI.FindProperty("kartuInstrumen");
        spKartu.arraySize = kartuInstrumen.Length;
        for (int i = 0; i < kartuInstrumen.Length; i++)
        {
            spKartu.GetArrayElementAtIndex(i).objectReferenceValue = kartuInstrumen[i];
        }
        soUI.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/ARScene.unity");
        Debug.Log("[Setup] Scene ARScene tersimpan.");
    }

    private static void IsiInstrumen(SerializedProperty elemen, string nama,
        GameObject prefab, AudioClip clip, string deskripsi)
    {
        elemen.FindPropertyRelative("nama").stringValue = nama;
        elemen.FindPropertyRelative("prefab").objectReferenceValue = prefab;
        elemen.FindPropertyRelative("audioClip").objectReferenceValue = clip;
        elemen.FindPropertyRelative("deskripsi").stringValue = deskripsi;
    }

    // =====================================================================
    //  Helper UI
    // =====================================================================

    private static Font FontBawaan()
    {
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private static GameObject BuatCanvas()
    {
        GameObject go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;
        return go;
    }

    private static void BuatEventSystem()
    {
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static GameObject BuatPanel(GameObject induk, string nama, Color warna, bool blokirSentuhan)
    {
        GameObject go = new GameObject(nama, typeof(Image));
        go.transform.SetParent(induk.transform, false);
        Image img = go.GetComponent<Image>();
        img.color = warna;
        img.raycastTarget = blokirSentuhan;
        Regangkan(go.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        return go;
    }

    private static Text BuatTeks(GameObject induk, string nama, string isi, int ukuran,
        Color warna, TextAnchor perataan, FontStyle gaya)
    {
        GameObject go = new GameObject(nama, typeof(Text));
        go.transform.SetParent(induk.transform, false);
        Text teks = go.GetComponent<Text>();
        teks.text = isi;
        teks.font = FontBawaan();
        teks.fontSize = ukuran;
        teks.fontStyle = gaya;
        teks.color = warna;
        teks.alignment = perataan;
        teks.horizontalOverflow = HorizontalWrapMode.Wrap;
        teks.verticalOverflow = VerticalWrapMode.Overflow;
        teks.raycastTarget = false;
        return teks;
    }

    private static Button BuatTombol(GameObject induk, string nama, string label, int ukuranFont, UnityAction aksi)
    {
        GameObject go = new GameObject(nama, typeof(Image), typeof(Button));
        go.transform.SetParent(induk.transform, false);

        Image img = go.GetComponent<Image>();
        img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        img.type = Image.Type.Sliced;
        img.color = WarnaAksen;

        Button tombol = go.GetComponent<Button>();
        if (aksi != null)
        {
            UnityEventTools.AddPersistentListener(tombol.onClick, aksi);
        }
        AturWarnaTombol(tombol, img, WarnaAksen);

        Text teks = BuatTeks(go, "Label", label, ukuranFont, WarnaTeksGelap, TextAnchor.MiddleCenter, FontStyle.Bold);
        Regangkan(teks.rectTransform, Vector2.zero, Vector2.one);
        return tombol;
    }

    /// <summary>
    /// Mengaktifkan umpan balik tekan bawaan Button (Transition = Color
    /// Tint) supaya tombol terasa "ditekan" tanpa perlu script tambahan —
    /// warnaDasar dipakai sebagai warna diam (normal), sedikit digelapkan
    /// untuk warna saat ditekan. Alpha warna tekan dijamin minimal 0.22
    /// supaya hitbox transparan (alpha 0, mis. di atas mockup) tetap
    /// memberi kilas gelap yang terlihat saat disentuh.
    /// </summary>
    private static void AturWarnaTombol(Button tombol, Image targetGraphic, Color warnaDasar)
    {
        tombol.targetGraphic = targetGraphic;

        Color warnaTekan = Color.Lerp(warnaDasar, Color.black, 0.3f);
        warnaTekan.a = Mathf.Max(warnaDasar.a, 0.22f);
        Color warnaHover = Color.Lerp(warnaDasar, Color.white, 0.12f);
        warnaHover.a = Mathf.Max(warnaDasar.a, 0.10f);
        Color warnaNonaktif = new Color(warnaDasar.r, warnaDasar.g, warnaDasar.b, warnaDasar.a * 0.5f);

        ColorBlock cb = tombol.colors;
        cb.normalColor = warnaDasar;
        cb.highlightedColor = warnaHover;
        cb.pressedColor = warnaTekan;
        cb.selectedColor = warnaDasar;
        cb.disabledColor = warnaNonaktif;
        cb.colorMultiplier = 1f;
        cb.fadeDuration = 0.08f;
        tombol.colors = cb;
    }

    /// <summary>
    /// Menempatkan sebuah gambar dekoratif (bukan tombol/ikon interaktif)
    /// pada satu titik anchor dengan ukuran tetap dalam piksel referensi.
    /// Tekstur dipaksa bertipe Sprite saat impor karena Image (uGUI) hanya
    /// menerima Sprite, sedangkan PNG lepasan default terimpor sebagai
    /// Texture2D biasa.
    /// </summary>
    private static void BuatGambar(GameObject induk, string nama, string pathTekstur, Vector2 anchor, Vector2 ukuran)
    {
        Sprite sprite = MuatSprite(pathTekstur);
        if (sprite == null)
        {
            Debug.LogWarning($"[Setup] Gambar tidak ditemukan: {pathTekstur}");
            return;
        }

        GameObject go = new GameObject(nama, typeof(Image));
        go.transform.SetParent(induk.transform, false);
        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = ukuran;
        rt.anchoredPosition = Vector2.zero;
    }

    /// <summary>
    /// Memuat sebuah tekstur lepasan sebagai Sprite, memaksa textureType
    /// ke Sprite dulu bila perlu (PNG lepasan default terimpor sebagai
    /// Texture2D biasa, sedangkan Image (uGUI) hanya menerima Sprite).
    /// </summary>
    private static Sprite MuatSprite(string pathTekstur)
    {
        TextureImporter importer = AssetImporter.GetAtPath(pathTekstur) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(pathTekstur);
    }

    /// <summary>
    /// Menyalin sebuah file mockup dari luar folder Assets (mis. folder
    /// "Revisi apk/") ke dalam Assets supaya Unity mengenalinya sebagai
    /// aset (dapat GUID + .meta), lalu memuatnya sebagai Sprite. Salinan
    /// hanya dilakukan sekali; pemanggilan berikutnya memakai aset yang
    /// sudah ada di tujuan tanpa menimpa ulang.
    /// </summary>
    private static Sprite SalinDanMuatSprite(string sumberRelatifProyek, string tujuanAsset)
    {
        string akarProyek = Path.GetDirectoryName(Application.dataPath);
        string sumberAbsolut = Path.Combine(akarProyek, sumberRelatifProyek);
        string tujuanAbsolut = Path.Combine(akarProyek, tujuanAsset);

        if (!File.Exists(sumberAbsolut))
        {
            Debug.LogError($"[Setup] Mockup tidak ditemukan: {sumberAbsolut}");
            return null;
        }

        // Selalu ditimpa (bukan hanya sekali) supaya kalau file mockup
        // sumber diganti/direvisi, aset di dalam Assets ikut ter-update
        // saat SceneBuilder dijalankan ulang.
        Directory.CreateDirectory(Path.GetDirectoryName(tujuanAbsolut));
        File.Copy(sumberAbsolut, tujuanAbsolut, true);
        AssetDatabase.ImportAsset(tujuanAsset, ImportAssetOptions.ForceUpdate);

        return MuatSprite(tujuanAsset);
    }

    /// <summary>
    /// Panel yang latarnya adalah gambar mockup penuh-layar (Image
    /// diregangkan ke seluruh rect induk). Dipakai sebagai pengganti
    /// panel yang disusun manual dari teks/ilustrasi/tombol solid ketika
    /// desain visual sudah final dalam bentuk PNG.
    /// </summary>
    private static GameObject BuatPanelMockup(GameObject induk, string nama,
        string sumberRelatifProyek, string tujuanAsset)
    {
        GameObject go = new GameObject(nama, typeof(Image));
        go.transform.SetParent(induk.transform, false);
        Image img = go.GetComponent<Image>();
        img.sprite = SalinDanMuatSprite(sumberRelatifProyek, tujuanAsset);
        img.color = Color.white;
        img.preserveAspect = false;
        img.raycastTarget = true; // blokir sentuhan tembus ke panel di belakangnya
        Regangkan(go.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        return go;
    }

    /// <summary>
    /// Tombol tak terlihat (alpha 0) yang hanya berfungsi sebagai area
    /// sentuh di atas gambar mockup — dipakai ketika visual tombol sudah
    /// tergambar di dalam background PNG, sehingga tidak perlu digambar
    /// ulang lewat sprite/warna Unity.
    /// </summary>
    private static Button BuatTombolTransparan(GameObject induk, string nama,
        Vector2 anchorMin, Vector2 anchorMax, UnityAction aksi)
    {
        GameObject go = new GameObject(nama, typeof(Image), typeof(Button));
        go.transform.SetParent(induk.transform, false);

        Image img = go.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f);
        img.raycastTarget = true;

        Button tombol = go.GetComponent<Button>();
        if (aksi != null)
        {
            UnityEventTools.AddPersistentListener(tombol.onClick, aksi);
        }
        AturWarnaTombol(tombol, img, img.color);

        Regangkan(go.GetComponent<RectTransform>(), anchorMin, anchorMax);
        return tombol;
    }

    /// <summary>Sama seperti BuatTombolTransparan, tetapi aksinya menerima satu argumen int (lihat BuatTombolInt).</summary>
    private static Button BuatTombolTransparanInt(GameObject induk, string nama,
        Vector2 anchorMin, Vector2 anchorMax, UnityAction<int> aksi, int argumen)
    {
        GameObject go = new GameObject(nama, typeof(Image), typeof(Button));
        go.transform.SetParent(induk.transform, false);

        Image img = go.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f);
        img.raycastTarget = true;

        Button tombol = go.GetComponent<Button>();
        UnityEventTools.AddIntPersistentListener(tombol.onClick, aksi, argumen);
        AturWarnaTombol(tombol, img, img.color);

        Regangkan(go.GetComponent<RectTransform>(), anchorMin, anchorMax);
        return tombol;
    }

    /// <summary>
    /// Tombol berbentuk ikon murni (tanpa label teks) — dipakai untuk
    /// panel yang menumpuk di atas kamera AR (tidak bisa pakai mockup
    /// PNG sebagai background), jadi ikonnya digambar langsung dari aset
    /// sprite (mis. paket WoodenBlocks) alih-alih hitbox transparan.
    /// </summary>
    private static Button BuatTombolIkon(GameObject induk, string nama, string pathIkon,
        Vector2 anchorMin, Vector2 anchorMax, UnityAction aksi)
    {
        GameObject go = new GameObject(nama, typeof(Image), typeof(Button));
        go.transform.SetParent(induk.transform, false);

        Image img = go.GetComponent<Image>();
        img.sprite = MuatSprite(pathIkon);
        img.preserveAspect = true;

        Button tombol = go.GetComponent<Button>();
        if (aksi != null)
        {
            UnityEventTools.AddPersistentListener(tombol.onClick, aksi);
        }
        AturWarnaTombol(tombol, img, Color.white);

        Regangkan(go.GetComponent<RectTransform>(), anchorMin, anchorMax);
        return tombol;
    }

    /// <summary>
    /// Sama seperti BuatTombolIkon, tetapi diposisikan di satu titik anchor
    /// dengan ukuran tetap dalam piksel (seperti LetakkanTombol), bukan
    /// diregangkan mengikuti area gambar mockup — dipakai untuk kartu
    /// error yang tidak punya latar mockup sendiri.
    /// </summary>
    private static Button BuatTombolIkonTitik(GameObject induk, string nama, string pathIkon,
        Vector2 anchor, Vector2 ukuran, UnityAction aksi)
    {
        GameObject go = new GameObject(nama, typeof(Image), typeof(Button));
        go.transform.SetParent(induk.transform, false);

        Image img = go.GetComponent<Image>();
        img.sprite = MuatSprite(pathIkon);
        img.preserveAspect = true;

        Button tombol = go.GetComponent<Button>();
        if (aksi != null)
        {
            UnityEventTools.AddPersistentListener(tombol.onClick, aksi);
        }
        AturWarnaTombol(tombol, img, Color.white);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = ukuran;
        rt.anchoredPosition = Vector2.zero;
        return tombol;
    }

    /// <summary>
    /// Tombol "Menu ✕" bergaya lama (kotak solid emas, pojok kanan atas) —
    /// dipasang per-panel untuk panel yang belum punya mockup/ikon back
    /// sendiri (placement, overlay, error), bukan lagi sibling global di
    /// kanvas.
    /// </summary>
    private static void TambahTombolMenuKembali(GameObject panel, UnityAction aksi)
    {
        Button tombol = BuatTombol(panel, "TombolKembali", "Menu ✕", 36, aksi);
        RectTransform rt = tombol.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-30f, -30f);
        rt.sizeDelta = new Vector2(240f, 90f);
    }

    private static void LetakkanTombol(Button tombol, Vector2 anchor, Vector2 ukuran)
    {
        RectTransform rt = tombol.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = ukuran;
        rt.anchoredPosition = Vector2.zero;
    }

    private static GameObject BuatBannerBawah(GameObject kanvas, string nama, string pesan)
    {
        GameObject panel = new GameObject(nama, typeof(RectTransform));
        panel.transform.SetParent(kanvas.transform, false);
        Regangkan(panel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);

        GameObject banner = new GameObject("Banner", typeof(Image));
        banner.transform.SetParent(panel.transform, false);
        Image img = banner.GetComponent<Image>();
        img.color = WarnaBanner;
        img.raycastTarget = false;
        Regangkan(banner.GetComponent<RectTransform>(), new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.16f));

        Text teks = BuatTeks(banner, "Pesan", pesan, 38, WarnaTeks, TextAnchor.MiddleCenter, FontStyle.Normal);
        Regangkan(teks.rectTransform, new Vector2(0.03f, 0f), new Vector2(0.97f, 1f));
        return panel;
    }

    private static GameObject BuatHalamanKonten(GameObject kanvas, string nama, string judul,
        string isi, UnityAction aksiKembali)
    {
        GameObject panel = BuatPanel(kanvas, nama, WarnaLatar, true);

        Text teksJudul = BuatTeks(panel, "Judul", judul, 64, WarnaAksen, TextAnchor.MiddleCenter, FontStyle.Bold);
        Regangkan(teksJudul.rectTransform, new Vector2(0f, 0.88f), new Vector2(1f, 0.97f));

        GameObject kartu = BuatKartu(panel, new Vector2(0.05f, 0.16f), new Vector2(0.95f, 0.86f));
        Text teksIsi = BuatTeks(kartu, "Isi", isi, 34, WarnaTeks, TextAnchor.UpperLeft, FontStyle.Normal);
        Regangkan(teksIsi.rectTransform, new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.96f));

        Button tombolKembali = BuatTombol(panel, "TombolKembali", "Kembali", 40, aksiKembali);
        LetakkanTombol(tombolKembali, new Vector2(0.5f, 0.08f), new Vector2(400f, 110f));
        return panel;
    }

    private static GameObject BuatKartu(GameObject induk, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject kartu = new GameObject("Kartu", typeof(Image));
        kartu.transform.SetParent(induk.transform, false);
        Image img = kartu.GetComponent<Image>();
        img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        img.type = Image.Type.Sliced;
        img.color = WarnaKartu;
        Regangkan(kartu.GetComponent<RectTransform>(), anchorMin, anchorMax);
        return kartu;
    }

    private static void BuatKartuError(GameObject panel, string kode, string judul, string pesan,
        string labelTombol1, UnityAction aksi1, string labelTombol2, UnityAction aksi2,
        string pathIkon1 = null, string pathIkon2 = null)
    {
        GameObject kartu = BuatKartu(panel, new Vector2(0.08f, 0.28f), new Vector2(0.92f, 0.72f));

        Text teksKode = BuatTeks(kartu, "Kode", kode, 72, WarnaError, TextAnchor.MiddleCenter, FontStyle.Bold);
        Regangkan(teksKode.rectTransform, new Vector2(0f, 0.82f), new Vector2(1f, 0.98f));

        Text teksJudul = BuatTeks(kartu, "Judul", judul, 44, WarnaTeks, TextAnchor.MiddleCenter, FontStyle.Bold);
        Regangkan(teksJudul.rectTransform, new Vector2(0.04f, 0.68f), new Vector2(0.96f, 0.82f));

        Text teksPesan = BuatTeks(kartu, "Pesan", pesan, 32, WarnaTeks, TextAnchor.UpperCenter, FontStyle.Normal);
        Regangkan(teksPesan.rectTransform, new Vector2(0.06f, 0.28f), new Vector2(0.94f, 0.66f));

        // Ukuran ikon (150x150) disamakan dengan tombol ikon lain (Overlay,
        // Menu Utama) supaya konsisten secara visual di seluruh aplikasi.
        Vector2 ukuranIkon = new Vector2(150f, 150f);

        if (labelTombol2 == null)
        {
            if (pathIkon1 != null)
            {
                BuatTombolIkonTitik(kartu, "Tombol1", pathIkon1, new Vector2(0.5f, 0.12f), ukuranIkon, aksi1);
            }
            else
            {
                Button tombol = BuatTombol(kartu, "Tombol1", labelTombol1, 36, aksi1);
                LetakkanTombol(tombol, new Vector2(0.5f, 0.12f), new Vector2(420f, 100f));
            }
        }
        else
        {
            if (pathIkon1 != null)
            {
                BuatTombolIkonTitik(kartu, "Tombol1", pathIkon1, new Vector2(0.28f, 0.12f), ukuranIkon, aksi1);
            }
            else
            {
                Button tombol1 = BuatTombol(kartu, "Tombol1", labelTombol1, 36, aksi1);
                LetakkanTombol(tombol1, new Vector2(0.28f, 0.12f), new Vector2(340f, 100f));
            }

            if (pathIkon2 != null)
            {
                BuatTombolIkonTitik(kartu, "Tombol2", pathIkon2, new Vector2(0.72f, 0.12f), ukuranIkon, aksi2);
            }
            else
            {
                Button tombol2 = BuatTombol(kartu, "Tombol2", labelTombol2, 36, aksi2);
                LetakkanTombol(tombol2, new Vector2(0.72f, 0.12f), new Vector2(340f, 100f));
            }
        }
    }

    private static void Regangkan(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color warna);
        return warna;
    }
}
