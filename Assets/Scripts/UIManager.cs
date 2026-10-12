using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Ngatur pindah-pindah halaman & nampilin pesan error.
// Satu kelas ini dipake buat DUA scene (MainMenu sama ARScene),
// panel yang gak dipake di suatu scene dibiarin kosong aja.
public class UIManager : MonoBehaviour
{
    [Header("Referensi Manager")]
    [SerializeField] private ARManager arManager;
    [SerializeField] private InstrumenController instrumenController;
    [SerializeField] private TouchRaycaster touchRaycaster;

    // musik latar Menu Utama, tombol suara cuma ngatur mute-nya
    [SerializeField] private AudioSource musikLatar;

    [Header("Halaman Scene MainMenu")]
    [SerializeField] private GameObject panelSplash;        // T-00
    [SerializeField] private GameObject panelMenuUtama;     // T-01
    [SerializeField] private GameObject panelPanduan;       // T-03
    [SerializeField] private GameObject panelTentang;       // T-04
    [SerializeField] private GameObject panelErrorE01;      // E-01

    // splash minimal tampil sekian detik biar gak berkedip doang
    [SerializeField] private float durasiMinimalSplash = 2.5f;

    [Header("Halaman Scene ARScene")]
    [SerializeField] private GameObject panelScanning;         // T-02A
    [SerializeField] private GameObject panelListInstrumen;    // T-02B
    [SerializeField] private GameObject panelPlacement;        // T-02C
    [SerializeField] private GameObject panelOverlayInstrumen; // T-02D
    [SerializeField] private GameObject panelErrorE02;         // E-02
    [SerializeField] private GameObject panelErrorE03;         // E-03
    [SerializeField] private GameObject popupInfo;
    [SerializeField] private Text teksNamaInstrumen;
    [SerializeField] private Text teksJudulPopup;
    [SerializeField] private Text teksDeskripsiPopup;
    [SerializeField] private GameObject tombolTambahInstrumen;
    [SerializeField] private Text teksInstruksiPlacement;

    // banner hint pencahayaan, anak dari panelScanning jadi ikut ilang pas panelnya diganti
    [SerializeField] private GameObject bannerHintPencahayaan;

    // card instrumen di list, urutannya sama kayak daftarInstrumen di InstrumenController
    [SerializeField] private Button[] kartuInstrumen;

    private void Start()
    {
        // deteksi lagi di scene mana dari panel mana yang keisi
        if (panelSplash != null) MulaiAlurSplash();
        if (panelScanning != null) MulaiAlurAR();
    }

    private void OnDestroy()
    {
        // wajib unsubscribe biar gak error pas objeknya udah mati (pindah scene)
        if (arManager != null)
        {
            arManager.OnBidangDatarTerdeteksi -= TanganiBidangTerdeteksi;
            arManager.OnTimeoutDeteksi -= TanganiTimeoutDeteksi;
            arManager.OnIzinKameraDitolak -= TanganiIzinKameraDitolak;
            arManager.OnPetunjukPencahayaan -= TanganiPetunjukPencahayaan;
        }
        if (instrumenController != null)
        {
            instrumenController.OnInstrumenDitempatkan -= TanganiInstrumenDitempatkan;
            instrumenController.OnInstrumenAktifBerubah -= TanganiInstrumenAktifBerubah;
            instrumenController.OnInstrumenDihapus -= TanganiInstrumenDihapus;
            instrumenController.OnSemuaInstrumenKosong -= TanganiSemuaInstrumenKosong;
        }
    }

    // ---------- alur scene MainMenu ----------

    private void MulaiAlurSplash()
    {
        TampilkanPanel(panelSplash);
        StartCoroutine(RutinSplash());
    }

    // splash tampil dulu sambil ngecek ARCore, baru lanjut ke menu atau error
    private IEnumerator RutinSplash()
    {
        float waktuMulai = Time.time;
        bool cekSelesai = false;
        bool perangkatDidukung = false;

        arManager.CekKompatibilitasPerangkat(hasil =>
        {
            cekSelesai = true;
            perangkatDidukung = hasil;
        });

        // nunggu dua-duanya: pengecekan selesai DAN durasi minimal splash lewat
        while (!cekSelesai || Time.time - waktuMulai < durasiMinimalSplash)
        {
            yield return null;
        }

        TampilkanPanel(perangkatDidukung ? panelMenuUtama : panelErrorE01);
    }

    // method-method ini dipanggil dari tombol UI, makanya public
    public void MulaiAR() { SceneManager.LoadScene("ARScene"); }
    public void BukaPanduan() { TampilkanPanel(panelPanduan); }
    public void BukaTentang() { TampilkanPanel(panelTentang); }
    public void KembaliKeMenuUtama() { TampilkanPanel(panelMenuUtama); }
    public void KembaliKeSceneMenu() { SceneManager.LoadScene("MainMenu"); }
    public void KeluarAplikasi() { Application.Quit(); }

    // tombol ikon SoundOff
    public void MatikanMusik() { if (musikLatar != null) musikLatar.mute = true; }

    // tombol ikon SoundOn
    public void NyalakanMusik() { if (musikLatar != null) musikLatar.mute = false; }

    // ---------- alur scene ARScene ----------

    private void MulaiAlurAR()
    {
        // subscribe event dari ARManager & InstrumenController, bukan sebaliknya
        arManager.OnBidangDatarTerdeteksi += TanganiBidangTerdeteksi;
        arManager.OnTimeoutDeteksi += TanganiTimeoutDeteksi;
        arManager.OnIzinKameraDitolak += TanganiIzinKameraDitolak;
        arManager.OnPetunjukPencahayaan += TanganiPetunjukPencahayaan;
        instrumenController.OnInstrumenDitempatkan += TanganiInstrumenDitempatkan;
        instrumenController.OnInstrumenAktifBerubah += TanganiInstrumenAktifBerubah;
        instrumenController.OnInstrumenDihapus += TanganiInstrumenDihapus;
        instrumenController.OnSemuaInstrumenKosong += TanganiSemuaInstrumenKosong;

        if (bannerHintPencahayaan != null) bannerHintPencahayaan.SetActive(false);
        TampilkanPanel(panelScanning); // T-02A
    }

    private void TanganiIzinKameraDitolak()
    {
        TampilkanPanel(panelErrorE02); // E-02
    }

    // pengingat di tengah proses scanning
    private void TanganiPetunjukPencahayaan()
    {
        if (bannerHintPencahayaan != null) bannerHintPencahayaan.SetActive(true);
    }

    public void BukaPengaturanAplikasi() { arManager.BukaPengaturanAplikasi(); }

    public void CobaIzinKameraLagi() { arManager.CobaIzinKameraLagi(); }

    private void TanganiBidangTerdeteksi()
    {
        // jangan mundur ke list kalo udah ada instrumen berdiri
        if (!instrumenController.SudahDitempatkan)
        {
            PerbaruiKartuInstrumen();
            TampilkanPanel(panelListInstrumen); // T-02B
        }
    }

    // dipanggil pas card instrumen di-tap, masuk mode PLACEMENT
    public void PilihKartuInstrumen(int tipeIndex)
    {
        arManager.SetVisualBidang(true);
        touchRaycaster.PilihTipeUntukDitempatkan(tipeIndex);

        if (teksInstruksiPlacement != null)
        {
            InstrumenModel model = instrumenController.GetModel(tipeIndex);
            teksInstruksiPlacement.text = $"Sentuh area pada bidang datar untuk menempatkan {model.nama}.";
        }

        TampilkanPanel(panelPlacement); // T-02C
    }

    // tombol "+ Tambah Instrumen", instrumen lama tetep ada
    public void TambahInstrumenLagi()
    {
        arManager.SetVisualBidang(true);
        PerbaruiKartuInstrumen();
        TampilkanPanel(panelListInstrumen); // T-02B
    }

    // matiin card instrumen yang udah ditaruh
    private void PerbaruiKartuInstrumen()
    {
        if (kartuInstrumen == null) return;

        for (int i = 0; i < kartuInstrumen.Length; i++)
        {
            if (kartuInstrumen[i] != null)
            {
                kartuInstrumen[i].interactable = !instrumenController.TipeSudahDitempatkan(i);
            }
        }
    }

    private void TanganiInstrumenDitempatkan(InstrumenModel model)
    {
        TampilkanPanel(panelOverlayInstrumen); // T-02D

        // bidang datar disembunyiin lagi, udah gak kepake
        arManager.SetVisualBidang(false);
        PerbaruiTombolTambah();
    }

    // instrumen aktif pindah, update nama di banner
    private void TanganiInstrumenAktifBerubah(InstrumenModel model)
    {
        if (teksNamaInstrumen != null) teksNamaInstrumen.text = model.nama;
    }

    private void TanganiInstrumenDihapus()
    {
        PerbaruiTombolTambah();
        InstrumenModel aktif = instrumenController.GetInstrumenAktif();
        if (aktif != null && teksNamaInstrumen != null) teksNamaInstrumen.text = aktif.nama;
    }

    // instrumen terakhir dihapus, balik ke list
    private void TanganiSemuaInstrumenKosong()
    {
        PerbaruiKartuInstrumen();
        TampilkanPanel(panelListInstrumen); // T-02B
    }

    private void PerbaruiTombolTambah()
    {
        if (tombolTambahInstrumen != null)
        {
            tombolTambahInstrumen.SetActive(!instrumenController.SemuaTipeSudahDitempatkan);
        }
    }

    // tombol Hapus di overlay
    public void HapusInstrumenAktif()
    {
        instrumenController.HapusInstrumen(instrumenController.GetObjekAktif());
    }

    public void BukaModeInfo()
    {
        touchRaycaster.SetModeInfoAktif(true);
        BukaPopupInfo();
    }

    public void TutupModeInfo()
    {
        touchRaycaster.SetModeInfoAktif(false);
        TutupPopupInfo();
    }

    private void TanganiTimeoutDeteksi()
    {
        TampilkanPanel(panelErrorE03); // E-03
    }

    // tombol "Coba Lagi" di E-03, ulang scanning dari awal
    public void CobaLagiDeteksi()
    {
        if (bannerHintPencahayaan != null) bannerHintPencahayaan.SetActive(false);
        TampilkanPanel(panelScanning);
        arManager.MulaiDeteksi();
    }

    public void BukaPopupInfo()
    {
        // isi popup diambil pas tombol ditekan biar selalu sesuai instrumen aktif
        InstrumenModel model = instrumenController.GetInstrumenAktif();
        if (teksJudulPopup != null) teksJudulPopup.text = model.nama;
        if (teksDeskripsiPopup != null) teksDeskripsiPopup.text = model.deskripsi;
        if (popupInfo != null) popupInfo.SetActive(true);
    }

    public void TutupPopupInfo()
    {
        if (popupInfo != null) popupInfo.SetActive(false);
    }

    // matiin semua panel, nyalain satu doang
    private void TampilkanPanel(GameObject target)
    {
        GameObject[] semuaPanel =
        {
            panelSplash, panelMenuUtama, panelPanduan, panelTentang, panelErrorE01,
            panelScanning, panelListInstrumen, panelPlacement, panelOverlayInstrumen,
            panelErrorE02, panelErrorE03
        };

        foreach (GameObject panel in semuaPanel)
        {
            if (panel != null) panel.SetActive(panel == target);
        }
    }
}
