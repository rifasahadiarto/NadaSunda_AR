using UnityEngine;

// Nempel di tiap bilah Saron / dawai Kacapi.
// Isinya cuma info nadanya: index, nama nada, sama audionya.
// TouchRaycaster yang baca ini pas ada sentuhan kena bilahnya.
public class BilahSaron : MonoBehaviour
{
    public int indexNada;
    public string namaNada;
    public AudioClip audioClipNada;
}
