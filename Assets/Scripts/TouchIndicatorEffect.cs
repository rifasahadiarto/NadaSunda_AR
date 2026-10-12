using UnityEngine;

// Indikator visual singkat di TITIK sentuh instrumen: riak lingkaran yang
// membesar lalu memudar. Dipanggil TouchRaycaster SETELAH audio berhasil
// dibunyiin (bukan sebelum) biar sinkron sama bunyinya.
//
// Dipakai buat Saron (per bilah), Kacapi (per dawai), Kendang (per zona
// gedug/kempyang), dan Goong (badan). Angklung TIDAK pakai ini karena
// dimainkan dengan cara digoyang, bukan diketuk di satu titik.
//
// Komponen ini ditaruh SEKALI aja di scene (mis. di objek yang sama dengan
// TouchRaycaster) lalu di-assign ke field "touchIndicator" di TouchRaycaster.
// Gak perlu ditempel ke tiap prefab instrumen -- posisinya ditentukan dari
// hit.point raycast, jadi cukup satu spawner global.
public class TouchIndicatorEffect : MonoBehaviour
{
    [Header("Opsional")]
    [Tooltip("Prefab efek sendiri (particle/sprite ripple). Kalau kosong, " +
             "dipakai riak lingkaran bawaan yang dibuat lewat kode.")]
    [SerializeField] private GameObject prefabIndikator;

    [Tooltip("Material buat riak bawaan. Kalau kosong, kode nyari shader " +
             "'Sprites/Default' -- pastikan shader itu masuk Always Included " +
             "Shaders (Project Settings > Graphics) biar gak ke-strip di build.")]
    [SerializeField] private Material materialRiak;

    [Header("Tuning riak bawaan")]
    [SerializeField] private float durasi = 0.4f;          // 0.3-0.5 dtk lalu hilang
    [SerializeField] private float radiusAwal = 0.012f;    // meter
    [SerializeField] private float radiusAkhir = 0.06f;    // meter
    [SerializeField] private Color warna = new Color(1f, 0.82f, 0.4f, 1f); // aksen emas
    [SerializeField] private int segmen = 32;
    [SerializeField] private float lebarGaris = 0.004f;

    // tampilkan indikator di titik dunia hasil raycast.
    // normalPermukaan dipakai biar riaknya nempel rata di permukaan yang disentuh.
    public void Tampilkan(Vector3 posisiDunia, Vector3 normalPermukaan)
    {
        Vector3 arahHadap = normalPermukaan.sqrMagnitude > 0.0001f
            ? normalPermukaan
            : Vector3.up;

        // sedikit diangkat dari permukaan biar gak z-fighting sama badan instrumen
        Vector3 posisi = posisiDunia + arahHadap * 0.002f;

        if (prefabIndikator != null)
        {
            GameObject efek = Instantiate(
                prefabIndikator, posisi, Quaternion.LookRotation(arahHadap));
            Destroy(efek, durasi);
            return;
        }

        BuatRiakBawaan(posisi, arahHadap);
    }

    private void BuatRiakBawaan(Vector3 posisi, Vector3 arahHadap)
    {
        GameObject go = new GameObject("RiakSentuh");
        go.transform.SetPositionAndRotation(posisi, Quaternion.LookRotation(arahHadap));

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.positionCount = segmen;
        lr.widthMultiplier = lebarGaris;
        lr.alignment = LineAlignment.TransformZ;
        lr.textureMode = LineTextureMode.Stretch;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.material = materialRiak != null
            ? materialRiak
            : new Material(Shader.Find("Sprites/Default"));

        go.AddComponent<RiakSentuhAnim>().Mulai(
            lr, durasi, radiusAwal, radiusAkhir, warna, segmen);
    }

    // animator kecil yang nempel di objek riak; ngurus animasi membesar +
    // fade out lalu hancurin dirinya sendiri setelah durasi habis
    private class RiakSentuhAnim : MonoBehaviour
    {
        private LineRenderer lr;
        private float durasi;
        private float radiusAwal;
        private float radiusAkhir;
        private Color warna;
        private int segmen;
        private float waktuBerjalan;

        public void Mulai(LineRenderer garis, float durasi, float radiusAwal,
                          float radiusAkhir, Color warna, int segmen)
        {
            lr = garis;
            this.durasi = durasi;
            this.radiusAwal = radiusAwal;
            this.radiusAkhir = radiusAkhir;
            this.warna = warna;
            this.segmen = segmen;
            Gambar(radiusAwal, 1f);
        }

        private void Update()
        {
            waktuBerjalan += Time.deltaTime;
            float progres = durasi > 0f ? Mathf.Clamp01(waktuBerjalan / durasi) : 1f;

            Gambar(Mathf.Lerp(radiusAwal, radiusAkhir, progres), 1f - progres);

            if (progres >= 1f) Destroy(gameObject);
        }

        private void Gambar(float radius, float alpha)
        {
            for (int i = 0; i < segmen; i++)
            {
                float sudut = (i / (float)segmen) * Mathf.PI * 2f;
                lr.SetPosition(i, new Vector3(
                    Mathf.Cos(sudut) * radius,
                    Mathf.Sin(sudut) * radius,
                    0f));
            }

            Color c = warna;
            c.a = alpha;
            lr.startColor = c;
            lr.endColor = c;
        }
    }
}
