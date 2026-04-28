using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 5f;
    public Vector3 offset = new Vector3(0, 0, -10f);

    [Header("Zoom Beállítások")]
    public float minZoom = 3f;   // Mennyire lehessen ráközelíteni
    public float maxZoom = 25f;  // Mennyire lehessen kizoomolni
    public float zoomSpeed = 5f; // Milyen gyorsan zoomoljon

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 1. Kamera követése
        transform.position = Vector3.Lerp(transform.position, target.position + offset, smoothSpeed * Time.deltaTime);

        // 2. Zoomolás görgővel
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            cam.orthographicSize -= scroll * zoomSpeed;
            // Ne engedjük túl közel vagy túl távol menni
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }
    }
}