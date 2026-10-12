using UnityEngine;

// Data 1 alat musik: nama, model 3D, suara utama, sama deskripsinya.
// Bukan MonoBehaviour, ini cuma wadah data doang.
[System.Serializable]
public class InstrumenModel
{
    public string nama;
    public GameObject prefab;
    public AudioClip audioClip;

    // biar kolom deskripsi di Inspector bisa lebih panjang
    [TextArea(3, 10)] public string deskripsi;
}
