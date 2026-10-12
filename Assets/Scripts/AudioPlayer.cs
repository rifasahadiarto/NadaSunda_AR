using System.Collections.Generic;
using UnityEngine;

// Muter suara instrumen. Ada jeda (debounce) biar suaranya gak numpuk
// kalo dipencet berkali-kali cepet.
[RequireComponent(typeof(AudioSource))]
public class AudioPlayer : MonoBehaviour
{
    // jeda minimal buat nada yang sama
    private const float JEDA_DEBOUNCE = 0.5f;

    // index khusus buat suara utama (Angklung/Kendang), -1 biar gak numpuk sama index bilah
    private const int INDEX_SUARA_UTAMA = -1;

    private AudioSource audioSource;

    // nyimpen kapan terakhir tiap nada dibunyiin
    private readonly Dictionary<int, float> waktuTerakhirPutar = new Dictionary<int, float>();

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    // mainin satu nada, ada jeda debounce per index nadanya.
    // return true kalo audio beneran dibunyiin, false kalo dilewat
    // (clip kosong atau masih dalem jeda debounce) -- dipake TouchRaycaster
    // biar indikator sentuh cuma muncul kalo bunyinya beneran keluar
    public bool MainkanNada(int indexNada, AudioClip clip)
    {
        if (clip == null) return false;

        // masih dalem jeda debounce, skip aja
        if (waktuTerakhirPutar.TryGetValue(indexNada, out float waktuTerakhir) &&
            Time.time - waktuTerakhir < JEDA_DEBOUNCE)
        {
            return false;
        }

        waktuTerakhirPutar[indexNada] = Time.time;

        // PlayOneShot biar nada lama gak keputus pas nada baru main
        audioSource.PlayOneShot(clip);
        return true;
    }

    // buat suara utama instrumen yang badannya satu (bukan granular)
    public bool MainkanSuaraInstrumen(AudioClip clip)
    {
        return MainkanNada(INDEX_SUARA_UTAMA, clip);
    }
}
