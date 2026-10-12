using System.Collections;
using UnityEngine;

// Efek mengangguk pas pencu Goong dipukul, cuma tampilan doang.
// Dipicu bareng TouchRaycaster pas mainin suara utama Goong.
public class GoongPulseEffect : MonoBehaviour
{
    [SerializeField] private Transform targetVisual;
    [SerializeField] private float sudutAnggukan = 6f;
    [SerializeField] private int jumlahAnggukan = 2;
    [SerializeField] private float durasiTotal = 0.3f;

    private Coroutine pulseBerjalan;

    // posisi netral direkam sekali di awal biar gak geser2
    private Quaternion rotasiNetral;

    private void Awake()
    {
        if (targetVisual != null) rotasiNetral = targetVisual.localRotation;
    }

    public void Mainkan()
    {
        if (targetVisual == null) return;

        if (pulseBerjalan != null) StopCoroutine(pulseBerjalan);
        pulseBerjalan = StartCoroutine(RutinAnggukan());
    }

    private IEnumerator RutinAnggukan()
    {
        Quaternion rotasiAwal = rotasiNetral;
        float durasiPerAyunan = durasiTotal / (jumlahAnggukan * 2f);

        // sumbu Z buat tilt depan-belakang
        for (int i = 0; i < jumlahAnggukan; i++)
        {
            yield return PutarKe(rotasiAwal * Quaternion.Euler(0f, 0f, sudutAnggukan), durasiPerAyunan);
            yield return PutarKe(rotasiAwal * Quaternion.Euler(0f, 0f, -sudutAnggukan), durasiPerAyunan);
        }

        targetVisual.localRotation = rotasiAwal;
        pulseBerjalan = null;
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
