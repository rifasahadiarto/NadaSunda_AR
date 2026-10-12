using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

// Nangkep sentuhan layar terus diterjemahin jadi aksi AR.
// Mode PLACEMENT (lagi milih tempat naruh instrumen) sama mode
// INTERAKSI (instrumen udah ada, tinggal disentuh) beda logikanya.
public class TouchRaycaster : MonoBehaviour
{
    [SerializeField] private Camera kameraAR;
    [SerializeField] private ARRaycastManager arRaycastManager;
    [SerializeField] private InstrumenController instrumenController;
    [SerializeField] private AudioPlayer audioPlayer;

    // spawner indikator visual di titik sentuh (Saron/Kacapi/Kendang/Goong).
    // opsional: kalau belum di-assign, interaksi tetep jalan tanpa indikator
    [SerializeField] private TouchIndicatorEffect touchIndicator;

    // jarak maksimal raycast fisika
    [SerializeField] private float jarakMaksimumRaycast = 20f;

    // --- tuning gesture "menggoyangkan" Angklung ---
    // Angklung bunyi kalau jari digesek bolak-balik kiri-kanan cepat di atas
    // badannya (meniru cara main aslinya), BUKAN diketuk sekali.
    // minimal jumlah pergantian arah (kiri->kanan / kanan->kiri) yang harus
    // terjadi dalam satu rentetan biar dianggap "digoyang"
    [SerializeField] private int minBalikArahGoyang = 2;
    // tiap segmen gerakan satu arah harus lebih panjang dari nilai ini (piksel)
    // biar getar kecil / jitter jari gak kehitung sebagai goyangan
    [SerializeField] private float jarakMinPerArahGoyang = 25f;
    // rentetan pergantian arah harus selesai dalam jendela waktu ini (detik),
    // lewat dari ini hitungannya direset (berarti gerakannya lambat, bukan goyang)
    [SerializeField] private float jendelaWaktuGoyang = 0.5f;

    // list dipake ulang biar gak bikin sampah memori tiap frame
    private static readonly List<ARRaycastHit> hasilHitAR = new List<ARRaycastHit>();

    // tipe instrumen yang lagi nunggu ditempatin, null = bukan mode placement
    private int? tipeInstrumenTerpilih;

    // true kalo popup info lagi kebuka, audio dimatiin sementara
    private bool modeInfoAktif;

    // dawai yang udah bunyi dalam satu sapuan genjrengan sekarang,
    // biar gak bunyi berulang kalo jari diem di dawai yang sama
    private readonly HashSet<Transform> dawaiTersentuhSaatIniStroke = new HashSet<Transform>();

    // pelacakan "goyangan" Angklung: true selama jari nempel & mulai dari
    // badan Angklung yang lagi aktif (bukan tap biasa, Angklung baru bunyi
    // kalo digesek bolak-balik kanan-kiri, meniru cara main aslinya)
    private bool sedangGoyangAngklung;
    private GameObject objekGoyangSaatIni;
    private float posisiXTerakhirGoyang;
    // arah gesekan horizontal terakhir: -1 kiri, +1 kanan, 0 belum ada
    private int arahGoyangTerakhir;
    // jarak yang udah terkumpul di segmen arah yang sekarang lagi jalan
    private float jarakSegmenGoyang;
    // berapa kali arah udah berbalik dalam rentetan goyangan sekarang
    private int jumlahBalikArahGoyang;
    // kapan pergantian arah pertama di rentetan ini terjadi (buat cek jendela waktu)
    private float waktuMulaiRentetanGoyang;

    // dipanggil UI pas user milih card instrumen dari list
    public void PilihTipeUntukDitempatkan(int tipeIndex)
    {
        tipeInstrumenTerpilih = tipeIndex;
    }

    // batalin mode placement tanpa naruh apa-apa
    public void BatalkanPenempatan()
    {
        tipeInstrumenTerpilih = null;
    }

    public void SetModeInfoAktif(bool aktif)
    {
        modeInfoAktif = aktif;
    }

    private void Update()
    {
        if (AdaSentuhanBaru(out Vector2 posisiSentuh))
        {
            // sapuan genjrengan & pelacakan goyangan baru mulai dari nol tiap sentuhan baru
            dawaiTersentuhSaatIniStroke.Clear();
            sedangGoyangAngklung = false;
            ResetPelacakanGoyang();
            ProsesSentuhan(posisiSentuh);
        }
        else if (AdaSentuhanBergerak(out Vector2 posisiGerak))
        {
            ProsesGenjrengan(posisiGerak);
            ProsesGoyangan(posisiGerak);
        }
    }

    // true cuma di frame pas sentuhan BARU dimulai
    private bool AdaSentuhanBaru(out Vector2 posisi)
    {
        posisi = default;
#if UNITY_EDITOR
        // simulasi sentuhan pake klik mouse di Editor
        if (Input.GetMouseButtonDown(0) &&
            (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
        {
            posisi = Input.mousePosition;
            return true;
        }
        return false;
#else
        if (Input.touchCount == 0) return false;

        Touch sentuhan = Input.GetTouch(0);
        if (sentuhan.phase != TouchPhase.Began) return false;

        // abaikan sentuhan yang kena tombol UI
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject(sentuhan.fingerId)) return false;

        posisi = sentuhan.position;
        return true;
#endif
    }

    private void ProsesSentuhan(Vector2 posisiLayar)
    {
        if (tipeInstrumenTerpilih.HasValue)
        {
            ProsesSentuhanPlacement(posisiLayar);
        }
        else
        {
            ProsesSentuhanInteraksi(posisiLayar);
        }
    }

    // mode PLACEMENT: sentuh bidang datar buat naruh instrumen
    private void ProsesSentuhanPlacement(Vector2 posisiLayar)
    {
        // harus di dalem area bidang yang beneran kedeteksi, bukan perpanjangannya
        if (arRaycastManager.Raycast(posisiLayar, hasilHitAR, TrackableType.PlaneWithinPolygon))
        {
            instrumenController.TempatkanInstrumen(tipeInstrumenTerpilih.Value, hasilHitAR[0].pose);
            tipeInstrumenTerpilih = null;
        }
    }

    // true kalo jari lagi digeser (bukan cuma nyentuh awal), buat genjrengan Kacapi
    private bool AdaSentuhanBergerak(out Vector2 posisi)
    {
        posisi = default;
#if UNITY_EDITOR
        // simulasi drag pake mouse ditahan sambil digeser
        if (Input.GetMouseButton(0) &&
            (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
        {
            posisi = Input.mousePosition;
            return true;
        }
        return false;
#else
        if (Input.touchCount == 0) return false;

        Touch sentuhan = Input.GetTouch(0);
        if (sentuhan.phase != TouchPhase.Moved) return false;

        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject(sentuhan.fingerId)) return false;

        posisi = sentuhan.position;
        return true;
#endif
    }

    // genjrengan khusus Kacapi: jari disapu, tiap dawai baru yang kesentuh langsung bunyi.
    // Saron TETEP harus diketuk satu-satu, gak ikut mode ini.
    private void ProsesGenjrengan(Vector2 posisiLayar)
    {
        if (!instrumenController.SudahDitempatkan) return;
        if (modeInfoAktif) return;

        InstrumenModel aktif = instrumenController.GetInstrumenAktif();
        if (aktif == null || aktif.nama != "Kacapi") return;

        Ray ray = kameraAR.ScreenPointToRay(posisiLayar);
        if (!Physics.Raycast(ray, out RaycastHit hit, jarakMaksimumRaycast)) return;
        if (!instrumenController.ApakahBagianInstrumenAktif(hit.transform)) return;

        BilahSaron dawai = hit.collider.GetComponentInParent<BilahSaron>();
        if (dawai == null) return;

        // udah kesentuh di sapuan ini, jangan bunyiin lagi
        if (!dawaiTersentuhSaatIniStroke.Add(dawai.transform)) return;

        // indikator sentuh cuma muncul kalo audio beneran keluar (sinkron sama bunyi)
        if (audioPlayer.MainkanNada(dawai.indexNada, dawai.audioClipNada))
        {
            touchIndicator?.Tampilkan(hit.point, hit.normal);
        }
    }

    private void ResetPelacakanGoyang()
    {
        arahGoyangTerakhir = 0;
        jarakSegmenGoyang = 0f;
        jumlahBalikArahGoyang = 0;
    }

    // gesture "menggoyangkan" Angklung: jari digesek bolak-balik kiri-kanan
    // cepat di atas badan Angklung yang lagi aktif. Angklung baru bunyi kalau
    // arah gesekan udah berbalik minimal `minBalikArahGoyang` kali dalam
    // jendela `jendelaWaktuGoyang` detik, dengan tiap segmen satu arah lebih
    // panjang dari `jarakMinPerArahGoyang` piksel. Ini niru cara main aslinya
    // (digoyang, bukan diketuk) sekaligus mastiin drag pelan / satu arah doang
    // (mis. pas muter objek) gak ikut ke-trigger.
    private void ProsesGoyangan(Vector2 posisiLayar)
    {
        if (!sedangGoyangAngklung || modeInfoAktif) return;

        float deltaX = posisiLayar.x - posisiXTerakhirGoyang;
        posisiXTerakhirGoyang = posisiLayar.x;

        int arahSekarang = deltaX > 0f ? 1 : (deltaX < 0f ? -1 : 0);
        if (arahSekarang == 0) return;

        // rentetan yang kelamaan dianggap batal (gerakannya lambat, bukan goyang)
        if (jumlahBalikArahGoyang > 0 &&
            Time.time - waktuMulaiRentetanGoyang > jendelaWaktuGoyang)
        {
            jumlahBalikArahGoyang = 0;
        }

        // masih gerak di arah yang sama (atau gerakan pertama) -> tambah panjang segmen
        if (arahGoyangTerakhir == 0 || arahSekarang == arahGoyangTerakhir)
        {
            jarakSegmenGoyang += Mathf.Abs(deltaX);
            arahGoyangTerakhir = arahSekarang;
            return;
        }

        // ---- arah berbalik di sini ----
        // cuma dihitung kalau segmen arah sebelumnya udah cukup panjang
        if (jarakSegmenGoyang >= jarakMinPerArahGoyang)
        {
            if (jumlahBalikArahGoyang == 0) waktuMulaiRentetanGoyang = Time.time;
            jumlahBalikArahGoyang++;
        }

        arahGoyangTerakhir = arahSekarang;
        jarakSegmenGoyang = Mathf.Abs(deltaX);

        if (jumlahBalikArahGoyang < minBalikArahGoyang) return;

        // instrumen aktif bisa saja udah ganti di tengah geseran (mis. lewat
        // tombol UI), pastikan Angklung yang dilacak ini masih yang aktif
        if (objekGoyangSaatIni == null ||
            !instrumenController.ApakahBagianInstrumenAktif(objekGoyangSaatIni.transform))
        {
            sedangGoyangAngklung = false;
            return;
        }

        InstrumenModel aktif = instrumenController.GetInstrumenAktif();
        if (aktif == null) return;

        // debounce 0,5 dtk ditangani di dalam AudioPlayer (index suara utama),
        // jadi gesture yang lanjut terus gak bikin bunyi numpuk
        if (audioPlayer.MainkanSuaraInstrumen(aktif.audioClip))
        {
            objekGoyangSaatIni.GetComponent<AngklungShakeEffect>()?.Mainkan();
        }

        // siap-siap buat rentetan goyangan berikutnya dalam sentuhan yang sama
        jumlahBalikArahGoyang = 0;
        jarakSegmenGoyang = 0f;
    }

    // mode INTERAKSI: sentuh instrumen yang udah berdiri
    private void ProsesSentuhanInteraksi(Vector2 posisiLayar)
    {
        if (!instrumenController.SudahDitempatkan) return;

        Ray ray = kameraAR.ScreenPointToRay(posisiLayar);
        if (Physics.Raycast(ray, out RaycastHit hit, jarakMaksimumRaycast))
        {
            GameObject instrumenTerkena = instrumenController.GetObjekInstrumen(hit.transform);

            if (instrumenTerkena != null &&
                !instrumenController.ApakahBagianInstrumenAktif(hit.transform))
            {
                // sentuh instrumen lain = pindah aktif doang, gak bunyi
                instrumenController.SetInstrumenAktif(instrumenTerkena);
                return;
            }

            if (modeInfoAktif) return; // popup info kebuka, audio dimatiin dulu

            // cari komponen BilahSaron di collider yang kesentuh (bilah/dawai)
            BilahSaron bilah = hit.collider.GetComponentInParent<BilahSaron>();
            if (bilah != null)
            {
                // indikator sentuh cuma muncul kalo audio beneran keluar (bukan
                // lagi kena debounce), biar sinkron sama bunyi
                if (audioPlayer.MainkanNada(bilah.indexNada, bilah.audioClipNada))
                {
                    touchIndicator?.Tampilkan(hit.point, hit.normal);
                }

                // efek visual doang, gak ngaruh ke audio di atas (Kacapi gak
                // punya komponen ini di dawainya, jadi aman null)
                bilah.GetComponent<SaronBounceEffect>()?.Mainkan();
                return;
            }

            // bukan bilah, tapi masih bagian instrumen aktif -> suara utama badan
            if (instrumenController.ApakahBagianInstrumenAktif(hit.transform))
            {
                InstrumenModel aktifSaatIni = instrumenController.GetInstrumenAktif();

                if (aktifSaatIni != null && aktifSaatIni.nama == "Angklung")
                {
                    // Angklung dimainkan dengan CARA DIGOYANG (gesek bolak-balik
                    // kiri-kanan), bukan ditekan sekali kayak instrumen lain --
                    // sentuhan awal ini cuma mulai pelacakan gesekan, belum bunyi.
                    // Lihat ProsesGoyangan.
                    sedangGoyangAngklung = true;
                    objekGoyangSaatIni = instrumenTerkena;
                    posisiXTerakhirGoyang = posisiLayar.x;
                    ResetPelacakanGoyang();
                    return;
                }

                // indikator sentuh cuma muncul kalo audio beneran keluar, biar
                // sinkron sama bunyi (Goong lewat sini; Angklung bunyi lewat
                // ProsesGoyangan)
                if (audioPlayer.MainkanSuaraInstrumen(aktifSaatIni.audioClip))
                {
                    touchIndicator?.Tampilkan(hit.point, hit.normal);
                }

                // efek visual doang, gak ngaruh ke audio di atas
                instrumenTerkena.GetComponent<GoongPulseEffect>()?.Mainkan();
                return;
            }
        }

        // Kendang/Goong badannya gak ada collider, jadi dicek pake bounding box biar tetep bisa dipilih
        GameObject dipilihLewatBounds = instrumenController.CariInstrumenPadaRay(ray);
        if (dipilihLewatBounds != null && !instrumenController.ApakahBagianInstrumenAktif(dipilihLewatBounds.transform))
        {
            instrumenController.SetInstrumenAktif(dipilihLewatBounds);
        }
    }
}
