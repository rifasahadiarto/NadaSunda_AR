using UnityEngine;

// Muter instrumen pake drag jari, ada inersia (nglambat pelan-pelan
// pas jari dilepas). CATATAN: gak dipasang ke prefab manapun sekarang,
// jadi fiturnya gak aktif di aplikasi.
public class InstrumenRotator : MonoBehaviour
{
    // derajat per piksel drag
    [SerializeField] private float kecepatanRotasi = 0.3f;

    // makin gede makin cepet berhentinya
    [SerializeField] private float redamanInersia = 3f;

    // batas kecepatan biar gak muter liar kalo di-flick kenceng
    [SerializeField] private float kecepatanSudutMaksimum = 540f;

    private float kecepatanSudut;
    private bool sedangDrag;

    private void Update()
    {
        TanganiInput();

        // lanjut muter pake inersia kalo jari udah gak nempel
        if (!sedangDrag && Mathf.Abs(kecepatanSudut) > 0.5f)
        {
            transform.Rotate(0f, kecepatanSudut * Time.deltaTime, 0f, Space.World);
            kecepatanSudut = Mathf.Lerp(kecepatanSudut, 0f, redamanInersia * Time.deltaTime);
        }
    }

    private void TanganiInput()
    {
#if UNITY_EDITOR
        // simulasi drag pake mouse di Editor
        if (Input.GetMouseButtonDown(0)) sedangDrag = MengenaiObjekIni(Input.mousePosition);
        if (sedangDrag && Input.GetMouseButton(0)) Putar(Input.GetAxis("Mouse X") * 15f);
        if (Input.GetMouseButtonUp(0)) sedangDrag = false;
#else
        // cuma respon 1 jari
        if (Input.touchCount != 1)
        {
            if (Input.touchCount == 0) sedangDrag = false;
            return;
        }

        Touch sentuhan = Input.GetTouch(0);
        switch (sentuhan.phase)
        {
            case TouchPhase.Began:
                sedangDrag = MengenaiObjekIni(sentuhan.position);
                break;
            case TouchPhase.Moved:
                if (sedangDrag) Putar(sentuhan.deltaPosition.x);
                break;
            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                sedangDrag = false;
                break;
        }
#endif
    }

    private void Putar(float deltaPiksel)
    {
        // minus biar arahnya kerasa natural ngikutin jari
        float derajat = -deltaPiksel * kecepatanRotasi;
        transform.Rotate(0f, derajat, 0f, Space.World);

        // simpen kecepatan buat inersia abis jari dilepas
        if (Time.deltaTime > 0f)
        {
            kecepatanSudut = Mathf.Clamp(
                derajat / Time.deltaTime,
                -kecepatanSudutMaksimum,
                kecepatanSudutMaksimum);
        }
    }

    // cek apa sentuhan/klik kena objek ini
    private bool MengenaiObjekIni(Vector2 posisiLayar)
    {
        Camera kamera = Camera.main;
        if (kamera == null) return false;

        Ray ray = kamera.ScreenPointToRay(posisiLayar);
        return Physics.Raycast(ray, out RaycastHit hit, 20f) &&
               hit.transform.IsChildOf(transform);
    }
}
