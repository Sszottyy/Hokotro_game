using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 5f;
    public Vector3 offset = new Vector3(0, 0, -10f);

    [Header("Zoom Beállítások")]
    public float minZoom = 3f;
    public float maxZoom = 25f;
    public float zoomSpeed = 5f;

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (target == null) return;

        transform.position = Vector3.Lerp(
            transform.position,
            target.position + offset,
            smoothSpeed * Time.deltaTime
        );

        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0f)
        {
            cam.orthographicSize -= scroll * zoomSpeed;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}