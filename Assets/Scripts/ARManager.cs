using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.XR.ARFoundation;

// Ngurus sesi ARCore: minta izin kamera, cek HP-nya support ARCore apa
// nggak, mulai deteksi, pantau bidang datar. Mancarin event doang,
// gak nyentuh UI langsung, biar UIManager yang atur tampilannya.
public class ARManager : MonoBehaviour
{
    [SerializeField] private ARSession arSession;
    [SerializeField] private ARPlaneManager planeManager;

    // batas waktu nyari bidang datar sebelum dianggap gagal (E-03)
    [SerializeField] private float batasWaktuDeteksi = 20f;

    public event Action OnBidangDatarTerdeteksi;
    public event Action OnTimeoutDeteksi;
    public event Action OnPetunjukPencahayaan;
    public event Action OnIzinKameraDitolak;

    public bool AdaBidangDatar { get; private set; }

    // waktu munculin hint pencahayaan, separuh dari batas waktu deteksi
    private const float WaktuHintPencahayaan = 8f;

    // status tampil/sembunyi bidang datar, disimpen karena ARCore terus nambah plane baru
    private bool visualBidangTampil = true;

    private Coroutine timerTimeout;
    private Coroutine timerHint;

    private void OnEnable()
    {
        if (planeManager != null) planeManager.planesChanged += TanganiPerubahanBidang;
    }

    private void OnDisable()
    {
        if (planeManager != null) planeManager.planesChanged -= TanganiPerubahanBidang;
    }

    private void Start()
    {
        // biar layar gak mati sendiri pas user cuma megang HP tanpa nyentuh
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        // arSession cuma keisi di ARScene, di MainMenu ini gak jalan
        if (arSession != null) MintaIzinKameraLaluMulai();
    }

    // pastiin izin kamera ada dulu sebelum mulai deteksi
    private void MintaIzinKameraLaluMulai()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += _ => MulaiDeteksi();
            callbacks.PermissionDenied += _ => OnIzinKameraDitolak?.Invoke();
            callbacks.PermissionDeniedAndDontAskAgain += _ => OnIzinKameraDitolak?.Invoke();
            Permission.RequestUserPermission(Permission.Camera, callbacks);
            return;
        }
#endif
        MulaiDeteksi();
    }

    // dipanggil tombol "Coba Lagi" di E-02
    public void CobaIzinKameraLagi()
    {
        MintaIzinKameraLaluMulai();
    }

    // buka halaman setting aplikasi buat nyalain izin kamera manual
    public void BukaPengaturanAplikasi()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
        using (var uriClass = new AndroidJavaClass("android.net.Uri"))
        {
            AndroidJavaObject uriObject = uriClass.CallStatic<AndroidJavaObject>(
                "fromParts", "package", Application.identifier, null);
            AndroidJavaObject intentObject = new AndroidJavaObject(
                "android.content.Intent", "android.settings.APPLICATION_DETAILS_SETTINGS", uriObject);
            intentObject.Call<AndroidJavaObject>("addCategory", "android.intent.category.DEFAULT");
            intentObject.Call<AndroidJavaObject>("setFlags", 0x10000000);
            currentActivity.Call("startActivity", intentObject);
        }
#endif
    }

    // mulai/ulang timer nyari bidang datar
    public void MulaiDeteksi()
    {
        if (timerTimeout != null) StopCoroutine(timerTimeout);
        if (timerHint != null) StopCoroutine(timerHint);
        timerTimeout = StartCoroutine(TimerBatasWaktuDeteksi());
        timerHint = StartCoroutine(TimerHintPencahayaan());
    }

    private IEnumerator TimerBatasWaktuDeteksi()
    {
        yield return new WaitForSeconds(batasWaktuDeteksi);
        if (!AdaBidangDatar) OnTimeoutDeteksi?.Invoke();
    }

    // munculin hint di tengah2 kalo belum ketemu bidang datar
    private IEnumerator TimerHintPencahayaan()
    {
        yield return new WaitForSeconds(WaktuHintPencahayaan);
        if (!AdaBidangDatar) OnPetunjukPencahayaan?.Invoke();
    }

    private void TanganiPerubahanBidang(ARPlanesChangedEventArgs args)
    {
        // trigger event sekali doang pas bidang pertama ketemu
        if (!AdaBidangDatar && (args.added.Count > 0 || planeManager.trackables.count > 0))
        {
            AdaBidangDatar = true;
            OnBidangDatarTerdeteksi?.Invoke();
        }

        // plane baru yang muncul belakangan ikut disembunyiin juga
        if (!visualBidangTampil)
        {
            foreach (ARPlane plane in args.added) TerapkanVisualBidang(plane, false);
        }
    }

    // nyala/matiin bercak putih bidang datar (deteksi tetep jalan)
    public void SetVisualBidang(bool tampil)
    {
        visualBidangTampil = tampil;
        if (planeManager == null) return;

        foreach (ARPlane plane in planeManager.trackables)
        {
            TerapkanVisualBidang(plane, tampil);
        }
    }

    private static void TerapkanVisualBidang(ARPlane plane, bool tampil)
    {
        plane.gameObject.SetActive(tampil);
    }

    // cek HP support ARCore apa nggak, hasilnya lewat callback karena prosesnya async
    public void CekKompatibilitasPerangkat(Action<bool> hasil)
    {
        StartCoroutine(CekKompatibilitasCoroutine(hasil));
    }

    private IEnumerator CekKompatibilitasCoroutine(Action<bool> hasil)
    {
#if UNITY_EDITOR
        // di Editor gak ada ARCore, anggap aja didukung
        yield return null;
        hasil?.Invoke(true);
#else
        if (ARSession.state == ARSessionState.None ||
            ARSession.state == ARSessionState.CheckingAvailability)
        {
            yield return ARSession.CheckAvailability();
        }

        if (ARSession.state == ARSessionState.NeedsInstall)
        {
            yield return ARSession.Install();
        }

        bool didukung = ARSession.state == ARSessionState.Ready ||
                        ARSession.state == ARSessionState.SessionInitializing ||
                        ARSession.state == ARSessionState.SessionTracking;
        hasil?.Invoke(didukung);
#endif
    }
}
