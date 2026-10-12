using System.Collections;
using UnityEngine;

// Efek goyang pas Angklung disentuh, cuma tampilan doang gak ngaruh
// ke audio. Dipicu bareng TouchRaycaster pas mainin suara utama.
public class AngklungShakeEffect : MonoBehaviour
{
    [SerializeField] private Transform targetVisual;
    [SerializeField] private float sudutGoyang = 8f;
    [SerializeField] private int jumlahGoyangan = 5;
    [SerializeField] private float durasiTotal = 0.5f;

    private Coroutine goyanganBerjalan;

    // posisi netral direkam sekali di awal, biar gak geser2 kalo disentuh berkali-kali cepet
    private Quaternion rotasiNetral;

    private void Awake()
    {
        if (targetVisual != null) rotasiNetral = targetVisual.localRotation;
    }

    public void Mainkan()
    {
        if (targetVisual == null) return;

        // stop goyangan lama biar gak numpuk
        if (goyanganBerjalan != null) StopCoroutine(goyanganBerjalan);
        goyanganBerjalan = StartCoroutine(RutinGoyang());
    }

    private IEnumerator RutinGoyang()
    {
        Quaternion rotasiAwal = rotasiNetral;
        float durasiPerAyunan = durasiTotal / (jumlahGoyangan * 2f);

        for (int i = 0; i < jumlahGoyangan; i++)
        {
            // sumbu X buat goyang kiri-kanan
            yield return PutarKe(rotasiAwal * Quaternion.Euler(sudutGoyang, 0f, 0f), durasiPerAyunan);
            yield return PutarKe(rotasiAwal * Quaternion.Euler(-sudutGoyang, 0f, 0f), durasiPerAyunan);
        }

        targetVisual.localRotation = rotasiAwal;
        goyanganBerjalan = null;
    }

    private IEnumerator PutarKe(Quaternion target, float durasi)
    {
        Quaternion mula = targetVisual.localRotation;
        float t = 0f;
        while (t < durasi)
        {
            t += Time.deltaTime;
            targetVisual.localRotation = Quaternion.Slerp(mula, target, t / durasi);
            yield return null;
        }
        targetVisual.localRotation = target;
    }
}
