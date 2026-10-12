using System.Collections;
using UnityEngine;

// Efek pantul singkat pas satu bilah Saron dipukul, cuma tampilan doang
// gak ngaruh ke audio. Ditempel PER-BILAH (bukan di root Saron) karena
// tiap bilah punya collider sendiri dan bisa disentuh independen.
// Dipicu bareng TouchRaycaster pas mainin nada bilah ini.
public class SaronBounceEffect : MonoBehaviour
{
    [SerializeField] private Transform targetVisual;
    [SerializeField] private float tinggiPantulan = 0.01f;
    [SerializeField] private int jumlahPantulan = 3;
    [SerializeField] private float durasiTotal = 0.25f;
    [SerializeField] private float faktorRedaman = 0.5f;

    private Coroutine pantulanBerjalan;

    // posisi netral direkam sekali di awal, biar gak geser2 kalo bilah dipukul berkali-kali cepet
    private Vector3 posisiNetral;

    private void Awake()
    {
        if (targetVisual == null) targetVisual = transform;
        posisiNetral = targetVisual.localPosition;
    }

    public void Mainkan()
    {
        if (targetVisual == null) return;

        // stop pantulan lama biar gak numpuk
        if (pantulanBerjalan != null) StopCoroutine(pantulanBerjalan);
        pantulanBerjalan = StartCoroutine(RutinPantul());
    }

    private IEnumerator RutinPantul()
    {
        float durasiPerGerak = durasiTotal / (jumlahPantulan * 2f);
        float amplitudo = tinggiPantulan;

        // tiap pantulan makin pendek (redaman) meniru getaran bilah logam
        // yang meluruh setelah dipukul tabuh
        for (int i = 0; i < jumlahPantulan; i++)
        {
            yield return GeserKe(posisiNetral + Vector3.down * amplitudo, durasiPerGerak);
            yield return GeserKe(posisiNetral, durasiPerGerak);
            amplitudo *= faktorRedaman;
        }

        targetVisual.localPosition = posisiNetral;
        pantulanBerjalan = null;
    }

    private IEnumerator GeserKe(Vector3 target, float durasi)
    {
        Vector3 mula = targetVisual.localPosition;
        float t = 0f;
        while (t < durasi)
        {
            t += Time.deltaTime;
            targetVisual.localPosition = Vector3.Lerp(mula, target, t / durasi);
            yield return null;
        }
        targetVisual.localPosition = target;
    }
}
