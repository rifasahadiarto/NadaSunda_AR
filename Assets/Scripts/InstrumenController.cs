using System;
using System.Collections.Generic;
using UnityEngine;

// Ngurus instrumen-instrumen yang udah ditaruh di scene AR.
// User pilih tipe dari list, taruh sendiri di bidang datar, instrumen
// baru otomatis jadi aktif. Bisa pindah aktif, bisa dihapus satu-satu.
// Tiap tipe cuma boleh ditaruh sekali.
public class InstrumenController : MonoBehaviour
{
    // diisi lewat Inspector: Angklung, Saron, Kendang, Kacapi, Goong
    [SerializeField] private InstrumenModel[] daftarInstrumen;

    // instance yang udah ditaruh, tipeDitempatkan[i] itu index tipe buat instrumenDitempatkan[i]
    private readonly List<GameObject> instrumenDitempatkan = new List<GameObject>();
    private readonly List<int> tipeDitempatkan = new List<int>();

    // instrumen yang lagi aktif, null cuma sebelum ada instrumen sama sekali
    private GameObject instrumenAktif;

    public event Action<InstrumenModel> OnInstrumenDitempatkan;
    public event Action<InstrumenModel> OnInstrumenAktifBerubah;
    public event Action OnInstrumenDihapus;
    public event Action OnSemuaInstrumenKosong;

    public bool SudahDitempatkan => instrumenDitempatkan.Count > 0;

    public bool SemuaTipeSudahDitempatkan => instrumenDitempatkan.Count >= daftarInstrumen.Length;

    public bool AdaYangAktif => instrumenAktif != null;

    public bool TipeSudahDitempatkan(int tipeIndex) => tipeDitempatkan.Contains(tipeIndex);

    public InstrumenModel GetModel(int tipeIndex) => daftarInstrumen[tipeIndex];

    public int JumlahTipe => daftarInstrumen.Length;

    public InstrumenModel GetInstrumenAktif()
    {
        int tipe = TipeDariObjek(instrumenAktif);
        return tipe >= 0 ? daftarInstrumen[tipe] : null;
    }

    public GameObject GetObjekAktif() => instrumenAktif;

    // naruh instrumen baru di posisi hasil raycast bidang datar
    public void TempatkanInstrumen(int tipeIndex, Pose pose)
    {
        if (tipeIndex < 0 || tipeIndex >= daftarInstrumen.Length) return;
        if (TipeSudahDitempatkan(tipeIndex)) return;

        Quaternion rotasiHadapKamera = HitungRotasiMenghadapKamera(pose.position);
        GameObject objekBaru = Instantiate(daftarInstrumen[tipeIndex].prefab, pose.position, rotasiHadapKamera);
        instrumenDitempatkan.Add(objekBaru);
        tipeDitempatkan.Add(tipeIndex);

        OnInstrumenDitempatkan?.Invoke(daftarInstrumen[tipeIndex]);

        SetInstrumenAktif(objekBaru);
    }

    // jadiin target instrumen aktif
    public void SetInstrumenAktif(GameObject target)
    {
        int tipe = TipeDariObjek(target);
        if (tipe < 0) return;

        instrumenAktif = target;

        OnInstrumenAktifBerubah?.Invoke(daftarInstrumen[tipe]);
    }

    // hapus instrumen, kalo yang dihapus lagi aktif otomatis pindah ke yang lain (atau balik ke list kalo kosong)
    public void HapusInstrumen(GameObject target)
    {
        int posisi = PosisiDariObjek(target);
        if (posisi < 0) return;

        bool targetAdalahAktif = target == instrumenAktif;

        instrumenDitempatkan.RemoveAt(posisi);
        tipeDitempatkan.RemoveAt(posisi);
        Destroy(target);

        if (targetAdalahAktif)
        {
            instrumenAktif = null;
            if (instrumenDitempatkan.Count > 0)
            {
                SetInstrumenAktif(instrumenDitempatkan[instrumenDitempatkan.Count - 1]);
            }
        }

        if (instrumenDitempatkan.Count == 0)
        {
            OnSemuaInstrumenKosong?.Invoke();
        }
        else
        {
            OnInstrumenDihapus?.Invoke();
        }
    }

    // cari instrumen (root) yang punya transform ini, buat tau instrumen apa yg kesentuh
    public GameObject GetObjekInstrumen(Transform target)
    {
        foreach (GameObject objek in instrumenDitempatkan)
        {
            if (target.IsChildOf(objek.transform)) return objek;
        }
        return null;
    }

    // true kalo transform ini bagian dari instrumen yang lagi aktif
    public bool ApakahBagianInstrumenAktif(Transform target)
    {
        return instrumenAktif != null && target.IsChildOf(instrumenAktif.transform);
    }

    // buat instrumen yang badannya gak punya collider (Kendang/Goong),
    // cari lewat bounding box aja biar tetep bisa dipilih jadi aktif
    public GameObject CariInstrumenPadaRay(Ray ray)
    {
        GameObject terdekat = null;
        float jarakTerdekat = float.MaxValue;
        foreach (GameObject objek in instrumenDitempatkan)
        {
            Bounds? bounds = HitungBoundsGabungan(objek);
            if (bounds.HasValue && bounds.Value.IntersectRay(ray, out float jarak) && jarak < jarakTerdekat)
            {
                jarakTerdekat = jarak;
                terdekat = objek;
            }
        }
        return terdekat;
    }

    // ---------- helper ----------

    private int PosisiDariObjek(GameObject objek)
    {
        return objek == null ? -1 : instrumenDitempatkan.IndexOf(objek);
    }

    private int TipeDariObjek(GameObject objek)
    {
        int posisi = PosisiDariObjek(objek);
        return posisi >= 0 ? tipeDitempatkan[posisi] : -1;
    }

    private static Bounds? HitungBoundsGabungan(GameObject objek)
    {
        Renderer[] daftarRenderer = objek.GetComponentsInChildren<Renderer>();
        if (daftarRenderer.Length == 0) return null;

        Bounds gabungan = daftarRenderer[0].bounds;
        foreach (Renderer r in daftarRenderer) gabungan.Encapsulate(r.bounds);
        return gabungan;
    }

    // hitung rotasi biar instrumen menghadap kamera, cuma muter horizontal (yaw) doang
    private static Quaternion HitungRotasiMenghadapKamera(Vector3 posisiObjek)
    {
        Camera kamera = Camera.main;
        if (kamera == null) return Quaternion.identity;

        Vector3 arahKeKamera = kamera.transform.position - posisiObjek;
        arahKeKamera.y = 0f;

        // jaga-jaga kalo kamera tepat di atas titik penempatan
        return arahKeKamera.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(arahKeKamera)
            : Quaternion.identity;
    }
}
